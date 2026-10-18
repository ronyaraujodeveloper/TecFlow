using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Core.Entities;
using Ofertio.Database.Filter;

namespace Ofertio.Tests.Unit.Controllers;

public class MetricsControllerLojaScopeTests
{
    [Fact]
    public async Task GetByFilterAsync_ShouldForwardLojaIdToRepository()
    {
        var repository = new Mock<IMetricRepository>();
        repository.Setup(r => r.GetByOwnerIdAsync(7, 12))
            .ReturnsAsync(Array.Empty<Metric>());

        var controller = new MetricsController(repository.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "7")],
                    authenticationType: "Test"))
            }
        };

        var action = await controller.GetByFilterAsync(new MetricFilter { LojaId = 12 });

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<MetricResponseDto>(ok.Value);
        Assert.True(envelope.Status);
        repository.Verify(r => r.GetByOwnerIdAsync(7, 12), Times.Once);
    }
}
