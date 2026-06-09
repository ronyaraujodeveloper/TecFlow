using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Campanhas")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        ICampaignRepository campaignRepository,
        ILogger<CampaignsController> logger)
    {
        _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<CampaignResponseDto>> GetByFilterAsync([FromQuery] CampaignFilter filter)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        filter.OwnerId ??= userId;

        var filtered = (await _campaignRepository.GetByOwnerIdAsync(userId, filter.LojaId)).ApplyFilter(filter);
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
        var campaign = await _campaignRepository.GetByIdAsync(id);
        if (campaign is null)
        {
            return NotFound(new CampaignResponseDto { Status = false, Descricao = "Campanha não encontrada." });
        }

        return Ok(new CampaignResponseDto { Status = true, Descricao = "OK", Data = campaign });
    }

    [HttpPost]
    public async Task<ActionResult<CampaignResponseDto>> CreateAsync([FromBody] CampaignDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var campaign = new Campaign
        {
            Name = dto.Name,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Budget = dto.Budget,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _campaignRepository.AddAsync(campaign);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = campaign.Id },
            new CampaignResponseDto { Status = true, Descricao = "Criada com sucesso.", Data = campaign });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CampaignResponseDto>> UpdateAsync(int id, [FromBody] CampaignDto dto)
    {
        var campaign = await _campaignRepository.GetByIdAsync(id);
        if (campaign is null)
        {
            return NotFound(new CampaignResponseDto { Status = false, Descricao = $"Campaign com ID {id} não foi localizada." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new CampaignResponseDto { Status = false, Descricao = "Usuário não autenticado ou token inválido." });
        }

        if (campaign.OwnerId != userId)
        {
            return Forbid();
        }

        campaign.Name = dto.Name;
        campaign.Description = dto.Description;
        campaign.Budget = dto.Budget;
        campaign.StartDate = dto.StartDate;
        campaign.EndDate = dto.EndDate;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _campaignRepository.UpdateAsync(campaign);
        return Ok(new CampaignResponseDto { Status = true, Descricao = "Atualizada com sucesso.", Data = campaign });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var campaign = await _campaignRepository.GetByIdAsync(id);
        if (campaign is null)
        {
            return NotFound(new CampaignResponseDto { Status = false, Descricao = "Campanha não encontrada." });
        }

        await _campaignRepository.DeleteAsync(id);
        return Ok(new CampaignResponseDto { Status = true, Descricao = "Removida com sucesso." });
    }
}
