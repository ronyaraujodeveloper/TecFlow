namespace TecFlow.Core.Entities;

/// <summary>Item da árvore de navegação do cliente. Itens sem rota são grupos (accordion).</summary>
public class MenuItem : BaseEntity
{
    /// <summary>Identificador estável usado pelo catálogo padrão (ex.: integracoes.radar).</summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Route { get; set; }

    public string? Icon { get; set; }

    public int? ParentId { get; set; }

    public MenuItem? Parent { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;
}
