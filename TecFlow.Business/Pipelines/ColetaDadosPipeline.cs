using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Business.Pipelines
{
    public class ColetaDadosPipeline
    {
        private readonly IDataService _dataService;
        private readonly ILogger<ColetaDadosPipeline> _logger;

        public ColetaDadosPipeline(IDataService dataService, ILogger<ColetaDadosPipeline> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Product>> ExecutarAsync()
        {
            var produtos = await _dataService.GetAllAsync<Product>();
            if (produtos == null || !produtos.Any())
            {
                _logger.LogWarning("Coleta: nenhum produto encontrado para processamento.");
                return new List<Product>();
            }

            _logger.LogInformation("Coleta: {Count} produto(s) carregado(s).", produtos.Count());
            return produtos.ToList();
        }
    }
}
