using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.Telegram;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TL;
using WTelegram;

namespace TecFlow.Infrastructure.Services.Telegram;

public sealed class TelegramUserMonitorHost : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, UserBotSlot> _slots = new();
    private readonly ConcurrentDictionary<int, Client> _pendingLogins = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramUserBotSessionStore _sessions;
    private readonly TelegramUserBotCodeStore _codes;
    private readonly ILogger<TelegramUserMonitorHost> _logger;

    public TelegramUserMonitorHost(
        IServiceScopeFactory scopeFactory,
        TelegramUserBotSessionStore sessions,
        TelegramUserBotCodeStore codes,
        ILogger<TelegramUserMonitorHost> logger)
    {
        _scopeFactory = scopeFactory;
        _sessions = sessions;
        _codes = codes;
        _logger = logger;
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        List<TelegramIntegration> rows;
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            rows = await context.TelegramIntegrations
                .AsNoTracking()
                .Where(item => item.IsActive
                    && item.UserBotApiId > 0
                    && item.UserBotApiHash != null
                    && item.UserBotApiHash != "")
                .ToListAsync(cancellationToken);
        }

        var activeIds = rows.Select(item => item.UserId).ToHashSet();
        foreach (var stale in _slots.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            await StopSlotAsync(stale);
        }

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_slots.ContainsKey(row.UserId) || _pendingLogins.ContainsKey(row.UserId))
            {
                continue;
            }

            if (!_sessions.HasSession(row.UserId))
            {
                continue;
            }

            try
            {
                StartSlot(row, cancellationToken);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                _logger.LogError(
                    ex,
                    "UserBot ignorado por falha de sessão/arquivo. A sincronização Bot API segue. UserId={UserId}",
                    row.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "UserBot não iniciado. A sincronização de canais via Bot Token segue. UserId={UserId}",
                    row.UserId);
            }
        }
    }

    private void StartSlot(TelegramIntegration row, CancellationToken stoppingToken)
    {
        var userId = row.UserId;
        var apiId = row.UserBotApiId!.Value;
        var apiHash = row.UserBotApiHash!;
        var phone = row.UserBotPhone ?? string.Empty;
        string sessionPath;
        try
        {
            sessionPath = _sessions.GetSessionPath(userId);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogError(
                ex,
                "Sem permissão para a pasta de sessões do UserBot. UserId={UserId}",
                userId);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        Client? client = null;
        try
        {
            client = new Client(what => what switch
            {
                "api_id" => apiId.ToString(),
                "api_hash" => apiHash,
                "phone_number" => phone,
                "verification_code" => _codes.Wait(userId, TimeSpan.FromMinutes(2)) ?? string.Empty,
                "session_pathname" => sessionPath,
                _ => null
            });
            client.OnUpdates += updates => OnUpdatesAsync(userId, client, updates);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível criar o cliente UserBot. UserId={UserId}", userId);
            client?.Dispose();
            cts.Dispose();
            return;
        }

        var loop = Task.Run(() => RunAsync(userId, client, cts.Token), cts.Token);
        _slots[userId] = new UserBotSlot(client, cts, loop);
    }

    private async Task RunAsync(int userId, Client client, CancellationToken cancellationToken)
    {
        try
        {
            await client.LoginUserIfNeeded();
            _logger.LogInformation("UserBot MTProto autenticado. UserId={UserId}", userId);
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sessão UserBot encerrada. UserId={UserId}", userId);
        }
        finally
        {
            _slots.TryRemove(userId, out _);
            client.Dispose();
        }
    }

    private async Task OnUpdatesAsync(int userId, Client client, UpdatesBase updates)
    {
        try
        {
            foreach (var update in updates.UpdateList)
            {
                var message = update switch
                {
                    UpdateNewChannelMessage { message: Message channelMessage } => channelMessage,
                    UpdateNewMessage { message: Message chatMessage } => chatMessage,
                    _ => null
                };
                if (message is null || string.IsNullOrWhiteSpace(message.message) && message.media is null)
                {
                    continue;
                }

                var text = message.message ?? string.Empty;
                var urls = TelegramBotRules.ExtractUrls(text).ToList();
                if (message.entities is not null)
                {
                    foreach (var entity in message.entities)
                    {
                        if (entity is MessageEntityTextUrl { url: { Length: > 0 } entityUrl })
                        {
                            urls.Add(entityUrl);
                        }
                    }
                }

                string? pageTitle = null;
                if (message.media is MessageMediaWebPage { webpage: WebPage page })
                {
                    pageTitle = page.title;
                    if (!string.IsNullOrWhiteSpace(page.url))
                    {
                        urls.Add(page.url);
                    }
                }

                var commerce = urls
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(url => TelegramUserMonitorRules.IsTrackedCommerceUrl(url, out _))
                    .ToList();
                if (commerce.Count == 0)
                {
                    continue;
                }

                var (chatId, title) = ResolvePeer(updates, message.peer_id);
                var raw = string.IsNullOrWhiteSpace(pageTitle) ? text : pageTitle + Environment.NewLine + text;
                foreach (var url in commerce)
                {
                    if (!raw.Contains(url, StringComparison.OrdinalIgnoreCase))
                    {
                        raw += Environment.NewLine + url;
                    }
                }

                string? mediaUrl = null;
                if (message.media is MessageMediaWebPage { webpage: WebPage web } && !string.IsNullOrWhiteSpace(web.url))
                {
                    mediaUrl = web.url;
                }

                await PersistAsync(
                    userId,
                    chatId,
                    title,
                    raw,
                    message.id.ToString(),
                    mediaUrl,
                    cancellationToken: CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao processar update MTProto. UserId={UserId}", userId);
        }

        await Task.CompletedTask;
    }

    private async Task PersistAsync(
        int userId,
        string chatId,
        string title,
        string rawText,
        string messageId,
        string? mediaUrl,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var capture = scope.ServiceProvider.GetRequiredService<IGroupOfferCaptureService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = await context.TelegramGroups
            .FirstOrDefaultAsync(item => item.UserId == userId && item.ChatId == chatId, cancellationToken);
        if (group is null)
        {
            group = new TelegramGroup
            {
                UserId = userId,
                ChatId = chatId,
                Name = string.IsNullOrWhiteSpace(title) ? chatId : title,
                IsAdmin = false,
                IsActive = true
            };
            await context.TelegramGroups.AddAsync(group, cancellationToken);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                group.Name = title;
            }

            group.IsActive = true;
            group.Touch();
        }

        await context.SaveChangesAsync(cancellationToken);
        await capture.CaptureAsync(
            new GroupOfferCaptureRequest
            {
                UserId = userId,
                Channel = GroupOfferCaptureRules.TelegramChannel,
                GroupId = chatId,
                GroupName = group.Name,
                ExternalMessageId = messageId,
                RawText = rawText,
                MediaUrl = mediaUrl,
                ReceivedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    private static (string ChatId, string Title) ResolvePeer(UpdatesBase updates, Peer? peer)
    {
        if (peer is PeerChannel channelPeer)
        {
            var title = "Canal Telegram";
            if (updates is Updates packed)
            {
                foreach (var chat in packed.chats.Values)
                {
                    if (chat is Channel channel && channel.id == channelPeer.channel_id)
                    {
                        title = string.IsNullOrWhiteSpace(channel.title) ? title : channel.title;
                        break;
                    }
                }
            }

            return (TelegramUserMonitorRules.BuildChannelChatId(channelPeer.channel_id), title);
        }

        if (peer is PeerChat chatPeer)
        {
            var title = "Grupo Telegram";
            if (updates is Updates packed)
            {
                foreach (var chat in packed.chats.Values)
                {
                    if (chat is Chat group && group.id == chatPeer.chat_id)
                    {
                        title = string.IsNullOrWhiteSpace(group.title) ? title : group.title;
                        break;
                    }
                }
            }

            return ((-chatPeer.chat_id).ToString(), title);
        }

        return ("0", "Telegram");
    }

    private async Task StopSlotAsync(int userId)
    {
        if (!_slots.TryRemove(userId, out var slot))
        {
            return;
        }

        await slot.Cts.CancelAsync();
        try
        {
            await slot.Loop.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch
        {
        }

        slot.Client.Dispose();
        slot.Cts.Dispose();
    }

    public bool IsAwaitingVerification(int userId) => _pendingLogins.ContainsKey(userId);

    public async Task SendCodeAsync(int userId, int apiId, string apiHash, string phone, CancellationToken cancellationToken)
    {
        await StopSlotAsync(userId);
        DisposePending(userId);
        var sessionPath = _sessions.GetSessionPath(userId);
        var client = new Client(what => what switch
        {
            "api_id" => apiId.ToString(),
            "api_hash" => apiHash,
            "session_pathname" => sessionPath,
            _ => null
        });
        Client? owned = client;
        try
        {
            var step = await SendCodeAsync(client, phone);
            if (string.Equals(step, "phone_number", StringComparison.OrdinalIgnoreCase))
            {
                step = await SendCodeAsync(client, phone);
            }

            if (client.User is not null)
            {
                return;
            }

            if (!string.Equals(step, "verification_code", StringComparison.OrdinalIgnoreCase) && step is not null)
            {
                throw new InvalidOperationException("Não foi possível solicitar o código. Confira ApiId, ApiHash e o telefone.");
            }

            _pendingLogins[userId] = client;
            owned = null;
        }
        finally
        {
            owned?.Dispose();
        }
    }

    public async Task MakeAuthAsync(int userId, string code, CancellationToken cancellationToken)
    {
        if (!_pendingLogins.TryGetValue(userId, out var client))
        {
            throw new InvalidOperationException("Solicite o código antes de autenticar.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var step = await MakeAuthAsync(client, code);
        if (string.Equals(step, "password", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Esta conta exige senha 2FA do Telegram. Use uma conta sem senha de nuvem.");
        }

        if (client.User is null)
        {
            throw new InvalidOperationException("Código inválido. Informe o PIN de 5 dígitos recebido no Telegram.");
        }

        DisposePending(userId);
    }

    private static Task<string?> SendCodeAsync(Client client, string phone) => client.Login(phone);

    private static Task<string?> MakeAuthAsync(Client client, string code) => client.Login(code);

    private void DisposePending(int userId)
    {
        if (_pendingLogins.TryRemove(userId, out var client))
        {
            client.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var userId in _slots.Keys.ToList())
        {
            await StopSlotAsync(userId);
        }

        foreach (var userId in _pendingLogins.Keys.ToList())
        {
            DisposePending(userId);
        }
    }

    private sealed record UserBotSlot(Client Client, CancellationTokenSource Cts, Task Loop);
}
