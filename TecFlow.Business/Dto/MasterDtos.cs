namespace TecFlow.Business.Dto;

public class MasterOperatorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MasterOperatorSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    /// <summary>Obrigatória na criação; opcional na edição (vazia mantém a atual).</summary>
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MasterPermissionMatrixDto
{
    public List<MasterPermissionRowDto> Permissions { get; set; } = [];
    public List<MasterRoleDto> Roles { get; set; } = [];
}

public class MasterRoleDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class MasterPermissionRowDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

public class MasterAuditLogDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class MasterAuditQueryDto
{
    public string? Action { get; set; }
    public string? Actor { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Take { get; set; } = 200;
}

public sealed record MasterActor(int UserId, string Email, string Role, string? IpAddress);

public class SystemSettingItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public bool HasValue { get; set; }
    /// <summary>Valor em claro para chaves não secretas; mascarado para secretas.</summary>
    public string DisplayValue { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class SystemSettingSaveDto
{
    /// <summary>Chave → valor. Valor vazio remove; valor mascarado (••••) é ignorado.</summary>
    public Dictionary<string, string?> Values { get; set; } = [];
}

public class SaasPlanDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxStores { get; set; }
    public int DailyVipQuota { get; set; }
    public bool RadarEnabled { get; set; }
    public int MaxDailySchedules { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public int TenantCount { get; set; }
}

public class TenantQueryDto
{
    public string? Status { get; set; }
    public string? Plan { get; set; }
    public string? Search { get; set; }
    public DateTime? RenewalBefore { get; set; }
    public long? MinDiskBytes { get; set; }
}

public class TenantSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public int? OwnerUserId { get; set; }
    public int UserCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusReason { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? RenewalAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long DiskBytes { get; set; }
    public int DiskFiles { get; set; }
    public int CreditBalance { get; set; }
    public bool HasGatewaySubscription { get; set; }
}

public class TenantLifecycleRequestDto
{
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int? TrialDays { get; set; }
}

public class TenantPlanChangeDto
{
    public string PlanCode { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = "Mensal";
}

public class TenantCreditAdjustDto
{
    /// <summary>Positivo concede, negativo remove créditos comprados.</summary>
    public int Amount { get; set; }
    public string? Reason { get; set; }
}

public class TenantStorageDto
{
    public Guid TenantId { get; set; }
    public long TotalBytes { get; set; }
    public int TotalFiles { get; set; }
    public List<TenantStorageFolderDto> Folders { get; set; } = [];
}

public class TenantStorageFolderDto
{
    public int UserId { get; set; }
    public string Path { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public int Files { get; set; }
}

public class MasterOperationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MasterDashboardDto
{
    public DateTime GeneratedAt { get; set; }
    public decimal Mrr { get; set; }
    public decimal Arr { get; set; }
    public decimal Arpu { get; set; }
    public decimal NetRevenueMonth { get; set; }
    public int PayingTenants { get; set; }
    public int TrialTenants { get; set; }
    public int OverdueTenants { get; set; }
    public int TotalTenants { get; set; }
    public decimal TrialConversionPercent { get; set; }
    public decimal ChurnRatePercent { get; set; }
    public List<MasterMonthlyPointDto> NewSubscribers { get; set; } = [];
    public int LinksConverted24h { get; set; }
    public int LinkClicks24h { get; set; }
    public int WhatsAppDispatches24h { get; set; }
    public int TelegramDispatches24h { get; set; }
    public long ApiRequests24h { get; set; }
}

public class MasterMonthlyPointDto
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SystemBannerDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class TenantNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class SegmentNotificationRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>todos | status:Inadimplente | plano:Pro | tenant:{guid}</summary>
    public string Segment { get; set; } = "todos";
}

public class ProductChangelogDto
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class ImpersonationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int TargetUserId { get; set; }
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
}

public class AsaasWebhookPayloadDto
{
    public string? Event { get; set; }
    public AsaasWebhookPaymentDto? Payment { get; set; }
}

public class AsaasWebhookPaymentDto
{
    public string? Id { get; set; }
    public string? Customer { get; set; }
    public string? Subscription { get; set; }
    public decimal Value { get; set; }
    public decimal NetValue { get; set; }
    public string? Status { get; set; }
    public string? BillingType { get; set; }
    public string? DueDate { get; set; }
    public string? PaymentDate { get; set; }
    public string? ExternalReference { get; set; }
}
