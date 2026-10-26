using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Business.Service.PublicPages;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.PublicPages;
using Ofertio.Util.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Ofertio.Tests.Unit.PublicPages;

public class PublicConverterRulesTests
{
    [Fact]
    public void ResolveBySlug_ShouldAcceptInactiveSlug()
    {
        var code = Guid.NewGuid();
        var pages = new List<PublicConverterPage>
        {
            new()
            {
                PublicCode = code,
                UserId = 7,
                Slug = "loja-antiga",
                IsActive = false
            },
            new()
            {
                PublicCode = code,
                UserId = 7,
                Slug = "loja-nova",
                IsActive = true
            }
        };

        var resolved = PublicConverterRules.ResolveBySlug(pages, "loja-antiga");

        Assert.NotNull(resolved);
        Assert.False(resolved!.IsActive);
        Assert.Equal("loja-antiga", resolved.Slug);
        Assert.Equal(code, resolved.PublicCode);
    }

    [Fact]
    public void DistinctActivePlatforms_ShouldKeepOneLogoPerBrand()
    {
        var accounts = new List<MarketplaceAccount>
        {
            new() { Id = 1, IsActive = true, MarketplaceType = MarketplaceType.Shopee },
            new() { Id = 2, IsActive = true, MarketplaceType = MarketplaceType.Shopee },
            new() { Id = 3, IsActive = true, MarketplaceType = MarketplaceType.Amazon },
            new() { Id = 4, IsActive = false, MarketplaceType = MarketplaceType.MagazineLuiza }
        };

        var distinct = PublicConverterRules.DistinctActivePlatforms(accounts);

        Assert.Equal(2, distinct.Count);
        Assert.Contains(distinct, item => item.MarketplaceType == MarketplaceType.Shopee && item.Id == 1);
        Assert.Contains(distinct, item => item.MarketplaceType == MarketplaceType.Amazon);
        Assert.DoesNotContain(distinct, item => item.MarketplaceType == MarketplaceType.MagazineLuiza);
    }

    [Fact]
    public void FirstActiveForPlatform_ShouldUseFirstOrDefaultById()
    {
        var accounts = new List<MarketplaceAccount>
        {
            new() { Id = 20, IsActive = true, MarketplaceType = MarketplaceType.Shopee },
            new() { Id = 5, IsActive = true, MarketplaceType = MarketplaceType.Shopee },
            new() { Id = 8, IsActive = true, MarketplaceType = MarketplaceType.Amazon }
        };

        var first = PublicConverterRules.FirstActiveForPlatform(accounts, MarketplaceType.Shopee);

        Assert.NotNull(first);
        Assert.Equal(5, first!.Id);
    }

    [Fact]
    public void CreateVersionedSlug_ShouldDeactivateOldAndKeepPublicCode()
    {
        var current = new PublicConverterPage
        {
            PublicCode = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            UserId = 12,
            TenantId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Slug = "antigo",
            IsActive = true
        };

        var created = PublicConverterRules.CreateVersionedSlug(current, "Novo Slug!");

        Assert.False(current.IsActive);
        Assert.True(created.IsActive);
        Assert.Equal(current.PublicCode, created.PublicCode);
        Assert.Equal(12, created.UserId);
        Assert.Equal("novo-slug", created.Slug);
    }

