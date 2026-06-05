// Arquivo: TecFlow.Core/Services/AnaliseCalculoService.cs
// Implementando o m�todo faltante e corrigindo a refer�ncia a Users.

using Microsoft.EntityFrameworkCore; // Para .CountAsync(), .ToListAsync()
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq; // Para .CountAsync() e outros Linq
using System.Threading.Tasks;
using TecFlow.Core.Entities; // Para User, Metrica, etc.
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;
using TecFlow.Business.Dto;

namespace TecFlow.Infrastructure.Services.Services
{
    // --- VERIFIQUE O NAMESPACE DESTA CLASSE ---
    // Se for diferente, ajuste os 'using' statements onde ela � referenciada.

    public class AnaliseCalculoService : IAnaliseCalculoService
    {
        private readonly ILogger<AnaliseCalculoService> _logger;
        private readonly AppDbContext _dbContext;
        // private readonly IMetricRepository _metricRepository; // Se Metric for gerenciada por um repo

        public AnaliseCalculoService(
            ILogger<AnaliseCalculoService> logger,
            AppDbContext dbContext
            // , IMetricRepository metricaRepository
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            // _metricRepository = metricaRepository ?? throw new ArgumentNullException(nameof(metricaRepository));

            // --- CORRE��O CR�TICA: ENTIDADE 'USERS' NO AppDbContext ---
            // Se a entidade 'Users' n�o estiver definida corretamente no models do AppDbContext
            // ou se o DbContext n�o carrega as entidades relevantes, o erro "'AppDbContext' does not contain a definition for 'Users'"
            // ocorrer�. Verifique o arquivo AppDbContext.cs para garantir que 'Users' � um DbSet<User>.
            // Se for um DbSet<ApplicationUser>, o acesso seria _dbContext.ApplicationUsers.
            // Assumindo um DbSet<User> chamado 'Users'.
        }

        public async Task<DashboardSummaryDto?> CalculateDashboardStatisticsAsync(int? userId)
        {
            var metrics = await _dbContext.Metrics
                .Where(m => userId == null || m.OwnerId == userId)
                .ToListAsync();

            if (!metrics.Any()) return new DashboardSummaryDto();

            var totalInvestment = metrics.Sum(m => m.Investment);
            var totalRevenue = metrics.Sum(m => m.Revenue);
            var totalClicks = metrics.Sum(m => m.Clicks);
            var totalViews = metrics.Sum(m => m.Views);
            var totalSales = metrics.Sum(m => m.Sales);

            return new DashboardSummaryDto
            {
                TotalViews = totalViews,
                TotalClicks = totalClicks,
                TotalSales = totalSales,
                TotalInvestment = totalInvestment,
                TotalRevenue = totalRevenue,
                TotalProfit = totalRevenue - totalInvestment,
                AverageCtr = totalViews > 0 ? ((double)totalClicks / totalViews) * 100 : 0,
                AverageConversion = totalClicks > 0 ? ((double)totalSales / totalClicks) * 100 : 0,
                AverageRoi = totalInvestment > 0 ? ((totalRevenue - totalInvestment) / totalInvestment) * 100 : 0,
                AverageCac = totalSales > 0 ? totalInvestment / totalSales : 0
            };
        }

        public async Task<IEnumerable<Metric>> CalculateConsolidatedMetricsAsync(IEnumerable<Metric> inputMetrics)
        {
            _logger.LogInformation("Iniciando consolida��o de {Count} m�tricas.", inputMetrics?.Count() ?? 0);

            if (inputMetrics == null || !inputMetrics.Any())
                return Enumerable.Empty<Metric>();

            try
            {
                // Agrupa por CampanhaId para retornar uma m�dia/soma consolidada (Exemplo de l�gica)
                var consolidadas = inputMetrics
                    .GroupBy(m => m.CampaignId)
                    .Select(g => new Metric
                    {
                        CampaignId = g.Key,
                        Views = g.Sum(x => x.Views),
                        Clicks = g.Sum(x => x.Clicks),
                        Sales = g.Sum(x => x.Sales),
                        Investment = g.Sum(x => x.Investment),
                        Revenue = g.Sum(x => x.Revenue)
                    });

                return await Task.FromResult(consolidadas.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consolidar m�tricas.");
                throw;
            }
        }
    }
}