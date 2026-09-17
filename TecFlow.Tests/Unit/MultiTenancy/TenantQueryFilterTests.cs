using Microsoft.EntityFrameworkCore;
using Moq;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.MultiTenancy;

public class TenantQueryFilterTests
{
    [Fact]
    public async Task UserAccounts_WhenTenantIdIsNull_AreVisibleForLoginLookup()
    {
        var tenant = new MutableCurrentTenantService { BypassTenantFilters = true };
        await using var context = CreateDbContext(tenant);

        var tenantId = Guid.NewGuid();
        context.UserAccounts.Add(new UserAccount
        {
            Name = "Demo",
            Email = "demo@tecso.local",
            PasswordHash = "hash",
            TenantId = tenantId
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        tenant.BypassTenantFilters = false;
        tenant.TenantId = null;

        var found = await context.UserAccounts
            .FirstOrDefaultAsync(u => u.Email == "demo@tecso.local");

        Assert.NotNull(found);
        Assert.Equal("demo@tecso.local", found!.Email);
    }

    [Fact]
    public async Task UserAccounts_WhenTenantIdIsSet_HideOtherTenants()
    {
        var tenant = new MutableCurrentTenantService { BypassTenantFilters = true };
        await using var context = CreateDbContext(tenant);

        var ownTenant = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        context.UserAccounts.AddRange(
            new UserAccount
            {
                Name = "Own",
                Email = "own@tecso.local",
                PasswordHash = "hash",
                TenantId = ownTenant
            },
            new UserAccount
            {
                Name = "Other",
                Email = "other@tecso.local",
                PasswordHash = "hash",
                TenantId = otherTenant
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        tenant.BypassTenantFilters = false;
        tenant.TenantId = ownTenant;

        var emails = await context.UserAccounts.Select(u => u.Email).ToListAsync();

        Assert.Contains("own@tecso.local", emails);
        Assert.DoesNotContain("other@tecso.local", emails);
    }

    private static AppDbContext CreateDbContext(ICurrentTenantService tenant)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

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
