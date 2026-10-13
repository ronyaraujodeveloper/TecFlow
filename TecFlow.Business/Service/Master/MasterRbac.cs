namespace TecFlow.Business.Service.Master;

public static class MasterRoles
{
    public const string TenantUser = "TenantUser";
    public const string SuperAdmin = "SuperAdmin";
    public const string SuporteTecnico = "SuporteTecnico";
    public const string FinanceiroSaaS = "FinanceiroSaaS";

    /// <summary>Valor para [Authorize(Roles = ...)] cobrindo todos os operadores master.</summary>
    public const string AnyMaster = SuperAdmin + "," + SuporteTecnico + "," + FinanceiroSaaS;

    public static readonly IReadOnlyList<string> MasterRoleList = [SuperAdmin, SuporteTecnico, FinanceiroSaaS];

    public static bool IsMaster(string? role) =>
        MasterRoleList.Any(item => string.Equals(item, role?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string? role) =>
        MasterRoleList.FirstOrDefault(item => string.Equals(item, role?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? TenantUser;

    public static string Label(string? role) => Normalize(role) switch
    {
        SuperAdmin => "Super Admin",
        SuporteTecnico => "Suporte Técnico",
        FinanceiroSaaS => "Financeiro SaaS",
        _ => "Usuário comum"
    };
}

public static class MasterPermissions
{
    public const string ManageOperators = "ManageOperators";
    public const string ViewAudit = "ViewAudit";
    public const string ManageGlobalKeys = "ManageGlobalKeys";
    public const string ManagePlans = "ManagePlans";
    public const string ManageBilling = "ManageBilling";
    public const string AdjustCredits = "AdjustCredits";
    public const string ManageTenants = "ManageTenants";
    public const string ViewTenants = "ViewTenants";
    public const string Impersonate = "Impersonate";
    public const string ViewMetrics = "ViewMetrics";
    public const string ManageCommunication = "ManageCommunication";

    public static readonly IReadOnlyList<string> All =
    [
        ManageOperators, ViewAudit, ManageGlobalKeys, ManagePlans, ManageBilling, AdjustCredits,
        ManageTenants, ViewTenants, Impersonate, ViewMetrics, ManageCommunication
    ];

    public static string PolicyName(string permission) => "master:" + permission;

    public static string Label(string permission) => permission switch
    {
        ManageOperators => "Gerir operadores master",
        ViewAudit => "Ver trilha de auditoria",
        ManageGlobalKeys => "Alterar chaves globais de API",
        ManagePlans => "Alterar planos SaaS",
        ManageBilling => "Cobrança e gateway de pagamento",
        AdjustCredits => "Ajustar créditos de Achadinhos",
        ManageTenants => "Moderar inquilinos (suspender, cancelar, trial)",
        ViewTenants => "Ver central de inquilinos",
        Impersonate => "Emular tenant (impersonation)",
        ViewMetrics => "Ver métricas executivas",
        ManageCommunication => "Avisos, notificações e changelog",
        _ => permission
    };
}

/// <summary>Nomes de policy para [Authorize(Policy = ...)].</summary>
public static class MasterPolicies
{
    private const string Prefix = "master:";

    public const string ManageOperators = Prefix + MasterPermissions.ManageOperators;
    public const string ViewAudit = Prefix + MasterPermissions.ViewAudit;
    public const string ManageGlobalKeys = Prefix + MasterPermissions.ManageGlobalKeys;
    public const string ManagePlans = Prefix + MasterPermissions.ManagePlans;
    public const string ManageBilling = Prefix + MasterPermissions.ManageBilling;
    public const string AdjustCredits = Prefix + MasterPermissions.AdjustCredits;
    public const string ManageTenants = Prefix + MasterPermissions.ManageTenants;
    public const string ViewTenants = Prefix + MasterPermissions.ViewTenants;
    public const string Impersonate = Prefix + MasterPermissions.Impersonate;
    public const string ViewMetrics = Prefix + MasterPermissions.ViewMetrics;
    public const string ManageCommunication = Prefix + MasterPermissions.ManageCommunication;
}

/// <summary>Matriz de permissões por papel do backoffice (RBAC).</summary>
public static class MasterPermissionMatrix
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Matrix =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [MasterRoles.SuperAdmin] = MasterPermissions.All.ToHashSet(),
            [MasterRoles.SuporteTecnico] = new HashSet<string>
            {
                MasterPermissions.ViewTenants,
                MasterPermissions.ManageTenants,
                MasterPermissions.Impersonate,
                MasterPermissions.ViewMetrics,
                MasterPermissions.ManageCommunication
            },
            [MasterRoles.FinanceiroSaaS] = new HashSet<string>
            {
                MasterPermissions.ViewTenants,
                MasterPermissions.ManagePlans,
                MasterPermissions.ManageBilling,
                MasterPermissions.AdjustCredits,
                MasterPermissions.ViewMetrics
            }
        };

    public static bool Has(string? role, string permission) =>
        role is not null
        && Matrix.TryGetValue(role.Trim(), out var permissions)
        && permissions.Contains(permission);

    public static IReadOnlyList<string> PermissionsFor(string? role) =>
        MasterPermissions.All.Where(permission => Has(role, permission)).ToList();

    public static IReadOnlyList<string> RolesFor(string permission) =>
        MasterRoles.MasterRoleList.Where(role => Has(role, permission)).ToList();
}
