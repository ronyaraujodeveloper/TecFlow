using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

[Table("Conteudos")]
public class Content : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    [Column("Nome")]
    public string Name { get; set; } = string.Empty;

    [Column("Descricao")]
    public string Description { get; set; } = string.Empty;

    [Column("DataInicio")]
    public DateTime StartDate { get; set; }

    [Column("DataFim")]
    public DateTime EndDate { get; set; }

    [Column("Orcamento")]
    public decimal Budget { get; set; }

    public ICollection<Affiliate> Affiliates { get; set; } = new List<Affiliate>();
    public int OwnerId { get; set; }
    public UserAccount? Owner { get; set; }
}
