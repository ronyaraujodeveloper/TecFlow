using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.Radar;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Radar;

public class LiveCheckSearchServiceTests
{
    private const string ProductUrl = "https://produto.mercadolivre.com.br/MLB-1234567890-notebook-dell-i7-_JM";

    [Fact]
    public async Task CheckUrlAsync_DirectMercadoLivreUrl_ShouldUseOfficialApiWithoutFetchingPublicPage()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(s => s.GetItemAsync(7, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfficialOfferSnapshotDto { IsAvailable = true, Status = GroupOfferStatuses.Ativo, Price = 3999m, Source = "Api" });
        var html = new Mock<IOfferValidationService>(MockBehavior.Strict);
        var sut = CreateSut(ml.Object, html.Object, new MercadoLivreAntiBotGuard());

        var snapshot = await sut.CheckUrlAsync(7, ProductUrl, MarketplaceType.MercadoLivre, 4299m);

        Assert.Equal("Api", snapshot.Source);
        Assert.Equal(3999m, snapshot.Price);
        html.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckUrlAsync_OnAntiBotChallenge_ShouldDiscardHtmlAndTripGuard()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        var html = new Mock<IOfferValidationService>();
        html.Setup(s => s.ValidateProductPageStatusAsync(ProductUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfferPageStatusDto
            {
                IsAvailable = true,
                Status = GroupOfferStatuses.Ativo,
                FinalUrl = "https://www.mercadolivre.com.br/gz/account-verification?go=x",
                Html = "<html>account-verification</html>"
            });
        var guard = new MercadoLivreAntiBotGuard();
        var sut = CreateSut(ml.Object, html.Object, guard);

        var snapshot = await sut.CheckUrlAsync(7, ProductUrl, MarketplaceType.MercadoLivre, 4299m);

        Assert.Equal(MercadoLivreAntiBotGuard.AntiBotSource, snapshot.Source);
        Assert.Equal(GroupOfferStatuses.Verificando, snapshot.Status);
        Assert.Equal(4299m, snapshot.Price);
        Assert.True(guard.IsBlocked);
        html.Verify(s => s.ValidateFromPageAsync(It.IsAny<OfferPageStatusDto>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckUrlAsync_WhileGuardIsBlocked_ShouldNotFetchMercadoLivrePage()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        var html = new Mock<IOfferValidationService>(MockBehavior.Strict);
        var guard = new MercadoLivreAntiBotGuard();
        guard.Trip();
        var sut = CreateSut(ml.Object, html.Object, guard);

        var snapshot = await sut.CheckUrlAsync(7, ProductUrl, MarketplaceType.MercadoLivre, 4299m);

        Assert.Equal(GroupOfferStatuses.Verificando, snapshot.Status);
        Assert.True(snapshot.IsAvailable);
        html.VerifyNoOtherCalls();
        ml.Verify(s => s.GetItemAsync(7, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AntiBotGuard_ShouldExpireAfterCooldown()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var guard = new MercadoLivreAntiBotGuard(clock);

        guard.Trip();
        Assert.True(guard.IsBlocked);

        clock.Advance(MercadoLivreAntiBotGuard.Cooldown + TimeSpan.FromSeconds(1));
        Assert.False(guard.IsBlocked);
    }

    [Theory]
    [InlineData("https://www.mercadolivre.com.br/gz/account-verification?go=x", "<html></html>", true)]
    [InlineData(ProductUrl, "<script>bot_challenge</script>", true)]
    [InlineData(ProductUrl, "<div class=\"ui-pdp-container\">bot_challenge</div>", false)]
    [InlineData(ProductUrl, "<div class=\"ui-pdp-title\">Notebook</div>", false)]
    public void IsChallengePage_ShouldSeparateChallengeFromProductPage(string finalUrl, string html, bool expected)
    {
        Assert.Equal(expected, MercadoLivreAntiBotGuard.IsChallengePage(finalUrl, html));
    }

    private static LiveCheckSearchService CreateSut(
        IMercadoLivreApiService ml,
        IOfferValidationService html,
        MercadoLivreAntiBotGuard guard) =>
        new(
            ml,
            new Mock<IShopeeAffiliateOfferService>().Object,
            new Mock<IAmazonPaApiService>().Object,
            html,
            CreateDbContext(),
            NullLogger<LiveCheckSearchService>.Instance,
            guard);

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, encryption.Object, new Ofertio.Database.MultiTenancy.NullCurrentTenantService());
    }

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
