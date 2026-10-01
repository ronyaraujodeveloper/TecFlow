using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.WhatsApp;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Services;

namespace TecFlow.Infrastructure.Services.WhatsApp;

public static class WhatsAppEvolutionRegistrationExtensions
{
    public static IServiceCollection AddTecFlowWhatsAppEvolution(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EvolutionApiOptions>(configuration.GetSection(EvolutionApiOptions.SectionName));
        services.AddScoped<IWhatsAppSessionService, WhatsAppSessionService>();
        services.AddScoped<IWhatsAppMessageProcessor, WhatsAppMessageProcessor>();
        services.AddHttpClient<IEvolutionApiService, EvolutionApiService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<EvolutionApiOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "http://localhost:8080"
                : options.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
