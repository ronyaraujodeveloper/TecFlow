using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Business.Service.PublicPages;
using Ofertio.Business.Service.WhatsApp;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.Email;

namespace Ofertio.Infrastructure.Services.PublicPages;

/// <summary>
/// Confere os grupos por trás dos botões da página pública. Uma rotina por dia, com pausa entre as chamadas
/// (o WhatsApp é acessado por API não oficial). Regras: duas falhas seguidas desativam; integração fora do ar
/// deixa o link "não verificado" e não desativa; o convite é atualizado se o administrador o redefiniu.
/// </summary>
public sealed class JoinLinkHealthService
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromHours(20);
    private const int BatchSize = 200;

    private readonly AppDbContext _context;
    private readonly IEvolutionApiService _evolution;
    private readonly ITelegramApiService _telegram;
    private readonly ITransactionalEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<JoinLinkHealthService> _logger;

    public JoinLinkHealthService(
        AppDbContext context,
        IEvolutionApiService evolution,
        ITelegramApiService telegram,
        ITransactionalEmailSender email,
        IOptions<EmailOptions> emailOptions,
        ILogger<JoinLinkHealthService> logger)
    {
        _context = context;
        _evolution = evolution;
        _telegram = telegram;
        _email = email;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    /// <summary>Confere os links vencidos (sem conferência há mais de 20 h). Devolve quantos mudaram de estado.</summary>
    public async Task<int> RunAsync(TimeSpan? pauseBetweenCalls = null, CancellationToken cancellationToken = default)
    {
        var limit = DateTime.UtcNow - MinInterval;
        var due = await _context.PublicPageJoinLinks.IgnoreQueryFilters()
            .Where(link => link.SourceGroupId != null
                && (link.Status == JoinLinkRules.StatusActive || link.Status == JoinLinkRules.StatusUnverified)
                && (link.LastCheckedAt == null || link.LastCheckedAt < limit))
            .OrderBy(link => link.LastCheckedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
        if (due.Count == 0)
        {
            return 0;
        }

        var changed = 0;
        foreach (var userGroup in due.GroupBy(link => link.UserId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = userGroup.Key;
            var whatsAppOpen = await IsWhatsAppOpenAsync(userId, cancellationToken);
            var telegramToken = await _context.TelegramIntegrations.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => item.Token)
                .FirstOrDefaultAsync(cancellationToken);

            foreach (var link in userGroup)
            {
                var result = await CheckAsync(link, whatsAppOpen, telegramToken, cancellationToken);
                if (ApplyResult(link, result))
                {
                    changed++;
                    await NotifyDeactivatedAsync(link, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                if (pauseBetweenCalls is { } pause && pause > TimeSpan.Zero)
                {
                    // Pausa com variação: não bater na API do WhatsApp em intervalos fixos.
                    await Task.Delay(pause + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)), cancellationToken);
                }
            }
        }

        return changed;
    }

    private async Task<bool> IsWhatsAppOpenAsync(int userId, CancellationToken cancellationToken)
    {
        var instance = await _context.WhatsAppIntegrations.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.InstanceName)
            .FirstOrDefaultAsync(cancellationToken) ?? WhatsAppSessionRules.BuildInstanceName(userId);
        var state = await _evolution.GetConnectionStateAsync(instance, cancellationToken);
        return string.Equals(state.State, "open", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<GroupInviteResult> CheckAsync(
        PublicPageJoinLink link,
        bool whatsAppOpen,
        string? telegramToken,
        CancellationToken cancellationToken)
    {
        if (link.Channel == JoinLinkRules.WhatsApp)
        {
            if (!whatsAppOpen)
            {
                return GroupInviteResult.Unavailable;
            }

            var instance = await _context.WhatsAppIntegrations.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.UserId == link.UserId)
                .Select(item => item.InstanceName)
                .FirstOrDefaultAsync(cancellationToken) ?? WhatsAppSessionRules.BuildInstanceName(link.UserId);
            return await _evolution.FetchGroupInviteAsync(instance, link.SourceGroupId!, cancellationToken);
        }

        return string.IsNullOrWhiteSpace(telegramToken)
            ? GroupInviteResult.Unavailable
            : await _telegram.GetChatInviteAsync(telegramToken, link.SourceGroupId!, cancellationToken);
    }

    /// <summary>Atualiza o link conforme o resultado. Devolve verdadeiro se acabou de ser desativado.</summary>
    public static bool ApplyResult(PublicPageJoinLink link, GroupInviteResult result, DateTime? now = null)
    {
        var moment = now ?? DateTime.UtcNow;
        link.LastCheckedAt = moment;
        link.Touch();

        switch (result.Outcome)
        {
            case GroupInviteOutcome.Ok:
                link.FailedChecks = 0;
                link.Status = JoinLinkRules.StatusActive;
                link.StatusReason = null;
                if (JoinLinkRules.TryNormalizeInviteUrl(link.Channel, result.InviteUrl, out var invite, out _))
                {
                    // O administrador pode ter redefinido o convite: o botão passa a usar o novo.
                    link.InviteUrl = invite;
                }

                if (result.MemberCount is > 0)
                {
                    link.MemberCount = result.MemberCount;
                }

                return false;

            case GroupInviteOutcome.NotFound:
                var (status, failures, reason) = JoinLinkRules.AfterFailedCheck(link.FailedChecks);
                link.FailedChecks = failures;
                link.Status = status;
                link.StatusReason = reason;
                return status == JoinLinkRules.StatusInactive;

            default:
                // Integração fora do ar ou erro passageiro: não dá para saber nada sobre o grupo. Não desativa.
                link.Status = JoinLinkRules.StatusUnverified;
                link.StatusReason = "Conexão fora do ar na última conferência.";
                return false;
        }
    }

    private async Task NotifyDeactivatedAsync(PublicPageJoinLink link, CancellationToken cancellationToken)
    {
        try
        {
            _context.TenantNotifications.Add(new TenantNotification
            {
                TenantId = link.TenantId,
                Title = "Botão de grupo desativado",
                Message = $"O botão \"{link.Label}\" ({link.Channel}) da sua página pública saiu do ar: o grupo não foi encontrado ou a sua conta deixou de ser administradora. Reative em Minhas Páginas Públicas se o grupo voltou.",
                Segment = "join-link"
            });

            var recipient = await _context.UserAccounts.IgnoreQueryFilters().AsNoTracking()
                .Where(user => user.Id == link.UserId && user.Email != "")
                .Select(user => user.Email)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                var url = TransactionalEmailRules.BuildPublicPagesUrl(_emailOptions.PublicBaseUrl);
                await _email.SendAsync(TransactionalEmailRules.JoinLinkDeactivated(recipient, link.Label, link.Channel, url), cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao avisar sobre o botão de grupo desativado. JoinLinkId={JoinLinkId}", link.Id);
        }
    }
}
