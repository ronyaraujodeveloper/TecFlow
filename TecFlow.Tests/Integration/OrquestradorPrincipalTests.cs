using Moq;
using System;
using System.Threading.Tasks;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Orquestrador;
using Xunit;

namespace TecFlow.Tests.Integration
{
    public class OrquestradorPrincipalTests
    {
        private readonly Mock<IOrquestradorService> _mockOrchestratorService;
        private readonly OrquestradorPrincipal _orchestrator;

        public OrquestradorPrincipalTests()
        {
            _mockOrchestratorService = new Mock<IOrquestradorService>();
            _orchestrator = new OrquestradorPrincipal(_mockOrchestratorService.Object);
        }

        [Fact]
        public async Task ExecuteFullPipelineAsync_ShouldOrchestrateSuccessfully()
        {
            _mockOrchestratorService
                .Setup(o => o.ExecuteFullPipelineAsync())
                .Returns(Task.CompletedTask);

            await _orchestrator.ExecuteFullPipelineAsync();

            _mockOrchestratorService.Verify(
                o => o.ExecuteFullPipelineAsync(),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteFullPipelineAsync_ShouldThrowWhenServiceFails()
        {
            _mockOrchestratorService
                .Setup(o => o.ExecuteFullPipelineAsync())
                .ThrowsAsync(new InvalidOperationException("Erro simulado no orquestrador"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orchestrator.ExecuteFullPipelineAsync());

            _mockOrchestratorService.Verify(
                o => o.ExecuteFullPipelineAsync(),
                Times.Once);
        }
    }
}
