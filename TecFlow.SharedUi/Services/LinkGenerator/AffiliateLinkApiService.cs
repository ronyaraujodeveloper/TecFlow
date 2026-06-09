using TecFlow.Business.Dto;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.LinkGenerator;

public sealed class AffiliateLinkApiService : IAffiliateLinkApiService
{
    private readonly IHttpService _httpService;
    private readonly ILoadingService _loadingService;

    public AffiliateLinkApiService(IHttpService httpService, ILoadingService loadingService)
    {
        _httpService = httpService;
        _loadingService = loadingService;
    }

    public Task<ApiResult<GerarLinkAfiliadoResponseDto>> GenerateAsync(
        GerarLinkAfiliadoDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Gerando link de comissão...");
        return _httpService.PostAsync<GerarLinkAfiliadoDto, GerarLinkAfiliadoResponseDto>(
            "api/affiliate-links/gerar",
            request,
            cancellationToken);
    }

    public async Task<AffiliateLinkHistoryResponseDto> ListHistoryAsync(
        AffiliateLinkFilter filter,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando histórico de links...");
        var result = await _httpService.GetAsync<AffiliateLinkHistoryResponseDto>(
            "api/affiliate-links/historico",
            filter,
            cancellationToken);

        if (result.Success && result.Data is not null)
        {
            return result.Data;
        }

        return new AffiliateLinkHistoryResponseDto
        {
            Status = false,
            Descricao = result.ErrorMessage ?? "Não foi possível carregar o histórico de links."
        };
    }
}