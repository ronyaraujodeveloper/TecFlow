using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Unit.Groups;

public class OfferIntegrityRulesTests
{
    [Theory]
    [InlineData("https://www.mercadolivre.com.br/social/loja-teste?matt_tool=1")]
    [InlineData("https://www.mercadolivre.com.br/lists/ofertas-do-dia")]
    [InlineData("https://www.mercadolivre.com.br/perfil/VENDEDOR123")]
    [InlineData("https://www.mercadolivre.com.br/perfil-vendedor/VENDEDOR123")]
    [InlineData("https://www.mercadolivre.com.br/SOCIAL/loja")]
    [InlineData("https://www.mercadolivre.com.br/social")]
    public void IsGenericStorefrontUrl_ShouldDetectStorefrontPaths(string url)
    {
        Assert.True(OfferIntegrityRules.IsGenericStorefrontUrl(url));
    }

    [Theory]
    [InlineData("https://produto.mercadolivre.com.br/MLB-123456789-smart-tv-50-_JM")]
    [InlineData("https://www.mercadolivre.com.br/smart-tv-50/p/MLB123")]
    [InlineData("https://shopee.com.br/produto-i.1.2")]
    [InlineData("https://www.amazon.com.br/dp/B0ABCDEFGH")]
    [InlineData("https://www.mercadolivre.com.br/perfilado-aluminio/p/MLB1")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("texto sem url")]
    public void IsGenericStorefrontUrl_ShouldKeepProductUrls(string? url)
    {
        Assert.False(OfferIntegrityRules.IsGenericStorefrontUrl(url));
    }

    [Theory]
    [InlineData("Smart TV 50\" 4K Samsung", 4.99)]
    [InlineData("Notebook Lenovo IdeaPad i5", 1.00)]
    [InlineData("Smartphone Motorola Moto G84", 2.50)]
    [InlineData("iPhone 15 128GB", 0.99)]
    [InlineData("Televisão LG 43 polegadas", 3.00)]
    public void IsSuspiciousPrice_ShouldFlagHighValueProductsWithIrrisoryPrice(string name, double price)
    {
        Assert.True(OfferIntegrityRules.IsSuspiciousPrice(name, (decimal)price));
    }

    [Theory]
    [InlineData("Smart TV 50\" 4K Samsung", 1999.90)]
    [InlineData("Capa para celular iPhone 15", 4.99)]
    [InlineData("Película de vidro para smartphone", 3.50)]
    [InlineData("Cabo USB-C para notebook", 4.00)]
    [InlineData("Caneta esferográfica azul", 1.50)]
    [InlineData("Smart TV 50\" 4K Samsung", 0)]
    [InlineData(null, 1.00)]
    public void IsSuspiciousPrice_ShouldIgnoreLegitimateCases(string? name, double price)
    {
        Assert.False(OfferIntegrityRules.IsSuspiciousPrice(name, (decimal)price));
    }

    [Fact]
    public void ApplyPriceSanity_ShouldReplaceActiveStatusWithPrecoSuspeito()
    {
        Assert.Equal(
            GroupOfferStatuses.PrecoSuspeito,
            OfferIntegrityRules.ApplyPriceSanity(GroupOfferStatuses.Ativo, "Notebook Dell", 2m));
        Assert.Equal(
            GroupOfferStatuses.Esgotado,
            OfferIntegrityRules.ApplyPriceSanity(GroupOfferStatuses.Esgotado, "Notebook Dell", 2m));
        Assert.Equal(
            GroupOfferStatuses.Ativo,
            OfferIntegrityRules.ApplyPriceSanity(GroupOfferStatuses.Ativo, "Notebook Dell", 2999m));
    }

    [Theory]
    [InlineData("<div>Publicação finalizada</div>")]
    [InlineData("<p>Este anúncio está pausado no momento</p>")]
    [InlineData("<span>ANÚNCIO ENCERRADO</span>")]
    public void ContainsClosedListingPhrase_ShouldDetectClosedOrPausedListings(string html)
    {
        Assert.True(OfferIntegrityRules.ContainsClosedListingPhrase(html));
    }

    [Fact]
    public void GroupOfferStatuses_ShouldExposeInactiveAndLabels()
    {
        Assert.True(GroupOfferStatuses.IsInactive(GroupOfferStatuses.Inativo));
        Assert.True(GroupOfferStatuses.IsInactive(GroupOfferStatuses.VitrineGenerica));
        Assert.True(GroupOfferStatuses.IsInactive(GroupOfferStatuses.Esgotado));
        Assert.False(GroupOfferStatuses.IsInactive(GroupOfferStatuses.PrecoSuspeito));
        Assert.Equal("Vitrine Genérica", GroupOfferStatuses.ToUiLabel(GroupOfferStatuses.VitrineGenerica));
        Assert.Equal("Preço Suspeito", GroupOfferStatuses.ToUiLabel(GroupOfferStatuses.PrecoSuspeito));
    }

    [Fact]
    public void OfferRevalidationOptions_ShouldClampAndDetectDueOffers()
    {
        var options = new OfferRevalidationOptions { IntervalHours = 0, BatchSize = 50_000 };
        var now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(TimeSpan.FromMinutes(15), options.Interval);
        Assert.Equal(1000, options.EffectiveBatchSize);
        Assert.True(OfferRevalidationOptions.IsDue(null, now, TimeSpan.FromHours(2)));
        Assert.True(OfferRevalidationOptions.IsDue(now.AddHours(-3), now, TimeSpan.FromHours(2)));
        Assert.False(OfferRevalidationOptions.IsDue(now.AddMinutes(-30), now, TimeSpan.FromHours(2)));
    }
}
