using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // O usuário precisa estar logado no seu sistema para vincular a conta
    public class TikTokAuthController : ControllerBase
    {
        private readonly ITikTokShopApi _tikTokApi;
        private readonly IUserAccountRepository _userRepo;

        public TikTokAuthController(ITikTokShopApi tikTokApi, IUserAccountRepository userRepo)
        {
            _tikTokApi = tikTokApi;
            _userRepo = userRepo;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Código inválido.");

            // 1. Troca o código pelo Token (usando o método que definimos no passo anterior)
            var token = await _tikTokApi.TrocarCodigoPorTokenAsync(code);

            // 2. Obtém o ID do usuário logado (via JWT)
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var usuario = await _userRepo.GetByIdAsync(userId);

            if (usuario == null) return NotFound("Usuário não encontrado.");

            // 3. Salva o token no banco
            usuario.TikTokShopAccessToken = token;
            await _userRepo.UpdateAsync(usuario);

            return Ok(new { Message = "Conta TikTok vinculada com sucesso!" });
        }
    }
}