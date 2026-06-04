using TecFlow.SharedUi.Extensions;
using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.Http;
using TecFlow.WebUi.Services.Auth;
using TecFlow.WebUi.Services.Http;

namespace TecFlow.WebUi.Extensions;

public static class WebUiServiceCollectionExtensions
{
    public static IServiceCollection AddWebUiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTecFlowClientServices(configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<IAccessTokenProvider, WebAccessTokenProvider>();
        services.AddScoped<IAuthCookieService, AuthCookieService>();

        return services;
    }
}
