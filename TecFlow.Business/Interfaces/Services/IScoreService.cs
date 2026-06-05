using System.Threading.Tasks;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services
{
    public interface IScoreService
    {
        /// <summary>
        /// Calcula um score de 0 a 100 baseado na performance histórica e volume de vendas.
        /// </summary>
        decimal CalcularScoreViral(Product product, Metric metric);
    }
}