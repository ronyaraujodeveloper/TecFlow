namespace TecFlow.Core.Entities;

public static class GroupOfferStatuses
{
    public const string Verificando = "Verificando";
    public const string Ativo = "Ativo";
    public const string Esgotado = "Esgotado";
    public const string PrecoAlterado = "PrecoAlterado";
    public const string Inativo = "Inativo";
    public const string VitrineGenerica = "VitrineGenerica";
    public const string PrecoSuspeito = "PrecoSuspeito";

    public static string ToUiLabel(string? status) => status switch
    {
        Ativo => "Ativo",
        Esgotado => "Esgotado",
        PrecoAlterado => "Preço Alterado",
        Inativo => "Inativo",
        VitrineGenerica => "Vitrine Genérica",
        PrecoSuspeito => "Preço Suspeito",
        _ => "Verificando"
    };

    public static bool IsInactive(string? status) =>
        status is Esgotado or Inativo or VitrineGenerica;

    public static bool IsRevalidatable(string? status) =>
        status is Ativo or PrecoAlterado or Verificando;
}
