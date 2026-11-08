using Microsoft.EntityFrameworkCore;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Service.Legal;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.Filter;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Legal;
using Ofertio.SharedUi.Services.Navigation;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Legal;

public class TermsDocumentRulesTests
{
    [Theory]
    [InlineData(null, "1.1")]
    [InlineData("1.0", "1.1")]
    [InlineData("1.9", "1.10")]
    [InlineData("2", "2.1")]
    [InlineData("texto", "1.1")]
    public void NextVersion_IncrementsTheMinorNumber(string? latest, string expected)
    {
        Assert.Equal(expected, TermsDocumentRules.NextVersion(latest));
    }

    [Fact]
    public void Reacceptance_WaitsForAPublishedVersion()
    {
        Assert.False(TermsDocumentRules.IsReacceptanceRequired(null, null));
        Assert.False(TermsDocumentRules.IsReacceptanceRequired("1.0", null));
        Assert.True(TermsDocumentRules.IsReacceptanceRequired(null, "1.1"));
        Assert.True(TermsDocumentRules.IsReacceptanceRequired("1.0", "1.1"));
        Assert.False(TermsDocumentRules.IsReacceptanceRequired("1.1", "1.1"));
    }

    [Theory]
    [InlineData(MarketplaceType.MercadoLivre, true)]
    [InlineData(MarketplaceType.Shopee, true)]
    [InlineData(MarketplaceType.TikTokShop, true)]
    [InlineData(MarketplaceType.Amazon, false)]
    public void ExternalApi_CoversMercadoLivreShopeeAndTikTok(MarketplaceType platform, bool expected)
    {
        Assert.Equal(expected, TermsDocumentRules.IsExternalApiPlatform(platform));
    }

    [Fact]
    public void ResolveClientIp_PrefersCloudflareThenTheFirstForwardedAddress()
    {
        Assert.Equal("203.0.113.10", TermsDocumentRules.ResolveClientIp("203.0.113.10", "198.51.100.4", "10.0.0.8"));
        Assert.Equal("198.51.100.4", TermsDocumentRules.ResolveClientIp("nao-e-ip", "198.51.100.4, 10.0.0.2", "10.0.0.8"));
        Assert.Equal("10.0.0.8", TermsDocumentRules.ResolveClientIp(null, "proxy", "10.0.0.8"));
        Assert.Null(TermsDocumentRules.ResolveClientIp(" ", " ", "localhost"));
    }

    [Fact]
    public void Sanitize_RemovesScriptsEventsAndUnsafeLinks()
    {
        var html = TermsDocumentRules.Sanitize(
            "<p onclick=\"alert(1)\">Olá</p><script>alert(1)</script><a href=\"javascript:alert(1)\">clique</a><a href=\"https://ofertio.com.br/legal/termos\">termos</a>");

        Assert.DoesNotContain("script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Olá</p>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://ofertio.com.br/legal/termos\"", html, StringComparison.Ordinal);
        Assert.Equal("Informe o texto do documento.", TermsDocumentRules.ValidateContent("<script>alert(1)</script>"));
    }

    [Fact]
    public void MasterMenu_ExposesTermsOnlyToTheLegalPermission()
    {
        var link = MasterRoutes.Links.Single(item => item.Href == MasterRoutes.Terms);
        Assert.Equal("Termos e privacidade", link.Label);
        Assert.Equal(MasterPermissions.ManageLegal, link.Permission);
        Assert.True(MasterPermissionMatrix.Has(MasterRoles.SuperAdmin, MasterPermissions.ManageLegal));
        Assert.False(MasterPermissionMatrix.Has(MasterRoles.SuporteTecnico, MasterPermissions.ManageLegal));
        Assert.False(MasterPermissionMatrix.Has(MasterRoles.FinanceiroSaaS, MasterPermissions.ManageLegal));
    }
}

