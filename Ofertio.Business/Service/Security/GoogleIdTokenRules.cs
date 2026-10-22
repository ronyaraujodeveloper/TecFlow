namespace Ofertio.Business.Service.Security;

/// <summary>Confere se o id_token do Google foi emitido para o Client ID desta aplicação.</summary>
public static class GoogleIdTokenRules
{
    public static bool AudienceMatches(string? audience, string? clientId)
    {
        if (string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(clientId))
        {
            return false;
        }

        var expected = clientId.Trim();
        var audiences = audience.Split(
            new[] { ' ', ',' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return audiences.Any(value => string.Equals(value, expected, StringComparison.Ordinal));
    }
}
