using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Processa eventos de cobrança ASAAS: confirma pagamentos, renova e marca inadimplência.</summary>
public sealed class AsaasWebhookService : IAsaasWebhookService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMasterAuditService _audit;
    private readonly ITenantAccessReader _accessReader;

    public AsaasWebhookService(
        AppDbContext context,
        IConfiguration configuration,
        IMasterAuditService audit,
        ITenantAccessReader accessReader)
    {
        _context = context;
        _configuration = configuration;
        _audit = audit;
        _accessReader = accessReader;
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
        row.Status = Truncate(payment.Status ?? eventName, 32);
        row.BillingType = Truncate(payment.BillingType ?? string.Empty, 24);
        row.DueDate = ParseDate(payment.DueDate);
        row.Touch();

        var now = DateTime.UtcNow;
        var previous = tenant.Status;
        string? message = null;
        switch (eventName)
        {
            case "PAYMENT_CONFIRMED":
            case "PAYMENT_RECEIVED":
                row.PaidAt ??= ParseDate(payment.PaymentDate) ?? now;
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
                break;
            case "PAYMENT_REFUNDED":
            case "PAYMENT_DELETED":
                row.PaidAt = null;
                message = $"Pagamento {payment.Id} estornado/removido.";
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _accessReader.Invalidate(tenant.Id);
        if (message is not null)
        {
            await _audit.LogAsync(TenantAdminService.SystemActor, "gateway." + eventName.ToLowerInvariant(), "Tenant",
                tenant.Id.ToString(), $"{tenant.Name}: {message} Status {previous} → {tenant.Status}.", cancellationToken);
        }

        return Ok(message ?? "Evento registrado.");
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
