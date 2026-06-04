using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Tenancy;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public TenantProvisioningService(AppDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Tenant> EnsureTenantForUserAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        if (user.TenantId != Guid.Empty)
        {
            var existing = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

            if (existing is not null)
            {
                return existing;
            }
        }

        var wasBypass = _currentTenant.BypassTenantFilters;
        _currentTenant.BypassTenantFilters = true;

        try
        {
            var tenant = new Tenant
            {
                Name = $"{user.Name} — {user.Email}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Tenants.AddAsync(tenant, cancellationToken);
            user.TenantId = tenant.Id;
            await _context.SaveChangesAsync(cancellationToken);

            return tenant;
        }
        finally
        {
            _currentTenant.BypassTenantFilters = wasBypass;
        }
    }
}
