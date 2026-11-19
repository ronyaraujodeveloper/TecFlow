using System.Globalization;
using Ofertio.Business.Dto;
using Ofertio.Business.Service.Attribution;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Dashboard;

public interface ILinkAnalyticsApiService
{
    Task<ApiResult<AnalyticsOverviewDto>> GetOverviewAsync(
        DateOnly from,
        DateOnly to,
        AnalyticsGrain grain,
        string? shopId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<AnalyticsProductDetailDto>> GetProductAsync(
        Guid linkGroupId,
        DateOnly from,
        DateOnly to,
        AnalyticsGrain grain,
        string? shopId,
        CancellationToken cancellationToken = default);
}

public sealed class LinkAnalyticsApiService : ILinkAnalyticsApiService
{
    private readonly IHttpService _httpService;

    public LinkAnalyticsApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public Task<ApiResult<AnalyticsOverviewDto>> GetOverviewAsync(
        DateOnly from,
        DateOnly to,
        AnalyticsGrain grain,
        string? shopId,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<AnalyticsOverviewDto>(
            "api/painel/visao-geral" + Query(from, to, grain, shopId),
            cancellationToken: cancellationToken);

    public Task<ApiResult<AnalyticsProductDetailDto>> GetProductAsync(
        Guid linkGroupId,
        DateOnly from,
        DateOnly to,
        AnalyticsGrain grain,
        string? shopId,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<AnalyticsProductDetailDto>(
            $"api/painel/produtos/{linkGroupId}" + Query(from, to, grain, shopId),
            cancellationToken: cancellationToken);

    private static string Query(DateOnly from, DateOnly to, AnalyticsGrain grain, string? shopId)
    {
        var query = $"?from={from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
            + $"&to={to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
            + $"&grain={AnalyticsPeriodRules.GrainName(grain)}";
        return string.IsNullOrWhiteSpace(shopId) ? query : query + "&shopId=" + Uri.EscapeDataString(shopId);
    }
}
