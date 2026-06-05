using System.Threading.Tasks;

namespace TecFlow.Business.Interfaces.Services
{
    public interface IOrquestradorService
    {
        Task ExecuteOrchestrationAsync();
        Task<string> GetExecutionStatusAsync();
        Task ExecuteFullPipelineAsync();
    }
}
