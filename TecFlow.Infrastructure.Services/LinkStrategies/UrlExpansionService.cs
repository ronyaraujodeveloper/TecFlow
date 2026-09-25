using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations;
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

        var originalUrl = currentUri.ToString();
        var currentUrl = originalUrl;

        if (ShouldPreferAutoRedirect(currentUrl))
        {
            currentUrl = SanitizeExpandedUrl(
                await FollowWithAutoRedirectAsync(currentUrl, cancellationToken),
                originalUrl);
            if (!AffiliateTrackingIdValidator.IsShortenerUrl(currentUrl)
                && !string.Equals(currentUrl, originalUrl, StringComparison.OrdinalIgnoreCase))
            {
                return AffiliateTrackingIdValidator.UnwrapTikTokLoginRedirect(currentUrl);
            }
        }

        var client = _httpClientFactory.CreateClient(TecFlow.Business.Integrations.Common.IntegrationHttpClientNames.UrlExpansion);

        for (var hop = 0; hop < MaxRedirects; hop++)
        {
            using var response = await SendWithoutAutoRedirectAsync(client, currentUrl, cancellationToken);
            var requestUri = SanitizeExpandedUrl(response.RequestMessage?.RequestUri?.ToString(), currentUrl);
            if (!string.Equals(requestUri, currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                currentUrl = requestUri;
            }

            if (!IsRedirect(response))
            {
                break;
            }

            var nextUrl = SanitizeExpandedUrl(ResolveRedirectLocation(currentUrl, response), currentUrl);
            if (string.Equals(nextUrl, currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            _logger.LogDebug("ExpandUrl hop {Hop}: {From} -> {To}", hop + 1, currentUrl, nextUrl);
            currentUrl = nextUrl;
        }

        if (AffiliateTrackingIdValidator.IsShortenerUrl(currentUrl))
        {
            currentUrl = SanitizeExpandedUrl(
                await FollowWithAutoRedirectAsync(currentUrl, cancellationToken),
                currentUrl);
        }

        return AffiliateTrackingIdValidator.UnwrapTikTokLoginRedirect(
            SanitizeExpandedUrl(currentUrl, originalUrl));
    }

    private async Task<string> FollowWithAutoRedirectAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var followClient = _httpClientFactory.CreateClient(
                TecFlow.Business.Integrations.Common.IntegrationHttpClientNames.UrlExpansionFollow);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            EnsureBrowserUserAgent(request);
            using var response = await followClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            var finalUrl = SanitizeExpandedUrl(response.RequestMessage?.RequestUri?.ToString(), url);
            if (!string.Equals(finalUrl, url, StringComparison.OrdinalIgnoreCase)
                || Uri.TryCreate(finalUrl, UriKind.Absolute, out _))
            {
                _logger.LogDebug("ExpandUrl auto-redirect: {From} -> {To}", url, finalUrl);
                return AffiliateTrackingIdValidator.UnwrapTikTokLoginRedirect(finalUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao seguir redirecionamento automático de {Url}.", url);
        }

        return url;
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
        request.Headers.Remove("User-Agent");
        request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);

        request.Headers.Remove("Accept");
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        request.Headers.Remove("Accept-Language");
        request.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
        request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        request.Headers.TryAddWithoutValidation("Referer", "https://www.tiktok.com/");
    }

    private const string BrowserUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

    private static bool ShouldPreferAutoRedirect(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return host.Contains("onelink.me", StringComparison.Ordinal)
            || host.Contains("meli.la", StringComparison.Ordinal)
            || host.Contains("vt.tiktok.com", StringComparison.Ordinal)
            || host.Contains("vm.tiktok.com", StringComparison.Ordinal)
            || host.Contains("magalu.me", StringComparison.Ordinal);
    }

    private static string SanitizeExpandedUrl(string? candidate, string fallback)
    {
        if (string.IsNullOrWhiteSpace(candidate)
            || AffiliateTrackingIdValidator.IsBooleanLiteral(candidate)
            || !Uri.TryCreate(candidate.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return fallback;
        }

        return uri.ToString();
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
