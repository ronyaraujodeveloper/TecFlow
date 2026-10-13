using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>
/// Contas corporativas de teste/dev da TecFlow (escopo de sistema). Não escreve no cofre global (/master/vault)
/// nem nas lojas dos afiliados (/marketplace-accounts).
/// </summary>
public sealed class DeveloperAccountService : IDeveloperAccountService
{
    private const string MaskPrefix = "••••";
    private const string TargetType = "SystemDeveloperAccount";

    private readonly AppDbContext _context;
    private readonly IDeveloperAccountHealthChecker _checker;
    private readonly IMasterAuditService _audit;

    public DeveloperAccountService(
        AppDbContext context,
        IDeveloperAccountHealthChecker checker,
        IMasterAuditService audit)
    {
        _context = context;
        _checker = checker;
        _audit = audit;
    }

    public async Task<IReadOnlyList<DeveloperAccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.SystemDeveloperAccounts
            .AsNoTracking()
            .OrderBy(item => item.Platform)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.Label)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return items.Select(item => Map(item, now)).ToList();
    }

    public async Task<(DeveloperAccountDto? Item, string? Error)> SaveAsync(
        MasterActor actor,
        DeveloperAccountDto request,
        CancellationToken cancellationToken = default)
    {
        SystemDeveloperAccount? entity = null;
        if (request.Id > 0)
        {
            entity = await _context.SystemDeveloperAccounts.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
            if (entity is null)
            {
                return (null, "Conta não encontrada.");
            }
        }

        var secret = KeepOrReplace(request.ClientSecret, entity?.ClientSecret);
        var error = DeveloperAccountRules.Validate(
            request.Platform,
            request.Kind,
            request.Label,
            request.ClientId,
            !string.IsNullOrWhiteSpace(secret));
        if (error is not null)
        {
            return (null, error);
        }

        var isNew = entity is null;
        entity ??= new SystemDeveloperAccount();
        entity.Platform = DeveloperPlatforms.Normalize(request.Platform)!;
        entity.Kind = DeveloperAccountKinds.Normalize(request.Kind)!;
        entity.Label = request.Label.Trim();
        entity.Environment = DeveloperEnvironments.Normalize(request.Environment);
        entity.ClientId = Clean(request.ClientId);
        entity.ClientSecret = secret;
        entity.Extra = Clean(request.Extra);
        entity.TestUsername = Clean(request.TestUsername);
        entity.TestPassword = KeepOrReplace(request.TestPassword, entity.TestPassword);
        entity.Notes = Clean(request.Notes);
        entity.SecretExpiresAt = request.SecretExpiresAt;
        entity.IsActive = request.IsActive;
        entity.UpdatedBy = actor.Email;
        if (isNew)
        {
            _context.SystemDeveloperAccounts.Add(entity);
        }
        else
        {
            entity.Touch();
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.DeveloperAccountSaved, TargetType, entity.Id.ToString(),
            $"{entity.Platform} / {entity.Kind}: {entity.Label} (segredos omitidos).", cancellationToken);
        return (Map(entity, DateTime.UtcNow), null);
    }

    public async Task<MasterOperationResultDto> DeleteAsync(MasterActor actor, int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SystemDeveloperAccounts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return Fail("Conta não encontrada.");
        }

        _context.SystemDeveloperAccounts.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.DeveloperAccountDeleted, TargetType, id.ToString(),
            $"{entity.Platform} / {entity.Kind}: {entity.Label}", cancellationToken);
        return new MasterOperationResultDto { Success = true, Message = "Conta removida." };
    }

    public async Task<DeveloperAccountCheckDto> CheckAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SystemDeveloperAccounts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return new DeveloperAccountCheckDto { Success = false, Message = "Conta não encontrada.", CheckedAt = DateTime.UtcNow };
        }

        var check = await _checker.CheckAsync(entity.Platform, entity.ClientId, entity.ClientSecret, entity.Extra, cancellationToken);
        var status = DeveloperAccountRules.ExpirationStatus(entity.SecretExpiresAt, DateTime.UtcNow);
        var message = status switch
        {
            SecretExpirationStatus.Expired => check.Message + " Atenção: o segredo está vencido pela data cadastrada.",
            SecretExpirationStatus.Expiring => check.Message + $" Atenção: o segredo vence em {entity.SecretExpiresAt:dd/MM/yyyy}.",
            _ => check.Message
        };

        entity.LastCheckAt = DateTime.UtcNow;
        entity.LastCheckOk = check.Success;
        entity.LastCheckMessage = message.Length > 1000 ? message[..1000] : message;
        await _context.SaveChangesAsync(cancellationToken);
        return new DeveloperAccountCheckDto
        {
            Platform = entity.Platform,
            Source = entity.Label,
            Success = check.Success,
            Message = message,
            LatencyMs = check.LatencyMs,
            CheckedAt = entity.LastCheckAt.Value
        };
    }

    private static string? KeepOrReplace(string? incoming, string? current)
    {
        if (string.IsNullOrWhiteSpace(incoming) || incoming.StartsWith(MaskPrefix, StringComparison.Ordinal))
        {
            return current;
        }

        return incoming.Trim();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static MasterOperationResultDto Fail(string message) => new() { Success = false, Message = message };

    private static DeveloperAccountDto Map(SystemDeveloperAccount item, DateTime now) => new()
    {
        Id = item.Id,
        Platform = item.Platform,
        Kind = item.Kind,
        Label = item.Label,
        Environment = item.Environment,
        ClientId = item.ClientId,
        ClientSecret = SystemSettingCatalog.Mask(item.ClientSecret),
        HasSecret = !string.IsNullOrEmpty(item.ClientSecret),
        Extra = item.Extra,
        TestUsername = item.TestUsername,
        TestPassword = SystemSettingCatalog.Mask(item.TestPassword),
        HasTestPassword = !string.IsNullOrEmpty(item.TestPassword),
        Notes = item.Notes,
        SecretExpiresAt = item.SecretExpiresAt,
        ExpirationStatus = DeveloperAccountRules.ExpirationStatus(item.SecretExpiresAt, now),
        IsActive = item.IsActive,
        LastCheckAt = item.LastCheckAt,
        LastCheckOk = item.LastCheckOk,
        LastCheckMessage = item.LastCheckMessage,
        UpdatedAt = item.UpdatedAt ?? item.CreatedAt,
        UpdatedBy = item.UpdatedBy
    };
}
