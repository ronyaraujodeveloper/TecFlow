using TecFlow.API.Security;
using TecFlow.Business.Service.Security;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Infrastructure.Security;

namespace TecFlow.Tests.Unit.Security;

public class MessagingIntegrationSecurityTests
{
    private const string Aes256Key = "z4wTRplZYgexzdRDmuV59SFL6cYlJ8sJGtPWtrxmiko=";

    [Fact]
    public void DataEncryptionService_ShouldRoundTripAes256()
    {
        var service = new DataEncryptionService(Aes256Key);
        var cipher = service.Encrypt("bot-token-secret");
        Assert.StartsWith("ENC1:", cipher);
        Assert.Equal("bot-token-secret", service.Decrypt(cipher));
        Assert.Equal(DataEncryptionService.MaskedValue, service.Mask("bot-token-secret"));
        Assert.Equal(string.Empty, service.Mask(null));
    }

    [Fact]
    public void WebhookSecurityFilter_ShouldRejectEmptyOrMismatchedSecrets()
    {
        Assert.False(WebhookSecurityFilter.SecretsEqual(null, "abc"));
        Assert.False(WebhookSecurityFilter.SecretsEqual("abc", ""));
        Assert.False(WebhookSecurityFilter.SecretsEqual("abc", "abd"));
        Assert.True(WebhookSecurityFilter.SecretsEqual("abc", "abc"));
    }

    [Fact]
    public void IntegrationOwnershipGuard_ShouldThrowWhenUserDiffers()
    {
        IntegrationOwnershipGuard.EnsureOwner(10, 10);
        Assert.Throws<UnauthorizedAccessException>(() => IntegrationOwnershipGuard.EnsureOwner(10, 99));
    }

    [Fact]
    public void SecretMasking_ShouldHideTokens()
    {
        Assert.Equal("****************", SecretMasking.Mask("plain-token"));
        Assert.Equal(string.Empty, SecretMasking.Mask(""));
    }

    [Fact]
    public void TryParseUserId_ShouldMatchInstanceOwner()
    {
        Assert.True(WhatsAppSessionRules.TryParseUserId("tecflow-u7", out var userId));
        Assert.Equal(7, userId);
        Assert.False(WhatsAppSessionRules.TryParseUserId("other", out _));
    }
}
