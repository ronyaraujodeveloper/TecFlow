using System.Threading.Tasks;
using Ofertio.Business.Dto;
using Ofertio.Core.Entities;

namespace Ofertio.Infrastructure.Services.Services;

public interface IAnaliseCalculoService
{
    Task<DashboardSummaryDto?> CalculateDashboardStatisticsAsync(int? userId, int? lojaId = null);

    Task<IEnumerable<Metric>> CalculateConsolidatedMetricsAsync(IEnumerable<Metric> inputMetrics);
}
