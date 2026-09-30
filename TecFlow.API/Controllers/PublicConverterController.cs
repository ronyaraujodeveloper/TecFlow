using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[ApiController]
[Route("api/public-converter")]
public sealed class PublicConverterController : ControllerBase
{
    private readonly IPublicConverterPageService _pages;

    public PublicConverterController(IPublicConverterPageService pages)
    {
        _pages = pages;
    }

    [AllowAnonymous]
    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicConverterPageResponseDto>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var page = await _pages.ResolveBySlugAsync(slug, cancellationToken);
        if (page is null)
        {
            return NotFound(new PublicConverterPageResponseDto
            {
                Status = false,
                Descricao = "Página pública não encontrada."
            });
        }

        return Ok(new PublicConverterPageResponseDto
        {
            Status = true,
            Descricao = "OK",
            Data = page
        });
    }

    [AllowAnonymous]
    [HttpPost("{slug}/convert")]
    public async Task<ActionResult<GerarLinkAfiliadoResponseDto>> ConvertAsync(
        string slug,
        [FromBody] PublicConverterConvertDto request,
        CancellationToken cancellationToken)
    {
        request ??= new PublicConverterConvertDto();
        var result = await _pages.ConvertAsync(slug, request.OriginalUrl, cancellationToken);
        if (!result.Success)
        {
            var notFound = string.Equals(result.Message, "Página pública não encontrada.", StringComparison.Ordinal);
            return notFound ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("/api/afiliados/paginas-publicas")]
    public async Task<ActionResult<PublicConverterPageResponseDto>> ListMineAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new PublicConverterPageResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _pages.ListMineAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPut("/api/afiliados/paginas-publicas/slug")]
    public async Task<ActionResult<PublicConverterPageResponseDto>> ChangeSlugAsync(
        [FromBody] ChangePublicConverterSlugDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new PublicConverterPageResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        request ??= new ChangePublicConverterSlugDto();
        var result = await _pages.ChangeSlugAsync(userId, request.Slug, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }
}
