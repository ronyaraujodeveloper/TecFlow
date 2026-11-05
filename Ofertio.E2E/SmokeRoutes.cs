namespace Ofertio.E2E;

/// <summary>Telas internas da fumaça. Nenhuma delas grava dado.</summary>
public static class SmokeRoutes
{
    public static readonly IReadOnlyList<(string Path, string Heading)> Pages =
    [
        ("/dashboard", "Painel"),
        ("/meus-dados", "Meus dados"),
        ("/propaganda/produtos", "Produtos de propaganda"),
        ("/busca", "Buscar Ofertas"),
        ("/integracoes/grupos/monitorados", "Grupos monitorados"),
        ("/minhas-lojas", "Minhas Lojas / Integrações")
    ];
}
