using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TecFlow.Portal.Configuration;

namespace TecFlow.Portal.Security;

public static class AppleClientSecretGenerator
{
    public static string Generate(AppleOAuthSettings settings)
    {
        var keyBytes = ResolvePrivateKeyBytes(settings);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);

        var signingCredentials = new SigningCredentials(
            new ECDsaSecurityKey(ecdsa),
            SecurityAlgorithms.EcdsaSha256);

        var now = DateTimeOffset.UtcNow;
        var token = new JwtSecurityToken(
            issuer: settings.TeamId,
            audience: "https://appleid.apple.com",
            claims: [new Claim("sub", settings.ClientId)],
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(5).UtcDateTime,
            signingCredentials: signingCredentials);

        token.Header["kid"] = settings.KeyId;
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static byte[] ResolvePrivateKeyBytes(AppleOAuthSettings settings)
    {
        if (File.Exists(settings.PrivateKey))
        {
            return ReadPemKey(File.ReadAllText(settings.PrivateKey));
        }

        return ReadPemKey(settings.PrivateKey.Replace("\\n", "\n", StringComparison.Ordinal));
    }

    private static byte[] ReadPemKey(string pem)
    {
        var lines = pem
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith("-----", StringComparison.Ordinal))
            .ToArray();

        return Convert.FromBase64String(string.Concat(lines));
    }
}
