using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Service.Application;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PublicacaoController : ControllerBase
{
    private readonly PublicacaoApplicationService _pubService;

    public PublicacaoController(PublicacaoApplicationService pubService)
    {
        _pubService = pubService;
    }

    [HttpPost("{produtoId}/publicar-tiktok")]
    public async Task<IActionResult> PublicarManual(int produtoId)
    {
        var resultado = await _pubService.PublicarAsync(produtoId);
        return Ok(new { status = resultado });
    }
}