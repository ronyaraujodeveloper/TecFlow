using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using TecFlow.Util.Security;

namespace TecFlow.Infrastructure.Security;

/// <summary>Data Protection para tokens de marketplace, com leitura do legado AES (<c>ENC1:</c>) e de texto puro.</summary>
public sealed class SecurityEncryptionService : ISecurityEncryptionService
{
    public const string CipherPrefix = "DP1:";
    public const string Purpose = "TecFlow.MarketplaceTokens.v1";

    private readonly IDataProtector _protector;
    private readonly IEncryptionService _legacyEncryption;
    private readonly ILogger<SecurityEncryptionService> _logger;

    public SecurityEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        IEncryptionService legacyEncryption,
        ILogger<SecurityEncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
        _legacyEncryption = legacyEncryption;
        _logger = logger;
    }

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText) || IsProtected(plainText))
        {
            return plainText;
        }

        return CipherPrefix + _protector.Protect(Unprotect(plainText));
    }

    public string Unprotect(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
        {
            return storedValue;
        }

        try
        {
            if (IsProtected(storedValue))
            {
                return _protector.Unprotect(storedValue[CipherPrefix.Length..]);
            }

            return AesEncryptionService.IsEncrypted(storedValue)
                ? _legacyEncryption.Decrypt(storedValue)
                : storedValue;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            _logger.LogWarning(
                "Não foi possível abrir um segredo de marketplace ({Format}); devolvendo o valor armazenado.",
                IsProtected(storedValue) ? "DataProtection" : "AES legado");
            return storedValue;
        }
    }

    public static bool IsProtected(string value) =>
        value.StartsWith(CipherPrefix, StringComparison.Ordinal);
}
