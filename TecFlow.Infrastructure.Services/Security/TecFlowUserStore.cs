using Microsoft.AspNetCore.Identity;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;

namespace TecFlow.Infrastructure.Services.Security;

/// <summary>UserStore mínimo para UserManager operar sobre UserAccount e AspNetUserLogins.</summary>
public class TecFlowUserStore :
    IUserStore<UserAccount>,
    IUserPasswordStore<UserAccount>,
    IUserEmailStore<UserAccount>,
    IUserLoginStore<UserAccount>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IUserLoginRepository _userLoginRepository;

    public TecFlowUserStore(
        IUserAccountRepository userAccountRepository,
        IUserLoginRepository userLoginRepository)
    {
        _userAccountRepository = userAccountRepository;
        _userLoginRepository = userLoginRepository;
    }

    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    public Task SetUserNameAsync(UserAccount user, string? userName, CancellationToken cancellationToken)
    {
        user.Email = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(Normalize(user.Email));

    public Task SetNormalizedUserNameAsync(UserAccount user, string? normalizedName, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<IdentityResult> CreateAsync(UserAccount user, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Use IUserAccountRepository para criar usuários.");

    public async Task<IdentityResult> UpdateAsync(UserAccount user, CancellationToken cancellationToken)
    {
        await _userAccountRepository.UpdateAsync(user);
        return IdentityResult.Success;
    }

    public Task<IdentityResult> DeleteAsync(UserAccount user, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Exclusão de usuários deve passar pelo repositório dedicado.");

    public async Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var id))
        {
            return null;
        }

        return await _userAccountRepository.GetByIdAsync(id);
    }

    public async Task<UserAccount?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        await FindByEmailAsync(normalizedUserName, cancellationToken);

    public Task SetEmailAsync(UserAccount user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    public Task<bool> GetEmailConfirmedAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task SetEmailConfirmedAsync(UserAccount user, bool confirmed, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task<UserAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        await _userAccountRepository.GetByEmailAsync(normalizedEmail);

    public Task<string?> GetNormalizedEmailAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(Normalize(user.Email));

    public Task SetNormalizedEmailAsync(UserAccount user, string? normalizedEmail, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SetPasswordHashAsync(UserAccount user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.PasswordHash);

    public Task<bool> HasPasswordAsync(UserAccount user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));

    public async Task AddLoginAsync(UserAccount user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        await _userLoginRepository.AddAsync(new UserExternalLogin
        {
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName
        }, cancellationToken);
    }

    public async Task RemoveLoginAsync(
        UserAccount user,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        await _userLoginRepository.RemoveAsync(loginProvider, providerKey, cancellationToken);
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var logins = await _userLoginRepository.GetByUserIdAsync(user.Id, cancellationToken);
        return logins
            .Select(login => new UserLoginInfo(login.LoginProvider, login.ProviderKey, login.ProviderDisplayName))
            .ToList();
    }

    public async Task<UserAccount?> FindByLoginAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        var login = await _userLoginRepository.GetByProviderAsync(loginProvider, providerKey, cancellationToken);
        if (login is null)
        {
            return null;
        }

        return await _userAccountRepository.GetByIdAsync(login.UserId);
    }

    private static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
}
