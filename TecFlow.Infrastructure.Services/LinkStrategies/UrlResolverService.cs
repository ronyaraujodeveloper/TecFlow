using System.Net;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

public sealed class UrlResolverService : IUrlResolverService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUrlExpansionService _expansion;
    private readonly ILogger<UrlResolverService> _logger;

    public UrlResolverService(
        IHttpClientFactory httpClientFactory,
        IUrlExpansionService expansion,
        ILogger<UrlResolverService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _expansion = expansion;
        _logger = logger;
    }

    public async Task<UrlResolverResultDto> ResolveCanonicalAsync(
        string capturedUrl,
        CancellationToken cancellationToken = default)
    {
        var working = (capturedUrl ?? string.Empty).Trim();
        if (!Uri.TryCreate(working, UriKind.Absolute, out var start)
            || (start.Scheme != Uri.UriSchemeHttp && start.Scheme != Uri.UriSchemeHttps))
        {
            return new UrlResolverResultDto { CanonicalUrl = working };
        }

        working = start.ToString();
        try
        {
            var client = _httpClientFactory.CreateClient(IntegrationHttpClientNames.UrlResolver);
            working = await FollowAsync(client, HttpMethod.Head, working, cancellationToken);
            if (!UrlUnshortenerService.IsSupportedMarketplaceUrl(working))
            {
                working = await FollowAsync(client, HttpMethod.Get, working, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HEAD/GET do desencurtador falhou. Url={Url}", capturedUrl);
        }

        if (!UrlUnshortenerService.IsSupportedMarketplaceUrl(working))
        {
            try
            {
                working = await _expansion.ExpandUrlAsync(working, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Fallback de expansão após HEAD/GET. Url={Url}", working);
            }
        }

        if (UrlUnshortenerService.IsSupportedMarketplaceUrl(working))
        {
            working = UrlUnshortenerService.StripForeignTracking(working);
        }

        UrlUnshortenerService.TryDetectMarketplace(working, out var platform);
        return new UrlResolverResultDto
        {
            CanonicalUrl = working,
            Platform = UrlUnshortenerService.IsSupportedMarketplaceUrl(working) ? platform : null,
            IsMarketplace = UrlUnshortenerService.IsSupportedMarketplaceUrl(working)
        };
    }

    private static async Task<string> FollowAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var finalUrl = response.RequestMessage?.RequestUri?.ToString();
        if (string.IsNullOrWhiteSpace(finalUrl)
            || !Uri.TryCreate(finalUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return url;
        }

        if (response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented)
        {
            return url;
        }

        return uri.ToString();
    }
}