    [Fact]
    public void CreateRandomSlug_ShouldStayBetweenSixAndTenCharactersAndNotRepeat()
    {
        var slugs = Enumerable.Range(0, 40).Select(_ => PublicConverterRules.CreateRandomSlug()).ToList();

        Assert.All(slugs, slug => Assert.True(PublicConverterRules.IsRandomSlug(slug)));
        Assert.Equal(slugs.Count, slugs.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(slugs, slug => slug.StartsWith("afiliado", StringComparison.Ordinal));
    }
}

public class PublicConverterControllerTests
{
    [Fact]
    public async Task GetBySlugAsync_ShouldReturnInactivePage()
    {
        var pages = new Mock<IPublicConverterPageService>();
        pages.Setup(service => service.ResolveBySlugAsync("slug-velho", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicConverterPageDto
            {
                Slug = "slug-velho",
                IsActive = false,
                PublicCode = Guid.NewGuid(),
                ConnectedPlatforms = [MarketplaceType.Amazon]
            });

        var controller = new PublicConverterController(pages.Object);
        var action = await controller.GetBySlugAsync("slug-velho", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<PublicConverterPageResponseDto>(ok.Value);
        Assert.True(body.Status);
        Assert.False(body.Data!.IsActive);
        Assert.Equal("slug-velho", body.Data.Slug);
    }
}

public class PublicConverterPageServiceTests
{
    [Fact]
    public async Task ListMine_ShouldCreateAUniqueRandomSlugAndKeepCustomizationLocked()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var first = await service.ListMineAsync(1);
        var second = await service.ListMineAsync(2);

        Assert.True(first.Status);
        Assert.False(first.CanCustomizeSlug);
        Assert.True(PublicConverterRules.IsRandomSlug(first.Data!.Slug));
        Assert.True(PublicConverterRules.IsRandomSlug(second.Data!.Slug));
        Assert.NotEqual(first.Data.Slug, second.Data.Slug);
    }

    [Fact]
    public async Task ChangeSlug_ShouldRejectDuplicateAndAllowThePlanThatWasReleased()
    {
        await using var context = CreateContext();
        var plan = new SaasPlan { Code = SaasPlanCodes.Pro, Name = "Pro", IsActive = true };
        var menu = new MenuItem { Key = DefaultMenuCatalog.CustomPublicSlug, Title = "Slug personalizado", IsActive = true };
        context.SaasPlans.Add(plan);
        context.MenuItems.Add(menu);
        await context.SaveChangesAsync();
        context.PlanMenuPermissions.Add(new PlanMenuPermission { PlanId = plan.Id, MenuItemId = menu.Id, IsVisible = true });
        var tenant = new Tenant { Name = "Loja Pro", PlanCode = SaasPlanCodes.Pro };
        context.Tenants.Add(tenant);
        context.UserAccounts.Add(new UserAccount
        {
            Id = 7,
            Name = "Pro",
            Email = "pro@ofertio.com",
            PasswordHash = "h",
            TenantId = tenant.Id,
            Plan = SaasPlanCodes.Pro
        });
        context.PublicConverterPages.Add(new PublicConverterPage
        {
            UserId = 1,
            Slug = "loja-da-ana",
            IsActive = true,
            PublicCode = Guid.NewGuid()
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var duplicate = await service.ChangeSlugAsync(7, "loja-da-ana");
        var custom = await service.ChangeSlugAsync(7, "minha-loja");

        Assert.False(duplicate.Status);
        Assert.Contains("já está em uso", duplicate.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.True(custom.Status);
        Assert.Equal("minha-loja", custom.Data!.Slug);
    }

    [Fact]
    public async Task ChangeSlug_ShouldStayBlockedWhenThePlanPermissionIsOff()
    {
        await using var context = CreateContext();
        var plan = new SaasPlan { Code = SaasPlanCodes.Starter, Name = "Starter", IsActive = true };
        var menu = new MenuItem { Key = DefaultMenuCatalog.CustomPublicSlug, Title = "Slug personalizado", IsActive = true };
        context.SaasPlans.Add(plan);
        context.MenuItems.Add(menu);
        await context.SaveChangesAsync();
        context.PlanMenuPermissions.Add(new PlanMenuPermission { PlanId = plan.Id, MenuItemId = menu.Id, IsVisible = false });
        var tenant = new Tenant { Name = "Loja", PlanCode = SaasPlanCodes.Starter };
        context.Tenants.Add(tenant);
        context.UserAccounts.Add(new UserAccount
        {
            Id = 4,
            Name = "Starter",
            Email = "starter@ofertio.com",
            PasswordHash = "h",
            TenantId = tenant.Id,
            Plan = SaasPlanCodes.Starter
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var created = await service.ListMineAsync(4);

        var blocked = await service.ChangeSlugAsync(4, "slug-livre");

        Assert.False(blocked.Status);
        Assert.Contains("não libera", blocked.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(created.Data!.Slug, (await service.ListMineAsync(4)).Data!.Slug);
    }

    [Fact]
    public async Task ChangeSlug_MasterShouldFollowOnlyTheActiveColumn()
    {
        await using var context = CreateContext();
        SeedMaster(context, slugActive: true);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        Assert.True((await service.ListMineAsync(9)).CanCustomizeSlug);
        var changed = await service.ChangeSlugAsync(9, "vitrine-master");

        Assert.True(changed.Status);
        Assert.Equal("vitrine-master", changed.Data!.Slug);
    }

    [Fact]
    public async Task ChangeSlug_MasterShouldBeBlockedWhenTheRowIsInactive()
    {
        await using var context = CreateContext();
        SeedMaster(context, slugActive: false);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var created = await service.ListMineAsync(9);
        var blocked = await service.ChangeSlugAsync(9, "vitrine-master");

        Assert.False(created.CanCustomizeSlug);
        Assert.False(blocked.Status);
        Assert.Contains("não libera", blocked.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    private static void SeedMaster(AppDbContext context, bool slugActive)
    {
        context.MenuItems.Add(new MenuItem { Key = DefaultMenuCatalog.CustomPublicSlug, Title = "Slug personalizado", IsActive = slugActive });
        context.UserAccounts.Add(new UserAccount
        {
            Id = 9,
            Name = "Master",
            Email = "master@ofertio.com",
            PasswordHash = "h",
            TenantId = Guid.NewGuid(),
            Plan = "Master",
            Role = MasterRoles.SuperAdmin
        });
    }

    private static PublicConverterPageService CreateService(AppDbContext context) =>
        new(
            context,
            Moq.Mock.Of<IMarketplaceAccountRepository>(),
            null!,
            null!,
            NullLogger<PublicConverterPageService>.Instance);

    private static AppDbContext CreateContext()
    {
        var encryption = new Moq.Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }
}
