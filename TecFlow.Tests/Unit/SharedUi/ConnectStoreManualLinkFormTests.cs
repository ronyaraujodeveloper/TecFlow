using TecFlow.Core.Enums;
using TecFlow.SharedUi.Services.Integrations;

namespace TecFlow.Tests.Unit.SharedUi;

public class ConnectStoreManualLinkFormTests
{
    [Fact]
    public void TryValidate_ShouldAcceptHomologPayload()
    {
        var ok = ConnectStoreManualLinkForm.TryValidate(
            MarketplaceType.Shopee,
            "Loja Homolog",
            " code_teste ",
            "123456",
            out var code,
            out var shopId,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("code_teste", code);
        Assert.Equal("123456", shopId);
    }

    [Fact]
    public void TryValidate_ShouldRejectSwappedShopeeShopId()
    {
        var ok = ConnectStoreManualLinkForm.TryValidate(
            MarketplaceType.Shopee,
            "Loja Homolog",
            "123456",
            "code_teste",
            out _,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("número inteiro", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidate_ShouldRejectBlankFields()
    {
        var ok = ConnectStoreManualLinkForm.TryValidate(
            MarketplaceType.TikTokShop,
            "Loja",
            "",
            null,
            out _,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("authorization code", error, StringComparison.OrdinalIgnoreCase);
    }
}
