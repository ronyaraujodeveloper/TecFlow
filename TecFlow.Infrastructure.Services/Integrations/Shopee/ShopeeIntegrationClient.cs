using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Integrations.Shopee;

namespace TecFlow.Infrastructure.Services.Integrations.Shopee;

public class ShopeeIntegrationClient : IShopeeIntegrationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopeeIntegrationClient> _logger;

    public ShopeeIntegrationClient(
        HttpClient httpClient,
        IOptions<ShopeeIntegrationOptions> options,
        ILogger<ShopeeIntegrationClient> logger)
    {
        _httpClient = httpClient;
        Options = options.Value;
        _logger = logger;
    }

    public string PlatformName => "Shopee";

    public ShopeeIntegrationOptions Options { get; }

    public Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        return _httpClient.GetAsync(NormalizePath(relativePath), cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsync(
        string relativePath,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        return _httpClient.PostAsync(NormalizePath(relativePath), content, cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(Options.PartnerId) || string.IsNullOrWhiteSpace(Options.PartnerKey))
        {
            _logger.LogWarning(
                "Shopee: PartnerId/PartnerKey não configurados em {Section}.",
                ShopeeIntegrationOptions.SectionName);
        }
    }

    private static string NormalizePath(string relativePath) =>
        relativePath.TrimStart('/');
}
