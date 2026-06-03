namespace TecFlow.Util.Address;

public interface ICepService
{
    Task<CepResultDto?> SearchCepAsync(string cep, CancellationToken cancellationToken = default);
}
