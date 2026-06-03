using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Database;
using TecFlow.Util.Security;

namespace TecFlow.Infrastructure.Services.Security;

/// <summary>
/// Re-criptografa credenciais legadas (plaintext) já persistidas antes do value converter do EF.
/// </summary>
public sealed class LegacyCredentialReEncryptService
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<LegacyCredentialReEncryptService> _logger;

    public LegacyCredentialReEncryptService(
        AppDbContext context,
        IEncryptionService encryptionService,
        ILogger<LegacyCredentialReEncryptService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<LegacyCredentialReEncryptResult> ExecuteAsync(
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await _context.Database
            .SqlQuery<UserCredentialSnapshot>($"""
                SELECT "Id",
                       "PasswordHash",
                       "TikTokShopAccessToken",
                       "TikTokRefreshToken"
                FROM "Usuarios"
                """)
            .ToListAsync(cancellationToken);

        var usersUpdated = 0;
        var fieldsEncrypted = 0;

        foreach (var snapshot in snapshots)
        {
            var passwordHash = snapshot.PasswordHash;
            var accessToken = snapshot.TikTokShopAccessToken;
            var refreshToken = snapshot.TikTokRefreshToken;
            var rowFieldsEncrypted = 0;

            if (NeedsEncryption(passwordHash))
            {
                passwordHash = _encryptionService.Encrypt(passwordHash);
                rowFieldsEncrypted++;
            }

            if (NeedsEncryption(accessToken))
            {
                accessToken = _encryptionService.Encrypt(accessToken!);
                rowFieldsEncrypted++;
            }

            if (NeedsEncryption(refreshToken))
            {
                refreshToken = _encryptionService.Encrypt(refreshToken!);
                rowFieldsEncrypted++;
            }

            if (rowFieldsEncrypted == 0)
            {
                continue;
            }

            fieldsEncrypted += rowFieldsEncrypted;
            usersUpdated++;

            _logger.LogInformation(
                "Usuário {UserId}: {FieldCount} campo(s) a re-criptografar{Mode}.",
                snapshot.Id,
                rowFieldsEncrypted,
                dryRun ? " (dry-run)" : string.Empty);

            if (dryRun)
            {
                continue;
            }

            await _context.Database.ExecuteSqlRawAsync(
                """
                UPDATE "Usuarios"
                SET "PasswordHash" = {0},
                    "TikTokShopAccessToken" = {1},
                    "TikTokRefreshToken" = {2}
                WHERE "Id" = {3}
                """,
                [passwordHash, accessToken!, refreshToken!, snapshot.Id],
                cancellationToken);
        }

        return new LegacyCredentialReEncryptResult
        {
            UsersScanned = snapshots.Count,
            UsersUpdated = usersUpdated,
            FieldsEncrypted = fieldsEncrypted,
            DryRun = dryRun
        };
    }

    private static bool NeedsEncryption(string? value) =>
        !string.IsNullOrEmpty(value) && !AesEncryptionService.IsEncrypted(value);
}
