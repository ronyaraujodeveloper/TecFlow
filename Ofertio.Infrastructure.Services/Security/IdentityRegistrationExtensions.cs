using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;

namespace Ofertio.Infrastructure.Services.Security;

public static class IdentityRegistrationExtensions
{
    public static IServiceCollection AddOfertioIdentity(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<UserAccount>, OfertioPasswordHasher>();
        services.AddScoped<OfertioUserStore>();
        services.AddScoped<IUserStore<UserAccount>>(provider => provider.GetRequiredService<OfertioUserStore>());
        services.AddScoped<IUserPasswordStore<UserAccount>>(provider => provider.GetRequiredService<OfertioUserStore>());
        services.AddScoped<IUserEmailStore<UserAccount>>(provider => provider.GetRequiredService<OfertioUserStore>());
        services.AddScoped<IUserLoginStore<UserAccount>>(provider => provider.GetRequiredService<OfertioUserStore>());

        services
            .AddIdentityCore<UserAccount>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddDefaultTokenProviders();

        services.AddHttpClient(nameof(SocialAuthTokenValidator));
        services.AddScoped<ISocialAuthTokenValidator, SocialAuthTokenValidator>();
        services.AddScoped<IPlatformAuthService, PlatformAuthService>();

        return services;
    }
}
