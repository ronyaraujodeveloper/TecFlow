using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

public sealed class MarketplaceOAuthStartResult
{
    public bool Success { get; init; }

    public string? AuthorizeUrl { get; init; }

    public string? ErrorMessage { get; init; }

    public static MarketplaceOAuthStartResult Fail(string message) =>
        new()
        {
            Success = false,
            ErrorMessage = message
        };

    public static MarketplaceOAuthStartResult Ok(string authorizeUrl) =>
        new()
        {
            Success = true,
            AuthorizeUrl = authorizeUrl
        };
}

public interface IMarketplaceOAuthConnectService
{
    Task<MarketplaceOAuthStartResult> StartAsync(
        MarketplaceType platform,
        string friendlyName,
        string callbackUri,
        string? lojaId = null,
        CancellationToken cancellationToken = default);
}

public sealed class MarketplaceOAuthConnectService : IMarketplaceOAuthConnectService
{
    private readonly IIntegracaoLojaApiService _integracaoApi;
    private readonly IIntegracaoLojaPendingLinkStore _pendingLinkStore;

    public MarketplaceOAuthConnectService(
        IIntegracaoLojaApiService integracaoApi,
        IIntegracaoLojaPendingLinkStore pendingLinkStore)
    {
        _integracaoApi = integracaoApi;
        _pendingLinkStore = pendingLinkStore;
    }

    public async Task<MarketplaceOAuthStartResult> StartAsync(
        MarketplaceType platform,
        string friendlyName,
        string callbackUri,
        string? lojaId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            return MarketplaceOAuthStartResult.Fail("Informe um apelido para identificar a loja.");
        }

        if (string.IsNullOrWhiteSpace(callbackUri))
        {
            return MarketplaceOAuthStartResult.Fail("Callback OAuth inválido.");
        }

        var ticket = _pendingLinkStore.Create(platform, friendlyName.Trim());
        var response = await _integracaoApi.GetAuthorizationUrlAsync(
            platform,
            callbackUri,
            ticket,
            friendlyName.Trim(),
            lojaId,
            cancellationToken);

        if (!response.Success || string.IsNullOrWhiteSpace(response.AuthorizationUrl))
        {
            return MarketplaceOAuthStartResult.Fail(
                string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "Não foi possível gerar a URL de autorização."
                    : response.ErrorMessage);
        }

        return MarketplaceOAuthStartResult.Ok(response.AuthorizationUrl);
    }
}
