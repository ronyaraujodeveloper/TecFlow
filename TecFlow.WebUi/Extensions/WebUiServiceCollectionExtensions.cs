using TecFlow.SharedUi.Extensions;
using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.Integrations;
using TecFlow.SharedUi.Services.State;
using TecFlow.WebUi.Services.Auth;
using TecFlow.WebUi.Services.Http;
using TecFlow.WebUi.Services.Integrations;
using TecFlow.WebUi.Services.State;

namespace TecFlow.WebUi.Extensions;

public static class WebUiServiceCollectionExtensions
{
    public static IServiceCollection AddWebUiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTecFlowClientServices(configuration);
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
