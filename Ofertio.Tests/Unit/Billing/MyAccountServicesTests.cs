using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Billing;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Billing;

public class MyAccountServicesTests
{
    private const int UserId = 7;
    private const int MasterId = 8;
    private static readonly Guid TenantId = Guid.NewGuid();

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    private static async Task<AppDbContext> SeedAsync()
    {
        var context = CreateContext();
        context.Tenants.Add(new Tenant
        {
            Id = TenantId,
            Name = "Loja",
            PlanCode = SaasPlanCodes.Starter,
            Status = TenantStatuses.Trial
        });
        context.UserAccounts.AddRange(
            new UserAccount { Id = UserId, TenantId = TenantId, Name = "Cliente", Email = "cliente@ofertio.com", PasswordHash = "hash", WhatsAppPhone = "11987654321" },
            new UserAccount { Id = MasterId, TenantId = TenantId, Name = "Master", Email = "master@ofertio.com", PasswordHash = "hash", Role = MasterRoles.SuperAdmin });
        context.SaasPlans.AddRange(
            new SaasPlan { Code = SaasPlanCodes.Starter, Name = "Starter", SortOrder = 1 },
            new SaasPlan { Code = SaasPlanCodes.Pro, Name = "Pro", MonthlyPrice = 99m, AnnualPrice = 990m, SortOrder = 2, Highlights = "Suporte prioritário" },
            new SaasPlan { Code = SaasPlanCodes.Agency, Name = "Agency", MonthlyPrice = 199m, AnnualPrice = 1990m, SortOrder = 3, IsActive = false });
        await context.SaveChangesAsync();
        return context;
    }

    private static UserProfileDto CompleteProfile() => new()
    {
        PersonType = PersonTypes.Individual,
        DisplayName = "Maria",
        FullName = "Maria da Silva",
        Document = "529.982.247-25",
        Phone = "(11) 98765-4321",
        PostalCode = "01310-100",
        Street = "Avenida Paulista",
        Number = "1000",
        District = "Bela Vista",
        City = "São Paulo",
        State = "SP"
    };

    private static PlanCheckoutDto ProAnnualCheckout() => new()
    {
        PlanCode = SaasPlanCodes.Pro,
        BillingCycle = "anual",
        Card = new CreditCardDto { Number = "4111 1111 1111 1111", HolderName = "MARIA SILVA", Expiry = "12/30", Cvv = "123" }
    };

    private static MyPlanService CreatePlanService(AppDbContext context, params string[] masterTestEmails) =>
        new(
            context,
            new UserProfileService(context),
            new SimulatedPlanPaymentGateway(),
            Options.Create(new BillingOptions { MasterCheckoutTestEmails = [.. masterTestEmails] }),
            NullLogger<MyPlanService>.Instance);

    [Fact]
    public async Task GetProfile_WithoutSavedProfile_ShouldSuggestAccountNameAndPhone()
    {
        await using var context = await SeedAsync();

        var result = await new UserProfileService(context).GetAsync(UserId);

        Assert.True(result.Status);
        Assert.Equal("Cliente", result.Data!.DisplayName);
        Assert.Equal("cliente@ofertio.com", result.Data.Email);
        Assert.Equal("11987654321", result.Data.Phone);
        Assert.Contains("CPF", result.MissingFields);
    }

    [Fact]
    public async Task SaveProfile_ShouldUpsertAndRenameUser()
    {
        await using var context = await SeedAsync();
        var service = new UserProfileService(context);

        var first = await service.SaveAsync(UserId, CompleteProfile());
        var update = CompleteProfile();
        update.Number = "2000";
        var second = await service.SaveAsync(UserId, update);

        Assert.True(first.Status, first.Descricao);
        Assert.True(second.Status, second.Descricao);
        Assert.Empty(second.MissingFields);
        var profile = await context.UserProfiles.SingleAsync();
        Assert.Equal("52998224725", profile.Document);
        Assert.Equal("2000", profile.Number);
        Assert.Equal("Maria", (await context.UserAccounts.SingleAsync(user => user.Id == UserId)).Name);
    }

    [Fact]
    public async Task SaveProfile_WithInvalidCpf_ShouldFailWithoutSaving()
    {
        await using var context = await SeedAsync();
        var profile = CompleteProfile();
        profile.Document = "111.111.111-11";

        var result = await new UserProfileService(context).SaveAsync(UserId, profile);

        Assert.False(result.Status);
        Assert.Contains("CPF inválido", result.Descricao);
        Assert.False(await context.UserProfiles.AnyAsync());
    }

