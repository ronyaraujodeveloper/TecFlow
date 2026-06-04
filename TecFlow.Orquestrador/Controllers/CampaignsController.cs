using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.Orquestrador.Controllers;

[ApiController]
[Route("api/Campanhas")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;

    public CampaignsController(ICampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    [HttpGet]
    public async Task<ActionResult<CampaignResponseDto>> GetByFilterAsync([FromQuery] CampaignFilter filter)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new CampaignResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        filter.OwnerId ??= userId;

        var filtered = (await _campaignRepository.GetByOwnerIdAsync(userId)).ApplyFilter(filter);
        var (items, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new CampaignResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CampaignResponseDto>> GetByIdAsync(int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new CampaignResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        var campaign = await _campaignRepository.GetByIdAsync(id);
        if (campaign is null)
        {
            return NotFound(new CampaignResponseDto { Status = false, Descricao = "Campanha não encontrada." });
        }

        if (campaign.OwnerId != userId)
        {
            return Forbid();
        }

        return Ok(new CampaignResponseDto { Status = true, Descricao = "OK", Data = campaign });
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out userId);
    }
}
