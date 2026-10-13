using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>Contas de desenvolvedor e de teste da TecFlow (escopo de sistema, segregado das lojas dos clientes).</summary>
public sealed class DeveloperAccountService : IDeveloperAccountService
{
    private const string MaskPrefix = "••••";
    private const string TargetType = "SystemDeveloperAccount";

    private readonly AppDbContext _context;
    private readonly ISystemSettingsService _settings;
    private readonly IDeveloperAccountHealthChecker _checker;
    private readonly IConfiguration _configuration;
    private readonly IMasterAuditService _audit;

    public DeveloperAccountService(
        AppDbContext context,
        ISystemSettingsService settings,
        IDeveloperAccountHealthChecker checker,
        IConfiguration configuration,
        IMasterAuditService audit)
    {
        _context = context;
        _settings = settings;
        _checker = checker;
        _configuration = configuration;
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
        return new MasterOperationResultDto
        {
            Success = true,
            Message = entity.IsGlobalDefault
                ? "Conta removida. As chaves publicadas continuam no cofre global até serem substituídas."
                : "Conta removida."
        };
    }

    public async Task<MasterOperationResultDto> PublishAsGlobalAsync(MasterActor actor, int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SystemDeveloperAccounts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return Fail("Conta não encontrada.");
        }

        if (entity.Kind != DeveloperAccountKinds.App || !entity.IsActive)
        {
            return Fail("Somente credenciais de app ativas podem ser publicadas como chave global.");
        }

        if (string.IsNullOrWhiteSpace(entity.ClientId) || string.IsNullOrWhiteSpace(entity.ClientSecret))
        {
            return Fail("A conta não tem ID e segredo preenchidos.");
        }

        var values = DeveloperAccountRules.GlobalSettingValues(entity.Platform, entity.ClientId, entity.ClientSecret, entity.Extra);
        var saved = await _settings.SaveAsync(actor, new SystemSettingSaveDto { Values = values.ToDictionary(pair => pair.Key, pair => pair.Value) }, cancellationToken);
        if (!saved.Success)
        {
            return saved;
        }

        var samePlatform = await _context.SystemDeveloperAccounts
            .Where(item => item.Platform == entity.Platform && item.IsGlobalDefault && item.Id != entity.Id)
            .ToListAsync(cancellationToken);
        foreach (var other in samePlatform)
        {
            other.IsGlobalDefault = false;
            other.Touch();
        }

        entity.IsGlobalDefault = true;
        entity.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.DeveloperAccountPublished, TargetType, entity.Id.ToString(),
            $"{entity.Platform}: {entity.Label} publicada como credencial global.", cancellationToken);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = $"{DeveloperPlatforms.Label(entity.Platform)}: credenciais publicadas no cofre global. Reinicie a API para aplicar em todas as integrações."
        };
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

    public async Task<IReadOnlyList<DeveloperAccountCheckDto>> CheckGlobalCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var targets = new[]
        {
            (DeveloperPlatforms.MercadoLivre,
                Config("Integrations:MercadoLivre:AppId"),
                Config("Integrations:MercadoLivre:SecretKey"),
                (string?)null),
            (DeveloperPlatforms.Shopee,
                Config("Integrations:Shopee:AffiliateAppId", "Integrations:Shopee:AppKey", "Integrations:Shopee:PartnerId"),
                Config("Integrations:Shopee:AffiliateSecret", "Integrations:Shopee:AppSecret", "Integrations:Shopee:PartnerKey"),
                null),
            (DeveloperPlatforms.Amazon,
                Config("Integrations:AmazonPaApi:AccessKey"),
                Config("Integrations:AmazonPaApi:SecretKey"),
                Config("Integrations:AmazonPaApi:PartnerTag"))
        };

        var checks = await Task.WhenAll(targets.Select(async target =>
        {
            var result = await _checker.CheckAsync(target.Item1, target.Item2, target.Item3, target.Item4, cancellationToken);
            return new DeveloperAccountCheckDto
            {
                Platform = target.Item1,
                Source = "Credencial global em uso pela API",
                Success = result.Success,
                Message = result.Message,
                LatencyMs = result.LatencyMs,
                CheckedAt = DateTime.UtcNow
            };
        }));
        return checks;
    }

    private string? Config(params string[] keys) =>
        keys.Select(key => _configuration[key]).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

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
        IsGlobalDefault = item.IsGlobalDefault,
        LastCheckAt = item.LastCheckAt,
        LastCheckOk = item.LastCheckOk,
        LastCheckMessage = item.LastCheckMessage,
        UpdatedAt = item.UpdatedAt ?? item.CreatedAt,
        UpdatedBy = item.UpdatedBy
    };
}
