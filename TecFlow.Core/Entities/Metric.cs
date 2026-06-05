using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

[Table("Metricas")]
public class Metric : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    [Column("CampanhaId")]
    public int CampaignId { get; set; }

    public Campaign? Campaign { get; set; }

    [Column("Visualizacoes")]
    public int Views { get; set; }

    [Column("Cliques")]
    public int Clicks { get; set; }

    [Column("Vendas")]
    public int Sales { get; set; }

    [Column("Investimento")]
    public decimal Investment { get; set; }

    [Column("Receita")]
    public decimal Revenue { get; set; }

    public int OwnerId { get; set; }
    public UserAccount? Owner { get; set; }

    [Column("MetricaId")]
    public int? ParentMetricId { get; set; }

    public ICollection<Metric> ChildMetrics { get; set; } = new List<Metric>();

    [NotMapped]
    public decimal Profit => Revenue - Investment;

    [NotMapped]
    public double Ctr => Views == 0 ? 0 : (double)Clicks / Views * 100;

    [NotMapped]
    public double Conversion => Clicks == 0 ? 0 : (double)Sales / Clicks * 100;

    [NotMapped]
    public double Roi => Investment == 0 ? 0 : (double)(Profit / Investment) * 100;
}
