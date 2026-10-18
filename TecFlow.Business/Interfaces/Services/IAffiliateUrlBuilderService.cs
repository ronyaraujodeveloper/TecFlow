using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Services;

public interface IAffiliateUrlBuilderService
{
    Task<string?> ResolveMercadoLivreMattToolAsync(int userId, CancellationToken cancellationToken = default);

    string BuildAffiliateUrl(
        string? originalUrl,
        string? primaryProductUrl,
        MarketplaceType? platform,
        string? mercadoLivreMattTool);
}
