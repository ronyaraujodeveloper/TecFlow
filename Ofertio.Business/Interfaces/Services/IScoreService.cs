using System.Threading.Tasks;
using Ofertio.Core.Entities;

namespace Ofertio.Business.Interfaces.Services
{
    public interface IScoreService
    {
        /// <summary>
        /// Calcula um score de 0 a 100 baseado na performance histórica e volume de vendas.
        /// </summary>
        decimal CalcularScoreViral(Product product, Metric metric);
    }
}