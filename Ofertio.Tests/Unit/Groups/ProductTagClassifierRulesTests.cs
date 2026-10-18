using Ofertio.Business.Service.Groups;
using Ofertio.Core.Entities;

namespace Ofertio.Tests.Unit.Groups;

public class ProductTagClassifierRulesTests
{
    [Theory]
    [InlineData("Cadeira Gamer Ergonômica Reclinável", ProductTagCatalog.Gamer)]
    [InlineData("Console PlayStation 5 Slim", ProductTagCatalog.Gamer)]
    [InlineData("Smart TV 50\" 4K Samsung", ProductTagCatalog.Eletronicos)]
    [InlineData("Notebook Lenovo IdeaPad 15", ProductTagCatalog.Informatica)]
    [InlineData("Fritadeira Air Fryer Mondial 4L", ProductTagCatalog.CasaCozinha)]
    [InlineData("Smartphone Motorola Moto G84 256GB", ProductTagCatalog.Smartphones)]
    [InlineData("Perfume Masculino Malbec 100ml", ProductTagCatalog.ModaBeleza)]
    public void Classify_ShouldTagByProductName(string productName, string expectedSlug)
    {
        Assert.Contains(expectedSlug, ProductTagClassifierRules.Classify(productName, null));
    }

    [Fact]
    public void Classify_ShouldReturnMultipleTagsInCatalogOrder()
    {
        var slugs = ProductTagClassifierRules.Classify("Headset Gamer Bluetooth RGB", null);

        Assert.Equal([ProductTagCatalog.Gamer, ProductTagCatalog.Eletronicos], slugs);
    }

    [Fact]
    public void Classify_ShouldIgnoreAccentsAndCase()
    {
        Assert.Contains(ProductTagCatalog.Informatica, ProductTagClassifierRules.Classify("PLACA MÃE ASUS", null));
        Assert.Contains(ProductTagCatalog.CasaCozinha, ProductTagClassifierRules.Classify("Fogão 4 bocas", null));
    }

    [Fact]
    public void Classify_ShouldReadTelegramMessageTextAndSkipUrls()
    {
        const string raw = "🔥 OFERTA\nKit 3 Panelas Tramontina\nR$ 89,90\nhttps://produto.mercadolivre.com.br/MLB-123-notebook-gamer-_JM";

        var slugs = ProductTagClassifierRules.Classify(null, raw);

        Assert.Equal([ProductTagCatalog.CasaCozinha], slugs);
    }

    [Theory]
    [InlineData("Tvbox genérico")]
    [InlineData("Ração para cães 15kg")]
    [InlineData("")]
    public void Classify_ShouldNotTagPartialWordsOrUnknownProducts(string productName)
    {
        Assert.Empty(ProductTagClassifierRules.Classify(productName, null));
    }

    [Fact]
    public void Catalog_ShouldHaveOneKeywordRulePerSeededTag()
    {
        Assert.Equal(
            ProductTagCatalog.Seed.Select(seed => seed.Slug).OrderBy(slug => slug),
            ProductTagClassifierRules.Keywords.Keys.OrderBy(slug => slug));
    }
}
