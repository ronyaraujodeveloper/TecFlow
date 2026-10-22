using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ofertio.Business.Integrations.Telegram;
using Ofertio.Business.Integrations.WhatsApp;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.WhatsApp;
using Ofertio.Infrastructure.Services;
using Ofertio.Infrastructure.Services.Groups;

namespace Ofertio.Infrastructure.Services.WhatsApp;

public static class WhatsAppEvolutionRegistrationExtensions
{
    public static IServiceCollection AddOfertioWhatsAppEvolution(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EvolutionApiOptions>(configuration.GetSection(EvolutionApiOptions.SectionName));
        services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));
        services.PostConfigure<TelegramBotOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.WebhookBaseUrl))
            {
                options.WebhookBaseUrl = configuration["Ofertio:ShortLinks:PublicBaseUrl"] ?? "http://localhost:5001";
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
        services.AddScoped<IOfferPipelineProcessor, Ofertio.Infrastructure.Services.Groups.OfferPipelineProcessor>();
        services.AddScoped<IGroupOfferCaptureService, Ofertio.Infrastructure.Services.Groups.GroupOfferCaptureService>();
        services.AddSingleton<IStructuredOfferParserService, Ofertio.Business.Service.Groups.StructuredOfferParserService>();
        services.AddOfertioUploadStorage();
        services.AddScoped<IImageOptimizationService, Ofertio.Infrastructure.Services.Groups.ImageOptimizationService>();
        services.AddScoped<IOfferProductMediaStore, Ofertio.Infrastructure.Services.Groups.OfferProductMediaStore>();
        services.AddScoped<IProductImageUrlLinkBackfillService, Ofertio.Infrastructure.Services.Groups.BackfillMediaPathsService>();
        services.AddScoped<IProductImageCleanupService, Ofertio.Infrastructure.Services.Groups.ProductImageCleanupService>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Groups.ProductImageCleanupHost>();
        services.Configure<Ofertio.Business.Service.Groups.OfferRevalidationOptions>(
            configuration.GetSection(Ofertio.Business.Service.Groups.OfferRevalidationOptions.SectionName));
        services.AddScoped<IOfferRevalidationService, Ofertio.Infrastructure.Services.Groups.OfferRevalidationService>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Groups.OfferRevalidationHost>();
        services.AddScoped<ICouponReclassificationService, Ofertio.Infrastructure.Services.Groups.CouponReclassificationService>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Groups.CouponReclassificationHost>();
        services.AddScoped<IProductTagBackfillService, Ofertio.Infrastructure.Services.Groups.ProductTagBackfillService>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Groups.ProductTagBackfillHost>();
        services.AddScoped<IGroupCapturedMessagesService, Ofertio.Infrastructure.Services.Groups.GroupCapturedMessagesService>();
        services.AddScoped<IAffiliateUrlBuilderService, Ofertio.Infrastructure.Services.LinkStrategies.AffiliateUrlBuilderService>();
        services.AddScoped<IMonitoredGroupService, Ofertio.Infrastructure.Services.Groups.MonitoredGroupService>();
        services.AddScoped<IAffiliateMiningProfileService, Ofertio.Infrastructure.Services.Radar.AffiliateMiningProfileService>();
        services.AddScoped<IProductArbitrageService, Ofertio.Infrastructure.Services.Radar.ProductArbitrageService>();
        services.AddScoped<IOfferRadarService, Ofertio.Infrastructure.Services.Radar.OfferRadarService>();
        services.AddScoped<IOfferMiningEngine, Ofertio.Infrastructure.Services.Radar.OfferMiningEngine>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Radar.OfferMiningHost>();
        services.AddScoped<IOfferHealthService, Ofertio.Infrastructure.Services.Radar.OfferHealthService>();
        services.AddScoped<IGroupAttributionService, Ofertio.Infrastructure.Services.Radar.GroupAttributionService>();
        services.AddScoped<IEvergreenLibraryService, Ofertio.Infrastructure.Services.Radar.EvergreenLibraryService>();
        services.AddScoped<IOfferMediaStudio, Ofertio.Infrastructure.Services.Radar.OfferMediaStudio>();
        services.AddSingleton<IOfferIntelligenceEngine, Ofertio.Infrastructure.Services.Radar.OfferIntelligenceEngine>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Radar.OfferIntelligenceHost>();
        services.AddScoped<IPreFlightService, Ofertio.Infrastructure.Services.Radar.PreFlightService>();
        services.AddSingleton<IPreFlightEngine, Ofertio.Infrastructure.Services.Radar.PreFlightEngine>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Radar.PreFlightHost>();
        services.AddScoped<IPriceHistoryTracker, Ofertio.Infrastructure.Services.Radar.PriceHistoryTracker>();
        services.AddScoped<IDealCreditsService, Ofertio.Infrastructure.Services.Radar.DealCreditsService>();
        services.AddScoped<IGlobalTrendingDealsEngine, Ofertio.Infrastructure.Services.Radar.GlobalTrendingDealsEngine>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Radar.GlobalTrendingDealsHost>();
        services.Configure<Ofertio.Business.Integrations.Amazon.AmazonPaApiOptions>(
            configuration.GetSection(Ofertio.Business.Integrations.Amazon.AmazonPaApiOptions.SectionName));
        services.Configure<Ofertio.Business.Integrations.MercadoLivre.MercadoLivreIntegrationOptions>(
            configuration.GetSection(Ofertio.Business.Integrations.MercadoLivre.MercadoLivreIntegrationOptions.SectionName));
        services.AddSingleton(_ => new Ofertio.Business.Integrations.MercadoLivre.MercadoLivreAntiBotGuard());
        services.AddHttpClient<IMercadoLivreApiService, Ofertio.Infrastructure.Services.Radar.MercadoLivreApiService>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.mercadolibre.com/");
            client.Timeout = sp.GetRequiredService<IOptions<Ofertio.Business.Integrations.MercadoLivre.MercadoLivreIntegrationOptions>>()
                .Value
                .ResolveHttpTimeout();
            Ofertio.Infrastructure.Services.Radar.MercadoLivreApiService.EnsurePublicUserAgent(client);
        }).ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
                | System.Net.DecompressionMethods.Deflate
                | System.Net.DecompressionMethods.Brotli
        });
        services.AddHttpClient<IShopeeAffiliateOfferService, Ofertio.Infrastructure.Services.Radar.ShopeeAffiliateOfferService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<Ofertio.Business.Integrations.Shopee.ShopeeIntegrationOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(options.AffiliateApiBaseUrl)
                ? "https://open-api.affiliate.shopee.com.br"
                : options.AffiliateApiBaseUrl.TrimEnd('/');
            client.BaseAddress = new Uri(baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(12);
        });
        services.AddHttpClient<IAmazonPaApiService, Ofertio.Infrastructure.Services.Radar.AmazonPaApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
        });
        services.AddScoped<ILiveCheckSearchService, Ofertio.Infrastructure.Services.Radar.LiveCheckSearchService>();
        services.AddScoped<IOfficialCatalogSearchService, Ofertio.Infrastructure.Services.Radar.OfficialCatalogSearchService>();
        services.AddScoped<Ofertio.Infrastructure.Services.Radar.ColdStorageArchiver>();
        services.AddScoped<IDataPurgeService, Ofertio.Infrastructure.Services.Radar.DataPurgeService>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Radar.DataPurgeHost>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Telegram.TelegramUserBotCodeStore>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Telegram.TelegramUserBotSessionStore>();
        services.AddSingleton<IUserBotSyncStatusService, Ofertio.Infrastructure.Services.Telegram.UserBotSyncStatusService>();
        services.AddSingleton<Ofertio.Infrastructure.Services.Telegram.TelegramUserMonitorHost>();
        services.AddHttpClient<IOfferValidationService, Ofertio.Infrastructure.Services.Groups.OfferValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OfertioOfferValidation/1.0");
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
