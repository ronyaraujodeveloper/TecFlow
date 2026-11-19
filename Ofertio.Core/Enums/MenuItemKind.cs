namespace Ofertio.Core.Enums;

/// <summary>Nível do item na hierarquia controlada em /master/menu-permissions: Grupo → Tela → Componente.</summary>
public enum MenuItemKind
{
    /// <summary>Título de agrupamento (sem rota); pode conter telas e outros grupos.</summary>
    Group = 1,

    /// <summary>Tela com rota própria.</summary>
    Page = 2,

    /// <summary>Parte de uma tela (gráfico, input, botão, seção); o pai é sempre uma Tela.</summary>
    Component = 3,

    /// <summary>Chave de permissão global (sem rota) que libera um conjunto de telas.</summary>
    Feature = 4
}
