using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Contexto temporário para concluir vinculação OAuth de marketplace.</summary>
public sealed class IntegracaoLojaPendingLink
{
    public required MarketplaceType PlatformType { get; init; }
    public required string FriendlyName { get; init; }

    /// <summary>Callback OAuth da origem onde o usuário iniciou (sessão autenticada).</summary>
    public string? CallbackUri { get; init; }
}

public interface IIntegracaoLojaPendingLinkStore
{
    string Create(MarketplaceType platformType, string friendlyName, string? callbackUri);

    IntegracaoLojaPendingLink? Peek(string ticketId);

    IntegracaoLojaPendingLink? Consume(string ticketId);
}
