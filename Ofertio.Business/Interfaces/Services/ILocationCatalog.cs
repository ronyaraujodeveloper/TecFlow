using Ofertio.Business.Service.Geo;

namespace Ofertio.Business.Interfaces.Services;

/// <summary>
/// Tabelas de país, estado e cidade em memória. Carrega do banco uma vez e reaproveita,
/// para que cada clique seja resolvido sem consulta.
/// </summary>
public interface ILocationCatalog
{
    /// <summary>Fotografia das tabelas. Se o banco falhar, devolve vazia e tenta de novo mais tarde.</summary>
    Task<LocationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>IDs de país, estado e cidade para a região estimada pelo IP.</summary>
    Task<LocationIds> ResolveAsync(GeoLocation? location, CancellationToken cancellationToken = default);
}
