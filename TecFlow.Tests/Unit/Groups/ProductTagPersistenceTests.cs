using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Groups;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Groups;

public class ProductTagPersistenceTests
{
    [Fact]
    public async Task Seed_ShouldCreateCatalogTags()
    {
        await using var context = CreateDbContext();

        var slugs = await context.ProductTags.OrderBy(tag => tag.Id).Select(tag => tag.Slug).ToListAsync();

        Assert.Equal(ProductTagCatalog.Seed.Select(seed => seed.Slug), slugs);
    }

    [Fact]
    public async Task SaveValidatedOfferAsync_ShouldAttachTagsFromMessage_AndFeedFilterByTag()
    {
        await using var context = CreateDbContext();
        var service = new GroupCapturedMessagesService(context, new StructuredOfferParserService(), null!, null!, NullLogger<GroupCapturedMessagesService>.Instance);

        await service.SaveValidatedOfferAsync(NewOffer("1", "Mouse Gamer Logitech G203", "https://produto.mercadolivre.com.br/MLB-1"));
        await service.SaveValidatedOfferAsync(NewOffer("2", "Panela de Pressão Elétrica", "https://produto.mercadolivre.com.br/MLB-2"));

        const int gamerId = 1;
        var gamer = await context.GroupCapturedMessages
            .Where(offer => offer.Tags.Any(tag => tag.Id == gamerId))
            .Select(offer => offer.ExternalMessageId)
            .ToListAsync();

        Assert.Equal(["1"], gamer);
        var panela = await context.GroupCapturedMessages.Include(offer => offer.Tags).SingleAsync(offer => offer.ExternalMessageId == "2");
        Assert.Equal([ProductTagCatalog.CasaCozinha], panela.Tags.Select(tag => tag.Slug));
    }

    [Fact]
    public async Task BackfillUntaggedOffersAsync_ShouldTagLegacyOffersOnce()
    {
        await using var context = CreateDbContext();
        context.GroupCapturedMessages.AddRange(
            NewOffer("a", "Notebook Acer Aspire 5", "https://a"),
            NewOffer("b", "Ração para cães 15kg", "https://b"),
            NewOffer("c", "Cupom geral", "https://c", OfferType.Coupon));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new ProductTagBackfillService(context, NullLogger<ProductTagBackfillService>.Instance);
        var first = await service.BackfillUntaggedOffersAsync();
        var second = await service.BackfillUntaggedOffersAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(6, await context.ProductTags.CountAsync());
        var notebook = await context.GroupCapturedMessages.Include(offer => offer.Tags).SingleAsync(offer => offer.ExternalMessageId == "a");
        Assert.Equal([ProductTagCatalog.Informatica], notebook.Tags.Select(tag => tag.Slug));
    }

    private static GroupCapturedMessage NewOffer(string externalId, string name, string url, OfferType type = OfferType.Product) =>
        new()
        {
            UserId = 7,
            Channel = "Telegram",
            GroupKey = "Telegram:1",
            GroupName = "Grupo",
            ExternalMessageId = externalId,
            RawText = $"{name}\nR$ 99,90\n{url}",
            ProductName = name,
            OriginalUrl = url,
            HasDirectProductUrl = true,
            Type = type
        };

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options, encryption.Object, new TecFlow.Database.MultiTenancy.NullCurrentTenantService());
        context.Database.EnsureCreated();
        return context;
    }
}
