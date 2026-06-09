using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Dto.Auth;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Security;
using TecFlow.Core.Entities;
using TecFlow.Infrastructure.Security;
using TecFlow.Util.Validation;

namespace TecFlow.Infrastructure.Services.Security;

public class PlatformAuthService : IPlatformAuthService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly ISocialAuthTokenValidator _socialAuthTokenValidator;
    private readonly UserManager<UserAccount> _userManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ILogger<PlatformAuthService> _logger;

    public PlatformAuthService(
        IUserAccountRepository userAccountRepository,
        IUserLoginRepository userLoginRepository,
        ISocialAuthTokenValidator socialAuthTokenValidator,
        UserManager<UserAccount> userManager,
        JwtTokenService jwtTokenService,
        ITenantProvisioningService tenantProvisioningService,
        ILogger<PlatformAuthService> logger)
    {
        _userAccountRepository = userAccountRepository;
        _userLoginRepository = userLoginRepository;
        _socialAuthTokenValidator = socialAuthTokenValidator;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _tenantProvisioningService = tenantProvisioningService;
        _logger = logger;
    }

    public async Task<(bool Success, AuthTokenDto? Token, string? ErrorMessage, string? ErrorCode)> LoginAsync(
        string platform,
        PlatformAuthDto request,
        CancellationToken cancellationToken = default)
    {
        var provider = AuthProviderNames.Normalize(request.Provider);
        if (provider is null)
        {
            return (false, null, "Provedor de autenticação é obrigatório.", "PROVIDER_REQUIRED");
        }

        if (provider == AuthProviderNames.EmailPassword)
        {
            return await LoginWithEmailPasswordAsync(platform, request, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.AccessToken) && string.IsNullOrWhiteSpace(request.IdToken))
        {
            return (false, null, $"Token do provedor {provider} é obrigatório para login social.", "SOCIAL_TOKEN_REQUIRED");
        }

        return await LoginWithSocialProviderAsync(platform, provider, request, cancellationToken);
    }

    public async Task<AuthProviderResponseDto> LinkProviderAsync(
        int userId,
        LinkProviderDto request,
        CancellationToken cancellationToken = default)
    {
        var provider = AuthProviderNames.Normalize(request.Provider);
        if (provider is null || !AuthProviderNames.IsSocialProvider(provider))
        {
            return Fail("Provedor social inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.AccessToken) && string.IsNullOrWhiteSpace(request.IdToken))
        {
            return Fail("Token social é obrigatório para vincular o provedor.");
        }

        var user = await _userAccountRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var payload = await _socialAuthTokenValidator.ValidateAsync(
            provider,
            request.AccessToken,
            request.IdToken,
            cancellationToken);

        if (payload is null)
        {
            return Fail("Token social inválido ou expirado.");
        }

        var existingLogin = await _userLoginRepository.GetByProviderAsync(provider, payload.ProviderKey, cancellationToken);
        if (existingLogin is not null && existingLogin.UserId != userId)
        {
            return Fail("Este provedor social já está vinculado a outra conta.");
        }

        if (existingLogin is null)
        {
            await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, payload.ProviderKey, provider));
        }

        return await BuildProviderResponseAsync(user, "Provedor vinculado com sucesso.", cancellationToken);
    }

    public async Task<AuthProviderResponseDto> UnlinkProviderAsync(
        int userId,
        string providerName,
        CancellationToken cancellationToken = default)
    {
        var provider = AuthProviderNames.Normalize(providerName);
        if (provider is null || !AuthProviderNames.IsSocialProvider(provider))
        {
            return Fail("Provedor social inválido.");
        }

        var user = await _userAccountRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var logins = await _userLoginRepository.GetByUserIdAsync(userId, cancellationToken);
        var targetLogin = logins.FirstOrDefault(login =>
            string.Equals(login.LoginProvider, provider, StringComparison.OrdinalIgnoreCase));

        if (targetLogin is null)
        {
            return Fail("Provedor não vinculado à conta.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        var remainingSocialLogins = logins.Count(login =>
            !string.Equals(login.LoginProvider, provider, StringComparison.OrdinalIgnoreCase));

        if (!hasPassword && remainingSocialLogins == 0)
        {
            return Fail("Não é possível remover o último método de autenticação. Cadastre uma senha ou vincule outro provedor antes de desvincular.");
        }

        var result = await _userManager.RemoveLoginAsync(user, targetLogin.LoginProvider, targetLogin.ProviderKey);
        if (!result.Succeeded)
        {
            return Fail(result.Errors.FirstOrDefault()?.Description ?? "Falha ao desvincular provedor.");
        }

        return await BuildProviderResponseAsync(user, "Provedor desvinculado com sucesso.", cancellationToken);
    }

    public async Task<AuthProviderResponseDto> ChangePasswordAsync(
        int userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userAccountRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var passwordValidation = ValidationHelper.ValidatePasswordStrength(request.NewPassword);
        if (!passwordValidation.IsValid)
        {
            return Fail(passwordValidation.Errors.FirstOrDefault() ?? "Nova senha não atende aos critérios de segurança.");
        }

        IdentityResult result;
        if (await _userManager.HasPasswordAsync(user))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return Fail("Senha atual é obrigatória.");
            }

            result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        else
        {
            result = await _userManager.AddPasswordAsync(user, request.NewPassword);
        }

        if (!result.Succeeded)
        {
            return Fail(result.Errors.FirstOrDefault()?.Description ?? "Não foi possível alterar a senha.");
        }

        return await BuildProviderResponseAsync(user, "Senha atualizada com sucesso.", cancellationToken);
    }

    public async Task<UserResponseDto> RegisterAsync(
        UserDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return FailRegister("E-mail é obrigatório.");
        }

        var email = request.Email.Trim();

        // REGRA 6: validação centralizada de formato de e-mail.
        if (!ValidationHelper.IsValidEmail(email))
        {
            return FailRegister("E-mail inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            return FailRegister("Senha é obrigatória.");
        }

        // REGRA 6: política de força de senhas do ecossistema TecFlow.
        var passwordValidation = ValidationHelper.ValidatePasswordStrength(request.PasswordHash);
        if (!passwordValidation.IsValid)
        {
            return FailRegister(passwordValidation.Errors.FirstOrDefault() ?? "Senha não atende aos critérios de segurança.");
        }

        var existingUser = await _userAccountRepository.GetByEmailAsync(email);
        if (existingUser is not null)
        {
            return FailRegister("Este e-mail já está cadastrado no sistema.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.Name)
            ? email.Split('@')[0]
            : request.Name.Trim();

        var user = new UserAccount
        {
            Name = displayName,
            Email = email,
            PasswordHash = string.Empty,
            Plan = "Free",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.PasswordHash);

        var tenant = await _tenantProvisioningService.EnsureTenantForUserAsync(user);
        user.TenantId = tenant.Id;

        await _userAccountRepository.AddAsync(user);

        return new UserResponseDto
        {
            Status = true,
            Descricao = "Conta criada com sucesso.",
            Data = MapUserDto(user)
        };
    }

    private static UserResponseDto FailRegister(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };

    private static UserDto MapUserDto(UserAccount user) =>
        new()
        {
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.WhatsAppPhone,
            IsActive = true
        };

    private async Task<(bool Success, AuthTokenDto? Token, string? ErrorMessage, string? ErrorCode)> LoginWithEmailPasswordAsync(
        string platform,
        PlatformAuthDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, null, "E-mail e senha são obrigatórios.", "CREDENTIALS_REQUIRED");
        }

        var email = request.Email.Trim();
        if (!ValidationHelper.IsValidEmail(email))
        {
            return (false, null, "E-mail inválido.", "INVALID_EMAIL");
        }

        var user = await _userAccountRepository.GetByEmailAsync(email);
        if (user is null)
        {
            return (false, null, "Credenciais inválidas.", "INVALID_CREDENTIALS");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return (false, null, "Credenciais inválidas.", "INVALID_CREDENTIALS");
        }

        return (true, BuildAuthToken(user, platform), null, null);
    }

    private async Task<(bool Success, AuthTokenDto? Token, string? ErrorMessage, string? ErrorCode)> LoginWithSocialProviderAsync(
        string platform,
        string provider,
        PlatformAuthDto request,
        CancellationToken cancellationToken)
    {
        var payload = await _socialAuthTokenValidator.ValidateAsync(
            provider,
            request.AccessToken,
            request.IdToken,
            cancellationToken);

        if (payload is null)
        {
            return (false, null, "Token social inválido ou expirado.", "SOCIAL_TOKEN_INVALID");
        }

        var existingByLogin = await _userManager.FindByLoginAsync(provider, payload.ProviderKey);
        if (existingByLogin is not null)
        {
            return (true, BuildAuthToken(existingByLogin, platform), null, null);
        }

        var existingByEmail = await _userAccountRepository.GetByEmailAsync(payload.Email);
        if (existingByEmail is not null)
        {
            _logger.LogInformation(
                "Auto-linking do provedor {Provider} para usuário existente {Email}.",
                provider,
                payload.Email);

            await _userManager.AddLoginAsync(
                existingByEmail,
                new UserLoginInfo(provider, payload.ProviderKey, provider));

            return (true, BuildAuthToken(existingByEmail, platform), null, null);
        }

        var newUser = new UserAccount
        {
            Name = payload.DisplayName ?? payload.Email.Split('@')[0],
            Email = payload.Email,
            PasswordHash = string.Empty,
            Plan = "Free",
            CreatedAt = DateTime.UtcNow
        };

        var tenant = await _tenantProvisioningService.EnsureTenantForUserAsync(newUser);
        newUser.TenantId = tenant.Id;
        await _userAccountRepository.AddAsync(newUser);
        await _userManager.AddLoginAsync(newUser, new UserLoginInfo(provider, payload.ProviderKey, provider));

        return (true, BuildAuthToken(newUser, platform), null, null);
    }

    private AuthTokenDto BuildAuthToken(UserAccount user, string platform) =>
        new()
        {
            Token = _jwtTokenService.GenerateToken(user),
            UserId = user.Id.ToString(),
            DisplayName = user.Name,
            Platform = platform,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
        };

    private async Task<AuthProviderResponseDto> BuildProviderResponseAsync(
        UserAccount user,
        string message,
        CancellationToken cancellationToken)
    {
        var logins = await _userLoginRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var hasPassword = await _userManager.HasPasswordAsync(user);

        return new AuthProviderResponseDto
        {
            Status = true,
            Descricao = message,
            Data = new AuthProviderInfoDto
            {
                Provider = string.Empty,
                HasPassword = hasPassword,
                LinkedProviders = logins.Select(login => login.LoginProvider).Distinct().ToList()
            }
        };
    }

    private static AuthProviderResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}
