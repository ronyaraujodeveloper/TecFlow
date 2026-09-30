using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;

namespace TecFlow.SharedUi.Services.LinkGenerator;

public interface IPublicConverterApiService
{
    Task<ApiResult<PublicConverterPageResponseDto>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<ApiResult<GerarLinkAfiliadoResponseDto>> ConvertAsync(
        string slug,
        string originalUrl,
        CancellationToken cancellationToken = default);

    Task<ApiResult<PublicConverterPageResponseDto>> ListMineAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<PublicConverterPageResponseDto>> ChangeSlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
