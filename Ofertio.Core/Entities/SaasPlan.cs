namespace Ofertio.Core.Entities;

/// <summary>Plano de assinatura SaaS com preços e limites de recursos (0 = ilimitado).</summary>
public class SaasPlan : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }

    public decimal AnnualPrice { get; set; }

    public int MaxStores { get; set; }

    public int DailyVipQuota { get; set; }

    public bool RadarEnabled { get; set; } = true;

    /// <summary>Disparos de WhatsApp e Telegram somados por dia. 0 = ilimitado.</summary>
    public int MaxDailySchedules { get; set; }

    /// <summary>Vantagens exibidas no modal de Meu plano, uma por linha.</summary>
    public string? Highlights { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
