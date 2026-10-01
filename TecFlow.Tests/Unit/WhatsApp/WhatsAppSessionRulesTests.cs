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
    public void TryParseUserId_ShouldExtractOwner()
    {
        Assert.True(WhatsAppSessionRules.TryParseUserId("tecflow-u42", out var userId));
        Assert.Equal(42, userId);
    }

    [Fact]
    public void ResolveConnectUiMessage_ShouldHideGeneric500()
    {
        var message = WhatsAppSessionRules.ResolveConnectUiMessage(
            "Internal Server Error",
            "Erro 500",
            500);
        Assert.Equal(WhatsAppSessionRules.EvolutionUnreachableMessage, message);
    }

    [Fact]
    public void ResolveConnectUiMessage_ShouldKeepBusinessError()
    {
        var message = WhatsAppSessionRules.ResolveConnectUiMessage(
            "Instância criada, mas o QR Code ainda não está disponível. Tente novamente em instantes.",
            null,
            400);
        Assert.Contains("QR Code", message, StringComparison.Ordinal);
    }
}
