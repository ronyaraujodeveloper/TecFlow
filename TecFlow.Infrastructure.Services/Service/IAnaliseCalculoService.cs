using System.Threading.Tasks;
using TecFlow.Business.Dto;
using TecFlow.Core.Entities;

namespace TecFlow.Infrastructure.Services.Services;

public interface IAnaliseCalculoService
{
    Task<DashboardSummaryDto?> CalculateDashboardStatisticsAsync(int? userId);

    Task<IEnumerable<Metric>> CalculateConsolidatedMetricsAsync(IEnumerable<Metric> inputMetrics);
}
