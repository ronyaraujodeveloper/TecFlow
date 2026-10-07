using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.Telegram;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TL;
using WTelegram;

namespace TecFlow.Infrastructure.Services.Telegram;

public sealed class TelegramUserMonitorHost : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, UserBotSlot> _slots = new();
    private readonly ConcurrentDictionary<int, Client> _pendingLogins = new();
    private readonly System.Threading.Channels.Channel<UserBotCapturedPayload> _queue = System.Threading.Channels.Channel.CreateUnbounded<UserBotCapturedPayload>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    private readonly System.Threading.Channels.Channel<int> _catchUpJobs = System.Threading.Channels.Channel.CreateUnbounded<int>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    private readonly ConcurrentDictionary<int, byte> _catchUpQueued = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _catchUpGates = new();
    private readonly ConcurrentDictionary<(int UserId, string ChatId, int MessageId), byte> _mediaQueued = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, ChatBase>> _chatsByUser = new();
    private readonly ConcurrentQueue<UserBotMediaJob> _mediaHigh = new();
    private readonly ConcurrentQueue<UserBotMediaJob> _mediaLow = new();
    private readonly SemaphoreSlim _mediaSignal = new(0);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramUserBotSessionStore _sessions;
    private readonly TelegramUserBotCodeStore _codes;
    private readonly IUserBotSyncStatusService _syncStatus;
    private readonly ILogger<TelegramUserMonitorHost> _logger;

    public TelegramUserMonitorHost(
        IServiceScopeFactory scopeFactory,
        TelegramUserBotSessionStore sessions,
        TelegramUserBotCodeStore codes,
        IUserBotSyncStatusService syncStatus,
        ILogger<TelegramUserMonitorHost> logger)
    {
        _scopeFactory = scopeFactory;
        _sessions = sessions;
        _codes = codes;
        _syncStatus = syncStatus;
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
                _syncStatus.MarkFailed(row.UserId, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "UserBot não iniciado. A sincronização de canais via Bot Token segue. UserId={UserId}",
                    row.UserId);
                _syncStatus.MarkFailed(row.UserId, ex.Message);
            }
        }
    }

    public async Task<UserBotCatchUpResult> CatchUpUserAsync(int userId, CancellationToken cancellationToken)
    {
        _syncStatus.MarkRunning(userId, "Iniciando conexão com WTelegramClient...");
        try
        {
            _logger.LogInformation("Iniciando conexão com WTelegramClient... UserId={UserId}", userId);
            if (!_slots.TryGetValue(userId, out _))
            {
                await TryStartSlotFromDatabaseAsync(userId, cancellationToken);
            }

            if (!_slots.TryGetValue(userId, out var slot))
            {
                _logger.LogWarning(
                    "Catch-up ignorado: UserBot ainda não está autenticado. UserId={UserId} HasSession={HasSession}",
                    userId,
                    _sessions.HasSession(userId));
                var offline = UserBotCatchUpResult.Offline();
                _syncStatus.MarkFailed(userId, offline.Message);
                return offline;
            }

            _logger.LogInformation("Conexão estabelecida! Varrendo histórico de grupos... UserId={UserId}", userId);
            _syncStatus.MarkRunning(userId);
            var result = await CatchUpAsync(userId, slot.Client, cancellationToken);
            await WaitForQueueIdleAsync(cancellationToken);
            if (!result.UserBotReady)
            {
                _syncStatus.MarkFailed(userId, result.Message);
            }
            else
            {
                _syncStatus.MarkCompleted(userId, result.Message);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico na execução em segundo plano do UserBot. UserId={UserId}", userId);
            _syncStatus.MarkFailed(userId, ex.Message);
            return UserBotCatchUpResult.Offline();
        }
    }

    public bool EnqueueCatchUp(int userId)
    {
        if (userId <= 0)
        {
            return false;
        }

        if (!_catchUpQueued.TryAdd(userId, 0))
        {
            return true;
        }

        if (_catchUpJobs.Writer.TryWrite(userId))
        {
            _syncStatus.MarkRunning(userId, "Sincronização iniciada! Os links estão sendo capturados em segundo plano.");
            _logger.LogInformation("Catch-up UserBot enfileirado em background. UserId={UserId}", userId);
            return true;
        }

        _catchUpQueued.TryRemove(userId, out _);
        return false;
    }

    public bool EnqueuePriorityPhoto(int userId, string chatId, string externalMessageId)
    {
        if (userId <= 0
            || string.IsNullOrWhiteSpace(chatId)
            || !long.TryParse(externalMessageId, out var parsed)
            || parsed is <= 0 or > int.MaxValue)
        {
            return false;
        }

        var messageId = (int)parsed;

        EnqueueMediaJob(new UserBotMediaJob(userId, chatId.Trim(), messageId), highPriority: true);
        return true;
    }

    public async Task RunForeverAsync(CancellationToken stoppingToken)
    {
        var drain = DrainQueueAsync(CancellationToken.None);
        var catchUpDrain = DrainCatchUpJobsAsync(stoppingToken);
        var mediaDrain = DrainMediaJobsAsync(stoppingToken);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ReconcileAsync(stoppingToken);
                    await LinkPendingDiskPhotosAsync(stoppingToken);
                    await BackfillMissingPhotosAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Ciclo do UserBot Telegram falhou. A sincronização de grupos via Bot API continua.");
                    _syncStatus.MarkFailed(0, ex.Message);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            _queue.Writer.TryComplete();
            _catchUpJobs.Writer.TryComplete();
            try
            {
                await drain.WaitAsync(TimeSpan.FromSeconds(15), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Fila de captura UserBot encerrada com aviso.");
            }

            try
            {
                await catchUpDrain.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Fila de catch-up UserBot encerrada com aviso.");
            }

            try
            {
                await mediaDrain.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Fila de mídia UserBot encerrada com aviso.");
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
            await CatchUpAsync(userId, client, cancellationToken);
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
                if (message is null)
                {
                    continue;
                }

                var (chatId, title) = ResolvePeer(updates, message.peer_id);
                RememberChatsFromUpdates(userId, updates);
                await TryEnqueueFromMessageAsync(userId, client, message, chatId, title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao processar update MTProto. UserId={UserId}", userId);
        }

        await Task.CompletedTask;
    }

    private async Task<UserBotCatchUpResult> CatchUpAsync(int userId, Client client, CancellationToken cancellationToken)
    {
        var gate = _catchUpGates.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        if (!await gate.WaitAsync(TimeSpan.FromMinutes(3), cancellationToken))
        {
            return UserBotCatchUpResult.Done(0, 0);
        }

        try
        {
            if (client.User is null)
            {
                if (!_sessions.HasSession(userId))
                {
                    return UserBotCatchUpResult.Offline();
                }

                await client.LoginUserIfNeeded();
                _logger.LogInformation("UserBot MTProto autenticado. UserId={UserId}", userId);
            }

            if (client.User is null)
            {
                return UserBotCatchUpResult.Offline();
            }

            Messages_DialogsBase dialogs;
            try
            {
                dialogs = await client.Messages_GetAllDialogs();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Catch-up de diálogos falhou. UserId={UserId}", userId);
                return UserBotCatchUpResult.Offline();
            }

            var channels = CollectHistoryChats(dialogs);
            var persisted = 0;
            var since = TelegramUserMonitorRules.HistoryCatchUpSinceUtc(DateTime.UtcNow);
            _logger.LogInformation(
                "Catch-up iniciando. UserId={UserId} Canais={Count}",
                userId,
                channels.Count);

            foreach (var chat in channels)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InputPeer inputPeer;
                try
                {
                    inputPeer = chat.ToInputPeer();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "InputPeer indisponível no catch-up. UserId={UserId}", userId);
                    continue;
                }

                var (chatId, title) = ResolvePeerFromChat(chat);
                RememberChat(userId, chat);
                var offsetId = 0;
                var totalFetched = 0;
                var reachedOld = false;
                try
                {
                    while (totalFetched < TelegramUserMonitorRules.HistoryCatchUpMaxPerChannel && !reachedOld)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var remaining = TelegramUserMonitorRules.HistoryCatchUpMaxPerChannel - totalFetched;
                        var pageSize = Math.Min(TelegramUserMonitorRules.HistoryCatchUpPageSize, remaining);
                        var history = await GetHistoryWithFloodRetryAsync(
                            client,
                            inputPeer,
                            offsetId,
                            pageSize,
                            cancellationToken);
                        var messages = history.Messages;
                        if (messages is null || messages.Length == 0)
                        {
                            break;
                        }

                        foreach (var item in messages)
                        {
                            if (item is not Message historic)
                            {
                                continue;
                            }

                            var receivedAt = historic.Date == default
                                ? DateTime.UtcNow
                                : DateTime.SpecifyKind(historic.Date, DateTimeKind.Utc);
                            if (!TelegramUserMonitorRules.IsWithinCatchUpWindow(receivedAt, since))
                            {
                                reachedOld = true;
                                continue;
                            }

                            var payload = await TryCreatePayloadAsync(
                                client,
                                userId,
                                historic,
                                chatId,
                                title,
                                cancellationToken);
                            if (payload is null)
                            {
                                continue;
                            }

                    await PersistAsync(payload, cancellationToken, prioritizePhoto: false);
                            persisted++;
                        }

                        var oldestId = messages.Min(item => item.ID);
                        if (oldestId <= 0 || oldestId == offsetId)
                        {
                            break;
                        }

                        offsetId = oldestId;
                        totalFetched += messages.Length;
                        if (messages.Length < pageSize)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(120), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GetHistory falhou. UserId={UserId} Chat={ChatId} Title={Title}", userId, chatId, title);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(120), cancellationToken);
            }

            _logger.LogInformation(
                "Catch-up concluído. UserId={UserId} Canais={Count} Persistidas={Persisted}",
                userId,
                channels.Count,
                persisted);
            return UserBotCatchUpResult.Done(channels.Count, persisted);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task TryEnqueueFromMessageAsync(
        int userId,
        Client client,
        Message message,
        string chatId,
        string title)
    {
        var payload = await TryCreatePayloadAsync(client, userId, message, chatId, title, CancellationToken.None);
        if (payload is not null)
        {
            _queue.Writer.TryWrite(payload);
        }
    }

    private async Task<UserBotCapturedPayload?> TryCreatePayloadAsync(
        Client client,
        int userId,
        Message message,
        string chatId,
        string title,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.message) && message.media is null && message.reply_markup is null)
        {
            return null;
        }

        var text = message.message ?? string.Empty;
        var urls = TelegramUserMonitorRules.ExtractHttpUrls(text).ToList();
        if (message.entities is not null)
        {
            foreach (var entity in message.entities)
            {
                if (entity is MessageEntityTextUrl { url: { Length: > 0 } entityUrl })
                {
                    urls.Add(entityUrl);
                }
                else if (entity is MessageEntityUrl urlEntity
                    && urlEntity.length > 0
                    && urlEntity.offset >= 0
                    && urlEntity.offset + urlEntity.length <= text.Length)
                {
                    urls.Add(text.Substring(urlEntity.offset, urlEntity.length));
                }
            }
        }

        CollectMarkupUrls(message, urls);

        string? pageTitle = null;
        if (message.media is MessageMediaWebPage { webpage: WebPage page })
        {
            pageTitle = page.title;
            if (!string.IsNullOrWhiteSpace(page.url))
            {
                urls.Add(page.url);
            }
        }

        var offerUrls = urls
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(url => TelegramUserMonitorRules.IsTrackedCommerceUrl(url, out _)
                || Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .ToList();
        if (offerUrls.Count == 0)
        {
            return null;
        }

        var raw = string.IsNullOrWhiteSpace(pageTitle) ? text : pageTitle + Environment.NewLine + text;
        foreach (var url in offerUrls)
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

        var hasPhoto = message.media is MessageMediaPhoto { photo: Photo };
        string? productImageUrl = null;
        if (hasPhoto && message.media is not null)
        {
            productImageUrl = await PersistTelegramPhotoAsync(
                client,
                userId,
                message,
                cancellationToken);
        }

        var receivedAt = message.Date == default
            ? DateTime.UtcNow
            : DateTime.SpecifyKind(message.Date, DateTimeKind.Utc);
        return new UserBotCapturedPayload(
            userId,
            chatId,
            title,
            raw,
            message.id.ToString(),
            mediaUrl,
            receivedAt,
            PhotoBytes: null,
            hasPhoto,
            productImageUrl);
    }

    private static void CollectMarkupUrls(Message message, List<string> urls)
    {
        if (message.reply_markup is not ReplyInlineMarkup inline || inline.rows is null)
        {
            return;
        }

        foreach (var row in inline.rows)
        {
            if (row.buttons is null)
            {
                continue;
            }

            foreach (var button in row.buttons)
            {
                switch (button)
                {
                    case KeyboardInlineButton { type: InlineButtonTypeUrl { url.Length: > 0 } urlType }:
                        urls.Add(urlType.url);
                        break;
                    case KeyboardInlineButton { type: InlineButtonTypeUrlAuth { url.Length: > 0 } authType }:
                        urls.Add(authType.url);
                        break;
                }
            }
        }
    }

    private async Task TryStartSlotFromDatabaseAsync(int userId, CancellationToken cancellationToken)
    {
        if (!_sessions.HasSession(userId) || _slots.ContainsKey(userId) || _pendingLogins.ContainsKey(userId))
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await context.TelegramIntegrations
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.IsActive
                && item.UserBotApiId > 0
                && item.UserBotApiHash != null
                && item.UserBotApiHash != "")
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return;
        }

        try
        {
            StartSlot(row, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível religar o UserBot no sync. UserId={UserId}", userId);
        }
    }

    private static async Task<Messages_MessagesBase> GetHistoryWithFloodRetryAsync(
        Client client,
        InputPeer inputPeer,
        int offsetId,
        int pageSize,
        CancellationToken cancellationToken,
        int addOffset = 0)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                return await client.Messages_GetHistory(
                    inputPeer,
                    offset_id: offsetId,
                    add_offset: addOffset,
                    limit: pageSize);
            }
            catch (Exception ex) when (TryGetFloodWaitSeconds(ex, out var seconds) && attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 90)), cancellationToken);
            }
        }

        return await client.Messages_GetHistory(inputPeer, offset_id: offsetId, add_offset: addOffset, limit: pageSize);
    }

    private static bool TryGetFloodWaitSeconds(Exception ex, out int seconds)
    {
        seconds = 0;
        var match = System.Text.RegularExpressions.Regex.Match(
            ex.Message ?? string.Empty,
            @"FLOOD_WAIT[_:]?(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out seconds);
    }

    private async Task WaitForQueueIdleAsync(CancellationToken cancellationToken)
    {
        var idleRounds = 0;
        while (idleRounds < 8 && !cancellationToken.IsCancellationRequested)
        {
            if (_queue.Reader.TryPeek(out _))
            {
                idleRounds = 0;
                await Task.Delay(80, cancellationToken);
                continue;
            }

            idleRounds++;
            await Task.Delay(80, cancellationToken);
        }
    }

    private async Task DrainCatchUpJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var userId in _catchUpJobs.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await CatchUpUserAsync(userId, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro crítico na execução em segundo plano do UserBot. UserId={UserId}", userId);
                    _syncStatus.MarkFailed(userId, ex.Message);
                }
                finally
                {
                    _catchUpQueued.TryRemove(userId, out _);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task DrainQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var payload in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await PersistAsync(payload, cancellationToken, prioritizePhoto: true);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Persistência da captura UserBot falhou. UserId={UserId} Chat={ChatId}",
                        payload.UserId,
                        payload.ChatId);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Leitor da fila UserBot encerrou com erro.");
        }
    }

    private async Task PersistAsync(
        UserBotCapturedPayload payload,
        CancellationToken cancellationToken,
        bool prioritizePhoto = false)
    {
        var userId = payload.UserId;
        var chatId = payload.ChatId;
        var title = payload.Title;
        var rawText = payload.RawText;
        var messageId = payload.MessageId;
        var mediaUrl = payload.MediaUrl;
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
                PhotoBytes = payload.PhotoBytes,
                ProductImageUrl = payload.ProductImageUrl,
                ReceivedAt = payload.ReceivedAt
            },
            cancellationToken);

        var photoAlreadyOnDisk = ProductImageStorageRules.IsLocalProductImage(payload.ProductImageUrl);
        if (payload.HasPhoto
            && !photoAlreadyOnDisk
            && int.TryParse(payload.MessageId, out var telegramMessageId)
            && telegramMessageId > 0)
        {
            EnqueueMediaJob(new UserBotMediaJob(payload.UserId, payload.ChatId, telegramMessageId), prioritizePhoto);
        }
    }

    private static List<ChatBase> CollectHistoryChats(Messages_DialogsBase dialogs)
    {
        var channels = new List<ChatBase>();
        var seen = new HashSet<long>();
        if (dialogs is Messages_Dialogs full)
        {
            AddChats(channels, seen, full.chats?.Values);
        }
        else if (dialogs is Messages_DialogsSlice slice)
        {
            AddChats(channels, seen, slice.chats?.Values);
        }

        foreach (var dialog in dialogs.Dialogs ?? [])
        {
            if (dialogs.UserOrChat(dialog.Peer) is ChatBase chat && (chat is TL.Channel or Chat) && seen.Add(chat.ID))
            {
                channels.Add(chat);
            }
        }

        return channels;
    }

    private static void AddChats(List<ChatBase> channels, HashSet<long> seen, IEnumerable<ChatBase>? chats)
    {
        if (chats is null)
        {
            return;
        }

        foreach (var chat in chats)
        {
            if (chat is TL.Channel or Chat && seen.Add(chat.ID))
            {
                channels.Add(chat);
            }
        }
    }

    private static (string ChatId, string Title) ResolvePeerFromChat(ChatBase chat)
    {
        if (chat is TL.Channel channel)
        {
            var title = string.IsNullOrWhiteSpace(channel.title) ? "Canal Telegram" : channel.title;
            return (TelegramUserMonitorRules.BuildChannelChatId(channel.id), title);
        }

        if (chat is Chat group)
        {
            var title = string.IsNullOrWhiteSpace(group.title) ? "Grupo Telegram" : group.title;
            return ((-group.id).ToString(), title);
        }

        return ("0", "Telegram");
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
                    if (chat is TL.Channel channel && channel.id == channelPeer.channel_id)
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
            if (_sessions.HasSession(userId))
            {
                return;
            }

            await ResumePendingLoginAsync(userId, cancellationToken);
            if (!_pendingLogins.TryGetValue(userId, out client))
            {
                throw new InvalidOperationException("Solicite o código antes de autenticar.");
            }
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

    private async Task ResumePendingLoginAsync(int userId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await context.TelegramIntegrations
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IsActive)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null
            || row.UserBotApiId is not > 0
            || string.IsNullOrEmpty(row.UserBotApiHash)
            || !TelegramUserMonitorRules.TryNormalizeE164Phone(row.UserBotPhone, out var phone))
        {
            throw new InvalidOperationException("Solicite o código antes de autenticar.");
        }

        await SendCodeAsync(userId, row.UserBotApiId.Value, row.UserBotApiHash, phone, cancellationToken);
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

    private void RememberChat(int userId, ChatBase chat)
    {
        var (chatId, _) = ResolvePeerFromChat(chat);
        if (chatId is "0" or "")
        {
            return;
        }

        var map = _chatsByUser.GetOrAdd(userId, _ => new ConcurrentDictionary<string, ChatBase>(StringComparer.OrdinalIgnoreCase));
        map[chatId] = chat;
    }

    private void RememberChatsFromUpdates(int userId, UpdatesBase updates)
    {
        if (updates.Chats is null)
        {
            return;
        }

        foreach (var chat in updates.Chats.Values)
        {
            RememberChat(userId, chat);
        }
    }

    private void EnqueueMediaJob(UserBotMediaJob job, bool highPriority)
    {
        var key = (job.UserId, job.ChatId, job.MessageId);
        if (highPriority)
        {
            _mediaHigh.Enqueue(job);
            _mediaQueued[key] = 0;
            _mediaSignal.Release();
            return;
        }

        if (!_mediaQueued.TryAdd(key, 0))
        {
            return;
        }

        _mediaLow.Enqueue(job);
        _mediaSignal.Release();
    }

    private async Task DrainMediaJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _mediaSignal.WaitAsync(cancellationToken);
                while (TryDequeueMedia(out var job))
                {
                    try
                    {
                        await DownloadPriorityPhotoAsync(job, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(
                            ex,
                            "Download priorizado de foto falhou. UserId={UserId} Chat={ChatId} MessageId={MessageId}",
                            job.UserId,
                            job.ChatId,
                            job.MessageId);
                    }
                    finally
                    {
                        _mediaQueued.TryRemove((job.UserId, job.ChatId, job.MessageId), out _);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private bool TryDequeueMedia(out UserBotMediaJob job)
    {
        if (_mediaHigh.TryDequeue(out job!))
        {
            return true;
        }

        return _mediaLow.TryDequeue(out job!);
    }

    private async Task DownloadPriorityPhotoAsync(UserBotMediaJob job, CancellationToken cancellationToken)
    {
        if (!_slots.TryGetValue(job.UserId, out var slot))
        {
            return;
        }

        var chat = await ResolveCachedChatAsync(slot.Client, job.UserId, job.ChatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        InputPeer inputPeer;
        try
        {
            inputPeer = chat.ToInputPeer();
        }
        catch
        {
            return;
        }

        var history = await GetHistoryWithFloodRetryAsync(
            slot.Client,
            inputPeer,
            offsetId: job.MessageId + 1,
            pageSize: 1,
            cancellationToken,
            addOffset: -1);
        var messages = history.Messages?.OfType<Message>().Where(item => item.id == job.MessageId).ToArray() ?? [];
        if (messages.Length == 0 || messages[0].media is not MessageMediaPhoto { photo: Photo })
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var captured = scope.ServiceProvider.GetRequiredService<IGroupCapturedMessagesService>();
        var relative = await PersistTelegramPhotoAsync(slot.Client, job.UserId, messages[0], cancellationToken);
        if (string.IsNullOrWhiteSpace(relative))
        {
            return;
        }

        await captured.UpdateImageUrlAsync(job.MessageId, relative, job.UserId, cancellationToken);
    }

    private async Task LinkPendingDiskPhotosAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var backfill = scope.ServiceProvider.GetRequiredService<IProductImageUrlLinkBackfillService>();
        var linked = await backfill.LinkExistingFilesOnceAsync(cancellationToken);
        if (linked > 0)
        {
            _logger.LogInformation("ImageUrl vinculado a arquivos já existentes no disco. Count={Count}", linked);
        }
    }

    private async Task BackfillMissingPhotosAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IOfferProductMediaStore>();
        var rows = await context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.Channel == GroupOfferCaptureRules.TelegramChannel
                && item.ExternalMessageId != null
                && item.ExternalMessageId != "")
            .OrderByDescending(item => item.CreatedAt)
            .Take(200)
            .Select(item => new
            {
                item.UserId,
                item.GroupKey,
                item.ExternalMessageId,
                item.ProductImageUrl
            })
            .ToListAsync(cancellationToken);

        var enqueued = 0;
        foreach (var row in rows)
        {
            if (!int.TryParse(row.ExternalMessageId, out var messageId) || messageId <= 0)
            {
                continue;
            }

            var missing = string.IsNullOrWhiteSpace(row.ProductImageUrl) || !store.ExistsOnDisk(row.ProductImageUrl);
            if (!missing)
            {
                continue;
            }

            var chatId = GroupOfferCaptureRules.TryParseTelegramChatId(row.GroupKey);
            if (string.IsNullOrWhiteSpace(chatId))
            {
                continue;
            }

            EnqueueMediaJob(new UserBotMediaJob(row.UserId, chatId, messageId), highPriority: false);
            enqueued++;
        }

        if (enqueued > 0)
        {
            _logger.LogInformation("Backfill de fotos enfileirado. Count={Count}", enqueued);
        }
    }

    private async Task<string?> PersistTelegramPhotoAsync(
        Client client,
        int tenantId,
        Message message,
        CancellationToken cancellationToken)
    {
        if (message.media is not MessageMediaPhoto { photo: Photo })
        {
            return null;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var webRoot = ProductImageStorageRules.ResolveWebRoot(
                environment.WebRootPath,
                environment.ContentRootPath,
                AppDomain.CurrentDomain.BaseDirectory);
            var fileName = $"{message.id}.jpg";
            var (absoluteDir, absolutePath, relativePath) = ProductImageStorageRules.BuildSaveTarget(
                webRoot,
                tenantId,
                fileName,
                DateTime.UtcNow);
            Directory.CreateDirectory(absoluteDir);
            await using (var stream = File.Create(absolutePath))
            {
                await DownloadMediaAsync(client, message.media, stream);
            }

            var info = new FileInfo(absolutePath);
            if (!info.Exists || info.Length <= 0)
            {
                return null;
            }

            relativePath = ProductImageStorageRules.EnsureLeadingSlash(relativePath) ?? relativePath;
            var externalId = message.id.ToString();
            await dbContext.Database.ExecuteSqlRawAsync(
                "UPDATE GroupCapturedMessages SET ProductImageUrl = {0}, MediaUrl = {1}, UpdatedAt = {2} WHERE ExternalMessageId = {3}",
                new object[] { relativePath, relativePath, DateTime.UtcNow, externalId },
                cancellationToken);
            _logger.LogInformation("Imagem salva no caminho: {path}", absolutePath);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Download imediato de foto falhou. Tenant={TenantId} MessageId={MessageId}", tenantId, message.id);
            return null;
        }
    }

    private static Task DownloadMediaAsync(Client client, MessageMedia media, Stream stream)
    {
        if (media is MessageMediaPhoto { photo: Photo photo })
        {
            return client.DownloadFileAsync(photo, stream);
        }

        if (media is MessageMediaDocument { document: Document document })
        {
            return client.DownloadFileAsync(document, stream);
        }

        return Task.CompletedTask;
    }

    private async Task<ChatBase?> ResolveCachedChatAsync(
        Client client,
        int userId,
        string chatId,
        CancellationToken cancellationToken)
    {
        if (_chatsByUser.TryGetValue(userId, out var map)
            && map.TryGetValue(chatId, out var cached))
        {
            return cached;
        }

        try
        {
            var dialogs = await client.Messages_GetAllDialogs();
            foreach (var chat in CollectHistoryChats(dialogs))
            {
                RememberChat(userId, chat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Não foi possível recarregar diálogos para mídia. UserId={UserId}", userId);
            return null;
        }

        return _chatsByUser.TryGetValue(userId, out map) && map.TryGetValue(chatId, out cached)
            ? cached
            : null;
    }

    private sealed record UserBotCapturedPayload(
        int UserId,
        string ChatId,
        string Title,
        string RawText,
        string MessageId,
        string? MediaUrl,
        DateTime ReceivedAt,
        byte[]? PhotoBytes,
        bool HasPhoto,
        string? ProductImageUrl);

    private sealed record UserBotMediaJob(int UserId, string ChatId, int MessageId);

    private sealed record UserBotSlot(Client Client, CancellationTokenSource Cts, Task Loop);
}
