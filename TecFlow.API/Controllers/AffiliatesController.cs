using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;
using TecFlow.Util.Validation;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/Afiliados")]
public class AffiliatesController : ControllerBase
{
    private readonly IAffiliateRepository _affiliateRepository;
    private readonly ILogger<AffiliatesController> _logger;

    public AffiliatesController(
        IAffiliateRepository affiliateRepository,
        ILogger<AffiliatesController> logger)
    {
        _affiliateRepository = affiliateRepository ?? throw new ArgumentNullException(nameof(affiliateRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<AffiliateResponseDto>> GetByFilterAsync([FromQuery] AffiliateFilter filter)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        filter.OwnerId ??= userId;

        var filtered = (await _affiliateRepository.GetByOwnerIdAsync(userId)).ApplyFilter(filter);
        var (items, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new AffiliateResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AffiliateResponseDto>> GetByIdAsync(int id)
    {
        var affiliate = await _affiliateRepository.GetByIdAsync(id);
        if (affiliate is null)
        {
            return NotFound(new AffiliateResponseDto { Status = false, Descricao = "Afiliado não encontrado." });
        }

        return Ok(new AffiliateResponseDto { Status = true, Descricao = "OK", Data = affiliate });
    }

    [HttpPost]
    public async Task<ActionResult<AffiliateResponseDto>> CreateAsync([FromBody] AffiliateDto dto)
    {
        if (!ValidationHelper.IsValidEmail(dto.Email))
        {
            return BadRequest(new AffiliateResponseDto { Status = false, Descricao = "E-mail inválido." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var affiliate = new Affiliate
        {
            Name = dto.Name,
            Email = dto.Email,
            AffiliateCode = dto.AffiliateCode,
            Commission = dto.Commission,
            CampaignId = dto.CampaignId,
            ContentId = dto.ContentId,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _affiliateRepository.AddAsync(affiliate);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = affiliate.Id },
            new AffiliateResponseDto { Status = true, Descricao = "Criado com sucesso.", Data = affiliate });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AffiliateResponseDto>> UpdateAsync(int id, [FromBody] AffiliateDto dto)
    {
        var affiliate = await _affiliateRepository.GetByIdAsync(id);
        if (affiliate is null)
        {
            return NotFound(new AffiliateResponseDto { Status = false, Descricao = $"Affiliate com ID {id} não foi localizado." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new AffiliateResponseDto { Status = false, Descricao = "Usuário não autenticado ou token inválido." });
        }

        if (affiliate.OwnerId != userId)
        {
            return Forbid();
        }

        if (!ValidationHelper.IsValidEmail(dto.Email))
        {
            return BadRequest(new AffiliateResponseDto { Status = false, Descricao = "E-mail inválido." });
        }

        affiliate.Name = dto.Name;
        affiliate.Email = dto.Email;
        affiliate.AffiliateCode = dto.AffiliateCode;
        affiliate.Commission = dto.Commission;
        affiliate.CampaignId = dto.CampaignId;
        affiliate.ContentId = dto.ContentId;
        affiliate.UpdatedAt = DateTime.UtcNow;

        await _affiliateRepository.UpdateAsync(affiliate);

        return Ok(new AffiliateResponseDto { Status = true, Descricao = "Atualizado com sucesso.", Data = affiliate });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var affiliate = await _affiliateRepository.GetByIdAsync(id);
        if (affiliate is null)
        {
            return NotFound(new AffiliateResponseDto { Status = false, Descricao = "Afiliado não encontrado." });
        }

        await _affiliateRepository.DeleteAsync(id);
        return Ok(new AffiliateResponseDto { Status = true, Descricao = "Removido com sucesso." });
    }
}
