using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Orquestrador;

[ApiController]
[Route("api/orquestrador")]
public class OrquestradorPrincipal : ControllerBase
{
    private readonly IOrquestradorService _orchestratorService;

    public OrquestradorPrincipal(IOrquestradorService orchestratorService)
    {
        _orchestratorService = orchestratorService;
    }

    /// <summary>
    /// Executa o pipeline completo, abrangendo coleta, processamento, IA e publicação.
    /// </summary>
    public async Task ExecuteFullPipelineAsync()
    {
        await _orchestratorService.ExecuteFullPipelineAsync();
    }

    [HttpPost("executar")]
    public async Task<IActionResult> ExecutePipelineAsync()
    {
        await _orchestratorService.ExecuteFullPipelineAsync();
        return Ok(new { Message = "Pipeline executado com sucesso." });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatusAsync()
    {
        var status = await _orchestratorService.GetExecutionStatusAsync();
        return Ok(new
        {
            Status = status,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
