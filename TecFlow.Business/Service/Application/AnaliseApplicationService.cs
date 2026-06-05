using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Business.Service.Application
{
    public class AnaliseApplicationService
    {
        private readonly IAnaliseService _analiseService;
        private readonly ILogger<AnaliseApplicationService> _logger;

        public AnaliseApplicationService(
            IAnaliseService analiseService,
            ILogger<AnaliseApplicationService> logger)
        {
            _analiseService = analiseService;
            _logger = logger;
        }
        public async Task<IEnumerable<object>> RunAnalisesAsync()
        {
            _logger.LogInformation("Executando análises via AnaliseApplicationService.");
            return await Task.FromResult<IEnumerable<object>>(new List<object>());
        }
    }
}