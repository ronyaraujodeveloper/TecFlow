namespace Ofertio.Core.Entities;

/// <summary>Item da árvore de navegação do cliente. Pastas (IsFolder) agrupam itens e não têm rota.</summary>
public class MenuItem : BaseEntity
{
    /// <summary>Identificador estável usado pelo catálogo padrão (ex.: integracoes.radar) ou "pasta.{id}" para pastas criadas no Master.</summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Route { get; set; }

    public string? Icon { get; set; }

    /// <summary>Selo exibido ao lado do título na sidebar (ex.: "EM BREVE").</summary>
    public string? Badge { get; set; }

    public bool IsFolder { get; set; }

    public int? ParentId { get; set; }

    public MenuItem? Parent { get; set; }

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Posição/pasta definida manualmente no Master (drag &amp; drop); o catálogo padrão não sobrescreve mais pai e ordem.</summary>
    public bool IsCustomized { get; set; }
}
