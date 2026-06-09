using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Contexto temporário para concluir vinculação OAuth de marketplace.</summary>
public sealed class IntegracaoLojaPendingLink
{
    public required MarketplaceType PlatformType { get; init; }
    public required string FriendlyName { get; init; }
}

public interface IIntegracaoLojaPendingLinkStore
{
    string Create(MarketplaceType platformType, string friendlyName);

    IntegracaoLojaPendingLink? Consume(string ticketId);
}
