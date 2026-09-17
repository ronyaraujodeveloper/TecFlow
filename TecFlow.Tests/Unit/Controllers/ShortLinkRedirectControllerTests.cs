using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Controllers;

public class ShortLinkRedirectControllerTests
{
    [Fact]
    public async Task RedirectAsync_ShouldReturnNotFound_WhenCodeIsInvalid()
    {
        var controller = CreateController(new Mock<IShortAffiliateLinkRepository>().Object, new Mock<ILinkClickTelemetryService>().Object);

        var result = await controller.RedirectAsync("ab", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedirectAsync_ShouldEnqueueTelemetryAndRedirect()
    {
        var linkId = Guid.NewGuid();
        var links = new Mock<IShortAffiliateLinkRepository>();
        links.Setup(s => s.GetByShortCodeAsync("abc1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShortAffiliateLink
            {
                AffiliateLinkId = linkId,
                ShortCode = "abc1234",
                DestinationUrl = "https://shopee.com.br/produto",
                OriginalUrl = "https://shopee.com.br/produto",
                PlatformType = MarketplaceType.Shopee
            });

        var telemetry = new Mock<ILinkClickTelemetryService>();
        var controller = CreateController(links.Object, telemetry.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.RedirectAsync("ABC1234", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://shopee.com.br/produto", redirect.Url);
        telemetry.Verify(s => s.EnqueueClickLog(linkId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    private static ShortLinkRedirectController CreateController(
        IShortAffiliateLinkRepository links,
        ILinkClickTelemetryService telemetry) =>
        new(links, telemetry, NullLogger<ShortLinkRedirectController>.Instance);
}
