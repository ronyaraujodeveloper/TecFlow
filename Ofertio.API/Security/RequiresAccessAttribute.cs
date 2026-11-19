using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.API.Security;

/// <summary>
/// Bloqueia o endpoint quando a chave da hierarquia de /master/menu-permissions (tela ou componente)
/// está negada para o plano do usuário. Mesma regra da sidebar e do AccessGate; Masters sempre passam.
/// Responde no formato { status, descricao, errorCode } usado pelo cliente (um 403 seria lido como sessão expirada).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresAccessAttribute : Attribute, IAsyncActionFilter
{
    public const string ErrorCode = "ACCESS_DENIED";

    public RequiresAccessAttribute(string key) => Key = key;

    public string Key { get; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var menus = context.HttpContext.RequestServices.GetService<IMenuPermissionService>();
        var claim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (menus is not null
            && int.TryParse(claim, out var userId)
            && userId > 0
            && !await menus.IsAllowedAsync(userId, Key, context.HttpContext.RequestAborted))
        {
            context.Result = new OkObjectResult(new
            {
                status = false,
                descricao = "Este recurso não está disponível no seu plano.",
                errorCode = ErrorCode
            });
            return;
        }

        await next();
    }
}
