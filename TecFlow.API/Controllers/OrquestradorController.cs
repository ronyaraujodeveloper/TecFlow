using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrquestradorController : ControllerBase
{
    private readonly IOrquestradorRepository _orquestrador;
    private readonly IOrquestradorService _orquestradorService;

    // O construtor agora recebe os dois serviços injetados
    public OrquestradorController(
        IOrquestradorRepository orquestrador,
        IOrquestradorService orquestradorService)
    {
        _orquestrador = orquestrador;
        _orquestradorService = orquestradorService; // <-- Isso corrige o seu erro!
    }

    [HttpGet("status-pipeline")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _orquestradorService.GetExecutionStatusAsync();
        return Ok(new { Status = status });
    }

    // ... restante do código
}