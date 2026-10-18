using Ofertio.Business.Dto;
using Ofertio.Business.Service.Groups;
using Ofertio.Core.Enums;

namespace Ofertio.Tests.Unit.Groups;

public class CouponMessageRulesTests
{
    [Fact]
    public void Classify_ShouldReturnCoupon_WhenMessageHasCouponWithoutProductUrl()
    {
        const string text =
            """
            🎟️ CUPOM SHOPEE: R$ 20 OFF acima de R$ 99
            Use o código SHOPEE20 no app
            https://shopee.com.br/m/cupons
            """;

        Assert.Equal(OfferType.Coupon, CouponMessageRules.Classify(text, hasDirectProductUrl: false));
    }

    [Fact]
    public void Classify_ShouldReturnCoupon_WhenOnlyCouponAmountsHavePrices()
    {
        const string text = "USAR CUPOM MELI15 — R$ 15 OFF em compras acima de R$ 120\nhttps://www.mercadolivre.com.br/p/MLB123";

        Assert.Equal(OfferType.Coupon, CouponMessageRules.Classify(text, hasDirectProductUrl: true));
    }

    [Fact]
    public void Classify_ShouldKeepProduct_WhenProductOfferAlsoCarriesCoupon()
    {
        const string text =
            """
            🔥 Fone Bluetooth
            ✅ R$ 479,90
            Cupom SURPRESAMELIMAIS
            https://www.mercadolivre.com.br/p/MLB123
            """;

        Assert.Equal(OfferType.Product, CouponMessageRules.Classify(text, hasDirectProductUrl: true));
    }

    [Theory]
    [InlineData("🔥 NOVO CUPOM MERCADO LIVRE\n\n🎟 Cupom: CARE15 - 15% OFF em R$ 5, Limite de R$ 145 OFF\nhttps://meli.la/abc")]
    [InlineData("🔥NOVOS CUPONS MERCADO LIVRE\n🛒 Em produtos selecionados\n10% OFF acima de R$ 149, Limite de R$ 200:\n✅ Cupom: OFFHOJE\nhttps://meli.la/abc")]
    [InlineData("NOVO CUPOM SHOPEE!\nR$ 20 OFF\nhttps://shopee.com.br/produto-i.1.2")]
    [InlineData("Mercado Livre\nCupom: PARAVC - 30% OFF\nCupom: MELIOFFHJ - 25% OFF\nhttps://meli.la/abc")]
    public void Classify_ShouldReturnCoupon_WhenMessageAnnouncesCoupons(string text)
    {
        Assert.Equal(OfferType.Coupon, CouponMessageRules.Classify(text, hasDirectProductUrl: true));
    }

    [Theory]
    [InlineData("💧 Purificador de Água Consul CPB34ASVNA\n\nPOR: R$ 391,42 no Pix\n\n🎟 Use o cupom: OFFHOJE\n\n🛒 Compre aqui:\nhttps://meli.la/1ByK7gG")]
    [InlineData("CUPOM RELÂMPAGO ⚡\n\n👟 Tênis Fila Recovery Feminino\n🔗 https://meli.la/1ZxLXMC\n\n✅ R$ 260,90\n🎟 Use o CUPOM: SITE1610")]
    [InlineData("🖥 Monitor Gamer Curvo AOC AGON G4Z 27\"\n\n💰 Valor: R$1.079,00 - 12x sem juros\nCupom: CUPOMPRIME10\n\n🔗 https://www.amazon.com.br/dp/B0FGDJQ68P?tag=geovani0d-20\n✅ Oferta verificada: conferimos manualmente o preço e cupom")]
    [InlineData("🔨 Martelo Amaciador De Carne Aço Inox 22cm | Shopee:\n\n🛒 GARANTA JÁ:\n🔗 https://s.shopee.com.br/7Ae6Jz1CfD\n\n✅ a partir de R$ 36,50\n\n🎟 CUPONS e OFERTAS - 10.10:\n🔗 https://s.shopee.com.br/5pmo7sve98")]
    public void Classify_ShouldKeepProduct_WhenProductOfferMentionsSingleCoupon(string text)
    {
        Assert.Equal(OfferType.Product, CouponMessageRules.Classify(text, hasDirectProductUrl: true));
    }

    [Fact]
    public void ExtractCouponCodes_ShouldReturnDistinctCodesInOrder()
    {
        const string text = "Cupom: CARE15\nCupom: PARAVC\nUse o cupom CARE15";

        Assert.Equal(["CARE15", "PARAVC"], CouponMessageRules.ExtractCouponCodes(text));
    }

