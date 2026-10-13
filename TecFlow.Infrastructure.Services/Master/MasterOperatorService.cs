using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Util.Validation;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>CRUD de operadores do backoffice (usuários com papel master).</summary>
public sealed class MasterOperatorService : IMasterOperatorService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<UserAccount> _passwordHasher;
    private readonly IMasterAuditService _audit;

    public MasterOperatorService(AppDbContext context, IPasswordHasher<UserAccount> passwordHasher, IMasterAuditService audit)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    public async Task<IReadOnlyList<MasterOperatorDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var roles = MasterRoles.MasterRoleList.ToList();
        var users = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => roles.Contains(item.Role))
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<(MasterOperatorDto? Item, string? Error)> CreateAsync(
        MasterActor actor,
        MasterOperatorSaveDto request,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(request, requirePassword: true);
        if (validation is not null)
        {
            return (null, validation);
        }

        var email = request.Email.Trim();
        var existing = await _context.UserAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (existing is not null)
        {
            if (MasterRoles.IsMaster(existing.Role))
            {
                return (null, "Este e-mail já pertence a um operador master.");
            }

            existing.Role = MasterRoles.Normalize(request.Role);
            existing.IsActive = request.IsActive;
            existing.Touch();
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(actor, MasterAuditActions.OperatorCreated, "Usuario", existing.Id.ToString(),
                $"Promovido usuário existente {existing.Email} a {existing.Role}.", cancellationToken);
            return (Map(existing), null);
        }

        var actorTenantId = await _context.UserAccounts
            .IgnoreQueryFilters()
            .Where(item => item.Id == actor.UserId)
            .Select(item => item.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        var user = new UserAccount
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = string.Empty,
            Plan = "Master",
            Role = MasterRoles.Normalize(request.Role),
            IsActive = request.IsActive,
            TenantId = actorTenantId
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password!);
        _context.UserAccounts.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.OperatorCreated, "Usuario", user.Id.ToString(),
            $"Operador {user.Email} criado como {user.Role}.", cancellationToken);
        return (Map(user), null);
    }

    public async Task<(MasterOperatorDto? Item, string? Error)> UpdateAsync(
        MasterActor actor,
        int id,
        MasterOperatorSaveDto request,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(request, requirePassword: false);
        if (validation is not null)
        {
            return (null, validation);
        }

        var user = await FindOperatorAsync(id, cancellationToken);
        if (user is null)
        {
            return (null, "Operador não encontrado.");
        }

        var newRole = MasterRoles.Normalize(request.Role);
        var guard = await GuardSuperAdminRemovalAsync(actor, user, newRole, request.IsActive, cancellationToken);
        if (guard is not null)
        {
            return (null, guard);
        }

        var changes = new List<string>();
        if (!string.Equals(user.Role, newRole, StringComparison.Ordinal))
        {
            changes.Add($"papel {user.Role} → {newRole}");
            user.Role = newRole;
        }

        if (user.IsActive != request.IsActive)
        {
            changes.Add(request.IsActive ? "reativado" : "inativado");
            user.IsActive = request.IsActive;
        }

        user.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            changes.Add("senha redefinida");
        }

        user.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.OperatorUpdated, "Usuario", user.Id.ToString(),
            $"Operador {user.Email}: {(changes.Count == 0 ? "dados atualizados" : string.Join(", ", changes))}.", cancellationToken);
        return (Map(user), null);
    }

    public async Task<MasterOperationResultDto> SetActiveAsync(
        MasterActor actor,
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await FindOperatorAsync(id, cancellationToken);
        if (user is null)
        {
            return Fail("Operador não encontrado.");
        }

        var guard = await GuardSuperAdminRemovalAsync(actor, user, user.Role, isActive, cancellationToken);
        if (guard is not null)
        {
            return Fail(guard);
        }

        user.IsActive = isActive;
        user.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.OperatorActivation, "Usuario", user.Id.ToString(),
            $"Operador {user.Email} {(isActive ? "reativado" : "inativado")}.", cancellationToken);
        return new MasterOperationResultDto { Success = true, Message = isActive ? "Operador reativado." : "Operador inativado." };
    }

    private Task<UserAccount?> FindOperatorAsync(int id, CancellationToken cancellationToken)
    {
        var roles = MasterRoles.MasterRoleList.ToList();
        return _context.UserAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && roles.Contains(item.Role), cancellationToken);
    }

    private async Task<string?> GuardSuperAdminRemovalAsync(
        MasterActor actor,
        UserAccount user,
        string newRole,
        bool newIsActive,
        CancellationToken cancellationToken)
    {
        if (user.Id == actor.UserId && (!newIsActive || newRole != user.Role))
        {
            return "Você não pode inativar nem alterar o próprio papel.";
        }

        var losesSuperAdmin = user.Role == MasterRoles.SuperAdmin
            && user.IsActive
            && (newRole != MasterRoles.SuperAdmin || !newIsActive);
        if (!losesSuperAdmin)
        {
            return null;
        }

        var activeSuperAdmins = await _context.UserAccounts
            .IgnoreQueryFilters()
            .CountAsync(item => item.Role == MasterRoles.SuperAdmin && item.IsActive, cancellationToken);
        return activeSuperAdmins <= 1 ? "É necessário manter ao menos um Super Admin ativo." : null;
    }

    private static string? Validate(MasterOperatorSaveDto request, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Informe o nome do operador.";
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email.Trim()))
        {
            return "Informe um e-mail válido.";
        }

        if (!MasterRoles.IsMaster(request.Role))
        {
            return "Papel inválido. Use SuperAdmin, SuporteTecnico ou FinanceiroSaaS.";
        }

        if (requirePassword && string.IsNullOrWhiteSpace(request.Password))
        {
            return "Informe a senha inicial do operador.";
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var strength = ValidationHelper.ValidatePasswordStrength(request.Password);
            if (!strength.IsValid)
            {
                return strength.Errors.FirstOrDefault() ?? "Senha fraca.";
            }
        }

        return null;
    }

    private static MasterOperatorDto Map(UserAccount user) =>
        new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            RoleLabel = MasterRoles.Label(user.Role),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

    private static MasterOperationResultDto Fail(string message) => new() { Success = false, Message = message };
}
