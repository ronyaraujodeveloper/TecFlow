using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Service.Common;
using Ofertio.Core.Entities;

namespace Ofertio.Tests.Unit.Controllers;

public class CampaignsControllerBudgetTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1000000.01)]
    public async Task CreateAsync_ShouldRejectBudgetOutsideTheAllowedRange(decimal budget)
    {
        var repo = new Mock<ICampaignRepository>();
        var controller = CreateController(repo);

        var result = await controller.CreateAsync(new CampaignDto { Name = "E2E", Budget = budget });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var body = Assert.IsType<CampaignResponseDto>(badRequest.Value);
        Assert.False(body.Status);
        Assert.Equal(MoneyAmountRules.InvalidBudgetMessage, body.Descricao);
        repo.Verify(r => r.AddAsync(It.IsAny<Campaign>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistAZeroBudget()
    {
        var repo = new Mock<ICampaignRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Campaign>())).Returns(Task.CompletedTask);
        var controller = CreateController(repo);

        var result = await controller.CreateAsync(new CampaignDto { Name = "E2E", Budget = 0m });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        repo.Verify(r => r.AddAsync(It.Is<Campaign>(c => c.Budget == 0m && c.OwnerId == 7)), Times.Once);
    }

    private static CampaignsController CreateController(Mock<ICampaignRepository> repo)
    {
        var controller = new CampaignsController(repo.Object, NullLogger<CampaignsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")]))
            }
        };
        return controller;
    }
}
