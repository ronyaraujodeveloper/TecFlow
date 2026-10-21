using Microsoft.Extensions.Options;
using Ofertio.SharedUi.Configuration;

namespace Ofertio.WebUi.Extensions;

/// <summary>
/// Reverse proxy (YARP) do encurtador Ofertio: o link de rastreio usa o host da WebUi ({base}/{storeSlug}/{code})
/// e o redirect com telemetria fica na API (ShortLinkRedirectController), que só escuta internamente.
/// As páginas do Blazor começam com segmento literal e têm precedência sobre estas rotas com parâmetros.
/// </summary>
public static class ShortLinkProxyExtensions
{
    public const string LegacyRedirectPattern = "/r/{code:length(6,8)}";
    public const string StoreRedirectPattern = "/{storeSlug:regex(^[A-Za-z0-9]{{1,80}}$)}/{code:length(6,8)}";

    public static IEndpointRouteBuilder MapShortLinkProxy(this IEndpointRouteBuilder endpoints)
    {
        var apiBaseUrl = endpoints.ServiceProvider
            .GetRequiredService<IOptions<OrquestradorApiOptions>>()
            .Value.BaseUrl.TrimEnd('/');

        endpoints.MapForwarder(LegacyRedirectPattern, apiBaseUrl).AllowAnonymous();
        endpoints.MapForwarder(StoreRedirectPattern, apiBaseUrl).AllowAnonymous();

        return endpoints;
    }
}
