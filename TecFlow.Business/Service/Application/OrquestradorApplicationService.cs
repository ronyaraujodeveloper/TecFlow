using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Pipelines;

namespace TecFlow.Business.Service.Application
{
    public class OrquestradorApplicationService : IOrquestradorService
    {
        private readonly ColetaDadosPipeline _coletaPipeline;
        private readonly GeracaoConteudoPipeline _geracaoPipeline;
        private readonly PublicacaoPipeline _publicacaoPipeline;
        private readonly ILogger<OrquestradorApplicationService> _logger;

        private readonly object _statusLock = new();
        private DateTimeOffset? _lastRunAt;
        private string _lastRunStatus = "Nunca executado";
        private string? _lastRunMessage;
        private string? _lastRunError;
        private bool _isRunning;

        public OrquestradorApplicationService(
            ColetaDadosPipeline coletaPipeline,
            GeracaoConteudoPipeline geracaoPipeline,
            PublicacaoPipeline publicacaoPipeline,
            ILogger<OrquestradorApplicationService> logger)
        {
            _coletaPipeline = coletaPipeline;
            _geracaoPipeline = geracaoPipeline;
            _publicacaoPipeline = publicacaoPipeline;
            _logger = logger;
        }

        public Task<string> GetExecutionStatusAsync()
        {
            lock (_statusLock)
            {
                return Task.FromResult(
                    $"Status: {_lastRunStatus}; Ultima execução: {_lastRunAt:u}; Mensagem: {_lastRunMessage ?? "-"}; Erro: {_lastRunError ?? "-"}; Em execução: {_isRunning}");
            }
        }

        public Task ExecuteOrchestrationAsync() => ExecuteFullPipelineAsync();

        public async Task ExecuteFullPipelineAsync()
        {
            lock (_statusLock)
            {
                _isRunning = true;
                _lastRunAt = DateTimeOffset.UtcNow;
                _lastRunStatus = "Executando";
                _lastRunMessage = null;
                _lastRunError = null;
            }

            try
            {
                _logger.LogInformation("Orquestrador: iniciando execução do pipeline completo...");

                var produtos = await _coletaPipeline.ExecutarAsync();
                if (produtos.Count == 0)
                {
                    AtualizarStatus("Concluído", "Nenhum produto encontrado.", null);
                    return;
                }

                var count = 0;
                foreach (var produto in produtos)
                {
                    await _geracaoPipeline.AtualizarDescricaoProdutoAsync(produto);
                    await _publicacaoPipeline.PublicarProdutoComScoreAsync(produto);
                    count++;
                }

                await _geracaoPipeline.GerarConteudoAdicionalAsync(produtos[0]);
                await _publicacaoPipeline.PublicarEmPlataformasAsync(produtos);

                AtualizarStatus("Concluído", $"Pipeline executado para {count} produtos.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orquestrador: falha ao executar o pipeline completo.");
                AtualizarStatus("Falha", "Erro durante a execução do pipeline.", ex.Message);
                throw;
            }
            finally
            {
                lock (_statusLock)
                {
                    _isRunning = false;
                }
            }
        }

        private void AtualizarStatus(string status, string message, string? error)
        {
            lock (_statusLock)
            {
                _lastRunStatus = status;
                _lastRunMessage = message;
                _lastRunError = error;
            }
        }
    }
}