    [Fact]
    public async Task Overview_ShouldListOnlyActivePlansWithHighlights()
    {
        await using var context = await SeedAsync();

        var overview = await CreatePlanService(context).GetOverviewAsync(UserId);

        Assert.True(overview.Status);
        Assert.False(overview.IsMaster);
        Assert.True(overview.IsPaymentSimulated);
        Assert.Equal(SaasPlanCodes.Starter, overview.CurrentPlanCode);
        Assert.Equal([SaasPlanCodes.Starter, SaasPlanCodes.Pro], overview.Plans.Select(plan => plan.Code));
        Assert.Equal("Suporte prioritário", overview.Plans[1].Highlights);
        Assert.NotEmpty(overview.MissingFields);
    }

    [Fact]
    public async Task Checkout_ForMaster_ShouldBeRefused()
    {
        await using var context = await SeedAsync();

        var result = await CreatePlanService(context).CheckoutAsync(MasterId, ProAnnualCheckout());

        Assert.False(result.Status);
        Assert.Contains("Master", result.Descricao);
        Assert.False(await context.PlanCheckouts.AnyAsync());
    }

    [Fact]
    public async Task Checkout_ForMasterInTestList_ShouldBehaveLikeClient()
    {
        await using var context = await SeedAsync();
        await new UserProfileService(context).SaveAsync(MasterId, CompleteProfile());
        var service = CreatePlanService(context, " MASTER@ofertio.com ");

        var overview = await service.GetOverviewAsync(MasterId);
        var result = await service.CheckoutAsync(MasterId, ProAnnualCheckout());

        Assert.False(overview.IsMaster);
        Assert.True(overview.IsMasterCheckoutTest);
        Assert.True(result.Status, result.Descricao);
        Assert.Equal(PlanCheckoutStatuses.Simulated, (await context.PlanCheckouts.SingleAsync()).Status);
    }

    [Fact]
    public async Task Checkout_ForMasterOutsideTestList_ShouldStillBeRefused()
    {
        await using var context = await SeedAsync();
        var service = CreatePlanService(context, "outro@ofertio.com");

        var overview = await service.GetOverviewAsync(MasterId);
        var result = await service.CheckoutAsync(MasterId, ProAnnualCheckout());

        Assert.True(overview.IsMaster);
        Assert.False(overview.IsMasterCheckoutTest);
        Assert.False(result.Status);
    }

    [Fact]
    public async Task Checkout_WithIncompleteProfile_ShouldReturnMissingFields()
    {
        await using var context = await SeedAsync();

        var result = await CreatePlanService(context).CheckoutAsync(UserId, ProAnnualCheckout());

        Assert.False(result.Status);
        Assert.Contains("CPF", result.MissingFields);
        Assert.False(await context.PlanCheckouts.AnyAsync());
    }

    [Fact]
    public async Task Checkout_InactivePlan_ShouldFail()
    {
        await using var context = await SeedAsync();
        await new UserProfileService(context).SaveAsync(UserId, CompleteProfile());
        var request = ProAnnualCheckout();
        request.PlanCode = SaasPlanCodes.Agency;

        var result = await CreatePlanService(context).CheckoutAsync(UserId, request);

        Assert.False(result.Status);
        Assert.Contains("Plano não encontrado", result.Descricao);
    }

    [Fact]
    public async Task Checkout_Simulated_ShouldStoreOnlyBrandAndLast4AndKeepTenantPlan()
    {
        await using var context = await SeedAsync();
        await new UserProfileService(context).SaveAsync(UserId, CompleteProfile());
        var service = CreatePlanService(context);

        var result = await service.CheckoutAsync(UserId, ProAnnualCheckout());

        Assert.True(result.Status, result.Descricao);
        var checkout = await context.PlanCheckouts.SingleAsync();
        Assert.Equal(PlanCheckoutStatuses.Simulated, checkout.Status);
        Assert.Equal(SaasMetricsRules.CycleAnnual, checkout.BillingCycle);
        Assert.Equal(990m, checkout.Amount);
        Assert.Equal("Visa", checkout.CardBrand);
        Assert.Equal("1111", checkout.CardLast4);
        Assert.Null(result.Data!.Card);
        Assert.Equal(SaasPlanCodes.Starter, (await context.Tenants.SingleAsync()).PlanCode);

        var reloaded = await service.GetCheckoutAsync(UserId, checkout.Id);
        var otherUser = await service.GetCheckoutAsync(MasterId, checkout.Id);
        Assert.True(reloaded.Status);
        Assert.False(otherUser.Status);
    }
}
