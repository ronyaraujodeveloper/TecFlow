using Microsoft.AspNetCore.Mvc.Filters;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.API.Security;

/// <summary>Endpoints do Painel Master operam sobre todos os inquilinos (sem filtro nem carimbo de tenant).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MasterScopeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var currentTenant = context.HttpContext.RequestServices.GetService<ICurrentTenantService>();
        if (currentTenant is not null)
        {
            currentTenant.BypassTenantFilters = true;
        }
    }
}
