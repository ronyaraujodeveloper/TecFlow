using Ofertio.Business.Service.Master;

namespace Ofertio.SharedUi.Services.Navigation;

public sealed record MasterRouteLink(
    string Href,
    string Label,
    string Icon,
    string? Permission,
    IReadOnlyList<MasterRouteLink>? Children = null);

/// <summary>Convenção de rotas do backoffice: toda administração global vive sob /master/*.</summary>
public static class MasterRoutes
{
    public const string Prefix = "/master";
    public const string Dashboard = "/master";
    public const string Tenants = "/master/tenants";
    public const string Plans = "/master/planos";
    public const string MenuPermissions = "/master/menu-permissions";
    /// <summary>Cofre isolado das chaves globais da plataforma (AppId/SecretKey, Evolution, Telegram, OpenAI, ASAAS).</summary>
    public const string Vault = "/master/vault";
    public const string LegacySettings = "/master/configuracoes";
    /// <summary>Conexões Master com APIs oficiais (Mercado Livre; previstas TikTok Shop e Shopee) usadas por todos os clientes.</summary>
    public const string GlobalIntegrations = GlobalIntegrationRules.OverviewPath;
    public const string GlobalMercadoLivre = GlobalIntegrationRules.MercadoLivrePath;
    /// <summary>Somente contas corporativas de teste/dev da Ofertio; lojas dos afiliados ficam em /marketplace-accounts.</summary>
    public const string DeveloperAccounts = "/master/marketplace-accounts";
    public const string Operators = "/master/operadores";
    public const string Users = "/master/usuarios";
    public const string Audit = "/master/auditoria";
    public const string Communication = "/master/comunicacao";

    /// <summary>Saída da emulação: chamada com a sessão do tenant emulado (papel TenantUser).</summary>
    public const string ImpersonationExit = "/master/emular/sair";

    public static readonly IReadOnlyList<MasterRouteLink> Links =
    [
        new(Dashboard, "Dashboard executivo", "bi-graph-up-arrow", MasterPermissions.ViewMetrics),
        new(Tenants, "Inquilinos (Tenants)", "bi-buildings", MasterPermissions.ViewTenants),
        new(Plans, "Planos & assinaturas", "bi-credit-card", MasterPermissions.ManagePlans),
        new(MenuPermissions, "Menus por plano", "bi-diagram-3", MasterPermissions.ManagePlans),
        new(Vault, "Cofre de chaves (Vault)", "bi-safe", MasterPermissions.ManageGlobalKeys),
        new(GlobalIntegrations, "Integrações Globais", "bi-globe-americas", MasterPermissions.ManageGlobalKeys,
        [
            new(GlobalMercadoLivre, "API Mercado Livre", "bi-bag-check", MasterPermissions.ManageGlobalKeys)
        ]),
        new(DeveloperAccounts, "Contas de teste / dev", "bi-person-badge", MasterPermissions.ManageGlobalKeys),
        new(Operators, "Operadores & RBAC", "bi-people", MasterPermissions.ManageOperators),
        new(Audit, "Trilha de auditoria", "bi-journal-lock", MasterPermissions.ViewAudit),
        new(Communication, "Comunicação global", "bi-megaphone", MasterPermissions.ManageCommunication)
    ];

    public static bool IsMasterRoute(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = "/" + path.Trim().TrimStart('/').Split('?', '#')[0].TrimEnd('/');
        return normalized.Equals(Prefix, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(Prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Rotas /master/* que o guard deixa passar para quem não é operador master.</summary>
    public static bool IsGuardExempt(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && path.Split('?', '#')[0].TrimEnd('/').Equals(ImpersonationExit, StringComparison.OrdinalIgnoreCase);
}
