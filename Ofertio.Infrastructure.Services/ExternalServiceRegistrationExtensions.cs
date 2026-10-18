// Arquivo: Ofertio.Infrastructure.ExternalServices\ServiceRegistrationExtensions.cs
// ... usings como Microsoft.Extensions.DependencyInjection ...
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Infrastructure.Services.Advertising;
using Ofertio.Infrastructure.Services.Analytics;
using Ofertio.Infrastructure.Services.Health;
using Ofertio.Infrastructure.Services.ExternalServices;
using Ofertio.Infrastructure.Services.Service.ExternalServices;

namespace Ofertio.Infrastructure.Services
{
    public static class ExternalServiceRegistrationExtensions
    {
        public static IServiceCollection AddOfertioExternalServices(this IServiceCollection services)
        {
            services.AddOfertioExternalApiClient<IShopeeApi, ShopeeApiService>();
            services.AddOfertioExternalApiClient<ITikTokShopApi, TikTokShopApiService>();
            services.AddOfertioExternalApiClient<ITikTokAdsApiService, TikTokAdsApiService>();
            services.AddOfertioExternalApiClient<IGeminiService, GeminiService>();
            services.AddOfertioExternalApiClient<IAIService, OpenAIService>();
            services.AddScoped<IAffiliateAnalyticsService, AffiliateAnalyticsService>();
            services.AddScoped<IAdvertisingProductService, AdvertisingProductService>();
            services.AddScoped<IPlatformHealthService, PlatformHealthService>();

            return services;
        }

        private static IServiceCollection AddOfertioExternalApiClient<TClient, TImplementation>(this IServiceCollection services)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddHttpClient<TClient, TImplementation>()
                .ConfigureHttpClient(httpClient =>
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.Timeout = TimeSpan.FromSeconds(100);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}