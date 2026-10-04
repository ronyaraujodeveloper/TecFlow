namespace TecFlow.Core.Entities;

public static class GroupOfferStatuses
{
    public const string Verificando = "Verificando";
    public const string Ativo = "Ativo";
    public const string Esgotado = "Esgotado";
    public const string PrecoAlterado = "PrecoAlterado";

    public static string ToUiLabel(string? status) => status switch
    {
        Ativo => "Ativo",
        Esgotado => "Esgotado",
        PrecoAlterado => "Preço Alterado",
        _ => "Verificando"
    };
}
