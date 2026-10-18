using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Ofertio.Database.MultiTenancy;
using Ofertio.Core.Security;

namespace Ofertio.Infrastructure.Security;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId => ResolveGuidClaim(OfertioClaimTypes.TenantId);

    public string? ShopId =>
        ResolveStringClaim(OfertioClaimTypes.ShopId)
        ?? ResolveShopIdFromHeader();

    private string? ResolveShopIdFromHeader()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers["X-Ofertio-Shop-Id"].FirstOrDefault();
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
