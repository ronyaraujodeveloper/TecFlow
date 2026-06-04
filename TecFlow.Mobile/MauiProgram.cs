using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TecFlow.Mobile.Services;
using TecFlow.Mobile.Services.Push;
using TecFlow.SharedUi.Extensions;
using TecFlow.SharedUi.Services.Auth;

namespace TecFlow.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("TecFlow.Mobile.appsettings.json");
        if (stream is not null)
        {
            builder.Configuration.AddJsonStream(stream);
        }

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddTecFlowClientServices(builder.Configuration);
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<IAuthCookieService, SessionAuthCookieService>();
        builder.Services.AddScoped<MobileAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<MobileAuthenticationStateProvider>());
        builder.Services.AddSingleton<MobilePushCoordinator>();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
