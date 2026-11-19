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

    /// <summary>Quantos botões de entrada em grupo a página pública pode ter. 0 = ilimitado.</summary>
    public int MaxJoinLinks { get; set; }

    /// <summary>Vantagens exibidas no modal de Meu plano, uma por linha.</summary>
    public string? Highlights { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Só vale para o plano Trial: dias grátis concedidos no cadastro (campanhas como Black Friday aumentam este valor).</summary>
    public int TrialDays { get; set; }

    /// <summary>Só vale para o plano Trial: exige cartão no cadastro e cobra o plano seguinte ao fim do teste.</summary>
    public bool RequireCard { get; set; }

    /// <summary>Só vale para o plano Trial: plano em que a conta cai ao fim do teste quando há cartão.</summary>
    public string? PostTrialPlanCode { get; set; }

    public int SortOrder { get; set; }
}
