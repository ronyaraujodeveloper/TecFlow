using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>Cliente ASAAS v3: cliente + assinatura recorrente com cobrança UNDEFINED (Pix, Boleto ou Cartão à escolha do pagador).</summary>
public sealed class AsaasPaymentGateway : ISaasPaymentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AsaasPaymentGateway> _logger;

    public AsaasPaymentGateway(HttpClient httpClient, IConfiguration configuration, ILogger<AsaasPaymentGateway> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymentGatewaySubscriptionResult> CreateSubscriptionAsync(
        string tenantName,
        string ownerEmail,
        string? existingCustomerId,
        decimal value,
        string billingCycle,
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration[SystemSettingCatalog.AsaasApiKey];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new PaymentGatewaySubscriptionResult(false,
                "Configure a API Key do ASAAS em Painel Master › Configurações › Pagamentos.", null, null);
        }

        if (value <= 0)
        {
            return new PaymentGatewaySubscriptionResult(false, "O plano do inquilino não tem preço definido.", null, null);
        }

        var baseUrl = (_configuration[SystemSettingCatalog.AsaasBaseUrl] is { Length: > 0 } configured
            ? configured
            : SystemSettingCatalog.AsaasDefaultBaseUrl).TrimEnd('/');

        try
        {
            var customerId = existingCustomerId;
            if (string.IsNullOrWhiteSpace(customerId))
            {
                var customer = await PostAsync<AsaasIdResponse>(
                    baseUrl + "/customers",
                    apiKey,
                    new { name = tenantName, email = ownerEmail, externalReference },
                    cancellationToken);
                customerId = customer.Body?.Id;
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return new PaymentGatewaySubscriptionResult(false, "ASAAS recusou o cliente: " + customer.Error, null, null);
                }
            }

            var cycle = string.Equals(billingCycle, SaasMetricsRules.CycleAnnual, StringComparison.OrdinalIgnoreCase) ? "YEARLY" : "MONTHLY";
            var subscription = await PostAsync<AsaasIdResponse>(
                baseUrl + "/subscriptions",
                apiKey,
                new
                {
                    customer = customerId,
                    billingType = "UNDEFINED",
                    value,
                    nextDueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                    cycle,
                    description = $"Assinatura TecFlow — {tenantName}",
                    externalReference
                },
                cancellationToken);
            if (string.IsNullOrWhiteSpace(subscription.Body?.Id))
            {
                return new PaymentGatewaySubscriptionResult(false, "ASAAS recusou a assinatura: " + subscription.Error, customerId, null);
            }

            return new PaymentGatewaySubscriptionResult(true, "Assinatura recorrente criada no ASAAS.", customerId, subscription.Body.Id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Falha ao criar assinatura ASAAS para {Reference}.", externalReference);
            return new PaymentGatewaySubscriptionResult(false, "Não foi possível falar com o ASAAS: " + ex.Message, null, null);
        }
    }

    private async Task<(T? Body, string? Error)> PostAsync<T>(string url, string apiKey, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation("access_token", apiKey);
        request.Headers.UserAgent.ParseAdd("TecFlow/1.0");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (default, $"HTTP {(int)response.StatusCode} {Shorten(raw)}");
        }

        return (JsonSerializer.Deserialize<T>(raw, JsonOptions), null);
    }

    private static string Shorten(string raw) => raw.Length <= 300 ? raw : raw[..300];

    private sealed class AsaasIdResponse
    {
        public string? Id { get; set; }
    }
}
