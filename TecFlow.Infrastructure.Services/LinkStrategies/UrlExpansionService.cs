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
            using var response = await SendWithoutAutoRedirectAsync(client, currentUrl, cancellationToken);
            if (!IsRedirect(response))
            {
                return currentUrl;
            }

            var nextUrl = ResolveRedirectLocation(currentUrl, response);
            if (string.IsNullOrWhiteSpace(nextUrl) || string.Equals(nextUrl, currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            _logger.LogDebug("ExpandUrl hop {Hop}: {From} -> {To}", hop + 1, currentUrl, nextUrl);
            currentUrl = nextUrl;
        }

        return currentUrl;
    }

    private static async Task<HttpResponseMessage> SendWithoutAutoRedirectAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
        EnsureBrowserUserAgent(getRequest);
        var getResponse = await client.SendAsync(
            getRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (IsRedirect(getResponse)
            || (getResponse.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed
                && getResponse.StatusCode != System.Net.HttpStatusCode.NotImplemented))
        {
            return getResponse;
        }

        getResponse.Dispose();
        using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
        EnsureBrowserUserAgent(headRequest);
        return await client.SendAsync(
            headRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    private static void EnsureBrowserUserAgent(HttpRequestMessage request)
    {
        if (request.Headers.UserAgent.Count > 0)
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 TecFlow/1.0");
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
