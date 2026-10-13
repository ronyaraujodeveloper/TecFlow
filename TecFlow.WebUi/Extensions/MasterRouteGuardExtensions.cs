using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.Navigation;

namespace TecFlow.WebUi.Extensions;

public static class MasterRouteGuardExtensions
{
    /// <summary>Redireciona para "/" qualquer acesso a /master/* de quem não é operador master (inclui navegação aprimorada do Blazor).</summary>
    public static IApplicationBuilder UseMasterRouteGuard(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value;
            if (MasterRoutes.IsMasterRoute(path)
                && !MasterRoutes.IsGuardExempt(path)
                && !MasterAccess.IsMaster(context.User))
            {
                context.Response.Redirect("/");
                return;
            }

            await next();
        });
}
