using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Tenancy;

public class TenantProvisioningService : ITenantProvisioningService
{
    public static readonly Guid DefaultTenantId = Guid.Parse("a1000000-0000-4000-8000-000000000001");

    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public TenantProvisioningService(AppDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Tenant> EnsureTenantForUserAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        var preferred = user.TenantId == Guid.Empty ? (Guid?)null : user.TenantId;
        var tenant = await EnsurePersistedTenantAsync(preferred, cancellationToken);

        if (user.TenantId == tenant.Id)
        {
            return tenant;
        }

        user.TenantId = tenant.Id;
        if (user.Id > 0)
        {
            var tracked = await _context.UserAccounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(usuario => usuario.Id == user.Id, cancellationToken);
            if (tracked is not null && tracked.TenantId != tenant.Id)
            {
                tracked.TenantId = tenant.Id;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return tenant;
    }

    public async Task<Tenant> EnsurePersistedTenantAsync(
        Guid? preferredTenantId = null,
        CancellationToken cancellationToken = default)
    {
        var wasBypass = _currentTenant.BypassTenantFilters;
        _currentTenant.BypassTenantFilters = true;

        try
        {
            if (preferredTenantId is { } preferred && preferred != Guid.Empty)
            {
                var byPreferred = await _context.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(tenant => tenant.Id == preferred, cancellationToken);
                if (byPreferred is not null)
                {
                    return byPreferred;
                }
            }

            var active = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(tenant => tenant.IsActive)
                .OrderBy(tenant => tenant.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (active is not null)
            {
                return active;
            }

            var any = await _context.Tenants
                .IgnoreQueryFilters()
                .OrderBy(tenant => tenant.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (any is not null)
            {
                return any;
            }

            var principal = new Tenant
            {
                Id = DefaultTenantId,
                Name = "Tenant Principal",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Tenants.AddAsync(principal, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return principal;
        }
        finally
        {
            _currentTenant.BypassTenantFilters = wasBypass;
        }
    }
}
