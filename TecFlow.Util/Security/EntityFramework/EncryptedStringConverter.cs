using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TecFlow.Util.Security.EntityFramework;

public static class EncryptedStringConverter
{
    public static ValueConverter<string, string> Create(IEncryptionService encryptionService) =>
        new(
            value => encryptionService.Encrypt(value),
            value => encryptionService.Decrypt(value));

    public static ValueConverter<string?, string?> CreateNullable(IEncryptionService encryptionService) =>
        new(
            value => value == null ? null : encryptionService.Encrypt(value),
            value => value == null ? null : encryptionService.Decrypt(value));

    public static ValueConverter<string?, string?> CreateProtected(ISecurityEncryptionService securityEncryption) =>
        new(
            value => value == null ? null : securityEncryption.Protect(value),
            value => value == null ? null : securityEncryption.Unprotect(value));
}
