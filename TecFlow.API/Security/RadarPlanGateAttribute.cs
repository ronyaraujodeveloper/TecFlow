using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;

namespace TecFlow.API.Security;

/// <summary>Bloqueia o Radar de Ofertas quando o plano SaaS do usuário não inclui o recurso.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RadarPlanGateAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var planLimits = context.HttpContext.RequestServices.GetService<IPlanLimitsService>();
        var claim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (planLimits is not null && int.TryParse(claim, out var userId) && userId > 0)
        {
            var limits = await planLimits.GetForUserAsync(userId, context.HttpContext.RequestAborted);
            if (!limits.IsUnlimited && !limits.RadarEnabled)
            {
                context.Result = new OkObjectResult(new
                {
                    status = false,
                    descricao = PlanLimitRules.RadarBlockedMessage(limits),
                    errorCode = "PLAN_FEATURE_BLOCKED"
                });
                return;
            }
        }

        await next();
    }
}
