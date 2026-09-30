using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IPublicConverterPageService
{
    Task<PublicConverterPageDto?> ResolveBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<PublicConverterPageResponseDto> ListMineAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<PublicConverterPageResponseDto> ChangeSlugAsync(
        int userId,
        string newSlug,
        CancellationToken cancellationToken = default);

    Task<GerarLinkAfiliadoResponseDto> ConvertAsync(
        string slug,
        string originalUrl,
        CancellationToken cancellationToken = default);
}
