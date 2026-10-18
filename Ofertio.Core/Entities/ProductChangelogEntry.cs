namespace Ofertio.Core.Entities;

/// <summary>Publicação de novidades do produto (What's New).</summary>
public class ProductChangelogEntry : BaseEntity
{
    public string Version { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public bool IsPublished { get; set; } = true;
}