    [Fact]
    public void Classify_ShouldKeepProduct_WhenNoCouponSignal()
    {
        Assert.Equal(
            OfferType.Product,
            CouponMessageRules.Classify("Cadeira Gamer R$ 599,00\nhttps://shopee.com.br/produto-i.1.2", hasDirectProductUrl: true));
    }

    [Theory]
    [InlineData("CUPOM: BRINCADEIRAS", "BRINCADEIRAS")]
    [InlineData("Use o cupom MELI20 e ganhe desconto", "MELI20")]
    [InlineData("USAR CUPOM: SHOPEE10", "SHOPEE10")]
    [InlineData("Código `AMZ-PRIME15` válido hoje", "AMZ-PRIME15")]
    [InlineData("CUPOM DE R$ 20 OFF ACIMA DE R$ 99", null)]
    [InlineData("Cupom: frete grátis para todo o site", null)]
    public void ExtractCouponCode_ShouldReadOnlyRealCodes(string text, string? expected)
    {
        Assert.Equal(expected, CouponMessageRules.ExtractCouponCode(text));
    }

    [Theory]
    [InlineData("R$ 20 OFF acima de R$ 99", "R$ 20 OFF")]
    [InlineData("Desconto de R$ 12,50 na primeira compra", "R$ 12,50 OFF")]
    [InlineData("Até 15% OFF em eletrônicos", "15% OFF")]
    [InlineData("Cupom de frete grátis", "Frete grátis")]
    [InlineData("Cupom especial", null)]
    public void ExtractDiscount_ShouldNormalizeDiscount(string text, string? expected)
    {
        Assert.Equal(expected, CouponMessageRules.ExtractDiscount(text));
    }

    [Theory]
    [InlineData("Cupom válido até 20/10", "Válido até 20/10")]
    [InlineData("Só hoje! Cupom MELI10", "Só hoje")]
    [InlineData("Cupom MELI10", null)]
    public void ExtractValidity_ShouldReadDateOrPhrase(string text, string? expected)
    {
        Assert.Equal(expected, CouponMessageRules.ExtractValidity(text));
    }

    [Fact]
    public void ExtractRules_ShouldIncludeMinimumPurchaseAndRuleLines()
    {
        const string text =
            """
            🎟️ CUPOM SHOPEE20
            💰 R$ 20 OFF acima de R$ 99
            ⚠️ Limitado a 1 uso por CPF
            https://shopee.com.br/m/cupons
            """;

        var rules = CouponMessageRules.ExtractRules(text);

        Assert.Equal("Compras acima de R$ 99", rules[0]);
        Assert.Contains("Limitado a 1 uso por CPF", rules);
        Assert.DoesNotContain(rules, rule => rule.Contains("http", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Cupom Mercado Livre", MarketplaceType.MercadoLivre)]
    [InlineData("Cupom SHOPEE no app", MarketplaceType.Shopee)]
    [InlineData("Cupom Amazon Prime", MarketplaceType.Amazon)]
    [InlineData("Cupom Magalu", null)]
    public void DetectPlatformFromText_ShouldMapHighlightedStores(string text, MarketplaceType? expected)
    {
        Assert.Equal(expected, CouponMessageRules.DetectPlatformFromText(text));
    }

    [Fact]
    public void DeduplicateOffers_ShouldKeepDifferentCouponsSharingTheSameStoreLink()
    {
        var offers = new[]
        {
            new GroupCapturedOfferDto { Id = 1, Type = OfferType.Coupon, CouponCode = "SHOPEE10", OriginalUrl = "https://shopee.com.br/m/cupons", ExternalMessageId = "10", GroupKey = "Telegram:1" },
            new GroupCapturedOfferDto { Id = 2, Type = OfferType.Coupon, CouponCode = "SHOPEE20", OriginalUrl = "https://shopee.com.br/m/cupons", ExternalMessageId = "11", GroupKey = "Telegram:1" },
            new GroupCapturedOfferDto { Id = 3, Type = OfferType.Coupon, CouponCode = "SHOPEE10", OriginalUrl = "https://shopee.com.br/m/cupons", ExternalMessageId = "12", GroupKey = "Telegram:2" }
        };

        var unique = GroupOfferCaptureRules.DeduplicateOffers(offers);

        Assert.Equal([1, 2], unique.Select(item => item.Id));
    }

    [Fact]
    public void Parse_ShouldFlagCouponMessages()
    {
        var parsed = new StructuredOfferParserService().Parse("🎟️ Cupom MELI15: R$ 15 OFF\nhttps://www.mercadolivre.com.br/cupons");

        Assert.Equal(OfferType.Coupon, parsed.Type);
        Assert.Equal("MELI15", parsed.CouponCode);
    }
}
