using Microsoft.AspNetCore.Identity;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;

namespace TecFlow.Infrastructure.Services.Security;

/// <summary>Mantém compatibilidade com hashes BCrypt existentes na base de usuários.</summary>
public class TecFlowPasswordHasher : IPasswordHasher<UserAccount>
{
    public string HashPassword(UserAccount user, string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public PasswordVerificationResult VerifyHashedPassword(
        UserAccount user,
        string hashedPassword,
        string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }
}
