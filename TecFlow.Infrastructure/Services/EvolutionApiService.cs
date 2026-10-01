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

        var payload = JsonSerializer.Serialize(BuildCreateInstanceBody(instanceName));

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

    public async Task<bool> SendTextMessageAsync(
        string instanceName,
        string remoteJid,
        string messageText,
        CancellationToken cancellationToken = default)
    {
        if (!EnsureConfigured()
            || string.IsNullOrWhiteSpace(instanceName)
            || string.IsNullOrWhiteSpace(remoteJid)
            || string.IsNullOrWhiteSpace(messageText))
        {
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(2500));

        var body = JsonSerializer.Serialize(new
        {
            number = remoteJid,
            text = messageText
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        try
        {
            using var response = await _httpClient.PostAsync(
                $"message/sendText/{Uri.EscapeDataString(instanceName)}",
                content,
                timeout.Token);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Evolution sendText falhou. Status={Status} Instance={Instance} Body={Body}",
                (int)response.StatusCode,
                instanceName,
                responseBody);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Evolution sendText cancelado (limite 3s). Instance={Instance}", instanceName);
            return false;
        }
    }

    public async Task<IReadOnlyList<EvolutionWhatsAppGroupDto>> FetchUserGroupsAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        if (!EnsureConfigured() || string.IsNullOrWhiteSpace(instanceName))
        {
            return [];
        }

        using var response = await _httpClient.GetAsync(
            $"group/fetchAllGroups/{Uri.EscapeDataString(instanceName)}?getParticipants=true",
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Evolution fetch groups falhou. Status={Status} Instance={Instance}",
                (int)response.StatusCode,
                instanceName);
        }

        return ParseGroups(json);
    }

    public async Task<bool> SendMediaMessageAsync(
        string instanceName,
        string remoteJid,
        string mediaUrl,
        string caption,
        CancellationToken cancellationToken = default)
    {
        if (!EnsureConfigured()
            || string.IsNullOrWhiteSpace(instanceName)
            || string.IsNullOrWhiteSpace(remoteJid)
            || string.IsNullOrWhiteSpace(mediaUrl))
        {
            return false;
        }

        var body = JsonSerializer.Serialize(new
        {
            number = remoteJid,
            mediatype = "image",
            media = mediaUrl,
            caption = caption ?? string.Empty
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(
            $"message/sendMedia/{Uri.EscapeDataString(instanceName)}",
            content,
            cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Evolution sendMedia falhou. Status={Status} Instance={Instance} Body={Body}",
            (int)response.StatusCode,
            instanceName,
            responseBody);
        return false;
    }

    private static IReadOnlyList<EvolutionWhatsAppGroupDto> ParseGroups(string json)
    {
        var groups = new List<EvolutionWhatsAppGroupDto>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return groups;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            CollectGroups(document.RootElement, groups);
        }
        catch (JsonException)
        {
            return groups;
        }

        return groups
            .Where(group => !string.IsNullOrWhiteSpace(group.Jid))
            .DistinctBy(group => group.Jid, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void CollectGroups(JsonElement element, List<EvolutionWhatsAppGroupDto> groups)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var jid = ReadString(element, "id", "jid", "groupJid");
                if (!string.IsNullOrWhiteSpace(jid) && jid.Contains("@g.us", StringComparison.OrdinalIgnoreCase))
                {
                    var participants = ReadElement(element, "participants");
                    var count = ReadInt(element, "size", "participantsCount", "participantCount");
                    if (count == 0 && participants.ValueKind == JsonValueKind.Array)
                    {
                        count = participants.GetArrayLength();
                    }

                    groups.Add(new EvolutionWhatsAppGroupDto
                    {
                        Jid = jid,
                        Name = ReadString(element, "subject", "name", "topic") ?? jid,
                        ParticipantCount = count,
                        IsAdmin = DetectAdmin(element, participants)
                    });
                }

                foreach (var property in element.EnumerateObject())
                {
                    CollectGroups(property.Value, groups);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectGroups(item, groups);
                }

                break;
        }
    }

    private static bool DetectAdmin(JsonElement group, JsonElement participants)
    {
        var owner = ReadString(group, "owner", "ownerJid") ?? string.Empty;
        if (participants.ValueKind != JsonValueKind.Array)
        {
            return ReadBool(group, "isAdmin", "admin");
        }

        foreach (var participant in participants.EnumerateArray())
        {
            var role = ReadString(participant, "admin", "role") ?? string.Empty;
            var isAdminRole = role.Contains("admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdminRole)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(owner))
            {
                return true;
            }

            var participantId = ReadString(participant, "id", "jid", "lid") ?? string.Empty;
            if (participantId.Contains(owner, StringComparison.OrdinalIgnoreCase)
                || owner.Contains(participantId.Split('@')[0], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return ReadBool(group, "isAdmin", "admin");
    }

    private static JsonElement ReadElement(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        foreach (var property in parent.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return default;
    }

    private static string? ReadString(JsonElement parent, params string[] names)
    {
        foreach (var name in names)
        {
            var element = ReadElement(parent, name);
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
        }

        return null;
    }

    private static int ReadInt(JsonElement parent, params string[] names)
    {
        foreach (var name in names)
        {
            var element = ReadElement(parent, name);
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
            {
                return value;
            }
        }

        return 0;
    }

    private static bool ReadBool(JsonElement parent, params string[] names)
    {
        foreach (var name in names)
        {
            var element = ReadElement(parent, name);
            if (element.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }

        return false;
    }

    private object BuildCreateInstanceBody(string instanceName)
    {
        if (!string.IsNullOrWhiteSpace(_options.WebhookUrl)
            && Uri.TryCreate(_options.WebhookUrl, UriKind.Absolute, out _))
        {
            return new
            {
                instanceName,
                qrcode = true,
                integration = "WHATSAPP-BAILEYS",
                webhook = new
                {
                    url = _options.WebhookUrl.Trim(),
                    byEvents = true,
                    events = new[] { "MESSAGES_UPSERT", "messages.upsert" }
                }
            };
        }

        return new
        {
            instanceName,
            qrcode = true,
            integration = "WHATSAPP-BAILEYS"
        };
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