public class TermsDocumentServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task Publish_ActivatesTheNextVersionAndAsksForANewAcceptance()
    {
        await using var context = await CreateContextAsync();
        var service = new TermsDocumentService(context);

        var first = await service.PublishAsync(new TermsVersionDto
        {
            DocumentKind = TermsDocumentRules.Terms,
            Content = "<p>Termos da versão nova.</p>"
        });
        Assert.True(first.Status);
        Assert.Equal("1.1", first.Data!.Version);
        Assert.True(first.Data.IsActive);

        var second = await service.PublishAsync(new TermsVersionDto
        {
            DocumentKind = TermsDocumentRules.Terms,
            Content = "<p>Termos revisados.</p>"
        });
        Assert.Equal("1.2", second.Data!.Version);

        var active = await context.TermsVersions.Where(item => item.DocumentKind == TermsDocumentRules.Terms && item.IsActive).ToListAsync();
        Assert.Single(active);
        Assert.Equal("1.2", active[0].Version);

        await service.RecordRegistrationAsync(await context.UserAccounts.SingleAsync(), "203.0.113.10");
        var before = await service.GetStatusAsync(7);
        Assert.False(before.RequiresReacceptance);

        await service.PublishAsync(new TermsVersionDto
        {
            DocumentKind = TermsDocumentRules.Terms,
            Content = "<p>Versão que exige novo aceite.</p>"
        });
        var after = await service.GetStatusAsync(7);
        Assert.True(after.RequiresReacceptance);
        Assert.Equal("1.3", after.ActiveTermsVersion);
    }

    [Fact]
    public async Task Registration_StoresVersionTimestampAndIp()
    {
        await using var context = await CreateContextAsync();
        var service = new TermsDocumentService(context);
        var user = await context.UserAccounts.SingleAsync();

        await service.RecordRegistrationAsync(user, "2001:db8::1");

        Assert.Equal(TermsDocumentRules.StaticVersion, user.TermsAcceptedVersion);
        Assert.NotNull(user.TermsAcceptedAt);
        Assert.Equal("2001:db8::1", user.TermsAcceptedIp);
        Assert.Equal(2, await context.UserTermsAcceptances.CountAsync(row => row.UserId == 7));
    }

    [Fact]
    public async Task ExternalApiConsent_IsSeparateFromTheAccountAcceptance()
    {
        await using var context = await CreateContextAsync();
        var service = new TermsDocumentService(context);
        await service.RecordRegistrationAsync(await context.UserAccounts.SingleAsync(), "203.0.113.10");

        var registered = await service.GetStatusAsync(7);
        Assert.False(registered.RequiresReacceptance);
        Assert.True(registered.RequiresExternalApiConsent);

        var refused = await service.AcceptAsync(7, new UserTermsAcceptanceDto { Accepted = false }, "203.0.113.9");
        Assert.False(refused.Status);
        Assert.Equal(TermsDocumentRules.AcceptanceRequiredMessage, refused.Descricao);

        var accepted = await service.AcceptAsync(
            7,
            new UserTermsAcceptanceDto { Accepted = true, Purpose = TermsDocumentRules.PurposeExternalApi },
            "198.51.100.20");
        Assert.True(accepted.Status);
        Assert.False(accepted.RequiresExternalApiConsent);

        var status = await service.GetStatusAsync(7);
        Assert.False(status.RequiresExternalApiConsent);
        var row = await context.UserTermsAcceptances.SingleAsync(item =>
            item.Purpose == TermsDocumentRules.PurposeExternalApi && item.DocumentKind == TermsDocumentRules.Terms);
        Assert.Equal("198.51.100.20", row.AcceptedIp);
    }

    [Fact]
    public async Task List_CapsThePageAtThirtyAndKeepsAnotherUsersRowsOut()
    {
        await using var context = await CreateContextAsync();
        context.UserAccounts.Add(new UserAccount
        {
            Id = 8,
            TenantId = TenantId,
            Name = "Outra",
            Email = "outra@ofertio.local",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        var now = DateTime.UtcNow;
        for (var index = 0; index < 31; index++)
        {
            context.UserTermsAcceptances.Add(new UserTermsAcceptance
            {
                UserId = 7,
                DocumentKind = TermsDocumentRules.Terms,
                Version = "1." + index,
                AcceptedAt = now.AddMinutes(index),
                Purpose = TermsDocumentRules.PurposeAccount,
                CreatedAt = now
            });
        }

        context.UserTermsAcceptances.Add(new UserTermsAcceptance
        {
            UserId = 8,
            DocumentKind = TermsDocumentRules.Terms,
            Version = "9.9",
            AcceptedAt = now,
            Purpose = TermsDocumentRules.PurposeAccount,
            CreatedAt = now
        });
        await context.SaveChangesAsync();

        var service = new TermsDocumentService(context);
        var page = await service.ListMineAsync(new UserTermsAcceptanceFilter
        {
            UserId = 7,
            Page = 1,
            PageSize = 500
        });

        Assert.Equal(30, page.DataList!.Count);
        Assert.DoesNotContain(page.DataList, item => item.Version == "9.9");
    }

    private static async Task<AppDbContext> CreateContextAsync()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
        context.Tenants.Add(new Tenant
        {
            Id = TenantId,
            Name = "Loja",
            PlanCode = SaasPlanCodes.Starter,
            Status = TenantStatuses.Ativo
        });
        context.UserAccounts.Add(new UserAccount
        {
            Id = 7,
            TenantId = TenantId,
            Name = "Ana",
            Email = "ana@ofertio.local",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return context;
    }
}
