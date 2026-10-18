using System.Threading.Tasks;

namespace Ofertio.Business.Interfaces.Services
{
    public interface IOrquestradorService
    {
        Task ExecuteOrchestrationAsync();
        Task<string> GetExecutionStatusAsync();
        Task ExecuteFullPipelineAsync();
    }
}
