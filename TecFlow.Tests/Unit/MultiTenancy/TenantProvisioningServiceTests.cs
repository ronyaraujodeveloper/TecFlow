using Microsoft.EntityFrameworkCore;
using Moq;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;
using TecFlow.Infrastructure.Services.Tenancy;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.MultiTenancy;

public class TenantProvisioningServiceTests
{
    [Fact]
    public async Task EnsurePersistedTenantAsync_ShouldCreateTenantPrincipal_WhenTenantsTableIsEmpty()
    {
        var current = new MutableCurrentTenantService { BypassTenantFilters = true };
        await using var context = CreateDbContext(current);
        var service = new TenantProvisioningService(context, current);

        var tenant = await service.EnsurePersistedTenantAsync();

        Assert.Equal("Tenant Principal", tenant.Name);
        Assert.Equal(TenantProvisioningService.DefaultTenantId, tenant.Id);
        Assert.Equal(1, await context.Tenants.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task EnsureTenantForUserAsync_ShouldReuseExistingTenant_WhenUserHasOrphanTenantId()
    {
        var current = new MutableCurrentTenantService { BypassTenantFilters = true };
        await using var context = CreateDbContext(current);
        context.Tenants.Add(new Tenant
        {
            Id = TenantProvisioningService.DefaultTenantId,
            Name = "Tenant Principal",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var user = new UserAccount
        {
            Name = "Demo",
            Email = "demo@tecso.local",
            PasswordHash = "hash",
            TenantId = Guid.NewGuid()
        };
        context.UserAccounts.Add(user);
        await context.SaveChangesAsync();

        var service = new TenantProvisioningService(context, current);
        var tenant = await service.EnsureTenantForUserAsync(user);

        Assert.Equal(TenantProvisioningService.DefaultTenantId, tenant.Id);
        Assert.Equal(TenantProvisioningService.DefaultTenantId, user.TenantId);
    }

    private static AppDbContext CreateDbContext(ICurrentTenantService tenant)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(item => item.Encrypt(It.IsAny<string>())).Returns<string>(value => value);
        encryption.Setup(item => item.Decrypt(It.IsAny<string>())).Returns<string>(value => value);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object, tenant);
    }

    private sealed class MutableCurrentTenantService : ICurrentTenantService
    {
        public Guid? TenantId { get; set; }
        public string? ShopId { get; set; }
        public bool BypassTenantFilters { get; set; }
    }
}
