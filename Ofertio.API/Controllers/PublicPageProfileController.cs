using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.PublicPages;

namespace Ofertio.API.Controllers;

/// <summary>Foto do afiliado e links de convite da página pública (Minhas Páginas Públicas).</summary>
[ApiController]
[Authorize]
[Route("api/afiliados/pagina-publica")]
public sealed class PublicPageProfileController : ControllerBase
{
    private readonly IPublicPageProfileService _profiles;

    public PublicPageProfileController(IPublicPageProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    public Task<ActionResult<PublicPageProfileResponseDto>> GetAsync(CancellationToken cancellationToken) =>
        RunAsync(userId => _profiles.GetMineAsync(userId, cancellationToken));

    [HttpPost("foto")]
    [RequestSizeLimit(AvatarImageRules.MaxBytes + 64 * 1024)]
    public Task<ActionResult<PublicPageProfileResponseDto>> UploadAvatarAsync(IFormFile? file, CancellationToken cancellationToken) =>
        RunAsync(async userId =>
        {
            if (file is null || file.Length == 0)
            {
                return new PublicPageProfileResponseDto { Status = false, Descricao = AvatarImageRules.InvalidTypeMessage };
            }

            await using var stream = file.OpenReadStream();
            return await _profiles.SaveAvatarAsync(userId, stream, file.Length, cancellationToken);
        });

    [HttpDelete("foto")]
    public Task<ActionResult<PublicPageProfileResponseDto>> RemoveAvatarAsync(CancellationToken cancellationToken) =>
        RunAsync(userId => _profiles.RemoveAvatarAsync(userId, cancellationToken));

    [HttpPost("links")]
    public Task<ActionResult<PublicPageProfileResponseDto>> CreateLinkAsync(
        [FromBody] JoinLinkCreateDto? request,
        CancellationToken cancellationToken) =>
        RunAsync(userId => _profiles.CreateJoinLinkAsync(userId, request ?? new JoinLinkCreateDto(), cancellationToken));

    [HttpPut("links/{id:int}")]
    public Task<ActionResult<PublicPageProfileResponseDto>> UpdateLinkAsync(
        int id,
        [FromBody] JoinLinkUpdateDto? request,
        CancellationToken cancellationToken) =>
        RunAsync(userId => _profiles.UpdateJoinLinkAsync(userId, id, request ?? new JoinLinkUpdateDto(), cancellationToken));

    [HttpDelete("links/{id:int}")]
    public Task<ActionResult<PublicPageProfileResponseDto>> DeleteLinkAsync(int id, CancellationToken cancellationToken) =>
        RunAsync(userId => _profiles.DeleteJoinLinkAsync(userId, id, cancellationToken));

    private async Task<ActionResult<PublicPageProfileResponseDto>> RunAsync(
        Func<int, Task<PublicPageProfileResponseDto>> action)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new PublicPageProfileResponseDto { Status = false, Descricao = "Usuário não autenticado." });
        }

        var result = await action(userId);
        return result.Status ? Ok(result) : BadRequest(result);
    }
}
