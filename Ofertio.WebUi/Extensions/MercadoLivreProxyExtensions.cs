using Microsoft.Extensions.Options;
using Ofertio.SharedUi.Configuration;

namespace Ofertio.WebUi.Extensions;

/// <summary>
/// Reverse proxy (YARP) das rotas cadastradas no app do Mercado Livre: o host público é o da WebUi
/// e a API só escuta internamente (OrquestradorApi:BaseUrl, ex.: http://localhost:5001).
/// </summary>
public static class MercadoLivreProxyExtensions
{
    public const string CallbackPath = "/api/mercadolivre/callback";
    public const string NotificationsPath = "/api/mercadolivre/notifications";

    public static IServiceCollection AddMercadoLivreProxy(this IServiceCollection services) =>
        services.AddHttpForwarder();

    public static IEndpointRouteBuilder MapMercadoLivreProxy(this IEndpointRouteBuilder endpoints)
    {
        var apiBaseUrl = endpoints.ServiceProvider
            .GetRequiredService<IOptions<OrquestradorApiOptions>>()
            .Value.BaseUrl.TrimEnd('/');

        endpoints.MapForwarder(CallbackPath, apiBaseUrl).AllowAnonymous();
        endpoints.MapForwarder(NotificationsPath, apiBaseUrl).AllowAnonymous();

        return endpoints;
    }
}
