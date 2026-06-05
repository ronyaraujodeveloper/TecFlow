using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Tests.Unit.Controllers;

public class DevicesControllerTests
{
    [Fact]
    public async Task RegisterAsync_ShouldReturnOk_WhenRegistrationSucceeds()
    {
        var hub = new Mock<INotificationHubService>();
        hub.Setup(h => h.RegisterDeviceAsync(7, It.IsAny<DeviceRegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeviceRegisterResponseDto
            {
                Status = true,
                Descricao = "Dispositivo registado.",
                DeviceRegistrationId = 1
            });

        var controller = new DevicesController(hub.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "7")],
                    authenticationType: "Test"))
            }
        };

        var result = await controller.RegisterAsync(
            new DeviceRegisterDto { DeviceToken = "abc", Platform = "android" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DeviceRegisterResponseDto>(ok.Value);
        Assert.True(dto.Status);
    }
}
