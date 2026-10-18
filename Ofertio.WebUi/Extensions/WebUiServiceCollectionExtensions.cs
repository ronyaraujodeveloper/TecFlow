using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ofertio.SharedUi.Extensions;
using Ofertio.SharedUi.Services.Auth;
using Ofertio.SharedUi.Services.Http;
using Ofertio.SharedUi.Services.Integrations;
using Ofertio.SharedUi.Services.State;
using Ofertio.WebUi.Services.Auth;
using Ofertio.WebUi.Services.Http;
using Ofertio.WebUi.Services.Integrations;
using Ofertio.WebUi.Services.State;

namespace Ofertio.WebUi.Extensions;

public static class WebUiServiceCollectionExtensions
{
    public static IServiceCollection AddWebUiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOfertioClientServices(configuration, environment);
        services.AddMemoryCache();
        services.AddSingleton<IAuthSignInTicketStore, AuthSignInTicketStore>();
        services.AddSingleton<IIntegracaoLojaPendingLinkStore, IntegracaoLojaPendingLinkStore>();
        services.AddHttpContextAccessor();
        services.AddScoped<IAccessTokenProvider, WebAccessTokenProvider>();
        services.AddScoped<IAuthCookieService, AuthCookieService>();
        services.AddScoped<IActiveStoreScopeService, ActiveStoreScopeService>();

        return services;
    }
}
