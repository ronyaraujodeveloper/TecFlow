using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Attribution;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Xunit;

namespace Ofertio.Tests.Unit.Controllers;

public sealed class ShortLinkChannelRouteTests
{
    private static string ChannelRegex()
    {
        var template = ShortLinkRedirectController.ChannelRedirectTemplate.Replace("[[", "[").Replace("]]", "]");
        var pattern = RoutePatternFactory.Parse(template);
        var policy = pattern.GetParameter("channel")!.ParameterPolicies.Single().Content!;
        return policy["regex(".Length..^1];
    }

    [Fact]
    public void ChannelRoute_ShouldAcceptExactlyTheLettersOfTheChannelTable()
    {
        var regex = ChannelRegex();

        foreach (var channel in LinkChannelRules.All)
        {
            Assert.Matches(regex, channel.Code.ToString());
        }

        foreach (var letter in "abcdfghjklmnqrsuvxyz0123456789")
        {
            Assert.DoesNotMatch(regex, letter.ToString());
        }

        Assert.DoesNotMatch(regex, "ww");
    }

    [Fact]
    public void ChannelRoute_ShouldHaveThreeSegmentsAndNotCollideWithTheTwoSegmentRoute()
    {
        var channel = RoutePatternFactory.Parse(ShortLinkRedirectController.ChannelRedirectTemplate.Replace("[[", "[").Replace("]]", "]"));
        var store = RoutePatternFactory.Parse(ShortLinkRedirectController.StoreRedirectTemplate.Replace("[[", "[").Replace("]]", "]"));

        Assert.Equal(3, channel.PathSegments.Count);
        Assert.Equal(2, store.PathSegments.Count);
    }

    [Fact]
    public void ProxyPattern_ShouldUseTheSameLetterSetAsTheApi()
    {
        var apiLetters = Regex.Match(ShortLinkRedirectController.ChannelRedirectTemplate, @"\[\[([a-z]+)\]\]").Groups[1].Value;
        var tableLetters = string.Concat(LinkChannelRules.All.Select(channel => channel.Code));
        Assert.Equal(tableLetters, apiLetters);

        // O projeto de testes não referencia a WebUi: lê o fonte da regra de encaminhamento (YARP).
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ofertio.WebUi", "Extensions", "ShortLinkProxyExtensions.cs")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            return;
        }

        var source = File.ReadAllText(Path.Combine(directory.FullName, "Ofertio.WebUi", "Extensions", "ShortLinkProxyExtensions.cs"));
        var proxyLetters = Regex.Match(source, @"ChannelRedirectPattern\s*=\s*""/\{channel:regex\(\^\[([a-z]+)\]\$\)\}").Groups[1].Value;
        Assert.Equal(tableLetters, proxyLetters);
    }

    [Fact]
    public void BlazorPages_ShouldNotUseThreeSegmentRoutesStartingWithAChannelLetter()
    {
        var letters = LinkChannelRules.All.Select(channel => channel.Code.ToString()).ToHashSet();
        var assemblies = new[] { typeof(Ofertio.SharedUi.Extensions.ServiceCollectionExtensions).Assembly };

        var conflicts = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>().Select(route => route.Template))
            .Select(template => template.Trim('/').Split('/'))
            .Where(parts => parts.Length == 3 && letters.Contains(parts[0].ToLowerInvariant()))
            .Select(parts => string.Join('/', parts))
            .ToList();

        Assert.Empty(conflicts);
    }

    private static ShortLinkRedirectController Build(Mock<ILinkClickTelemetryService> telemetry)
    {
        var link = new ShortAffiliateLink
        {
            AffiliateLinkId = Guid.NewGuid(),
            ShortCode = "xh8pmzh",
            DestinationUrl = "https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724",
            OriginalUrl = "https://www.mercadolivre.com.br/p/MLB20621890",
            PlatformType = MarketplaceType.MercadoLivre,
            TenantId = Guid.NewGuid()
        };
        var repository = new Mock<IShortAffiliateLinkRepository>();
        repository.Setup(item => item.GetByShortCodeAsync("xh8pmzh", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var availability = new Mock<ILinkAvailabilityService>();
        availability.Setup(item => item.CheckAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkAvailability(true, null));
        return new ShortLinkRedirectController(repository.Object, telemetry.Object, availability.Object, NullLogger<ShortLinkRedirectController>.Instance);
    }

    [Fact]
    public async Task RedirectWithChannel_ShouldRecordTheChannelFromThePathAndTheGroupKey()
    {
        var telemetry = new Mock<ILinkClickTelemetryService>();
        var controller = Build(telemetry);
        var key = GroupKeyRules.Compute("s", 5, "WhatsApp", "120363@g.us");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Request = { QueryString = new QueryString("?g=" + key) } }
        };

        var result = await controller.RedirectWithChannelAsync("w", "97303724", "xh8pmzh", CancellationToken.None);

        Assert.IsType<RedirectResult>(result);
        telemetry.Verify(item => item.EnqueueClickLog(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            "WhatsApp", key, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task RedirectWithChannel_ShouldNeverStoreARawGroupIdFromTheQuery()
    {
        var telemetry = new Mock<ILinkClickTelemetryService>();
        var controller = Build(telemetry);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Request = { QueryString = new QueryString("?g=120363%40g.us") } }
        };

        await controller.RedirectWithChannelAsync("t", "97303724", "xh8pmzh", CancellationToken.None);

        telemetry.Verify(item => item.EnqueueClickLog(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            "Telegram", null, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task LegacyRedirect_ShouldKeepReadingTheOldTrackingParameters()
    {
        var telemetry = new Mock<ILinkClickTelemetryService>();
        var controller = Build(telemetry);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Request = { QueryString = new QueryString("?tf_src=Telegram&tf_grp=-1001") } }
        };

        var result = await controller.RedirectAsync("97303724", "xh8pmzh", CancellationToken.None);

        Assert.IsType<RedirectResult>(result);
        telemetry.Verify(item => item.EnqueueClickLog(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            "Telegram", "-1001", It.IsAny<string?>()), Times.Once);
    }
}
