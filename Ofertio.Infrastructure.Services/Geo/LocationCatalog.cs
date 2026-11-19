using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Geo;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Geo;

/// <summary>
/// Carrega países, estados e cidades na primeira vez que alguém precisa e guarda em memória enquanto o processo vive
/// (são tabelas de referência, mudam só por migration). Se a carga falhar, devolve vazio e tenta de novo após alguns minutos.
/// </summary>
public sealed class LocationCatalog : ILocationCatalog, IDisposable
{
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LocationCatalog> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LocationSnapshot? _snapshot;
    private DateTime _retryAtUtc = DateTime.MinValue;

    public LocationCatalog(IServiceScopeFactory scopeFactory, ILogger<LocationCatalog> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<LocationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_snapshot is { } loaded)
        {
            return loaded;
        }

        if (DateTime.UtcNow < _retryAtUtc)
        {
            return LocationSnapshot.Empty;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_snapshot is { } raced)
            {
                return raced;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var countries = await db.Countries.AsNoTracking().ToListAsync(cancellationToken);
            var states = await db.States.AsNoTracking().ToListAsync(cancellationToken);
            var cities = await db.Cities.AsNoTracking().ToListAsync(cancellationToken);

            var snapshot = new LocationSnapshot(countries, states, cities);
            if (snapshot.IsEmpty)
            {
                // Tabelas ainda vazias (migration não aplicada): não guarda, para pegar os dados quando chegarem.
                _logger.LogWarning("Tabelas de país, estado e cidade vazias. A região dos cliques ficará em branco até a carga.");
                _retryAtUtc = DateTime.UtcNow.Add(RetryAfterFailure);
                return snapshot;
            }

            _snapshot = snapshot;
            return snapshot;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Não foi possível carregar as tabelas de país, estado e cidade. Nova tentativa em {Minutes} minutos.", RetryAfterFailure.TotalMinutes);
            _retryAtUtc = DateTime.UtcNow.Add(RetryAfterFailure);
            return LocationSnapshot.Empty;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocationIds> ResolveAsync(GeoLocation? location, CancellationToken cancellationToken = default)
    {
        if (location is null || location.IsEmpty)
        {
            return LocationIds.Empty;
        }

        var snapshot = await GetSnapshotAsync(cancellationToken);
        return LocationMatchRules.Resolve(snapshot, location);
    }

    public void Dispose() => _gate.Dispose();
}
