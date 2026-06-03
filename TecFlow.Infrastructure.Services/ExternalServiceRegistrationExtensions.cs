// Arquivo: TecFlow.Infrastructure.ExternalServices\ServiceRegistrationExtensions.cs
// ... usings como Microsoft.Extensions.DependencyInjection ...
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Services.ExternalServices;
using TecFlow.Infrastructure.Services.Service.ExternalServices;

namespace TecFlow.Infrastructure.Services
{
    public static class ExternalServiceRegistrationExtensions
    {
        public static IServiceCollection AddTecFlowExternalServices(this IServiceCollection services)
        {
            services.AddTecFlowExternalApiClient<IShopeeApi, ShopeeApiService>();
            services.AddTecFlowExternalApiClient<ITikTokShopApi, TikTokShopApiService>();
            services.AddTecFlowExternalApiClient<ITikTokAdsApiService, TikTokAdsApiService>();
            services.AddTecFlowExternalApiClient<IGeminiService, GeminiService>();
            services.AddTecFlowExternalApiClient<IAIService, OpenAIService>();

            return services;
        }

        private static IServiceCollection AddTecFlowExternalApiClient<TClient, TImplementation>(this IServiceCollection services)
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