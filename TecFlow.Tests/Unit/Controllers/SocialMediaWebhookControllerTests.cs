using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Messaging;
using TecFlow.Business.Messaging;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Controllers;

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
