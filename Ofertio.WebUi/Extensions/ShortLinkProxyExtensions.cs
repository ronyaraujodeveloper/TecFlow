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

    /// <summary>Botão "entrar no grupo" da página pública (só destino cadastrado, com contagem).</summary>
    public const string JoinRedirectPattern = "/j/{id:int}";

    /// <summary>/w, /t, /e, /p, /i, /o + loja + código: mesma tabela de letras de <c>LinkChannelRules.All</c>.</summary>
    public const string ChannelRedirectPattern = "/{channel:regex(^[wtepio]$)}/{storeSlug:regex(^[A-Za-z0-9]{{1,80}}$)}/{code:length(6,8)}";

    public static IEndpointRouteBuilder MapShortLinkProxy(this IEndpointRouteBuilder endpoints)
    {
        var apiBaseUrl = endpoints.ServiceProvider
            .GetRequiredService<IOptions<OrquestradorApiOptions>>()
            .Value.BaseUrl.TrimEnd('/');

        endpoints.MapForwarder(LegacyRedirectPattern, apiBaseUrl).AllowAnonymous();
        endpoints.MapForwarder(StoreRedirectPattern, apiBaseUrl).AllowAnonymous();
        endpoints.MapForwarder(ChannelRedirectPattern, apiBaseUrl).AllowAnonymous();
        endpoints.MapForwarder(JoinRedirectPattern, apiBaseUrl).AllowAnonymous();

        return endpoints;
    }
}
