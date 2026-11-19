using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

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

    /// <summary>Conta uma visita ou uma conversão da página (só contagem, sem identificar o visitante).</summary>
    Task RecordEventAsync(
        string slug,
        string kind,
        PublicVisitorInfo visitor,
        CancellationToken cancellationToken = default);
}
