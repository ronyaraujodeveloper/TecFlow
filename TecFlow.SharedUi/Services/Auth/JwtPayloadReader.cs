using System.Text;
using System.Text.Json;

namespace TecFlow.SharedUi.Services.Auth;

/// <summary>Lê claims do payload de um JWT emitido pela API (sem validar assinatura — uso apenas para exibição/UI).</summary>
public static class JwtPayloadReader
{
    public const string RoleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    public const string EmailClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

    public static IReadOnlyDictionary<string, string> Read(string? token)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var parts = token?.Split('.');
        if (parts is not { Length: >= 2 })
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(DecodeBase64Url(parts[1]));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.Array => property.Value.EnumerateArray().Select(item => item.ToString()).FirstOrDefault(),
                    _ => null
                };
                if (!string.IsNullOrEmpty(value))
                {
                    result[property.Name] = value;
                }
            }
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
        }

        return result;
    }

    public static string? Get(IReadOnlyDictionary<string, string> payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (payload.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static string? ReadRole(string? token)
    {
        var payload = Read(token);
        return Get(payload, RoleClaim, "role");
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64
        };
        return Convert.FromBase64String(base64);
    }
}
