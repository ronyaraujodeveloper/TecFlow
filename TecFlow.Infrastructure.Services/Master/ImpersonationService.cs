using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Database;
using TecFlow.Infrastructure.Security;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>Emite token com as claims do inquilino alvo para o operador master enxergar o painel do cliente.</summary>
public sealed class ImpersonationService : IImpersonationService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IMasterAuditService _audit;

    public ImpersonationService(AppDbContext context, JwtTokenService jwtTokenService, IMasterAuditService audit)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _audit = audit;
    }

    public async Task<ImpersonationResultDto> StartAsync(MasterActor actor, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return Fail("Inquilino não encontrado.");
        }

        var actorUser = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == actor.UserId, cancellationToken);
        if (actorUser is null || !actorUser.IsActive || !MasterRoles.IsMaster(actorUser.Role))
        {
            return Fail("Operador master inválido.");
        }

        var target = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.IsActive)
            .OrderBy(item => item.Role == MasterRoles.TenantUser ? 0 : 1)
            .ThenBy(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null)
        {
            return Fail("O inquilino não possui usuário ativo para emular.");
        }

        if (target.Id == actorUser.Id)
        {
            return Fail("Você já está no próprio inquilino; use a visão de cliente.");
        }

        var token = _jwtTokenService.GenerateImpersonationToken(target, actorUser, tenant.Name);
        await _audit.LogAsync(actor, MasterAuditActions.ImpersonationStarted, "Tenant", tenant.Id.ToString(),
            $"Emulação iniciada como {target.Email} ({tenant.Name}).", cancellationToken);
        return new ImpersonationResultDto
        {
            Success = true,
            Message = $"Você está navegando como o tenant {tenant.Name}.",
            AccessToken = token,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            TargetUserId = target.Id,
            TargetUserName = target.Name,
            TargetEmail = target.Email
        };
    }

    public Task StopAsync(MasterActor actor, Guid? tenantId, CancellationToken cancellationToken = default) =>
        _audit.LogAsync(actor, MasterAuditActions.ImpersonationStopped, "Tenant", tenantId?.ToString(),
            "Emulação encerrada; retorno ao Painel Master.", cancellationToken);

    private static ImpersonationResultDto Fail(string message) => new() { Success = false, Message = message };
}
