using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

/// <summary>Expansão cíclica de URLs encurtadas via cabeçalho Location (301/302).</summary>
public sealed class UrlExpansionService : IUrlExpansionService
{
    private const int MaxRedirects = 10;

    private static readonly int[] RedirectStatusCodes = [301, 302, 303, 307, 308];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UrlExpansionService> _logger;

    public UrlExpansionService(IHttpClientFactory httpClientFactory, ILogger<UrlExpansionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shortenedUrl))
        {
            throw new AffiliateLinkGenerationException("URL encurtada não informada.");
        }

        if (!Uri.TryCreate(shortenedUrl.Trim(), UriKind.Absolute, out var currentUri))
        {
            throw new AffiliateLinkGenerationException("URL informada é inválida.");
        }

        var client = _httpClientFactory.CreateClient(TecFlow.Business.Integrations.Common.IntegrationHttpClientNames.UrlExpansion);
        var currentUrl = currentUri.ToString();

        for (var hop = 0; hop < MaxRedirects; hop++)
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, currentUrl);
            using var headResponse = await client.SendAsync(
                headRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (IsRedirect(headResponse))
            {
                var nextUrl = ResolveRedirectLocation(currentUrl, headResponse);
                if (string.IsNullOrWhiteSpace(nextUrl))
                {
                    break;
                }

                _logger.LogDebug("ExpandUrl hop {Hop}: {From} -> {To}", hop + 1, currentUrl, nextUrl);
                currentUrl = nextUrl;
                continue;
            }

            if (headResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed
                || headResponse.StatusCode == System.Net.HttpStatusCode.NotImplemented)
            {
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, currentUrl);
                using var getResponse = await client.SendAsync(
                    getRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (IsRedirect(getResponse))
                {
                    var nextUrl = ResolveRedirectLocation(currentUrl, getResponse);
                    if (string.IsNullOrWhiteSpace(nextUrl))
                    {
                        break;
                    }

                    currentUrl = nextUrl;
                    continue;
                }
            }

            return currentUrl;
        }

        return currentUrl;
    }

    private static bool IsRedirect(HttpResponseMessage response) =>
        RedirectStatusCodes.Contains((int)response.StatusCode);

    private static string? ResolveRedirectLocation(string currentUrl, HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        if (location is null)
        {
            return null;
        }

        if (location.IsAbsoluteUri)
        {
            return location.ToString();
        }

        return Uri.TryCreate(new Uri(currentUrl), location, out var resolved)
            ? resolved.ToString()
            : null;
    }
}
