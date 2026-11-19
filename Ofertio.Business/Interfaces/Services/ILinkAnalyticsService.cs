using Ofertio.Business.Dto;
using Ofertio.Business.Service.Attribution;

namespace Ofertio.Business.Interfaces.Services;

/// <summary>Pedido de análise: período em datas locais de Brasília, agrupamento e filtro opcional de loja.</summary>
public sealed record AnalyticsRequest(
    Guid TenantId,
    int UserId,
    DateOnly From,
    DateOnly To,
    AnalyticsGrain Grain,
    string? ShopId);

/// <summary>Números do painel do cliente a partir dos cliques dos links curtos (robôs de prévia ficam de fora).</summary>
public interface ILinkAnalyticsService
{
    Task<(AnalyticsOverviewDto? Data, string? Error)> GetOverviewAsync(
        AnalyticsRequest request,
        CancellationToken cancellationToken = default);

    Task<(AnalyticsProductDetailDto? Data, string? Error)> GetProductAsync(
        AnalyticsRequest request,
        Guid linkGroupId,
        CancellationToken cancellationToken = default);
}
