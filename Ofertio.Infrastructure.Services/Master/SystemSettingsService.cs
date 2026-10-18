using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Cofre de chaves globais: valores criptografados no banco e recarregados na configuração da API.</summary>
public sealed class SystemSettingsService : ISystemSettingsService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMasterAuditService _audit;

    public SystemSettingsService(AppDbContext context, IConfiguration configuration, IMasterAuditService audit)
    {
        _context = context;
        _configuration = configuration;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SystemSettingItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _context.SystemSettings
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var byKey = stored.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

        return SystemSettingCatalog.All
            .Select(definition =>
            {
                byKey.TryGetValue(definition.Key, out var row);
                var value = row?.Value;
                var effective = string.IsNullOrEmpty(value) ? _configuration[definition.Key] : value;
                return new SystemSettingItemDto
                {
                    Key = definition.Key,
                    Category = definition.Category,
                    Label = definition.Label,
                    IsSecret = definition.IsSecret,
                    HasValue = !string.IsNullOrEmpty(effective),
                    DisplayValue = definition.IsSecret ? SystemSettingCatalog.Mask(effective) : effective ?? string.Empty,
                    Placeholder = definition.Placeholder,
                    UpdatedAt = row?.UpdatedAt ?? row?.CreatedAt,
                    UpdatedBy = row?.UpdatedBy
                };
            })
            .ToList();
    }

    public async Task<MasterOperationResultDto> SaveAsync(
        MasterActor actor,
        SystemSettingSaveDto request,
        CancellationToken cancellationToken = default)
    {
        var changed = new List<string>();
        var rows = await _context.SystemSettings.ToListAsync(cancellationToken);
        var pending = new List<(SystemSettingDefinition Definition, string Value, SystemSetting? Row)>();
        var invalid = new List<string>();
        foreach (var (rawKey, rawValue) in request.Values)
        {
            var definition = SystemSettingCatalog.Find(rawKey);
            if (definition is null)
            {
                continue;
            }

            var value = rawValue?.Trim() ?? string.Empty;
            var row = rows.FirstOrDefault(item => string.Equals(item.Key, definition.Key, StringComparison.OrdinalIgnoreCase));
            if (definition.IsSecret)
            {
                var current = string.IsNullOrEmpty(row?.Value) ? _configuration[definition.Key] : row.Value;
                if (SystemSettingCatalog.IsUnchangedMask(value, current))
                {
                    continue;
                }

                if (SystemSettingCatalog.ContainsMaskCharacter(value))
                {
                    invalid.Add(definition.Label);
                    continue;
                }
            }

            pending.Add((definition, value, row));
        }

        if (invalid.Count > 0)
        {
            return new MasterOperationResultDto
            {
                Success = false,
                Message = "Apague a máscara (••••) e digite a chave completa em: " + string.Join(", ", invalid) + ". Nada foi salvo."
            };
        }

        foreach (var (definition, value, row) in pending)
        {
            if (row is null)
            {
                if (value.Length == 0)
                {
                    continue;
                }

                _context.SystemSettings.Add(new SystemSetting
                {
                    Key = definition.Key,
                    Category = definition.Category,
                    Value = value,
                    IsSecret = definition.IsSecret,
                    UpdatedBy = actor.Email
                });
                changed.Add(definition.Key);
                continue;
            }

            if (string.Equals(row.Value, value, StringComparison.Ordinal))
            {
                continue;
            }

            if (value.Length == 0)
            {
                _context.SystemSettings.Remove(row);
                changed.Add(definition.Key);
                continue;
            }

            row.Value = value;
            row.Category = definition.Category;
            row.IsSecret = definition.IsSecret;
            row.UpdatedBy = actor.Email;
            row.Touch();
            changed.Add(definition.Key);
        }

        if (changed.Count == 0)
        {
            return new MasterOperationResultDto { Success = true, Message = "Nenhuma chave alterada." };
        }

        await _context.SaveChangesAsync(cancellationToken);
        _configuration.ReloadOfertioDatabaseSettings();
        await _audit.LogAsync(actor, MasterAuditActions.SettingsChanged, "SystemSetting", null,
            "Chaves alteradas: " + string.Join(", ", changed) + " (valores omitidos).", cancellationToken);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = $"{changed.Count} chave(s) salva(s) com criptografia. Reinicie a API para aplicar em todas as integrações."
        };
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _context.SystemSettings
            .AsNoTracking()
            .Where(item => item.Key == key)
            .Select(item => item.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? _configuration[key] : value;
    }
}
