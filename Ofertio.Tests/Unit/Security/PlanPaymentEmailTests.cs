using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Email;
using Ofertio.Infrastructure.Services.Master;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Security;

public class PlanPaymentEmailTests
{
    [Fact]
    public async Task HandleAsync_SendsTheConfirmationOnce_WhenAsaasReceivesThePayment()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Ana",
            PlanCode = "Pro",
            BillingCycle = "Mensal",
            Status = "Ativo"
        });
        context.UserAccounts.Add(new UserAccount
        {
            Name = "Ana",
            Email = "ana@ofertio.com.br",
            PasswordHash = "hash",
            TenantId = tenantId
        });
        await context.SaveChangesAsync();

        var email = new CapturingEmailSender();
        var service = CreateService(context, email);
        var payload = new AsaasWebhookPayloadDto
        {
            Event = "PAYMENT_RECEIVED",
            Payment = new AsaasWebhookPaymentDto
            {
                Id = "pay_1",
                ExternalReference = tenantId.ToString(),
                Value = 49.9m,
                Status = "RECEIVED",
                PaymentDate = "2026-11-10"
            }
        };

        var first = await service.HandleAsync(payload);
        var second = await service.HandleAsync(payload);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Single(email.Sent);
        Assert.Equal("ana@ofertio.com.br", email.Sent[0].To);
        Assert.Contains("Pagamento confirmado", email.Sent[0].Subject, StringComparison.Ordinal);
        Assert.Contains("Pro", email.Sent[0].TextBody, StringComparison.Ordinal);
        Assert.Contains("Mensal", email.Sent[0].TextBody, StringComparison.Ordinal);
        Assert.Contains("/meu-plano", email.Sent[0].TextBody, StringComparison.Ordinal);
        Assert.Contains(OfertioMailAddresses.Contact, email.Sent[0].TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void SimulatedCheckout_DoesNotBuildAPlanEmail()
    {
        var message = Infrastructure.Services.Billing.SimulatedPlanPaymentGateway.SimulatedMessage;
        Assert.Contains("nenhuma cobrança foi feita", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("e-mail", message, StringComparison.OrdinalIgnoreCase);
    }

    private static AsaasWebhookService CreateService(AppDbContext context, CapturingEmailSender email)
    {
        var audit = new Mock<IMasterAuditService>();
        audit.Setup(service => service.LogAsync(
                It.IsAny<MasterActor>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var access = new Mock<ITenantAccessReader>();
        return new AsaasWebhookService(
            context,
            new ConfigurationBuilder().Build(),
            audit.Object,
            access.Object,
            email,
            Options.Create(new EmailOptions { PublicBaseUrl = "https://ofertio.com.br" }),
            NullLogger<AsaasWebhookService>.Instance);
    }

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
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
