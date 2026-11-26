using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Messaging;
using Ofertio.Business.Messaging;
using Ofertio.Core.Enums;

namespace Ofertio.Tests.Unit.Controllers;

public class SocialMediaWebhookControllerTests
{
    [Fact]
    public async Task ReceiveCommentAsync_ShouldReturnAccepted_WhenPayloadIsValid()
    {
        var publisher = new Mock<IEngagementEventPublisher>();
        SocialMediaCommentReceivedEvent? published = null;
        publisher
            .Setup(p => p.PublishCommentReceivedAsync(It.IsAny<SocialMediaCommentReceivedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SocialMediaCommentReceivedEvent, CancellationToken>((e, _) => published = e)
            .Returns(Task.CompletedTask);

        var controller = new SocialMediaWebhookController(publisher.Object, NullLogger<SocialMediaWebhookController>.Instance);
        var request = new SocialMediaCommentWebhookRequest
        {
            PostId = "post-99",
            Platform = SocialMediaType.Instagram,
            Username = "@fan",
            CommentText = "eu quero o link"
        };

        var result = await controller.ReceiveCommentAsync(request, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(published);
        Assert.Equal("post-99", published!.PostId);
        Assert.Equal(SocialMediaType.Instagram, published.Platform);
        publisher.Verify(
            p => p.PublishCommentReceivedAsync(It.IsAny<SocialMediaCommentReceivedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Controller_ExigeSegredoDeWebhook()
    {
        Assert.NotEmpty(typeof(SocialMediaWebhookController)
            .GetCustomAttributes(typeof(Ofertio.API.Security.WebhookSecurityAttribute), inherit: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("segredo-errado")]
    public async Task FiltroDeWebhook_SemSegredoCorreto_RespondeUnauthorized_ENaoPublica(string? header)
    {
        var filter = new Ofertio.API.Security.WebhookSecurityFilter(
            Microsoft.Extensions.Options.Options.Create(new Ofertio.Business.Security.WebhookSecurityOptions { Secret = "segredo-certo" }));
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        if (header is not null)
        {
            httpContext.Request.Headers[Ofertio.Business.Security.WebhookSecurityOptions.HeaderName] = header;
        }

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var executing = new Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
        var actionRan = false;

        await filter.OnActionExecutionAsync(executing, () =>
        {
            actionRan = true;
            return Task.FromResult(new Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext(actionContext, [], new object()));
        });

        Assert.IsType<UnauthorizedResult>(executing.Result);
        Assert.False(actionRan);
    }

    [Fact]
    public async Task ReceiveCommentAsync_ShouldReturnBadRequest_WhenRequiredFieldsMissing()
    {
        var controller = new SocialMediaWebhookController(
            new Mock<IEngagementEventPublisher>().Object,
            NullLogger<SocialMediaWebhookController>.Instance);

        var result = await controller.ReceiveCommentAsync(
            new SocialMediaCommentWebhookRequest { PostId = "", CommentText = "x", Username = "y" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
