using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.PublicPages;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace TecFlow.Tests.Unit.PublicPages;

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
