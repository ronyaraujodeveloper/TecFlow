using System.Security.Claims;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Security;

namespace Ofertio.SharedUi.Services.Auth;

/// <summary>Leitura das claims do backoffice na sessão da UI (papel, visão e emulação).</summary>
public static class MasterAccess
{
    public const string ViewMaster = "master";
    public const string ViewClient = "cliente";

    public static string Role(ClaimsPrincipal? user) =>
        MasterRoles.Normalize(Value(user, ClaimTypes.Role));

    public static bool IsMaster(ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true && MasterRoles.IsMaster(Value(user, ClaimTypes.Role));

    public static bool IsImpersonating(ClaimsPrincipal? user) =>
        !string.IsNullOrEmpty(Value(user, OfertioClaimTypes.ImpersonatorAccessToken));

    public static bool IsMasterView(ClaimsPrincipal? user) =>
        IsMaster(user) && !string.Equals(Value(user, OfertioClaimTypes.ViewMode), ViewClient, StringComparison.OrdinalIgnoreCase);

    public static bool Has(ClaimsPrincipal? user, string permission) =>
        IsMaster(user) && MasterPermissionMatrix.Has(Role(user), permission);

    public static string ImpersonatedTenant(ClaimsPrincipal? user) =>
        Value(user, OfertioClaimTypes.ImpersonatedTenantName) ?? string.Empty;

    private static string? Value(ClaimsPrincipal? user, string claimType) => user?.FindFirst(claimType)?.Value;
}
