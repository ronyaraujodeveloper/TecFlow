namespace TecFlow.Core.Security;

public static class TecFlowClaimTypes
{
    public const string AccessToken = "TecFlow:access_token";
    public const string Platform = "TecFlow:platform";
    public const string AuthProvider = "TecFlow:auth_provider";
    public const string TenantId = "TecFlow:tenant_id";
    public const string ShopId = "TecFlow:shop_id";

    /// <summary>Id do operador master que emitiu o token de emulação.</summary>
    public const string ImpersonatorId = "TecFlow:impersonator_id";
    public const string ImpersonatorEmail = "TecFlow:impersonator_email";
    public const string ImpersonatedTenantName = "TecFlow:impersonated_tenant";

    /// <summary>Cookie WebUi: token original do operador para voltar ao Painel Master.</summary>
    public const string ImpersonatorAccessToken = "TecFlow:impersonator_access_token";

    /// <summary>Cookie WebUi: visão ativa do operador master (master ou cliente).</summary>
    public const string ViewMode = "TecFlow:view_mode";
}
