using System.Security.Claims;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Security;

namespace Ofertio.API.Middlewares;

/// <summary>
/// Bloqueia inquilinos pausados, suspensos ou cancelados (exceto operadores master e rotas públicas).
/// Meus dados e Meu plano (/api/conta) ficam abertos para o cliente regularizar a assinatura.
/// </summary>
public sealed class TenantAccessGuardMiddleware
{
    private static readonly string[] ExemptPrefixes =
    [
        "/api/auth",
        "/api/conta",
        "/api/sistema",
        "/api/webhooks",
        "/api/v1/integrations",
        "/metrics",
        "/swagger",
        "/r/",
        "/uploads"
    ];

    private readonly RequestDelegate _next;

    public TenantAccessGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantAccessReader accessReader)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true
            || IsExempt(context.Request.Path)
            || MasterRoles.IsMaster(user.FindFirstValue(ClaimTypes.Role))
            || !string.IsNullOrEmpty(user.FindFirstValue(OfertioClaimTypes.ImpersonatorId))
            || !Guid.TryParse(user.FindFirstValue(OfertioClaimTypes.TenantId), out var tenantId))
        {
            await _next(context);
            return;
        }

        var status = await accessReader.GetStatusAsync(tenantId, context.RequestAborted);
        if (!TenantLifecycleRules.IsAccessBlocked(status))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            status = false,
            descricao = TenantLifecycleRules.FormatBlockedMessage(status),
            errorCode = "TENANT_BLOCKED"
        });
    }

    private static bool IsExempt(PathString path) =>
        ExemptPrefixes.Any(prefix => path.StartsWithSegments(prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
}
