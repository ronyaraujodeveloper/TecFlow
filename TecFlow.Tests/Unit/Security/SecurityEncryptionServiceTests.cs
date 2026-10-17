using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using TecFlow.Infrastructure.Security;
using TecFlow.Util.Security;
using TecFlow.Util.Security.EntityFramework;

namespace TecFlow.Tests.Unit.Security;

public class SecurityEncryptionServiceTests
{
    private const string Aes256Key = "z4wTRplZYgexzdRDmuV59SFL6cYlJ8sJGtPWtrxmiko=";
    private const string Token = "APP_USR-1234567890-101426-abcdef-150141361";

    private static readonly DataEncryptionService LegacyEncryption = new(Aes256Key);

    private static SecurityEncryptionService CreateService(IDataProtectionProvider? provider = null) =>
        new(provider ?? new EphemeralDataProtectionProvider(), LegacyEncryption, NullLogger<SecurityEncryptionService>.Instance);

    [Fact]
    public void Protect_ShouldRoundTripWithDataProtectionPrefix()
    {
        var service = CreateService();

        var cipher = service.Protect(Token);

        Assert.StartsWith(SecurityEncryptionService.CipherPrefix, cipher);
        Assert.DoesNotContain(Token, cipher);
        Assert.Equal(Token, service.Unprotect(cipher));
    }

    [Fact]
    public void Protect_ShouldBeIdempotent()
    {
        var service = CreateService();
        var cipher = service.Protect(Token);

        Assert.Equal(cipher, service.Protect(cipher));
    }

    [Fact]
    public void Protect_ShouldReEncryptLegacyAesValue()
    {
        var service = CreateService();
        var legacy = LegacyEncryption.Encrypt(Token);

        var cipher = service.Protect(legacy);

        Assert.StartsWith(SecurityEncryptionService.CipherPrefix, cipher);
        Assert.Equal(Token, service.Unprotect(cipher));
    }

    [Fact]
    public void Unprotect_ShouldReadLegacyAesValue()
    {
        var service = CreateService();

        Assert.Equal(Token, service.Unprotect(LegacyEncryption.Encrypt(Token)));
    }

    [Fact]
    public void Unprotect_ShouldReturnLegacyPlainText()
    {
        var service = CreateService();

        Assert.Equal(Token, service.Unprotect(Token));
        Assert.Equal(string.Empty, service.Unprotect(string.Empty));
    }

    [Fact]
    public void Unprotect_ShouldReturnStoredValue_WhenKeyRingDoesNotMatch()
    {
        var cipher = CreateService().Protect(Token);
        var otherKeyRing = CreateService();

        Assert.Equal(cipher, otherKeyRing.Unprotect(cipher));
    }

    [Fact]
    public void CreateProtected_ShouldConvertBothDirections()
    {
        var service = CreateService();
        var converter = EncryptedStringConverter.CreateProtected(service);

        var stored = (string)converter.ConvertToProvider(Token)!;

        Assert.StartsWith(SecurityEncryptionService.CipherPrefix, stored);
        Assert.Equal(Token, converter.ConvertFromProvider(stored));
        Assert.Equal(Token, converter.ConvertFromProvider(LegacyEncryption.Encrypt(Token)));
    }
}
