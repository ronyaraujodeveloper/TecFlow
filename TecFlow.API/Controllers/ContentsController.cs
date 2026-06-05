using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database.Filter;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/Conteudos")]
public class ContentsController : ControllerBase
{
    private readonly IContentRepository _contentRepository;

    public ContentsController(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ContentResponseDto>> GetByFilterAsync([FromQuery] ContentFilter filter)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        filter.OwnerId ??= userId;

        var items = (await _contentRepository.GetByOwnerIdAsync(userId))
            .ApplyFilter(filter)
            .ToList();

        return Ok(new ContentResponseDto { Status = true, Descricao = "OK", DataList = items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContentResponseDto>> GetByIdAsync(int id)
    {
        var content = await _contentRepository.GetByIdAsync(id);
        if (content is null)
        {
            return NotFound(new ContentResponseDto { Status = false, Descricao = "Conteúdo não encontrado." });
        }

        return Ok(new ContentResponseDto { Status = true, Descricao = "OK", Data = content });
    }

    [HttpPost]
    public async Task<ActionResult<ContentResponseDto>> CreateAsync([FromBody] ContentDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var content = new Content
        {
            Name = dto.Name,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Budget = dto.Budget,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _contentRepository.AddAsync(content);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = content.Id },
            new ContentResponseDto { Status = true, Descricao = "Criado com sucesso.", Data = content });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContentResponseDto>> UpdateAsync(int id, [FromBody] ContentDto dto)
    {
        var content = await _contentRepository.GetByIdAsync(id);
        if (content is null)
        {
            return NotFound(new ContentResponseDto { Status = false, Descricao = "Conteúdo não encontrado." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (content.OwnerId != userId)
        {
            return Forbid();
        }

        content.Name = dto.Name;
        content.Description = dto.Description;
        content.StartDate = dto.StartDate;
        content.EndDate = dto.EndDate;
        content.Budget = dto.Budget;
        content.UpdatedAt = DateTime.UtcNow;

        await _contentRepository.UpdateAsync(content);
        return Ok(new ContentResponseDto { Status = true, Descricao = "Atualizado com sucesso.", Data = content });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var content = await _contentRepository.GetByIdAsync(id);
        if (content is null)
        {
            return NotFound(new ContentResponseDto { Status = false, Descricao = "Conteúdo não encontrado." });
        }

        await _contentRepository.DeleteAsync(id);
        return Ok(new ContentResponseDto { Status = true, Descricao = "Removido com sucesso." });
    }
}
