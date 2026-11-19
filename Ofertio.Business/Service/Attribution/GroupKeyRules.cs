using System.Security.Cryptography;
using System.Text;

namespace Ofertio.Business.Service.Attribution;

/// <summary>
/// Chave curta e opaca de um grupo (WhatsApp ou Telegram) para ir no link no lugar do JID/ChatId.
/// É determinística (HMAC): dá para recalcular a chave de cada grupo do usuário e resolver o nome sem guardar nada.
/// O segredo não pode mudar depois de em uso, senão as chaves antigas deixam de ser resolvidas.
/// </summary>
public static class GroupKeyRules
{
    public const int KeyLength = 8;
    public const string QueryKey = "g";
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz234567";
    private const string FallbackSecret = "ofertio-group-key-v1";

    public static string Compute(string? secret, int userId, string? channelName, string? groupId)
    {
        var key = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(secret) ? FallbackSecret : secret);
        var payload = Encoding.UTF8.GetBytes($"{userId}|{(channelName ?? string.Empty).Trim().ToLowerInvariant()}|{(groupId ?? string.Empty).Trim()}");
        var hash = HMACSHA256.HashData(key, payload);
        return ToBase32(hash.AsSpan(0, 5));
    }

    /// <summary>Formato da chave: 8 caracteres de a-z e 2-7 com pelo menos uma letra (não confunde com JID ou ChatId antigos).</summary>
    public static bool IsKey(string? value)
    {
        if (value is null || value.Length != KeyLength)
        {
            return false;
        }

        var hasLetter = false;
        foreach (var ch in value)
        {
            if (Alphabet.IndexOf(ch) < 0)
            {
                return false;
            }

            hasLetter |= ch is >= 'a' and <= 'z';
        }

        return hasLetter;
    }

    private static string ToBase32(ReadOnlySpan<byte> data)
    {
        var builder = new StringBuilder(KeyLength);
        var buffer = 0;
        var bits = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                builder.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        return builder.ToString();
    }
}
