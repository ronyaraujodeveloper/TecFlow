using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TecFlow.Database.MultiTenancy;
using TecFlow.Core.Security;

namespace TecFlow.Infrastructure.Security;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId => ResolveGuidClaim(TecFlowClaimTypes.TenantId);

    public string? ShopId =>
        ResolveStringClaim(TecFlowClaimTypes.ShopId)
        ?? ResolveShopIdFromHeader();

    private string? ResolveShopIdFromHeader()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers["X-TecFlow-Shop-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(header) ? null : header.Trim();
    }

    public bool BypassTenantFilters { get; set; }

    private Guid? ResolveGuidClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? ResolveStringClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
