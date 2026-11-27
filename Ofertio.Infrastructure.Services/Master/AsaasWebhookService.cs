using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.Email;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Processa eventos de cobrança ASAAS: confirma pagamentos, renova e marca inadimplência.</summary>
public sealed class AsaasWebhookService : IAsaasWebhookService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMasterAuditService _audit;
    private readonly ITenantAccessReader _accessReader;
    private readonly ITransactionalEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<AsaasWebhookService> _logger;

    public AsaasWebhookService(
        AppDbContext context,
        IConfiguration configuration,
        IMasterAuditService audit,
        ITenantAccessReader accessReader,
        ITransactionalEmailSender email,
        IOptions<EmailOptions> emailOptions,
        ILogger<AsaasWebhookService> logger)
    {
        _context = context;
        _configuration = configuration;
        _audit = audit;
        _accessReader = accessReader;
        _email = email;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public Task<bool> ValidateTokenAsync(string? token, CancellationToken cancellationToken = default)
    {
        var expected = _configuration[SystemSettingCatalog.AsaasWebhookToken];
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrEmpty(token))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(token)));
    }

    public async Task<MasterOperationResultDto> HandleAsync(AsaasWebhookPayloadDto payload, CancellationToken cancellationToken = default)
    {
        var payment = payload.Payment;
        var eventName = payload.Event?.Trim().ToUpperInvariant() ?? string.Empty;
        if (payment is null || string.IsNullOrWhiteSpace(payment.Id))
        {
            return Ok("Evento sem pagamento ignorado.");
        }

        var tenant = await FindTenantAsync(payment, cancellationToken);
        if (tenant is null)
        {
            return Ok("Pagamento sem inquilino correspondente.");
        }

        var row = await _context.SaasPayments.FirstOrDefaultAsync(item => item.GatewayPaymentId == payment.Id, cancellationToken);
        if (row is null)
        {
            row = new SaasPayment { GatewayPaymentId = payment.Id, TenantId = tenant.Id };
            _context.SaasPayments.Add(row);
        }

        row.Amount = payment.Value;
        row.NetAmount = payment.NetValue > 0 ? payment.NetValue : payment.Value;
        var previousPaymentStatus = row.Status;
        var alreadyPaid = row.PaidAt is not null;
        row.Status = Truncate(payment.Status ?? eventName, 32);
        row.BillingType = Truncate(payment.BillingType ?? string.Empty, 24);
        row.DueDate = ParseDate(payment.DueDate);
        row.Touch();

        var now = DateTime.UtcNow;
        var previous = tenant.Status;
        string? message = null;
        string? noticeKind = null;
        switch (eventName)
        {
            case "PAYMENT_CONFIRMED":
            case "PAYMENT_RECEIVED":
                // Idempotente: o Asaas manda CONFIRMED e depois RECEIVED para o mesmo pagamento
                // e reenvia eventos sem 200. Só o primeiro estende a assinatura e reativa o inquilino;
                // os seguintes só atualizam o status do pagamento.
                if (alreadyPaid)
                {
                    message = $"Pagamento {payment.Id} já processado; evento {eventName} sem nova renovação.";
                    break;
                }

                noticeKind = "confirmed";
                row.PaidAt = ParseDate(payment.PaymentDate) ?? now;
                if (TenantStatuses.Normalize(tenant.Status) != TenantStatuses.Cancelado)
                {
                    TenantAdminService.ApplyTransition(
                        tenant,
                        new TenantLifecycleTransition(TenantStatuses.Ativo, tenant.TrialEndsAt, null),
                        "Pagamento confirmado pelo gateway.",
                        now);
                    var baseDate = tenant.RenewalAt > now ? tenant.RenewalAt.Value : now;
                    tenant.RenewalAt = string.Equals(tenant.BillingCycle, SaasMetricsRules.CycleAnnual, StringComparison.OrdinalIgnoreCase)
                        ? baseDate.AddYears(1)
                        : baseDate.AddMonths(1);
                }

                message = $"Pagamento {payment.Id} confirmado (R$ {row.Amount:N2}).";
                break;
            case "PAYMENT_OVERDUE":
                if (TenantStatuses.Normalize(tenant.Status) is TenantStatuses.Ativo or TenantStatuses.Trial)
                {
                    TenantAdminService.ApplyTransition(
                        tenant,
                        new TenantLifecycleTransition(TenantStatuses.Inadimplente, tenant.TrialEndsAt, null),
                        "Cobrança vencida no gateway.",
                        now);
                }

                message = $"Pagamento {payment.Id} vencido.";
                if (!string.Equals(previousPaymentStatus, "OVERDUE", StringComparison.OrdinalIgnoreCase))
                {
                    noticeKind = "overdue";
                }

                break;
            case "PAYMENT_REFUNDED":
            case "PAYMENT_DELETED":
                row.PaidAt = null;
                message = $"Pagamento {payment.Id} estornado/removido.";
                if (!string.Equals(previousPaymentStatus, "REFUNDED", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(previousPaymentStatus, "DELETED", StringComparison.OrdinalIgnoreCase))
                {
                    noticeKind = "canceled";
                }

                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        if (noticeKind is not null)
        {
            await TrySendPlanNoticeAsync(tenant, row, noticeKind, cancellationToken);
        }
        _accessReader.Invalidate(tenant.Id);
        if (message is not null)
        {
            await _audit.LogAsync(TenantAdminService.SystemActor, "gateway." + eventName.ToLowerInvariant(), "Tenant",
                tenant.Id.ToString(), $"{tenant.Name}: {message} Status {previous} → {tenant.Status}.", cancellationToken);
        }

        return Ok(message ?? "Evento registrado.");
    }

    private async Task TrySendPlanNoticeAsync(
        Tenant tenant,
        SaasPayment payment,
        string kind,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipient = await _context.UserAccounts
                .IgnoreQueryFilters()
                .Where(user => user.TenantId == tenant.Id && user.Email != "")
                .OrderBy(user => user.Id)
                .Select(user => user.Email)
                .FirstOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(recipient))
            {
                _logger.LogInformation(
                    "Aviso de assinatura sem destinatário. Tenant {TenantId} Tipo {Kind}.",
                    tenant.Id,
                    kind);
                return;
            }

            var planUrl = TransactionalEmailRules.BuildPlanUrl(_emailOptions.PublicBaseUrl);
            var message = kind switch
            {
                "confirmed" => TransactionalEmailRules.PlanPaymentConfirmed(
                    recipient,
                    tenant.PlanCode,
                    tenant.BillingCycle,
                    payment.Amount,
                    payment.PaidAt ?? DateTime.UtcNow,
                    planUrl),
                "overdue" => TransactionalEmailRules.PlanPaymentOverdue(recipient, tenant.PlanCode, payment.Amount, planUrl),
                _ => TransactionalEmailRules.PlanPaymentCanceled(recipient, payment.Amount, planUrl)
            };
            await _email.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar o aviso de assinatura do inquilino {TenantId}.", tenant.Id);
        }
    }

    private async Task<Tenant?> FindTenantAsync(AsaasWebhookPaymentDto payment, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(payment.ExternalReference, out var tenantId))
        {
            var byReference = await _context.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
            if (byReference is not null)
            {
                return byReference;
            }
        }

        if (!string.IsNullOrWhiteSpace(payment.Subscription))
        {
            var bySubscription = await _context.Tenants
                .FirstOrDefaultAsync(item => item.GatewaySubscriptionId == payment.Subscription, cancellationToken);
            if (bySubscription is not null)
            {
                return bySubscription;
            }
        }

        return string.IsNullOrWhiteSpace(payment.Customer)
            ? null
            : await _context.Tenants.FirstOrDefaultAsync(item => item.GatewayCustomerId == payment.Customer, cancellationToken);
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static MasterOperationResultDto Ok(string message) => new() { Success = true, Message = message };
}
