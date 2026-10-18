using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Util.Security;

namespace Ofertio.Infrastructure.Services.Integrations.Auth;

/// <summary>
/// AES-256-GCM com chave derivada (HKDF) de <c>Encryption:Key</c>: o state não pode ser lido nem forjado
/// fora da API, e qualquer alteração invalida a tag de autenticação.
/// </summary>
public sealed class OAuthStateProtector : IOAuthStateProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly byte[] KeyInfo = Encoding.UTF8.GetBytes("Ofertio.OAuthState.v1");

    private readonly byte[] _key;

    public OAuthStateProtector(IConfiguration configuration)
        : this(configuration[$"{EncryptionOptions.SectionName}:Key"])
    {
    }

    public OAuthStateProtector(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException("Encryption:Key não configurada; o state do OAuth não pode ser selado.");
        }

        _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Convert.FromBase64String(base64Key), 32, info: KeyInfo);
    }

    public string Protect(MarketplaceOAuthState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var plain = JsonSerializer.SerializeToUtf8Bytes(state);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plain, cipher, tag);
        }

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipher.CopyTo(payload, NonceSize + TagSize);
        return MarketplaceOAuthStateRules.Prefix + ToBase64Url(payload);
    }

    public bool TryUnprotect(string? protectedState, out MarketplaceOAuthState? state)
    {
        state = null;
        if (!MarketplaceOAuthStateRules.IsProtected(protectedState))
        {
            return false;
        }

        try
        {
            var payload = FromBase64Url(protectedState!.Trim()[MarketplaceOAuthStateRules.Prefix.Length..]);
            if (payload.Length <= NonceSize + TagSize)
            {
                return false;
            }

            var nonce = payload.AsSpan(0, NonceSize);
            var tag = payload.AsSpan(NonceSize, TagSize);
            var cipher = payload.AsSpan(NonceSize + TagSize);
            var plain = new byte[cipher.Length];
            using (var aes = new AesGcm(_key, TagSize))
            {
                aes.Decrypt(nonce, cipher, tag, plain);
            }

            var opened = JsonSerializer.Deserialize<MarketplaceOAuthState>(plain);
            if (opened is null || opened.UserId <= 0 || opened.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            state = opened;
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException)
        {
            return false;
        }
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}
