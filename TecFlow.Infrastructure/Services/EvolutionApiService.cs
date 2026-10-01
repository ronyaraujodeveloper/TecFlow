using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.WhatsApp;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services;

public sealed class EvolutionApiService : IEvolutionApiService
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiOptions _options;
    private readonly ILogger<EvolutionApiService> _logger;

    public EvolutionApiService(
        HttpClient httpClient,
        IOptions<EvolutionApiOptions> options,
        ILogger<EvolutionApiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        ApplyApiKeyHeader();
    }

    public async Task<bool> CreateInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        if (!EnsureConfigured())
        {
            return false;
        }

        var payload = JsonSerializer.Serialize(new
        {
            instanceName,
            qrcode = true,
            integration = "WHATSAPP-BAILEYS"
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("instance/create", content, cancellationToken);
        if (response.IsSuccessStatusCode
            || response.StatusCode == HttpStatusCode.Conflict
            || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return true;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Evolution create instance falhou. Status={Status} Instance={Instance} Body={Body}",
            (int)response.StatusCode,
            instanceName,
            body);
        return false;
    }

    public async Task<string?> FetchQrCodeAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        if (!EnsureConfigured())
        {
            return null;
        }

        using var response = await _httpClient.GetAsync(
            $"instance/connect/{Uri.EscapeDataString(instanceName)}",
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Evolution QR falhou. Status={Status} Instance={Instance}",
                (int)response.StatusCode,
                instanceName);
            return ExtractQr(json);
        }

        return ExtractQr(json);
    }

    public async Task<EvolutionConnectionStateDto> GetConnectionStateAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        var result = new EvolutionConnectionStateDto { State = "close" };
        if (!EnsureConfigured())
        {
            return result;
        }

        using var stateResponse = await _httpClient.GetAsync(
            $"instance/connectionState/{Uri.EscapeDataString(instanceName)}",
            cancellationToken);
        var stateJson = await stateResponse.Content.ReadAsStringAsync(cancellationToken);
        result.State = ExtractFirst(stateJson, "state", "status", "connectionStatus") ?? "close";

        using var infoResponse = await _httpClient.GetAsync(
            $"instance/fetchInstances?instanceName={Uri.EscapeDataString(instanceName)}",
            cancellationToken);
        var infoJson = await infoResponse.Content.ReadAsStringAsync(cancellationToken);
        result.PhoneNumber = ExtractFirst(infoJson, "owner", "wuid", "wid", "phone", "number");
        result.ProfileName = ExtractFirst(infoJson, "profileName", "pushName", "name");
        return result;
    }

    private void ApplyApiKeyHeader()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("apikey");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("apikey", _options.ApiKey);
        }
    }

    private bool EnsureConfigured()
    {
        if (_options.IsConfigured)
        {
            return true;
        }

        _logger.LogWarning("Evolution API não configurada (EvolutionApi:BaseUrl).");
        return false;
    }

    private static string? ExtractQr(string json)
    {
        return ExtractFirst(json, "base64", "qrcode", "code", "qr");
    }

    private static string? ExtractFirst(string json, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            foreach (var name in names)
            {
                var found = FindProperty(document.RootElement, name);
                if (!string.IsNullOrWhiteSpace(found) && found != "{}")
                {
                    return found;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string? FindProperty(JsonElement element, string name)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            return property.Value.GetString();
                        }

                        if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        {
                            var nested = FindProperty(property.Value, name);
                            if (!string.IsNullOrWhiteSpace(nested))
                            {
                                return nested;
                            }
                        }
                    }

                    var child = FindProperty(property.Value, name);
                    if (!string.IsNullOrWhiteSpace(child))
                    {
                        return child;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var child = FindProperty(item, name);
                    if (!string.IsNullOrWhiteSpace(child))
                    {
                        return child;
                    }
                }

                break;
        }

        return null;
    }
}
