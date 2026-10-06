using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Telegram;
using TecFlow.Business.Integrations.WhatsApp;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Infrastructure.Services;

namespace TecFlow.Infrastructure.Services.WhatsApp;

public static class WhatsAppEvolutionRegistrationExtensions
{
    public static IServiceCollection AddTecFlowWhatsAppEvolution(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EvolutionApiOptions>(configuration.GetSection(EvolutionApiOptions.SectionName));
        services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));
        services.PostConfigure<TelegramBotOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.WebhookBaseUrl))
            {
                options.WebhookBaseUrl = configuration["TecFlow:ShortLinks:PublicBaseUrl"] ?? "http://localhost:5001";
            }
        });
        services.AddScoped<IWhatsAppSessionService, WhatsAppSessionService>();
        services.AddScoped<IWhatsAppMessageProcessor, WhatsAppMessageProcessor>();
        services.AddSingleton<IWhatsAppBroadcastJobCoordinator, WhatsAppBroadcastJobCoordinator>();
        services.AddScoped<IWhatsAppBroadcastService, WhatsAppBroadcastService>();
        services.AddScoped<ITelegramApiService, TelegramApiService>();
        services.AddScoped<ITelegramMessageProcessor, TelegramMessageProcessor>();
        services.AddScoped<ITelegramIntegrationService, TelegramIntegrationService>();
        services.AddScoped<ITelegramBroadcastService, TelegramBroadcastService>();
        services.AddScoped<IGroupOfferCaptureService, TecFlow.Infrastructure.Services.Groups.GroupOfferCaptureService>();
        services.AddScoped<IOfferProductMediaStore, TecFlow.Infrastructure.Services.Groups.OfferProductMediaStore>();
        services.AddScoped<IProductImageCleanupService, TecFlow.Infrastructure.Services.Groups.ProductImageCleanupService>();
        services.AddSingleton<TecFlow.Infrastructure.Services.Groups.ProductImageCleanupHost>();
        services.AddScoped<IGroupCapturedMessagesService, TecFlow.Infrastructure.Services.Groups.GroupCapturedMessagesService>();
        services.AddScoped<IMonitoredGroupService, TecFlow.Infrastructure.Services.Groups.MonitoredGroupService>();
        services.AddScoped<IAffiliateMiningProfileService, TecFlow.Infrastructure.Services.Radar.AffiliateMiningProfileService>();
        services.AddScoped<IProductArbitrageService, TecFlow.Infrastructure.Services.Radar.ProductArbitrageService>();
        services.AddScoped<IOfferRadarService, TecFlow.Infrastructure.Services.Radar.OfferRadarService>();
        services.AddScoped<IOfferMiningEngine, TecFlow.Infrastructure.Services.Radar.OfferMiningEngine>();
        services.AddSingleton<TecFlow.Infrastructure.Services.Radar.OfferMiningHost>();
        services.AddScoped<IOfferHealthService, TecFlow.Infrastructure.Services.Radar.OfferHealthService>();
        services.AddScoped<IGroupAttributionService, TecFlow.Infrastructure.Services.Radar.GroupAttributionService>();
        services.AddScoped<IEvergreenLibraryService, TecFlow.Infrastructure.Services.Radar.EvergreenLibraryService>();
        services.AddScoped<IOfferMediaStudio, TecFlow.Infrastructure.Services.Radar.OfferMediaStudio>();
        services.AddSingleton<IOfferIntelligenceEngine, TecFlow.Infrastructure.Services.Radar.OfferIntelligenceEngine>();
        services.AddSingleton<TecFlow.Infrastructure.Services.Radar.OfferIntelligenceHost>();
        services.AddScoped<IPreFlightService, TecFlow.Infrastructure.Services.Radar.PreFlightService>();
        services.AddSingleton<IPreFlightEngine, TecFlow.Infrastructure.Services.Radar.PreFlightEngine>();
        services.AddSingleton<TecFlow.Infrastructure.Services.Radar.PreFlightHost>();
        services.AddSingleton<TecFlow.Infrastructure.Services.Telegram.TelegramUserBotCodeStore>();
        services.AddSingleton<TecFlow.Infrastructure.Services.Telegram.TelegramUserBotSessionStore>();
        services.AddSingleton<IUserBotSyncStatusService, TecFlow.Infrastructure.Services.Telegram.UserBotSyncStatusService>();
        services.AddSingleton<TecFlow.Infrastructure.Services.Telegram.TelegramUserMonitorHost>();
        services.AddHttpClient<IOfferValidationService, TecFlow.Infrastructure.Services.Groups.OfferValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TecFlowOfferValidation/1.0");
        });
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
