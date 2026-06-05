using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Service
{
    public class ScoreService : IScoreService
    {
        public decimal CalcularScoreViral(Product product, Metric metric)
        {
            if (metric.Views == 0) return 0;

            decimal pesoVendas = (decimal)metric.Sales / (metric.Clicks > 0 ? metric.Clicks : 1) * 50;
            decimal pesoCtr = (decimal)metric.Ctr * 0.3m;
            decimal pesoRating = (decimal)product.Rating * 4m;

            decimal score = pesoVendas + pesoCtr + pesoRating;

            return Math.Clamp(score, 0, 100);
        }
    }
}