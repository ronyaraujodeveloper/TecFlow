using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Xunit;

namespace Ofertio.Tests.Unit.Master;

public sealed class LinkAvailabilityRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Ativo")]
    [InlineData("Trial")]
    [InlineData("Inadimplente")]
    [InlineData(null)]
    public void IsServing_AlwaysTrueWhenAccountIsNotBlocked(string? status) =>
        Assert.True(LinkAvailabilityRules.IsServing(status, Now.AddDays(-400), Now.AddDays(-400), Now, 30));

    [Fact]
    public void IsServing_CanceledKeepsServingInsideGraceAndStopsAfter()
    {
        var canceledAt = Now.AddDays(-10);
        Assert.True(LinkAvailabilityRules.IsServing("Cancelado", canceledAt, canceledAt, Now, 30));
        Assert.True(LinkAvailabilityRules.IsServing("Cancelado", canceledAt, canceledAt, canceledAt.AddDays(30).AddSeconds(-1), 30));
        Assert.False(LinkAvailabilityRules.IsServing("Cancelado", canceledAt, canceledAt, canceledAt.AddDays(30), 30));
    }

    [Fact]
    public void IsServing_SuspendedCountsFromStatusChange()
    {
        Assert.True(LinkAvailabilityRules.IsServing("Suspenso", Now.AddDays(-5), null, Now, 30));
        Assert.False(LinkAvailabilityRules.IsServing("Suspenso", Now.AddDays(-31), null, Now, 30));
    }

    [Fact]
    public void IsServing_ZeroGraceBlocksImmediately() =>
        Assert.False(LinkAvailabilityRules.IsServing("Cancelado", Now, Now, Now, 0));

    [Fact]
    public void IsServing_WithoutReferenceDateDoesNotBreakLinks() =>
        Assert.True(LinkAvailabilityRules.IsServing("Cancelado", null, null, Now, 30));

    [Theory]
    [InlineData(null, 30)]
    [InlineData("", 30)]
    [InlineData("abc", 30)]
    [InlineData("-3", 30)]
    [InlineData("15", 15)]
    [InlineData("0", 0)]
    [InlineData("99999", 365)]
    public void ParseGraceDays_ShouldNormalize(string? raw, int expected) =>
        Assert.Equal(expected, LinkAvailabilityRules.ParseGraceDays(raw));

    [Fact]
    public void ServingUntil_IsNullWhenNotBlocked() =>
        Assert.Null(LinkAvailabilityRules.ServingUntil("Ativo", Now, null, 30));

    [Fact]
    public void CanceledEmail_MentionsDeadlineAndNeverOtherAffiliate()
    {
        var mail = TransactionalEmailRules.AccountCanceled("a@b.com", 30, Now.AddDays(30), "https://x/meu-plano");

        Assert.Contains("20/12/2026", mail.TextBody);
        Assert.Contains("seus próprios dados de afiliado", mail.TextBody);
        Assert.Contains("https://x/meu-plano", mail.HtmlBody);
    }
}

public sealed class ShortLinkRedirectAvailabilityTests
{
    private static ShortLinkRedirectController Build(bool serving, out Mock<ILinkClickTelemetryService> telemetry)
    {
        var link = new ShortAffiliateLink
        {
            ShortCode = "abc123",
            DestinationUrl = "https://www.mercadolivre.com.br/p/MLB1?matt_word=CLIENTE",
            TenantId = Guid.NewGuid(),
            IsActive = true
        };
        var repository = new Mock<IShortAffiliateLinkRepository>();
        repository.Setup(item => item.GetByShortCodeAsync("abc123", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var availability = new Mock<ILinkAvailabilityService>();
        availability.Setup(item => item.CheckAsync(link.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkAvailability(serving, null));
        telemetry = new Mock<ILinkClickTelemetryService>();

        return new ShortLinkRedirectController(repository.Object, telemetry.Object, availability.Object, NullLogger<ShortLinkRedirectController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task Redirect_WhenServing_KeepsOwnersAffiliateDestinationUntouched()
    {
        var controller = Build(true, out _);

        var result = await controller.RedirectAsync("loja", "abc123", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://www.mercadolivre.com.br/p/MLB1?matt_word=CLIENTE", redirect.Url);
    }

    [Fact]
    public async Task Redirect_WhenOwnerOutsideGrace_Returns410WithoutRedirectOrClickLog()
    {
        var controller = Build(false, out var telemetry);

        var result = await controller.RedirectAsync("loja", "abc123", CancellationToken.None);

        var page = Assert.IsType<ContentResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, page.StatusCode);
        Assert.Contains(LinkAvailabilityRules.UnavailableLinkTitle, page.Content);
        telemetry.VerifyNoOtherCalls();
    }
}