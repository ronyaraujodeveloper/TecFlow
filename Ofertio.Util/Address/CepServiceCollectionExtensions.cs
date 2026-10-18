using Microsoft.Extensions.DependencyInjection;

namespace Ofertio.Util.Address;

public static class CepServiceCollectionExtensions
{
    public static IServiceCollection AddOfertioCepService(this IServiceCollection services)
    {
        services.AddHttpClient<ICepService, CepService>(client =>
        {
            client.BaseAddress = new Uri("https://viacep.com.br/ws/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
