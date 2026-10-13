namespace TecFlow.Infrastructure.Services.Master;

public static class MasterAuditActions
{
    public const string OperatorCreated = "operador.criado";
    public const string OperatorUpdated = "operador.alterado";
    public const string OperatorActivation = "operador.ativacao";
    public const string SettingsChanged = "chaves.alteradas";
    public const string PlanSaved = "plano.salvo";
    public const string TenantLifecycle = "tenant.ciclo";
    public const string TenantPlanChanged = "tenant.plano";
    public const string CreditsAdjusted = "creditos.ajuste";
    public const string MediaPurged = "tenant.midia.purga";
    public const string GatewaySubscription = "gateway.assinatura";
    public const string ImpersonationStarted = "emulacao.inicio";
    public const string ImpersonationStopped = "emulacao.fim";
    public const string BannerSaved = "aviso.salvo";
    public const string BannerDeleted = "aviso.excluido";
    public const string NotificationSent = "notificacao.enviada";
    public const string ChangelogSaved = "changelog.salvo";
    public const string ChangelogDeleted = "changelog.excluido";
}
