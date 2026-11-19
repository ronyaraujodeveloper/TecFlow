using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

/// <summary>Foto do afiliado e links de convite da página pública (lado do dono).</summary>
public interface IPublicPageProfileService
{
    Task<PublicPageProfileResponseDto> GetMineAsync(int userId, CancellationToken cancellationToken = default);

    /// <param name="content">Conteúdo da imagem; só JPEG, PNG ou WebP até 2 MB, conferido pelo conteúdo.</param>
    Task<PublicPageProfileResponseDto> SaveAvatarAsync(
        int userId,
        Stream content,
        long length,
        CancellationToken cancellationToken = default);

    Task<PublicPageProfileResponseDto> RemoveAvatarAsync(int userId, CancellationToken cancellationToken = default);

    Task<PublicPageProfileResponseDto> CreateJoinLinkAsync(
        int userId,
        JoinLinkCreateDto request,
        CancellationToken cancellationToken = default);

    Task<PublicPageProfileResponseDto> UpdateJoinLinkAsync(
        int userId,
        int joinLinkId,
        JoinLinkUpdateDto request,
        CancellationToken cancellationToken = default);

    Task<PublicPageProfileResponseDto> DeleteJoinLinkAsync(
        int userId,
        int joinLinkId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Destino do botão /j/{id}: só devolve links ativos cujo convite continua no formato validado.
    /// Nunca aceita endereço livre: o redirecionamento usa exclusivamente o que está cadastrado.
    /// </summary>
    Task<JoinRedirectTarget?> ResolveJoinTargetAsync(int joinLinkId, CancellationToken cancellationToken = default);
}

public sealed record JoinRedirectTarget(Guid TenantId, int UserId, string Channel, string InviteUrl);
