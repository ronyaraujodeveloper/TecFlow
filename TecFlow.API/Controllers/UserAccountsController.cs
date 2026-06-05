using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Security;
using TecFlow.Util.Validation;

namespace TecFlow.API.Controllers;

[ApiController]
[Route("api/Usuarios")]
public class UserAccountsController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly JwtTokenService _jwtTokenService;

    public UserAccountsController(IUserAccountRepository repository, JwtTokenService jwt)
    {
        _userAccountRepository = repository;
        _jwtTokenService = jwt;
    }

    [HttpGet]
    public async Task<ActionResult<UserAccountResponseDto>> GetByFilterAsync([FromQuery] UserAccountFilter filter)
    {
        var items = (await _userAccountRepository.GetAllAsync())
            .ApplyFilter(filter)
            .ToList();

        return Ok(new UserAccountResponseDto { Status = true, Descricao = "OK", DataList = items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserAccountResponseDto>> GetByIdAsync(int id)
    {
        var userAccount = await _userAccountRepository.GetByIdAsync(id);
        if (userAccount is null)
        {
            return NotFound(new UserAccountResponseDto { Status = false, Descricao = "Usuário não encontrado." });
        }

        return Ok(new UserAccountResponseDto { Status = true, Descricao = "OK", Data = userAccount });
    }

    [HttpPost]
    public async Task<ActionResult<UserAccountResponseDto>> CreateAsync([FromBody] UserAccountDto dto)
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

        var userAccount = new UserAccount
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash),
            Plan = dto.Plan.Trim(),
            WhatsAppPhone = ValidationHelper.NormalizeBrazilianCellPhone(dto.WhatsAppPhone),
            CreatedAt = DateTime.UtcNow
        };

        await _userAccountRepository.AddAsync(userAccount);

        return Ok(new UserAccountResponseDto { Status = true, Descricao = "Criado com sucesso.", Data = userAccount });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserAccountResponseDto>> UpdateAsync(int id, [FromBody] UserAccountDto dto)
    {
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
        return Ok(new UserAccountResponseDto { Status = true, Descricao = "Atualizado com sucesso.", Data = userAccount });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        await _userAccountRepository.DeleteAsync(id);
        return Ok(new UserAccountResponseDto { Status = true, Descricao = "Removido com sucesso." });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync(LoginDto dto)
    {
        if (!ValidationHelper.IsValidEmail(dto.Email))
        {
            return BadRequest(new { Message = "E-mail inválido.", ErrorCode = "INVALID_EMAIL" });
        }

        var userAccount = await _userAccountRepository.GetByEmailAsync(dto.Email);
        if (userAccount is null)
        {
            return Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, userAccount.PasswordHash))
        {
            return Unauthorized();
        }

        var token = _jwtTokenService.GenerateToken(userAccount);
        return Ok(new { token });
    }
}
