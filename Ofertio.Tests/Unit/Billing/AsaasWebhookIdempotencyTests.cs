using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Email;
using Ofertio.Infrastructure.Services.Master;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Billing;

/// <summary>
/// Auditoria A5: cada pagamento estende a assinatura uma única vez.
/// O Asaas envia PAYMENT_CONFIRMED e depois PAYMENT_RECEIVED para o mesmo pagamento
/// e reenvia eventos quando não recebe 200; nenhum desses casos pode render período extra.
/// </summary>
public class AsaasWebhookIdempotencyTests
{
    private static readonly DateTime RenewalBase = DateTime.UtcNow.AddDays(10);

    [Fact]
    public async Task ConfirmedDepoisReceived_DoMesmoPagamento_EstendeUmCicloSo()
    {
        await using var context = CreateContext();
        var tenantId = await SeedTenantAsync(context, SaasMetricsRules.CycleMonthly);
        var service = CreateService(context);

        await service.HandleAsync(Payload("PAYMENT_CONFIRMED", "pay_1", tenantId, "CONFIRMED"));
        await service.HandleAsync(Payload("PAYMENT_RECEIVED", "pay_1", tenantId, "RECEIVED"));

        var tenant = await context.Tenants.SingleAsync();
        Assert.Equal(RenewalBase.AddMonths(1), tenant.RenewalAt);
    }

    [Fact]
    public async Task MesmoEventoReenviado_TresVezes_EstendeUmCicloSo()
    {
        await using var context = CreateContext();
        var tenantId = await SeedTenantAsync(context, SaasMetricsRules.CycleMonthly);
        var service = CreateService(context);
        var payload = Payload("PAYMENT_RECEIVED", "pay_1", tenantId, "RECEIVED");

        await service.HandleAsync(payload);
        await service.HandleAsync(payload);
        await service.HandleAsync(payload);

        var tenant = await context.Tenants.SingleAsync();
        Assert.Equal(RenewalBase.AddMonths(1), tenant.RenewalAt);
    }

    [Fact]
    public async Task PlanoAnual_ConfirmedDepoisReceived_EstendeUmAnoSo()
    {
        await using var context = CreateContext();
        var tenantId = await SeedTenantAsync(context, SaasMetricsRules.CycleAnnual);
        var service = CreateService(context);

        await service.HandleAsync(Payload("PAYMENT_CONFIRMED", "pay_1", tenantId, "CONFIRMED"));
        await service.HandleAsync(Payload("PAYMENT_RECEIVED", "pay_1", tenantId, "RECEIVED"));

        var tenant = await context.Tenants.SingleAsync();
        Assert.Equal(RenewalBase.AddYears(1), tenant.RenewalAt);
    }

    [Fact]
    public async Task PagamentosDiferentes_CadaUmEstendeUmCiclo()
    {
        await using var context = CreateContext();
        var tenantId = await SeedTenantAsync(context, SaasMetricsRules.CycleMonthly);
        var service = CreateService(context);

        await service.HandleAsync(Payload("PAYMENT_CONFIRMED", "pay_1", tenantId, "CONFIRMED"));
        await service.HandleAsync(Payload("PAYMENT_CONFIRMED", "pay_2", tenantId, "CONFIRMED"));

        var tenant = await context.Tenants.SingleAsync();
        Assert.Equal(RenewalBase.AddMonths(2), tenant.RenewalAt);
    }

    [Fact]
    public async Task EventoRepetido_NaoReativaInquilinoSuspensoDepoisDoPagamento()
    {
        await using var context = CreateContext();
        var tenantId = await SeedTenantAsync(context, SaasMetricsRules.CycleMonthly);
        var service = CreateService(context);
        await service.HandleAsync(Payload("PAYMENT_CONFIRMED", "pay_1", tenantId, "CONFIRMED"));

        var tenant = await context.Tenants.SingleAsync();
        tenant.Status = TenantStatuses.Suspenso;
        await context.SaveChangesAsync();

        await service.HandleAsync(Payload("PAYMENT_RECEIVED", "pay_1", tenantId, "RECEIVED"));

        tenant = await context.Tenants.SingleAsync();
        Assert.Equal(TenantStatuses.Suspenso, tenant.Status);
    }

    [Fact]
    public async Task EventoRepetido_AtualizaOStatusDoPagamento()
    {
        await using var context = CreateContext();
        var tenantId = await SeedTenantAsync(context, SaasMetricsRules.CycleMonthly);
        var service = CreateService(context);

        await service.HandleAsync(Payload("PAYMENT_CONFIRMED", "pay_1", tenantId, "CONFIRMED"));
        var second = await service.HandleAsync(Payload("PAYMENT_RECEIVED", "pay_1", tenantId, "RECEIVED"));

        var payment = await context.SaasPayments.SingleAsync();
        Assert.True(second.Success);
        Assert.Equal("RECEIVED", payment.Status);
        Assert.NotNull(payment.PaidAt);
    }

    private static AsaasWebhookPayloadDto Payload(string eventName, string paymentId, Guid tenantId, string status) => new()
    {
        Event = eventName,
        Payment = new AsaasWebhookPaymentDto
        {
            Id = paymentId,
            ExternalReference = tenantId.ToString(),
            Value = 49.9m,
            Status = status,
            PaymentDate = "2026-11-10"
        }
    };

    private static async Task<Guid> SeedTenantAsync(AppDbContext context, string billingCycle)
    {
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Ana",
            PlanCode = "Pro",
            BillingCycle = billingCycle,
            Status = TenantStatuses.Ativo,
            RenewalAt = RenewalBase
        });
        await context.SaveChangesAsync();
        return tenantId;
    }

    private static AsaasWebhookService CreateService(AppDbContext context)
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
        return new AsaasWebhookService(
            context,
            new ConfigurationBuilder().Build(),
            audit.Object,
            new Mock<ITenantAccessReader>().Object,
            new Mock<ITransactionalEmailSender>().Object,
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
}
