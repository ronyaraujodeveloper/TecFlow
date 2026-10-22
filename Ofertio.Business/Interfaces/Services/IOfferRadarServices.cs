using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

public interface IAffiliateMiningProfileService
{
    Task<AffiliateMiningProfileResponseDto> GetAsync(int userId, CancellationToken cancellationToken = default);

    Task<AffiliateMiningProfileResponseDto> SaveAsync(
        int userId,
        AffiliateMiningProfileDto request,
        CancellationToken cancellationToken = default);
}

public interface IProductArbitrageService
{
    Task<OfferArbitrageResponseDto> SearchAlternativesAsync(
        int userId,
        OfferArbitrageRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface IOfferRadarService
{
    Task<OfferRadarResponseDto> ListAsync(
        int userId,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default);

    Task<OfferRadarResponseDto> ScheduleAsync(
        int userId,
        int itemId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<OfferRadarResponseDto> QueueAutoPilotAsync(
        int userId,
        int itemId,
        CancellationToken cancellationToken = default);
}

public interface IOfferMiningEngine
{
    Task<int> MineUserAsync(int userId, CancellationToken cancellationToken = default);
}

public interface ICatalogOfferIndex
{
    Task UpsertAsync(
        int userId,
        string searchTerm,
        IReadOnlyCollection<OfficialCatalogProductDto> offers,
        CancellationToken cancellationToken = default);

    Task<bool> HasFreshTermAsync(
        int userId,
        string term,
        DateTime freshnessCutoffUtc,
        CancellationToken cancellationToken = default);
}

public interface IOfferRadarHybridQuery
{
    /// <summary>
    /// Consulta o índice local dos termos do perfil. Sem resultado dentro da janela de cache do Master,
    /// dispara a busca oficial (que respeita o cache do Mercado Livre) e promove ofertas ao radar.
    /// </summary>
    Task<string?> RefreshProfileTermsAsync(
        int userId,
        AffiliateMiningProfileDto profile,
        int maxApiTerms,
        CancellationToken cancellationToken = default);
}

public interface IStorePriceComparisonService
{
    Task<StorePriceComparisonResponseDto> CompareAsync(
        int userId,
        StorePriceComparisonRequestDto request,
        CancellationToken cancellationToken = default);
}
