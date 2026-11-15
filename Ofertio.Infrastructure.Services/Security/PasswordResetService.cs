using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto.Auth;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.Email;
using Ofertio.Util.Validation;

namespace Ofertio.Infrastructure.Services.Security;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _context;
    private readonly IUserAccountRepository _users;
    private readonly IPasswordHasher<UserAccount> _passwordHasher;
    private readonly ITransactionalEmailSender _email;
    private readonly EmailOptions _options;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        AppDbContext context,
        IUserAccountRepository users,
        IPasswordHasher<UserAccount> passwordHasher,
        ITransactionalEmailSender email,
        IOptions<EmailOptions> options,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _users = users;
        _passwordHasher = passwordHasher;
        _email = email;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AuthProviderResponseDto> RequestAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (!ValidationHelper.IsValidEmail(email))
        {
            return Fail("E-mail inválido.");
        }

        var user = await _users.GetByEmailAsync(email!.Trim());
        if (user is null)
        {
            return Accepted();
        }

        var now = DateTime.UtcNow;
        var pending = await _context.PasswordResetTokens
            .Where(item => item.UserId == user.Id && item.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var item in pending)
        {
            item.UsedAt = now;
            item.Touch();
        }

        var token = TransactionalEmailRules.CreateToken();
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = TransactionalEmailRules.HashToken(token),
            ExpiresAt = now.Add(TransactionalEmailRules.TokenLifetime),
            CreatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var url = TransactionalEmailRules.BuildResetUrl(_options.PublicBaseUrl, token);
            await _email.SendAsync(TransactionalEmailRules.PasswordReset(user.Email, url), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar a redefinição de senha do usuário {UserId}.", user.Id);
        }

        return Accepted();
    }

    public async Task<AuthProviderResponseDto> ConfirmAsync(
        string? token,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        var strength = ValidationHelper.ValidatePasswordStrength(newPassword);
        if (!strength.IsValid)
        {
            return Fail(strength.Errors.FirstOrDefault() ?? "Senha não atende aos critérios de segurança.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Fail(TransactionalEmailRules.InvalidTokenMessage);
        }

        var hash = TransactionalEmailRules.HashToken(token.Trim());
        var now = DateTime.UtcNow;
        var stored = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(
                item => item.TokenHash == hash && item.UsedAt == null && item.ExpiresAt > now,
                cancellationToken);
        if (stored is null)
        {
            return Fail(TransactionalEmailRules.InvalidTokenMessage);
        }

        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == stored.UserId, cancellationToken);
        if (user is null)
        {
            return Fail(TransactionalEmailRules.InvalidTokenMessage);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword!);
        user.Touch();
        stored.UsedAt = now;
        stored.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthProviderResponseDto
        {
            Status = true,
            Descricao = TransactionalEmailRules.PasswordUpdatedMessage
        };
    }

    private static AuthProviderResponseDto Accepted() => new()
    {
        Status = true,
        Descricao = TransactionalEmailRules.RequestAcceptedMessage
    };

    private static AuthProviderResponseDto Fail(string message) => new()
    {
        Status = false,
        Descricao = message
    };
}
