using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Central 360° de inquilinos: ciclo de vida, plano, créditos, disco e assinatura.</summary>
public sealed class TenantAdminService : ITenantAdminService
{
    public const string CreditKindMasterAdjust = "MasterAdjust";

    public static readonly MasterActor SystemActor = new(0, "sistema@ofertio", "Sistema", null);

    private readonly AppDbContext _context;
    private readonly IUploadStorage _uploads;
    private readonly IMasterAuditService _audit;
    private readonly ITenantAccessReader _accessReader;
    private readonly ISaasPaymentGateway _gateway;
    private readonly ICurrentTenantService _currentTenant;

    public TenantAdminService(
        AppDbContext context,
        IUploadStorage uploads,
        IMasterAuditService audit,
        ITenantAccessReader accessReader,
        ISaasPaymentGateway gateway,
        ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
        _uploads = uploads;
        _audit = audit;
        _accessReader = accessReader;
        _gateway = gateway;
    }

    public async Task<IReadOnlyList<TenantSummaryDto>> ListAsync(TenantQueryDto query, CancellationToken cancellationToken = default)
    {
        var tenants = _context.Tenants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = TenantStatuses.Normalize(query.Status);
            tenants = tenants.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Plan))
        {
            var plan = query.Plan.Trim();
            tenants = tenants.Where(item => item.PlanCode == plan);
        }

        if (query.RenewalBefore.HasValue)
        {
            var limit = query.RenewalBefore.Value;
            tenants = tenants.Where(item => item.RenewalAt != null && item.RenewalAt <= limit);
        }

        var tenantRows = await tenants.OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var tenantIds = tenantRows.Select(item => item.Id).ToList();
        var users = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => tenantIds.Contains(item.TenantId))
            .Select(item => new { item.Id, item.TenantId, item.Email, item.Plan, item.Role, item.CreatedAt })
            .ToListAsync(cancellationToken);
        var wallets = await _context.TenantCredits
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => tenantIds.Contains(item.TenantId))
            .ToDictionaryAsync(item => item.TenantId, item => item.DailyBalance + item.PurchasedBalance, cancellationToken);
        var webRoot = ResolveWebRoot();

        var search = query.Search?.Trim();
        var result = new List<TenantSummaryDto>();
        foreach (var tenant in tenantRows)
        {
            var tenantUsers = users.Where(item => item.TenantId == tenant.Id).OrderBy(item => item.CreatedAt).ToList();
            var owner = tenantUsers.FirstOrDefault(item => !MasterRoles.IsMaster(item.Role)) ?? tenantUsers.FirstOrDefault();
            if (!string.IsNullOrEmpty(search)
                && !tenant.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !tenantUsers.Any(item => item.Email.Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var (bytes, files) = MeasureUsers(webRoot, tenantUsers.Select(item => item.Id));
            if (query.MinDiskBytes is > 0 && bytes < query.MinDiskBytes.Value)
            {
                continue;
            }

            result.Add(new TenantSummaryDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                OwnerEmail = owner?.Email,
                OwnerUserId = owner?.Id,
                UserCount = tenantUsers.Count,
                Status = TenantStatuses.Normalize(tenant.Status),
                StatusReason = tenant.StatusReason,
                StatusChangedAt = tenant.StatusChangedAt,
                PlanCode = string.IsNullOrWhiteSpace(tenant.PlanCode) ? owner?.Plan ?? string.Empty : tenant.PlanCode,
                BillingCycle = tenant.BillingCycle,
                TrialEndsAt = tenant.TrialEndsAt,
                RenewalAt = tenant.RenewalAt,
                CreatedAt = tenant.CreatedAt,
                DiskBytes = bytes,
                DiskFiles = files,
                CreditBalance = wallets.TryGetValue(tenant.Id, out var balance) ? balance : 0,
                HasGatewaySubscription = !string.IsNullOrWhiteSpace(tenant.GatewaySubscriptionId)
            });
        }

        return result;
    }

    public async Task<MasterOperationResultDto> ApplyLifecycleAsync(
        MasterActor actor,
        Guid tenantId,
        TenantLifecycleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return Fail("Inquilino não encontrado.");
        }

        var action = request.Action?.Trim().ToLowerInvariant() ?? string.Empty;
        var reason = request.Reason?.Trim();
        if (TenantLifecycleRules.RequiresReason(action) && string.IsNullOrWhiteSpace(reason))
        {
            return Fail("Informe o motivo da ação.");
        }

        var now = DateTime.UtcNow;
        var transition = TenantLifecycleRules.Apply(tenant.Status, action, now, tenant.TrialEndsAt, request.TrialDays);
        if (transition is null)
        {
            return Fail($"Ação \"{request.Action}\" inválida para o status {TenantStatuses.Normalize(tenant.Status)}.");
        }

        var previous = TenantStatuses.Normalize(tenant.Status);
        ApplyTransition(tenant, transition, reason, now);
        await _context.SaveChangesAsync(cancellationToken);
        _accessReader.Invalidate(tenant.Id);
        await _audit.LogAsync(actor, MasterAuditActions.TenantLifecycle, "Tenant", tenant.Id.ToString(),
            $"{tenant.Name}: {action} ({previous} → {tenant.Status}). Motivo: {reason ?? "-"}." +
            (action == TenantLifecycleActions.EstenderTrial ? $" Trial até {tenant.TrialEndsAt:dd/MM/yyyy}." : string.Empty),
            cancellationToken);
        return new MasterOperationResultDto { Success = true, Message = $"Inquilino {tenant.Name} agora está {tenant.Status}." };
    }

    public async Task<MasterOperationResultDto> ChangePlanAsync(
        MasterActor actor,
        Guid tenantId,
        TenantPlanChangeDto request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return Fail("Inquilino não encontrado.");
        }

        var code = request.PlanCode?.Trim() ?? string.Empty;
        if (!await _context.SaasPlans.AnyAsync(item => item.Code == code, cancellationToken))
        {
            return Fail("Plano inexistente.");
        }

        var cycle = string.Equals(request.BillingCycle, SaasMetricsRules.CycleAnnual, StringComparison.OrdinalIgnoreCase)
            ? SaasMetricsRules.CycleAnnual
            : SaasMetricsRules.CycleMonthly;
        var previous = $"{tenant.PlanCode ?? "-"}/{tenant.BillingCycle}";
        tenant.PlanCode = code;
        tenant.BillingCycle = cycle;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.TenantPlanChanged, "Tenant", tenant.Id.ToString(),
            $"{tenant.Name}: plano {previous} → {code}/{cycle}.", cancellationToken);
        return new MasterOperationResultDto { Success = true, Message = $"Plano alterado para {code} ({cycle})." };
    }

    public async Task<MasterOperationResultDto> AdjustCreditsAsync(
        MasterActor actor,
        Guid tenantId,
        TenantCreditAdjustDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount == 0 || Math.Abs(request.Amount) > 100_000)
        {
            return Fail("Informe um ajuste diferente de zero (máx. 100.000).");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Fail("Informe o motivo do ajuste de créditos.");
        }

        if (!await _context.Tenants.AnyAsync(item => item.Id == tenantId, cancellationToken))
        {
            return Fail("Inquilino não encontrado.");
        }

        _currentTenant.BypassTenantFilters = true;
        var wallet = await _context.TenantCredits
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        if (wallet is null)
        {
            wallet = new TenantCredit { TenantId = tenantId };
            _context.TenantCredits.Add(wallet);
        }

        var applied = request.Amount < 0 ? -Math.Min(wallet.PurchasedBalance, -request.Amount) : request.Amount;
        wallet.PurchasedBalance += applied;
        wallet.Touch();
        _context.CreditTransactions.Add(new CreditTransaction
        {
            TenantId = tenantId,
            Amount = applied,
            Kind = CreditKindMasterAdjust
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.CreditsAdjusted, "Tenant", tenantId.ToString(),
            $"Ajuste de {applied:+#;-#;0} crédito(s) (solicitado {request.Amount:+#;-#;0}). Saldo comprado: {wallet.PurchasedBalance}. Motivo: {request.Reason.Trim()}.",
            cancellationToken);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = $"Créditos ajustados em {applied:+#;-#;0}. Saldo comprado atual: {wallet.PurchasedBalance}."
        };
    }

    public async Task<TenantStorageDto> GetStorageAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var userIds = await TenantUserIdsAsync(tenantId, cancellationToken);
        var webRoot = ResolveWebRoot();
        var dto = new TenantStorageDto { TenantId = tenantId };
        foreach (var userId in userIds)
        {
            var folder = ProductImageStorageRules.TryResolveTenantProductsFolder(webRoot, userId);
            var (bytes, files) = MeasureFolder(folder);
            dto.Folders.Add(new TenantStorageFolderDto
            {
                UserId = userId,
                Path = Path.Combine(_uploads.UploadsRoot, "products", userId.ToString()),
                Bytes = bytes,
                Files = files
            });
        }

        dto.TotalBytes = dto.Folders.Sum(item => item.Bytes);
        dto.TotalFiles = dto.Folders.Sum(item => item.Files);
        return dto;
    }

    public async Task<MasterOperationResultDto> PurgeMediaAsync(
        MasterActor actor,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var before = await GetStorageAsync(tenantId, cancellationToken);
        var webRoot = ResolveWebRoot();
        var deleted = before.Folders.Sum(folder =>
            ProductImageStorageRules.TryDeleteTenantProductFiles(
                ProductImageStorageRules.TryResolveTenantProductsFolder(webRoot, folder.UserId)));
        await _audit.LogAsync(actor, MasterAuditActions.MediaPurged, "Tenant", tenantId.ToString(),
            $"Purga manual: {deleted} arquivo(s) removido(s) de {before.TotalFiles} ({FormatBytes(before.TotalBytes)}).",
            cancellationToken);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = $"{deleted} arquivo(s) removido(s) ({FormatBytes(before.TotalBytes)} liberados)."
        };
    }

    public async Task<MasterOperationResultDto> CreateGatewaySubscriptionAsync(
        MasterActor actor,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return Fail("Inquilino não encontrado.");
        }

        if (!string.IsNullOrWhiteSpace(tenant.GatewaySubscriptionId))
        {
            return Fail("Este inquilino já possui assinatura no gateway.");
        }

        var owner = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new { item.Email, item.Plan })
            .FirstOrDefaultAsync(cancellationToken);
        var planCode = string.IsNullOrWhiteSpace(tenant.PlanCode) ? owner?.Plan : tenant.PlanCode;
        var plan = await _context.SaasPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Code == planCode, cancellationToken);
        if (plan is null || owner is null)
        {
            return Fail("Defina um plano SaaS válido para o inquilino antes de gerar a cobrança.");
        }

        var annual = string.Equals(tenant.BillingCycle, SaasMetricsRules.CycleAnnual, StringComparison.OrdinalIgnoreCase);
        var result = await _gateway.CreateSubscriptionAsync(
            tenant.Name,
            owner.Email,
            tenant.GatewayCustomerId,
            annual ? plan.AnnualPrice : plan.MonthlyPrice,
            tenant.BillingCycle,
            tenant.Id.ToString(),
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(result.CustomerId))
        {
            tenant.GatewayCustomerId = result.CustomerId;
        }

        if (result.Success)
        {
            tenant.GatewaySubscriptionId = result.SubscriptionId;
            tenant.PlanCode = plan.Code;
            tenant.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.GatewaySubscription, "Tenant", tenant.Id.ToString(),
            $"{tenant.Name}: {result.Message} Plano {plan.Code}/{tenant.BillingCycle}.", cancellationToken);
        return new MasterOperationResultDto { Success = result.Success, Message = result.Message };
    }

    public async Task<int> RunBillingCycleAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var candidates = await _context.Tenants
            .Where(item => item.Status == TenantStatuses.Inadimplente || item.Status == TenantStatuses.Trial)
            .ToListAsync(cancellationToken);
        var changed = 0;
        foreach (var tenant in candidates)
        {
            string? reason = null;
            TenantLifecycleTransition? transition = null;
            if (TenantLifecycleRules.ShouldAutoSuspend(tenant.Status, tenant.StatusChangedAt, now))
            {
                reason = $"Bloqueio automático: inadimplente há mais de {TenantLifecycleRules.OverdueGraceDays} dias.";
                transition = TenantLifecycleRules.Apply(tenant.Status, TenantLifecycleActions.Suspender, now, tenant.TrialEndsAt);
            }
            else if (TenantLifecycleRules.ShouldExpireTrial(tenant.Status, tenant.TrialEndsAt, tenant.GatewaySubscriptionId, now))
            {
                reason = "Trial expirado sem assinatura ativa.";
                transition = TenantLifecycleRules.Apply(tenant.Status, TenantLifecycleActions.MarcarInadimplente, now, tenant.TrialEndsAt);
            }

            if (transition is null)
            {
                continue;
            }

            var previous = tenant.Status;
            ApplyTransition(tenant, transition, reason, now);
            await _context.SaveChangesAsync(cancellationToken);
            _accessReader.Invalidate(tenant.Id);
            await _audit.LogAsync(SystemActor, MasterAuditActions.TenantLifecycle, "Tenant", tenant.Id.ToString(),
                $"{tenant.Name}: {previous} → {tenant.Status}. {reason}", cancellationToken);
            changed++;
        }

        return changed;
    }

    internal static void ApplyTransition(Tenant tenant, TenantLifecycleTransition transition, string? reason, DateTime now)
    {
        tenant.Status = transition.Status;
        tenant.TrialEndsAt = transition.TrialEndsAt;
        if (transition.CanceledAt.HasValue)
        {
            tenant.CanceledAt = transition.CanceledAt;
        }
        else if (transition.Status != TenantStatuses.Cancelado)
        {
            tenant.CanceledAt = null;
        }

        tenant.StatusReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Length > 512 ? reason[..512] : reason;
        tenant.StatusChangedAt = now;
        tenant.IsActive = !TenantLifecycleRules.IsAccessBlocked(transition.Status);
        tenant.UpdatedAt = now;
    }

    private async Task<List<int>> TenantUserIdsAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

    private string ResolveWebRoot() => _uploads.MediaRoot;

    private static (long Bytes, int Files) MeasureUsers(string webRoot, IEnumerable<int> userIds)
    {
        long bytes = 0;
        var files = 0;
        foreach (var userId in userIds)
        {
            var (folderBytes, folderFiles) = MeasureFolder(ProductImageStorageRules.TryResolveTenantProductsFolder(webRoot, userId));
            bytes += folderBytes;
            files += folderFiles;
        }

        return (bytes, files);
    }

    internal static (long Bytes, int Files) MeasureFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return (0, 0);
        }

        try
        {
            var infos = new DirectoryInfo(folder).EnumerateFiles("*", SearchOption.AllDirectories).ToList();
            return (infos.Sum(item => item.Length), infos.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (0, 0);
        }
    }

    public static string FormatBytes(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):N2} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):N1} MB",
        >= 1L << 10 => $"{bytes / (double)(1L << 10):N0} KB",
        _ => $"{bytes} B"
    };

    private static MasterOperationResultDto Fail(string message) => new() { Success = false, Message = message };
}
