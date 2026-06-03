using Microsoft.Extensions.Logging;
using Moq;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Service.Application;
using Xunit;

namespace TecFlow.Tests.Services;

public class CampaignsApplicationServiceTests
{
    private readonly Mock<ICampaignRepository> _mockCampaignRepository;
    private readonly Mock<ILogger<CampaignsApplicationService>> _mockLogger;
    private readonly CampaignsApplicationService _campaignsApplicationService;

    public CampaignsApplicationServiceTests()
    {
        _mockCampaignRepository = new Mock<ICampaignRepository>();
        _mockLogger = new Mock<ILogger<CampaignsApplicationService>>();

        _campaignsApplicationService = new CampaignsApplicationService(
            _mockCampaignRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallRepositoryWithCorrectId()
    {
        const int idToDelete = 1;
        _mockCampaignRepository
            .Setup(repo => repo.DeleteAsync(idToDelete))
            .Returns(Task.CompletedTask);

        await _campaignsApplicationService.DeleteAsync(idToDelete);

        _mockCampaignRepository.Verify(repo => repo.DeleteAsync(idToDelete), Times.Once);
    }
}
