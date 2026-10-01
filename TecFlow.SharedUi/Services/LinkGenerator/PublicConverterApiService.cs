using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.LinkGenerator;

public sealed class PublicConverterApiService : IPublicConverterApiService
{
    public const string PublicPath = "api/public-converter";
    public const string MinePath = "api/afiliados/paginas-publicas";

    private readonly IHttpService _httpService;
    private readonly ILoadingService _loadingService;

    public PublicConverterApiService(IHttpService httpService, ILoadingService loadingService)
    {
        _httpService = httpService;
        _loadingService = loadingService;
    }

    public Task<ApiResult<PublicConverterPageResponseDto>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<PublicConverterPageResponseDto>(
            $"{PublicPath}/{Uri.EscapeDataString(slug)}",
            cancellationToken: cancellationToken);

    public Task<ApiResult<GerarLinkAfiliadoResponseDto>> ConvertAsync(
        string slug,
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Convertendo link...");
        return _httpService.PostAsync<PublicConverterConvertDto, GerarLinkAfiliadoResponseDto>(
            $"{PublicPath}/{Uri.EscapeDataString(slug)}/convert",
            new PublicConverterConvertDto { OriginalUrl = originalUrl },
            cancellationToken);
    }

    public async Task<ApiResult<PublicConverterPageResponseDto>> ListMineAsync(
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando páginas públicas...");
        return await _httpService.GetAsync<PublicConverterPageResponseDto>(MinePath, cancellationToken: cancellationToken);
    }

    public async Task<ApiResult<PublicConverterPageResponseDto>> ChangeSlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Atualizando slug...");
        return await _httpService.PutAsync<ChangePublicConverterSlugDto, PublicConverterPageResponseDto>(
            $"{MinePath}/slug",
            new ChangePublicConverterSlugDto { Slug = slug },
            cancellationToken);
    }
}
