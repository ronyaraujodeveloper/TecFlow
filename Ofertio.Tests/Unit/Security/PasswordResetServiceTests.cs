using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Email;
using Ofertio.Infrastructure.Services.Repositories;
using Ofertio.Infrastructure.Services.Security;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Security;

public class PasswordResetServiceTests
{
    [Fact]
    public void PasswordReset_UsesTheSameTextWhetherOrNotTheAccountExists()
    {
        Assert.Contains("Se o e-mail estiver cadastrado", TransactionalEmailRules.RequestAcceptedMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("senha nova", TransactionalEmailRules.PasswordReset("ana@ofertio.com.br", "https://ofertio.com.br/recuperar-senha?token=abc").TextBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestAsync_ReturnsTheSameMessageAndSendsNothing_WhenTheEmailIsUnknown()
    {
        await using var context = await CreateContext();
        var email = new CapturingEmailSender();
        var service = CreateService(context, email);

        var missing = await service.RequestAsync("ninguem@ofertio.com.br");
        var known = await service.RequestAsync("ana@ofertio.com.br");

        Assert.True(missing.Status);
        Assert.Equal(missing.Descricao, known.Status ? TransactionalEmailRules.RequestAcceptedMessage : known.Descricao);
        Assert.Equal(TransactionalEmailRules.RequestAcceptedMessage, missing.Descricao);
        Assert.Equal(TransactionalEmailRules.RequestAcceptedMessage, known.Descricao);
        Assert.Single(email.Sent);
        Assert.Equal("ana@ofertio.com.br", email.Sent[0].To);
        Assert.Equal(OfertioMailAddresses.Contact, email.Sent[0].TextBody.Contains(OfertioMailAddresses.Contact) ? OfertioMailAddresses.Contact : string.Empty);
        Assert.DoesNotContain(await context.PasswordResetTokens.Select(item => item.TokenHash).SingleAsync(), email.Sent[0].TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmAsync_UpdatesThePasswordOnce_AndDropsThePreviousLink()
    {
        await using var context = await CreateContext();
        var email = new CapturingEmailSender();
        var service = CreateService(context, email);
        var hasher = new OfertioPasswordHasher();

        await service.RequestAsync("ana@ofertio.com.br");
        var firstToken = ReadToken(email.Sent[0].TextBody);
        await service.RequestAsync("ana@ofertio.com.br");
        var secondToken = ReadToken(email.Sent[1].TextBody);

        var stale = await service.ConfirmAsync(firstToken, "NovaSenha@123");
        Assert.False(stale.Status);
        Assert.Equal(TransactionalEmailRules.InvalidTokenMessage, stale.Descricao);

        var weak = await service.ConfirmAsync(secondToken, "abc");
        Assert.False(weak.Status);

        var updated = await service.ConfirmAsync(secondToken, "NovaSenha@123");
        Assert.True(updated.Status);
        Assert.Equal(TransactionalEmailRules.PasswordUpdatedMessage, updated.Descricao);

        var user = await context.UserAccounts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, "NovaSenha@123"));

        var reused = await service.ConfirmAsync(secondToken, "OutraSenha@123");
        Assert.False(reused.Status);
    }

    private static PasswordResetService CreateService(AppDbContext context, CapturingEmailSender email)
    {
        var provisioning = new Mock<ITenantProvisioningService>();
        return new PasswordResetService(
            context,
            new UserAccountRepository(context, provisioning.Object),
            new OfertioPasswordHasher(),
            email,
            Options.Create(new EmailOptions { PublicBaseUrl = "https://dev.ofertio.com.br" }),
            NullLogger<PasswordResetService>.Instance);
    }

    private static async Task<AppDbContext> CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
        context.UserAccounts.Add(new UserAccount
        {
            Name = "Ana",
            Email = "ana@ofertio.com.br",
            PasswordHash = new OfertioPasswordHasher().HashPassword(null!, "SenhaAntiga@1"),
            TenantId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();
        return context;
    }

    private static string ReadToken(string body)
    {
        var marker = "token=";
        var start = body.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var token = body[(start + marker.Length)..].Split(['\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries)[0];
        return Uri.UnescapeDataString(token);
    }

    private sealed class CapturingEmailSender : ITransactionalEmailSender
    {
        public List<TransactionalEmail> Sent { get; } = [];

        public Task SendAsync(TransactionalEmail message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }
}
