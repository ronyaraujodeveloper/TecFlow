using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Unit.WhatsApp;

public class WhatsAppSessionRulesTests
{
    [Fact]
    public void BuildInstanceName_ShouldIsolateByUserId()
    {
        Assert.Equal("tecflow-u42", WhatsAppSessionRules.BuildInstanceName(42));
        Assert.NotEqual(
            WhatsAppSessionRules.BuildInstanceName(1),
            WhatsAppSessionRules.BuildInstanceName(2));
    }

    [Theory]
    [InlineData("open", WhatsAppConnectionStatuses.Connected)]
    [InlineData("connecting", WhatsAppConnectionStatuses.AwaitingQr)]
    [InlineData("close", WhatsAppConnectionStatuses.Disconnected)]
    public void MapFromEvolutionState_ShouldMapOpenCloseConnecting(string evolution, string expected)
    {
        Assert.Equal(expected, WhatsAppSessionRules.MapFromEvolutionState(evolution));
    }

    [Fact]
    public void ToUiLabel_ShouldMatchPublicCardCopy()
    {
        Assert.Equal("Conectado", WhatsAppSessionRules.ToUiLabel(WhatsAppConnectionStatuses.Connected));
        Assert.Equal("Aguardando Leitura", WhatsAppSessionRules.ToUiLabel(WhatsAppConnectionStatuses.AwaitingQr));
        Assert.Equal("Desconectado", WhatsAppSessionRules.ToUiLabel(WhatsAppConnectionStatuses.Disconnected));
    }

    [Fact]
    public void NormalizeQrDataUrl_ShouldPrefixBase64()
    {
        var url = WhatsAppSessionRules.NormalizeQrDataUrl("abc123");
        Assert.StartsWith("data:image/png;base64,", url);
    }
}
