using TecFlow.Business.Dto;
using TecFlow.Business.Service.Master;

namespace TecFlow.Business.Interfaces.Services;

public interface IMasterAuditService
{
    Task LogAsync(
        MasterActor actor,
        string action,
        string targetType,
        string? targetId,
        string? details,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MasterAuditLogDto>> QueryAsync(MasterAuditQueryDto query, CancellationToken cancellationToken = default);
}

public interface IMasterOperatorService
{
    Task<IReadOnlyList<MasterOperatorDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<(MasterOperatorDto? Item, string? Error)> CreateAsync(MasterActor actor, MasterOperatorSaveDto request, CancellationToken cancellationToken = default);

    Task<(MasterOperatorDto? Item, string? Error)> UpdateAsync(MasterActor actor, int id, MasterOperatorSaveDto request, CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> SetActiveAsync(MasterActor actor, int id, bool isActive, CancellationToken cancellationToken = default);
}

public interface ISystemSettingsService
{
    Task<IReadOnlyList<SystemSettingItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> SaveAsync(MasterActor actor, SystemSettingSaveDto request, CancellationToken cancellationToken = default);

    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
}

public interface ISaasPlanService
{
    Task<IReadOnlyList<SaasPlanDto>> ListAsync(bool includeInactive = true, CancellationToken cancellationToken = default);

    Task<(SaasPlanDto? Item, string? Error)> SaveAsync(MasterActor actor, SaasPlanDto request, CancellationToken cancellationToken = default);
}

public interface IPlanLimitsService
{
    Task<PlanLimits> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
}

public interface ITenantAdminService
{
    Task<IReadOnlyList<TenantSummaryDto>> ListAsync(TenantQueryDto query, CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> ApplyLifecycleAsync(MasterActor actor, Guid tenantId, TenantLifecycleRequestDto request, CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> ChangePlanAsync(MasterActor actor, Guid tenantId, TenantPlanChangeDto request, CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> AdjustCreditsAsync(MasterActor actor, Guid tenantId, TenantCreditAdjustDto request, CancellationToken cancellationToken = default);

    Task<TenantStorageDto> GetStorageAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> PurgeMediaAsync(MasterActor actor, Guid tenantId, CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> CreateGatewaySubscriptionAsync(MasterActor actor, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Aplica auto-bloqueio de inadimplentes e expiração de trials. Retorna quantos inquilinos mudaram.</summary>
    Task<int> RunBillingCycleAsync(CancellationToken cancellationToken = default);
}

public interface ITenantAccessReader
{
    /// <summary>Status atual do inquilino (com cache curto) ou null se não existir.</summary>
    Task<string?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);

    void Invalidate(Guid tenantId);
}

public interface IImpersonationService
{
    Task<ImpersonationResultDto> StartAsync(MasterActor actor, Guid tenantId, CancellationToken cancellationToken = default);

    Task StopAsync(MasterActor actor, Guid? tenantId, CancellationToken cancellationToken = default);
}

public interface IMasterMetricsService
{
    Task<MasterDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IApiRequestCounter
{
    void Increment();

    long CountLast24Hours();
}

public interface ISystemCommunicationService
{
    Task<IReadOnlyList<SystemBannerDto>> ListBannersAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<SystemBannerDto> SaveBannerAsync(MasterActor actor, SystemBannerDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteBannerAsync(MasterActor actor, int id, CancellationToken cancellationToken = default);

    Task<int> NotifySegmentAsync(MasterActor actor, SegmentNotificationRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantNotificationDto>> ListNotificationsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<bool> MarkNotificationReadAsync(Guid tenantId, int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductChangelogDto>> ListChangelogAsync(bool onlyPublished, CancellationToken cancellationToken = default);

    Task<ProductChangelogDto> SaveChangelogAsync(MasterActor actor, ProductChangelogDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteChangelogAsync(MasterActor actor, int id, CancellationToken cancellationToken = default);
}

public sealed record PaymentGatewaySubscriptionResult(bool Success, string Message, string? CustomerId, string? SubscriptionId);

public interface ISaasPaymentGateway
{
    Task<PaymentGatewaySubscriptionResult> CreateSubscriptionAsync(
        string tenantName,
        string ownerEmail,
        string? existingCustomerId,
        decimal value,
        string billingCycle,
        string externalReference,
        CancellationToken cancellationToken = default);
}

public interface IAsaasWebhookService
{
    Task<bool> ValidateTokenAsync(string? token, CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> HandleAsync(AsaasWebhookPayloadDto payload, CancellationToken cancellationToken = default);
}
