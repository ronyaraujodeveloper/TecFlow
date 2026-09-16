using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    public bool IsSandboxMode => Options.IsSandboxMode;

    public Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (TryCreateSandboxResponse(HttpMethod.Get, relativePath, out var sandbox))
        {
            return Task.FromResult(sandbox);
        }

        return _httpClient.GetAsync(NormalizePath(relativePath), cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsync(
        string relativePath,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        if (TryCreateSandboxResponse(HttpMethod.Post, relativePath, out var sandbox))
        {
            return Task.FromResult(sandbox);
        }

        return _httpClient.PostAsync(NormalizePath(relativePath), content, cancellationToken);
    }

    public string BuildSandboxTrackedUrl(
        string productUrl,
        string? subId = null,
        string? universalLink = null,
        string? deepLink = null) =>
        ShopeeCommissionUrlBuilder.Merge(
            productUrl,
            Options.SandboxTrackingCode,
            subId,
            universalLink,
            deepLink);

    private bool TryCreateSandboxResponse(HttpMethod method, string relativePath, out HttpResponseMessage response)
    {
        if (!IsSandboxMode)
        {
            response = null!;
            return false;
        }

        _logger.LogInformation(
            "Shopee sandbox: credenciais vazias em {Section}. Ignorando {Method} {Path} e usando tracking {TrackingCode}.",
            ShopeeIntegrationOptions.SectionName,
            method,
            relativePath,
            Options.SandboxTrackingCode);

        response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $"{{\"sandbox\":true,\"tracking_code\":\"{Options.SandboxTrackingCode}\"}}",
                Encoding.UTF8,
                "application/json")
        };
        return true;
    }

    private static string NormalizePath(string relativePath) =>
        relativePath.TrimStart('/');
}
