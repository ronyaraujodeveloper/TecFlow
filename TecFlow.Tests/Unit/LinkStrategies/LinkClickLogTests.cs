using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.Repositories;
using TecFlow.Infrastructure.Services.ShortLinks;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class LinkClickLogTests
{
    private static readonly Guid TestTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private const string ProductUrl = "https://shopee.com.br/produto-i.123.456";
    private const string ConvertedUrl = "https://shopee.com.br/produto-i.123.456?tracking_code=tecflow_sandbox_subid&sub_id=u10";
    private const string ShopId = "shop-sandbox-19";

    [Fact]
    public async Task AffiliateLinkGenerationService_ShouldPersistLinkClickLogWithTenantAndShop()
    {
        var affiliateLinkId = Guid.Parse("33333333-4444-5555-6666-777777777777");
        var recorder = new RecordingClickLogRepository();
        var telemetry = CreateTelemetryService(recorder);
        var context = new AffiliateLinkGenerationContext
        {
            ClientIpAddress = "187.22.10.44",
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)",
            ReferrerUrl = "https://web.whatsapp.com/"
        };

        var service = new AffiliateLinkGenerationService(
            new PlatformLinkResolver(
                [new StubShopeeStrategy()],
                NullLogger<PlatformLinkResolver>.Instance),
            new PassthroughUrlExpansionService(),
            new FixedStoreResolver(CreateStore()),
            new FixedShortLinkService(affiliateLinkId),
            telemetry,
            context,
            NullLogger<AffiliateLinkGenerationService>.Instance);

        var result = await service.GenerateAsync(
            new GerarLinkAfiliadoDto
            {
                OriginalUrl = ProductUrl,
                StoreId = IntegracaoLojaScopeHelper.EncodeStoreScope(1)
            },
            userId: 10);

        Assert.True(result.Success);
        Assert.Equal(ProductUrl, result.OriginalUrl);
        Assert.Equal(ConvertedUrl, result.AffiliateUrl);
        Assert.Equal("http://localhost:5001/r/abcdef1", result.ShortenedUrl);
        Assert.NotNull(recorder.LastAdded);
        Assert.Equal(affiliateLinkId, recorder.LastAdded!.AffiliateLinkId);
        Assert.Equal(TestTenantId, recorder.LastAdded.TenantId);
        Assert.Equal(ShopId, recorder.LastAdded.ShopId);
        Assert.Equal(ProductUrl, recorder.LastAdded.OriginalUrl);
        Assert.Equal(ConvertedUrl, recorder.LastAdded.ConvertedUrl);
        Assert.Equal("Shopee", recorder.LastAdded.Platform);
        Assert.Equal(LinkClickLog.EventKindGeneration, recorder.LastAdded.EventKind);
        Assert.NotEqual(default, recorder.LastAdded.CreatedAt);
        Assert.Equal("187.22.10.***", recorder.LastAdded.IpAddress);
        Assert.Equal("Mobile", recorder.LastAdded.DeviceType);
        Assert.Equal("WhatsApp", recorder.LastAdded.ReferrerUrl);
        Assert.Contains("iPhone", recorder.LastAdded.UserAgent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LinkClickLogRepository_ShouldSaveRequiredFieldsWithoutNullViolations()
    {
        await using var db = CreateDbContext();
        var affiliateLinkId = Guid.NewGuid();
        db.ShortAffiliateLinks.Add(new ShortAffiliateLink
        {
            AffiliateLinkId = affiliateLinkId,
            ShortCode = "abc1234",
            DestinationUrl = ConvertedUrl,
            OriginalUrl = ProductUrl,
            PlatformType = MarketplaceType.Shopee,
            UserId = 10,
            IntegracaoLojaId = 1,
            TenantId = TestTenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var repository = new LinkClickLogRepository(db);
        var log = LinkClickLogFactory.CreateGeneration(
            affiliateLinkId,
            TestTenantId,
            ShopId,
            ProductUrl,
            ConvertedUrl,
            MarketplaceType.Shopee,
            "10.0.0.8",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            "https://tecflow.local/gerador");

        await repository.AddAsync(log);

        var persisted = await db.LinkClickLogs.AsNoTracking().SingleAsync();
        Assert.Equal(TestTenantId, persisted.TenantId);
        Assert.Equal(ShopId, persisted.ShopId);
        Assert.Equal(ProductUrl, persisted.OriginalUrl);
        Assert.Equal(ConvertedUrl, persisted.ConvertedUrl);
        Assert.Equal("Shopee", persisted.Platform);
        Assert.False(string.IsNullOrWhiteSpace(persisted.IpAddress));
        Assert.False(string.IsNullOrWhiteSpace(persisted.UserAgent));
        Assert.Equal("Desktop", persisted.DeviceType);
        Assert.True(persisted.CreatedAt > DateTime.MinValue);
        Assert.Equal(affiliateLinkId, persisted.AffiliateLinkId);
        Assert.NotNull(persisted.ShopId);
        Assert.NotNull(persisted.OriginalUrl);
        Assert.NotNull(persisted.ConvertedUrl);
        Assert.NotNull(persisted.Platform);
    }

    [Fact]
    public void LinkClickLogFactory_ShouldRejectMissingRequiredKeys()
    {
        Assert.Throws<InvalidOperationException>(() =>
            LinkClickLogFactory.CreateGeneration(
                Guid.Empty,
                TestTenantId,
                ShopId,
                ProductUrl,
                ConvertedUrl,
                MarketplaceType.Shopee,
                null,
                null,
                null));

        Assert.Throws<InvalidOperationException>(() =>
            LinkClickLogFactory.CreateGeneration(
                Guid.NewGuid(),
                Guid.Empty,
                ShopId,
                ProductUrl,
                ConvertedUrl,
                MarketplaceType.Shopee,
                null,
                null,
                null));

        Assert.Throws<InvalidOperationException>(() =>
            LinkClickLogFactory.CreateGeneration(
                Guid.NewGuid(),
                TestTenantId,
                ShopId,
                "",
                ConvertedUrl,
                MarketplaceType.Shopee,
                null,
                null,
                null));
    }

    [Fact]
    public async Task LinkClickLogRepository_ShouldCountOnlyClickEvents()
    {
        await using var db = CreateDbContext();
        var affiliateLinkId = Guid.NewGuid();
        db.ShortAffiliateLinks.Add(new ShortAffiliateLink
        {
            AffiliateLinkId = affiliateLinkId,
            ShortCode = "xyz9876",
            DestinationUrl = ConvertedUrl,
            OriginalUrl = ProductUrl,
            PlatformType = MarketplaceType.Shopee,
            UserId = 10,
            TenantId = TestTenantId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repository = new LinkClickLogRepository(db);
        await repository.AddAsync(LinkClickLogFactory.CreateGeneration(
            affiliateLinkId, TestTenantId, ShopId, ProductUrl, ConvertedUrl, MarketplaceType.Shopee, null, null, null));
        await repository.AddAsync(LinkClickLogFactory.CreateClick(
            affiliateLinkId, TestTenantId, ShopId, ProductUrl, ConvertedUrl, MarketplaceType.Shopee, "1.1.1.1", "Mozilla/5.0", null));

        var counts = await repository.GetClickCountsByAffiliateLinkIdsAsync([affiliateLinkId]);
        Assert.Equal(1, counts[affiliateLinkId]);
    }

    private static ILinkClickTelemetryService CreateTelemetryService(ILinkClickLogRepository repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILinkClickLogRepository>(repository);
        var provider = services.BuildServiceProvider();
        return new LinkClickTelemetryService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<LinkClickTelemetryService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object, new TecFlow.Database.MultiTenancy.NullCurrentTenantService());
    }

    private static IntegracaoLoja CreateStore() =>
        new()
        {
            Id = 1,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = ShopId,
            FriendlyName = "Loja Homolog",
            AccessToken = "token",
            PlatformType = MarketplaceType.Shopee
        };

    private sealed class RecordingClickLogRepository : ILinkClickLogRepository
    {
        public LinkClickLog? LastAdded { get; private set; }

        public Task AddAsync(LinkClickLog entity, CancellationToken cancellationToken = default)
        {
            LastAdded = entity;
            return Task.CompletedTask;
        }

        public Task<Dictionary<Guid, int>> GetClickCountsByAffiliateLinkIdsAsync(
            IEnumerable<Guid> affiliateLinkIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, int>());
    }

    private sealed class StubShopeeStrategy : IPlatformLinkStrategy
    {
        public MarketplaceType PlatformType => MarketplaceType.Shopee;

        public string PlatformName => "Shopee";

        public bool CanProcess(string url) => url.Contains("shopee", StringComparison.OrdinalIgnoreCase);

        public Task<string> GenerateDeepLinkAsync(
            string originalUrl,
            Guid storeId,
            string affiliateId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ConvertedUrl);
    }

    private sealed class PassthroughUrlExpansionService : IUrlExpansionService
    {
        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(shortenedUrl);
    }

    private sealed class FixedStoreResolver : IIntegracaoLojaScopeResolver
    {
        private readonly IntegracaoLoja _store;

        public FixedStoreResolver(IntegracaoLoja store) => _store = store;

        public Task<IntegracaoLoja> ResolveAsync(
            Guid storeScopeId,
            int userId,
            MarketplaceType expectedPlatform,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_store);
    }

    private sealed class FixedShortLinkService : IShortLinkService
    {
        private readonly Guid _affiliateLinkId;

        public FixedShortLinkService(Guid affiliateLinkId) => _affiliateLinkId = affiliateLinkId;

        public Task<(string PublicShortUrl, Guid AffiliateLinkId)> CreateShortLinkAsync(
            string destinationUrl,
            string originalUrl,
            MarketplaceType platformType,
            int userId,
            Guid tenantId,
            int? integracaoLojaId,
            string? customNickname,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(("http://localhost:5001/r/abcdef1", _affiliateLinkId));
    }
}
