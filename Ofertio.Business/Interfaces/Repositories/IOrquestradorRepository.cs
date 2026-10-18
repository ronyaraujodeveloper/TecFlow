namespace Ofertio.Business.Interfaces.Repositories
{
    public interface IOrquestradorRepository
    {
        Task ExecuteFullPipelineAsync();
        Task ExecuteIncrementalPipelineAsync();
        Task ExecuteProcessingAsync();
        Task ExecuteIncrementalPipelineInternalAsync();
    }
}
