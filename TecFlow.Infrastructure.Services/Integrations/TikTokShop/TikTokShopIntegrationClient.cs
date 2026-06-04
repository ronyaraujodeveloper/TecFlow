using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Integrations.TikTokShop;

namespace TecFlow.Infrastructure.Services.Integrations.TikTokShop;

public class TikTokShopIntegrationClient : ITikTokShopIntegrationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TikTokShopIntegrationClient> _logger;

    public TikTokShopIntegrationClient(
        HttpClient httpClient,
        IOptions<TikTokShopIntegrationOptions> options,
        ILogger<TikTokShopIntegrationClient> logger)
    {
        _httpClient = httpClient;
        Options = options.Value;
        _logger = logger;
    }

    public string PlatformName => "TikTokShop";

    public TikTokShopIntegrationOptions Options { get; }

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
        if (string.IsNullOrWhiteSpace(Options.AppKey) || string.IsNullOrWhiteSpace(Options.AppSecret))
        {
            _logger.LogWarning(
                "TikTok Shop: AppKey/AppSecret não configurados em {Section}.",
                TikTokShopIntegrationOptions.SectionName);
        }
    }

    private static string NormalizePath(string relativePath) =>
        relativePath.TrimStart('/');
}
