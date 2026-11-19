using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Geo;
using Ofertio.Infrastructure.Services.PublicPages;
using Ofertio.Util.Address;
using Ofertio.Util.Security;
using Xunit;

namespace Ofertio.Tests.Unit.Geo;

/// <summary>A região do visitante é gravada como IDs das tabelas de referência, não mais como texto.</summary>
public sealed class PublicPageEventLocationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ServiceProvider BuildProvider()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var name = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddScoped(_ => new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options,
            encryption.Object,
            new NullCurrentTenantService()));
        return services.BuildServiceProvider();
    }

    private static async Task SeedAsync(AppDbContext context)
    {
        context.Countries.AddRange(
            new Country { Id = 1, IsoCode = "BR", Name = "Brasil" },
            new Country { Id = 2, IsoCode = "PT", Name = "Portugal" });
        context.States.Add(new State { Id = 10, CountryId = 1, Code = "SP", Name = "São Paulo", IbgeCode = 35 });
        context.Cities.Add(new City { Id = 100, StateId = 10, Name = "Campinas", NormalizedName = LocationNameNormalizer.Normalize("Campinas"), IbgeCode = 3509502 });
        await context.SaveChangesAsync();
    }

    private static async Task<PublicPageEvent> RecordAsync(GeoLocation? geo)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedAsync(context);

        var geoService = new Mock<IGeoLocationService>();
        geoService.Setup(item => item.Lookup(It.IsAny<string?>())).Returns(geo);
        using var catalog = new LocationCatalog(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<LocationCatalog>.Instance);
        var service = new PublicPageEventService(context, NullLogger<PublicPageEventService>.Instance, geoService.Object, catalog);

        await service.RecordAsync(TenantId, 7, PublicPageEvent.KindView, new PublicVisitorInfo("187.12.34.56", "Mozilla/5.0"));

        return await context.PublicPageEvents.IgnoreQueryFilters().SingleAsync();
    }

    [Fact]
    public async Task Record_InBrazil_ShouldStoreTheThreeIdsAndNoText()
    {
        var saved = await RecordAsync(new GeoLocation("BR", "São Paulo", "Campinas", "SP"));

        Assert.Equal((1, 10, 100), (saved.CountryId, saved.StateId, saved.CityId));
        Assert.Null(saved.Country);
        Assert.Null(saved.Region);
        Assert.Null(saved.City);
    }

    [Fact]
    public async Task Record_Abroad_ShouldStoreOnlyTheCountry()
    {
        var saved = await RecordAsync(new GeoLocation("PT", "Lisboa", "Lisboa"));

        Assert.Equal(2, saved.CountryId);
        Assert.Null(saved.StateId);
        Assert.Null(saved.CityId);
    }

    [Fact]
    public async Task Record_WithoutLocation_ShouldStillCountTheVisit()
    {
        var saved = await RecordAsync(null);

        Assert.Null(saved.CountryId);
        Assert.Equal(PublicPageEvent.KindView, saved.Kind);
    }
}
