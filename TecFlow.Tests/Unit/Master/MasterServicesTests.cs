using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;
using TecFlow.Infrastructure.Services.Master;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Master;

public class MasterServicesTests
{
    private static readonly MasterActor Actor = new(1, "admin@tecflow.com", MasterRoles.SuperAdmin, "127.0.0.1");

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    [Fact]
    public async Task AuditTrail_ShouldRejectUpdatesAndDeletes()
    {
        await using var context = CreateContext();
        var audit = new MasterAuditService(context);
        await audit.LogAsync(Actor, "tenant.suspender", "Tenant", "abc", "motivo");

        var log = await context.MasterAuditLogs.SingleAsync();
        log.Details = "adulterado";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        context.Entry(log).State = EntityState.Unchanged;
        context.MasterAuditLogs.Remove(log);
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
    }

    [Fact]
    public async Task SystemSettings_ShouldMaskSecretsAndIgnoreMaskedValuesOnSave()
    {
        await using var context = CreateContext();
        var audit = new Mock<IMasterAuditService>();
        var service = new SystemSettingsService(context, new ConfigurationBuilder().Build(), audit.Object);

        var first = await service.SaveAsync(Actor, new SystemSettingSaveDto
        {
            Values = new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "sk-secret-123456",
                [SystemSettingCatalog.ShortLinkDomain] = "https://tflow.link",
                ["Chave:Desconhecida"] = "x"
            }
        });

        Assert.True(first.Success);
        Assert.Equal(2, await context.SystemSettings.CountAsync());

        var listed = await service.ListAsync();
        var openAi = listed.Single(item => item.Key == "OpenAI:ApiKey");
        Assert.Equal("••••3456", openAi.DisplayValue);
        Assert.Equal("https://tflow.link", listed.Single(item => item.Key == SystemSettingCatalog.ShortLinkDomain).DisplayValue);

        var second = await service.SaveAsync(Actor, new SystemSettingSaveDto
        {
            Values = new Dictionary<string, string?> { ["OpenAI:ApiKey"] = openAi.DisplayValue }
        });

        Assert.Equal("Nenhuma chave alterada.", second.Message);
        Assert.Equal("sk-secret-123456", await service.GetValueAsync("OpenAI:ApiKey"));
        audit.Verify(item => item.LogAsync(
            Actor,
            It.IsAny<string>(),
            "SystemSetting",
            null,
            It.Is<string>(details => !details.Contains("sk-secret")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SystemSettings_NewSecretDifferentFromMask_ShouldReplaceStoredValue()
    {
        await using var context = CreateContext();
        var service = new SystemSettingsService(context, new ConfigurationBuilder().Build(), new Mock<IMasterAuditService>().Object);
        await service.SaveAsync(Actor, new SystemSettingSaveDto { Values = new() { ["Integrations:MercadoLivre:SecretKey"] = "old-secret-1111" } });

        var result = await service.SaveAsync(Actor, new SystemSettingSaveDto { Values = new() { ["Integrations:MercadoLivre:SecretKey"] = "new-secret-2222" } });

        Assert.True(result.Success);
        Assert.Equal("new-secret-2222", await service.GetValueAsync("Integrations:MercadoLivre:SecretKey"));
    }

    [Fact]
    public async Task SystemSettings_SecretTypedAfterMask_ShouldBeRejectedAndKeepStoredValue()
    {
        await using var context = CreateContext();
        var service = new SystemSettingsService(context, new ConfigurationBuilder().Build(), new Mock<IMasterAuditService>().Object);
        await service.SaveAsync(Actor, new SystemSettingSaveDto { Values = new() { ["Integrations:MercadoLivre:SecretKey"] = "old-secret-1111" } });

        var result = await service.SaveAsync(Actor, new SystemSettingSaveDto
        {
            Values = new()
            {
                ["Integrations:MercadoLivre:SecretKey"] = "••••1111abc",
                ["Integrations:MercadoLivre:AppId"] = "123"
            }
        });

        Assert.False(result.Success);
        Assert.Equal("old-secret-1111", await service.GetValueAsync("Integrations:MercadoLivre:SecretKey"));
        Assert.Null(await context.SystemSettings.SingleOrDefaultAsync(item => item.Key == "Integrations:MercadoLivre:AppId"));
    }

    [Fact]
    public async Task SystemSettings_EmptyValue_ShouldRemoveRow()
    {
        await using var context = CreateContext();
        var service = new SystemSettingsService(context, new ConfigurationBuilder().Build(), new Mock<IMasterAuditService>().Object);
        await service.SaveAsync(Actor, new SystemSettingSaveDto { Values = new() { ["OpenAI:ApiKey"] = "sk-1" } });

        await service.SaveAsync(Actor, new SystemSettingSaveDto { Values = new() { ["OpenAI:ApiKey"] = "" } });

        Assert.Equal(0, await context.SystemSettings.CountAsync());
    }

    [Fact]
    public async Task PlanLimits_ShouldResolveTenantPlanAndExemptMasters()
    {
        await using var context = CreateContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Loja", PlanCode = SaasPlanCodes.Starter };
        context.Tenants.Add(tenant);
        context.SaasPlans.Add(new SaasPlan
        {
            Code = SaasPlanCodes.Starter,
            Name = "Starter",
            MaxStores = 2,
            DailyVipQuota = 5,
            RadarEnabled = false,
            MaxDailySchedules = 10,
            IsActive = true
        });
        context.UserAccounts.Add(new UserAccount { Id = 10, Name = "Cliente", Email = "c@x.com", PasswordHash = "h", TenantId = tenant.Id, Role = MasterRoles.TenantUser });
        context.UserAccounts.Add(new UserAccount { Id = 11, Name = "Admin", Email = "a@x.com", PasswordHash = "h", TenantId = tenant.Id, Role = MasterRoles.SuperAdmin });
        await context.SaveChangesAsync();
        var service = new SaasPlanService(context);

        var client = await service.GetForUserAsync(10);
        var master = await service.GetForUserAsync(11);

        Assert.Equal(SaasPlanCodes.Starter, client.PlanCode);
        Assert.Equal(2, client.MaxStores);
        Assert.False(client.RadarEnabled);
        Assert.True(master.IsUnlimited);
        Assert.True((await service.GetForUserAsync(999)).IsUnlimited);
    }
}
