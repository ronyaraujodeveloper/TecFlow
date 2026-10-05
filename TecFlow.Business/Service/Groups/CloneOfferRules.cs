namespace TecFlow.Business.Service.Groups;

public static class CloneOfferRules
{
    public const string GroupCloneSource = "GroupClone";

    public const string SuccessToast =
        "Oferta clonada com sucesso! Link de comissão gerado e salvo em Histórico de Links.";

    public const string ScheduleActionLabel = "Agendar Disparo";

    public const string MissingStoreMessage =
        "Não foi possível converter o link. Cadastre uma loja ativa da mesma plataforma.";

    public const string ResolveFailedMessage =
        "Não foi possível desencurtar o link até a loja. Tente de novo ou abra o link original.";
}
