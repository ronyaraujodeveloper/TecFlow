using Microsoft.AspNetCore.Authorization;

namespace Ofertio.API.Security;

public static class OfertioAuthorizationExtensions
{
    /// <summary>
    /// Política padrão da API: todo endpoint exige usuário autenticado, a menos que declare
    /// [AllowAnonymous] (login, cadastro, webhooks, redirects e páginas públicas).
    /// Esquecer o [Authorize] num controller novo deixa de expor dados.
    /// </summary>
    public static IServiceCollection AddOfertioAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services.AddOfertioMasterAuthorization();
    }
}
