namespace Ofertio.Core.Security;

public static class OfertioClaimTypes
{
    public const string AccessToken = "Ofertio:access_token";
    public const string Platform = "Ofertio:platform";
    public const string AuthProvider = "Ofertio:auth_provider";
    public const string TenantId = "Ofertio:tenant_id";
    public const string ShopId = "Ofertio:shop_id";

    /// <summary>Id do operador master que emitiu o token de emulação.</summary>
    public const string ImpersonatorId = "Ofertio:impersonator_id";
    public const string ImpersonatorEmail = "Ofertio:impersonator_email";
    public const string ImpersonatedTenantName = "Ofertio:impersonated_tenant";

    /// <summary>Cookie WebUi: token original do operador para voltar ao Painel Master.</summary>
    public const string ImpersonatorAccessToken = "Ofertio:impersonator_access_token";

    /// <summary>Cookie WebUi: visão ativa do operador master (master ou cliente).</summary>
    public const string ViewMode = "Ofertio:view_mode";
}
