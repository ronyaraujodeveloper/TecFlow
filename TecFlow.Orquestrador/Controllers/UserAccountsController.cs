using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Util.Validation;

namespace TecFlow.Orquestrador.Controllers;

[ApiController]
[Route("api/Usuarios")]
public class UserAccountsController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ILogger<UserAccountsController> _logger;

    public UserAccountsController(
        IUserAccountRepository userAccountRepository,
        ILogger<UserAccountsController> logger)
    {
        _userAccountRepository = userAccountRepository;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateAsync([FromBody] UserAccountDto dto, CancellationToken cancellationToken)
    {
        if (!ValidationHelper.IsValidEmail(dto.Email))
        {
            return BadRequest(new UserAccountResponseDto { Status = false, Descricao = "E-mail inválido." });
        }

        if (!ValidationHelper.IsValidBrazilianCellPhone(dto.WhatsAppPhone))
        {
            return BadRequest(new UserAccountResponseDto { Status = false, Descricao = "Telefone/WhatsApp inválido." });
        }

        var passwordValidation = ValidationHelper.ValidatePasswordStrength(dto.PasswordHash);
        if (!passwordValidation.IsValid)
        {
            return BadRequest(new UserAccountResponseDto
            {
                Status = false,
                Descricao = "Senha não atende aos critérios de segurança."
            });
        }

        if (await _userAccountRepository.GetByEmailAsync(dto.Email.Trim()) is not null)
        {
            return Conflict(new UserAccountResponseDto { Status = false, Descricao = "E-mail já cadastrado." });
        }

        var normalizedPhone = ValidationHelper.NormalizeBrazilianCellPhone(dto.WhatsAppPhone)!;

        var userAccount = new UserAccount
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash),
            Plan = dto.Plan.Trim(),
            WhatsAppPhone = normalizedPhone,
            CreatedAt = DateTime.UtcNow
        };

        await _userAccountRepository.AddAsync(userAccount);

        _logger.LogInformation("Utilizador criado. UserId={UserId}, Email={Email}", userAccount.Id, userAccount.Email);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = userAccount.Id },
            new UserAccountResponseDto { Status = true, Descricao = "Criado com sucesso.", Data = userAccount });
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId) || userId != id)
        {
            return Forbid();
        }

        var userAccount = await _userAccountRepository.GetByIdAsync(id);
        if (userAccount is null)
        {
            return NotFound(new UserAccountResponseDto { Status = false, Descricao = "Usuário não encontrado." });
        }

        return Ok(new UserAccountResponseDto { Status = true, Descricao = "OK", Data = userAccount });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateAsync(
        int id,
        [FromBody] UserAccountDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId) || userId != id)
        {
            return Forbid();
        }

        if (!ValidationHelper.IsValidBrazilianCellPhone(dto.WhatsAppPhone))
        {
            return BadRequest(new UserAccountResponseDto { Status = false, Descricao = "Telefone/WhatsApp inválido." });
        }

        var userAccount = await _userAccountRepository.GetByIdAsync(id);
        if (userAccount is null)
        {
            return NotFound(new UserAccountResponseDto { Status = false, Descricao = "Usuário não encontrado." });
        }

        userAccount.Name = dto.Name.Trim();
        userAccount.Plan = dto.Plan.Trim();
        userAccount.WhatsAppPhone = ValidationHelper.NormalizeBrazilianCellPhone(dto.WhatsAppPhone);
        userAccount.UpdatedAt = DateTime.UtcNow;

        await _userAccountRepository.UpdateAsync(userAccount);

        _logger.LogInformation("Utilizador atualizado. UserId={UserId}", userAccount.Id);

        return Ok(new UserAccountResponseDto { Status = true, Descricao = "Atualizado com sucesso.", Data = userAccount });
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out userId);
    }
}
