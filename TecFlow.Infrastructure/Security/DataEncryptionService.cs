using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TecFlow.Util.Security;

namespace TecFlow.Infrastructure.Security;

public interface IDataEncryptionService
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);

    string Mask(string? secret);
}

/// <summary>Criptografia AES-256 (CBC/PKCS7) para tokens, API keys e SessionData de Telegram/WhatsApp.</summary>
public sealed class DataEncryptionService : IDataEncryptionService, IEncryptionService
{
    public const string MaskedValue = "****************";

    private readonly byte[] _key;

    public DataEncryptionService(IOptions<EncryptionOptions> options)
        : this(options.Value.Key)
    {
    }

    public DataEncryptionService(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException(
                "Encryption:Key não configurada. Defina uma chave Base64 de 32 bytes (AES-256).");
        }

        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Encryption:Key deve decodificar para exatamente 32 bytes (AES-256).");
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText) || AesEncryptionService.IsEncrypted(plainText))
        {
            return plainText;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var iv = RandomNumberGenerator.GetBytes(16);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var payload = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, payload, iv.Length, encrypted.Length);
        return AesEncryptionService.CipherPrefix + Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText) || !AesEncryptionService.IsEncrypted(cipherText))
        {
            return cipherText;
        }

        var payload = Convert.FromBase64String(cipherText[AesEncryptionService.CipherPrefix.Length..]);
        var iv = payload.AsSpan(0, 16).ToArray();
        var encrypted = payload.AsSpan(16).ToArray();
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public string Mask(string? secret) =>
        string.IsNullOrEmpty(secret) ? string.Empty : MaskedValue;
}
