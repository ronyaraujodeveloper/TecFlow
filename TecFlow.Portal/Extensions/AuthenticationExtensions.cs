using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TecFlow.Portal.Configuration;
using TecFlow.Portal.Security;

namespace TecFlow.Portal.Extensions;

public static class AuthenticationExtensions
{
    public const string AppleScheme = "Apple";

    public static IServiceCollection AddPortalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PortalAuthenticationOptions>(
            configuration.GetSection(PortalAuthenticationOptions.SectionName));

        var authSection = configuration.GetSection(PortalAuthenticationOptions.SectionName);
        var authOptions = authSection.Get<PortalAuthenticationOptions>() ?? new PortalAuthenticationOptions();

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(authOptions.CookieExpireHours);
                options.SlidingExpiration = true;
            });

        if (IsGoogleConfigured(authOptions.Google))
        {
            authenticationBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ClientId = authOptions.Google.ClientId;
                options.ClientSecret = authOptions.Google.ClientSecret;
                options.SaveTokens = true;
                options.Scope.Add("email");
                options.Scope.Add("profile");
            });
        }

        if (IsFacebookConfigured(authOptions.Facebook))
        {
            authenticationBuilder.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ClientId = authOptions.Facebook.ClientId;
                options.ClientSecret = authOptions.Facebook.ClientSecret;
                options.SaveTokens = true;
                options.Scope.Add("email");
                options.Scope.Add("public_profile");
            });
        }

        if (IsAppleConfigured(authOptions.Apple))
        {
            authenticationBuilder.AddOpenIdConnect(AppleScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = "https://appleid.apple.com";
                options.ClientId = authOptions.Apple.ClientId;
                options.ClientSecret = AppleClientSecretGenerator.Generate(authOptions.Apple);
                options.CallbackPath = "/signin-apple";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.FormPost;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("name");
            });
        }

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    private static bool IsGoogleConfigured(OAuthProviderSettings settings) =>
        settings.Enabled
        && !string.IsNullOrWhiteSpace(settings.ClientId)
        && !string.IsNullOrWhiteSpace(settings.ClientSecret);

    private static bool IsFacebookConfigured(OAuthProviderSettings settings) =>
        settings.Enabled
        && !string.IsNullOrWhiteSpace(settings.ClientId)
        && !string.IsNullOrWhiteSpace(settings.ClientSecret);

    private static bool IsAppleConfigured(AppleOAuthSettings settings) =>
        settings.Enabled
        && !string.IsNullOrWhiteSpace(settings.ClientId)
        && !string.IsNullOrWhiteSpace(settings.KeyId)
        && !string.IsNullOrWhiteSpace(settings.TeamId)
        && !string.IsNullOrWhiteSpace(settings.PrivateKey);
}
