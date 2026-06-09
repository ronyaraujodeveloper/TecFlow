using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;

namespace TecFlow.Infrastructure.Services.Security;

public static class IdentityRegistrationExtensions
{
    public static IServiceCollection AddTecFlowIdentity(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<UserAccount>, TecFlowPasswordHasher>();
        services.AddScoped<TecFlowUserStore>();
        services.AddScoped<IUserStore<UserAccount>>(provider => provider.GetRequiredService<TecFlowUserStore>());
        services.AddScoped<IUserPasswordStore<UserAccount>>(provider => provider.GetRequiredService<TecFlowUserStore>());
        services.AddScoped<IUserEmailStore<UserAccount>>(provider => provider.GetRequiredService<TecFlowUserStore>());
        services.AddScoped<IUserLoginStore<UserAccount>>(provider => provider.GetRequiredService<TecFlowUserStore>());

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
