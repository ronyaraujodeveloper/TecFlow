using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Billing;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.Master;

namespace Ofertio.Infrastructure.Services.Billing;

/// <summary>
/// Meu plano: monta o resumo da assinatura e registra o pedido. O plano do inquilino não muda aqui:
/// com cobrança real, quem troca o plano é o webhook do gateway ao confirmar o pagamento.
/// </summary>
public sealed class MyPlanService : IMyPlanService
{
    private readonly AppDbContext _context;
    private readonly UserProfileService _profiles;
    private readonly IPlanPaymentGateway _gateway;
    private readonly ILogger<MyPlanService> _logger;

    public MyPlanService(
        AppDbContext context,
        UserProfileService profiles,
        IPlanPaymentGateway gateway,
        ILogger<MyPlanService> logger)
    {
        _context = context;
        _profiles = profiles;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<MyPlanResponseDto> GetOverviewAsync(int userId, CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account is null)
        {
            return new MyPlanResponseDto { Status = false, Descricao = "Usuário não encontrado." };
        }

        var plans = await ActivePlansAsync(cancellationToken);
        var current = plans.FirstOrDefault(plan => string.Equals(plan.Code, account.PlanCode, StringComparison.OrdinalIgnoreCase));
        var profile = await _profiles.LoadAsync(userId, cancellationToken);
        var lastCheckout = await _context.PlanCheckouts.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new MyPlanResponseDto
        {
            Status = true,
            Descricao = "OK",
            IsMaster = account.IsMaster,
            CurrentPlanCode = current?.Code ?? account.PlanCode,
            CurrentPlanName = current?.Name ?? account.PlanCode,
            BillingCycle = account.BillingCycle,
            TenantStatus = account.TenantStatus,
            TrialEndsAt = account.TrialEndsAt,
            RenewalAt = account.RenewalAt,
            IsPaymentSimulated = _gateway.IsSimulated,
            MissingFields = UserProfileRules.MissingFields(profile),
            Plans = plans,
            LastCheckout = lastCheckout is null ? null : ToDto(lastCheckout)
        };
    }

    public async Task<PlanCheckoutResponseDto> CheckoutAsync(int userId, PlanCheckoutDto request, CancellationToken cancellationToken = default)
    {
        request ??= new PlanCheckoutDto();
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account is null)
        {
            return Fail("Usuário não encontrado.");
        }

        if (account.IsMaster)
        {
            return Fail("A conta Master não assina planos.");
        }

        var cycle = PlanPricingRules.NormalizeCycle(request.BillingCycle);
        if (cycle is null)
        {
            return Fail("Escolha o pagamento mensal ou anual.");
        }

        var plans = await ActivePlansAsync(cancellationToken);
        var plan = plans.FirstOrDefault(item => string.Equals(item.Code, request.PlanCode?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (plan is null)
        {
            return Fail("Plano não encontrado ou indisponível.");
        }

        var amount = PlanPricingRules.Price(plan, cycle);
        if (amount <= 0)
        {
            return Fail("Este plano não tem preço para o ciclo escolhido.");
        }

        if (string.Equals(account.PlanCode, plan.Code, StringComparison.OrdinalIgnoreCase)
            && string.Equals(account.BillingCycle, cycle, StringComparison.OrdinalIgnoreCase)
            && TenantStatuses.Normalize(account.TenantStatus) == TenantStatuses.Ativo)
        {
            return Fail($"Você já está no plano {plan.Name} ({cycle.ToLowerInvariant()}).");
        }

        var profile = await _profiles.LoadAsync(userId, cancellationToken);
        var missing = UserProfileRules.MissingFields(profile);
        if (profile is null || missing.Count > 0)
        {
            return new PlanCheckoutResponseDto
            {
                Status = false,
                Descricao = $"Complete seus dados cadastrais antes de assinar: {string.Join(", ", missing)}.",
                MissingFields = missing
            };
        }

        var cardErrors = CreditCardRules.Validate(request.Card, DateTime.UtcNow);
        if (cardErrors.Count > 0)
        {
            return Fail(string.Join(" ", cardErrors));
        }

        var card = request.Card!;
        var payment = await _gateway.ChargeAsync(
            new PlanPaymentRequest(account.TenantId, userId, plan.Code, cycle, amount, profile, card),
            cancellationToken);

        var checkout = new PlanCheckout
        {
            TenantId = account.TenantId,
            UserId = userId,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            BillingCycle = cycle,
            Amount = amount,
            Status = payment.Status,
            CardBrand = CreditCardRules.DetectBrand(card.Number),
            CardLast4 = CreditCardRules.Last4(card.Number),
            GatewayPaymentId = payment.GatewayPaymentId,
            Message = payment.Message.Length <= 512 ? payment.Message : payment.Message[..512]
        };
        _context.PlanCheckouts.Add(checkout);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Pedido de assinatura {CheckoutId}: usuário {UserId}, plano {PlanCode} {Cycle}, status {Status}.",
            checkout.Id, userId, plan.Code, cycle, checkout.Status);

        return new PlanCheckoutResponseDto
        {
            Status = payment.Approved,
            Descricao = payment.Message,
            Data = ToDto(checkout)
        };
    }

    public async Task<PlanCheckoutResponseDto> GetCheckoutAsync(int userId, int checkoutId, CancellationToken cancellationToken = default)
    {
        var checkout = await _context.PlanCheckouts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == checkoutId && item.UserId == userId, cancellationToken);
        return checkout is null
            ? Fail("Pedido não encontrado.")
            : new PlanCheckoutResponseDto { Status = true, Descricao = checkout.Message ?? "OK", Data = ToDto(checkout) };
    }

    private async Task<List<SaasPlanDto>> ActivePlansAsync(CancellationToken cancellationToken)
    {
        var plans = await _context.SaasPlans.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return plans.Select(SaasPlanService.Map).ToList();
    }

    private async Task<AccountSnapshot?> LoadAccountAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.UserAccounts.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.TenantId, item.Plan, item.Role })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        var tenant = await _context.Tenants.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Id == user.TenantId)
            .Select(item => new { item.PlanCode, item.BillingCycle, item.Status, item.TrialEndsAt, item.RenewalAt })
            .FirstOrDefaultAsync(cancellationToken);
        return new AccountSnapshot(
            user.TenantId,
            MasterRoles.IsMaster(user.Role),
            string.IsNullOrWhiteSpace(tenant?.PlanCode) ? user.Plan : tenant.PlanCode,
            tenant?.BillingCycle,
            tenant?.Status,
            tenant?.TrialEndsAt,
            tenant?.RenewalAt);
    }

    private static PlanCheckoutDto ToDto(PlanCheckout checkout) =>
        new()
        {
            Id = checkout.Id,
            PlanCode = checkout.PlanCode,
            PlanName = checkout.PlanName,
            BillingCycle = checkout.BillingCycle,
            Amount = checkout.Amount,
            Status = checkout.Status,
            CardBrand = checkout.CardBrand,
            CardLast4 = checkout.CardLast4,
            Message = checkout.Message,
            CreatedAt = checkout.CreatedAt
        };

    private static PlanCheckoutResponseDto Fail(string message) => new() { Status = false, Descricao = message };

    private sealed record AccountSnapshot(
        Guid TenantId,
        bool IsMaster,
        string? PlanCode,
        string? BillingCycle,
        string? TenantStatus,
        DateTime? TrialEndsAt,
        DateTime? RenewalAt);
}
