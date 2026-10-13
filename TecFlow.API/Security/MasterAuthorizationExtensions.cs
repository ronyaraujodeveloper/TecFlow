using TecFlow.Business.Service.Master;

namespace TecFlow.API.Security;

public static class MasterAuthorizationExtensions
{
    /// <summary>Uma policy "master:{permissão}" por item da matriz RBAC.</summary>
    public static IServiceCollection AddTecFlowMasterAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in MasterPermissions.All)
            {
                var roles = MasterPermissionMatrix.RolesFor(permission).ToArray();
                options.AddPolicy(MasterPermissions.PolicyName(permission), policy =>
                    policy.RequireAuthenticatedUser().RequireRole(roles));
            }
        });
        return services;
    }
}
