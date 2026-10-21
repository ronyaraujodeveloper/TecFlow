using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.Master;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Integrations;

public class GlobalIntegrationServiceTests
{
    [Fact]
    public async Task CompleteMercadoLivreAuthorizationAsync_ShouldRejectUserWithoutGlobalKeysPermission()
    {
        await using var context = CreateContext();
        context.UserAccounts.Add(User(MasterRoles.FinanceiroSaaS));
        await context.SaveChangesAsync();
        var auth = new Mock<IMarketplaceAuthService>();
        var sut = CreateSut(context, auth.Object);

        var result = await sut.CompleteMercadoLivreAuthorizationAsync(1, "TG-code", "https://dev.ofertio.com.br/integracoes/oauth/callback", "verifier");

        Assert.False(result.Success);
        auth.Verify(x => x.ExchangeMercadoLivreCodeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(context.GlobalIntegrationTokens);
    }

    [Fact]
    public async Task CompleteMercadoLivreAuthorizationAsync_ShouldStoreMasterTokenForSuperAdmin()
    {
        await using var context = CreateContext();
        context.UserAccounts.Add(User(MasterRoles.SuperAdmin));
        context.GlobalIntegrationTokens.Add(new GlobalIntegrationToken
        {
            Provider = MarketplaceType.MercadoLivre,
            AccessToken = "APP_USR-old",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            LastError = "recusado"
        });
        await context.SaveChangesAsync();
        var expiresAt = DateTime.UtcNow.AddHours(6);
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(x => x.ExchangeMercadoLivreCodeAsync("TG-code", It.IsAny<string?>(), "verifier", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketplaceOAuthExchangeResult
            {
                Success = true,
                AccessToken = "APP_USR-new",
                RefreshToken = "TG-refresh",
                ExpiresAt = expiresAt
            });
        var sut = CreateSut(context, auth.Object);

        var result = await sut.CompleteMercadoLivreAuthorizationAsync(1, "TG-code", null, "verifier");

        Assert.True(result.Success);
        var stored = await context.GlobalIntegrationTokens.SingleAsync();
        Assert.Equal("APP_USR-new", stored.AccessToken);
        Assert.Equal("TG-refresh", stored.RefreshToken);
        Assert.Equal(expiresAt, stored.ExpiresAt);
        Assert.Null(stored.LastError);
        Assert.Equal("master@ofertio.local", stored.AuthorizedBy);

        var status = await sut.GetMercadoLivreStatusAsync();
        Assert.Equal(GlobalIntegrationRules.StatusActive, status.Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1441)]
    public async Task SaveMercadoLivreSettingsAsync_ShouldRejectOutOfRangeMinutes(int minutes)
    {
        await using var context = CreateContext();
        var settings = new Mock<ISystemSettingsService>();
        var sut = CreateSut(context, Moq.Mock.Of<IMarketplaceAuthService>(), settings.Object);

        var result = await sut.SaveMercadoLivreSettingsAsync(Actor(), new GlobalIntegrationSettingsDto { SearchCacheMinutes = minutes });

        Assert.False(result.Success);
        settings.Verify(x => x.SaveAsync(It.IsAny<MasterActor>(), It.IsAny<SystemSettingSaveDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveMercadoLivreSettingsAsync_ShouldPersistCacheMinutesInGlobalSettings()
    {
        await using var context = CreateContext();
        SystemSettingSaveDto? saved = null;
        var settings = new Mock<ISystemSettingsService>();
        settings.Setup(x => x.SaveAsync(It.IsAny<MasterActor>(), It.IsAny<SystemSettingSaveDto>(), It.IsAny<CancellationToken>()))
            .Callback<MasterActor, SystemSettingSaveDto, CancellationToken>((_, request, _) => saved = request)
            .ReturnsAsync(new MasterOperationResultDto { Success = true });
        var sut = CreateSut(context, Moq.Mock.Of<IMarketplaceAuthService>(), settings.Object);

        var result = await sut.SaveMercadoLivreSettingsAsync(Actor(), new GlobalIntegrationSettingsDto { SearchCacheMinutes = 30 });

        Assert.True(result.Success);
        Assert.Equal("30", saved!.Values[MercadoLivreSearchCacheRules.SettingKey]);
    }

    private static GlobalIntegrationService CreateSut(
        AppDbContext context,
        IMarketplaceAuthService auth,
        ISystemSettingsService? settings = null)
    {
        var monitor = new Mock<IOptionsMonitor<MercadoLivreIntegrationOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(new MercadoLivreIntegrationOptions { AppId = "123", SecretKey = "secret" });
        return new GlobalIntegrationService(
            context,
            auth,
            settings ?? Moq.Mock.Of<ISystemSettingsService>(),
            Moq.Mock.Of<IMasterAuditService>(),
            monitor.Object,
            NullLogger<GlobalIntegrationService>.Instance);
    }

    private static UserAccount User(string role) => new()
    {
        Id = 1,
        TenantId = Guid.NewGuid(),
        Name = "Master",
        Email = "master@ofertio.local",
        PasswordHash = "hash",
        Role = role
    };

    private static MasterActor Actor() => new(1, "master@ofertio.local", MasterRoles.SuperAdmin, null);

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, encryption.Object, new Ofertio.Database.MultiTenancy.NullCurrentTenantService());
    }
}
