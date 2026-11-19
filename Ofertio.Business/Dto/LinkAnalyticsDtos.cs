namespace Ofertio.Business.Dto;

/// <summary>Resumo de cliques do período (robôs de prévia ficam de fora e aparecem à parte).</summary>
public class AnalyticsSummaryDto
{
    public int Clicks { get; set; }
    public int UniqueClicks { get; set; }
    /// <summary>Aberturas automáticas (prévia de link de WhatsApp, Telegram etc.) que não entram nos cliques.</summary>
    public int PreviewsFiltered { get; set; }
    public int LinksGenerated { get; set; }
    public int PreviousClicks { get; set; }
    public int PreviousUniqueClicks { get; set; }
    public DateOnly? BestDay { get; set; }
    public int BestDayClicks { get; set; }
}

public class AnalyticsPointDto
{
    public DateOnly Start { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Total { get; set; }
    public Dictionary<string, int> ByChannel { get; set; } = new();
}

public class AnalyticsBreakdownDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public double Percent { get; set; }
}

public class AnalyticsGroupDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public int UniqueClicks { get; set; }
}

public class AnalyticsProductDto
{
    public Guid LinkGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Platform { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public int UniqueClicks { get; set; }
}

/// <summary>Visão geral do painel: tudo que a primeira tela precisa em uma chamada só.</summary>
public class AnalyticsOverviewDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Grain { get; set; } = "day";
    public AnalyticsSummaryDto Summary { get; set; } = new();
    public List<AnalyticsPointDto> Series { get; set; } = [];
    /// <summary>Canais presentes no período, na ordem de uso (cores estáveis no gráfico).</summary>
    public List<string> ChannelOrder { get; set; } = [];
    public List<AnalyticsBreakdownDto> Channels { get; set; } = [];
    public List<AnalyticsBreakdownDto> Devices { get; set; } = [];
    public List<AnalyticsGroupDto> Groups { get; set; } = [];
    public List<AnalyticsProductDto> TopProducts { get; set; } = [];
    /// <summary>7 linhas (segunda a domingo) por 24 horas, no horário de Brasília.</summary>
    public List<List<int>> Heatmap { get; set; } = [];
    public bool HasAnyLink { get; set; }

    /// <summary>Visitas, conversões e entradas em grupo da página pública no período.</summary>
    public AnalyticsFunnelDto Funnel { get; set; } = new();

    /// <summary>Cliques por estado e por cidade (região estimada pelo IP). Vazio sem a base de localização.</summary>
    public List<AnalyticsBreakdownDto> States { get; set; } = [];

    public List<AnalyticsBreakdownDto> Cities { get; set; } = [];

    public bool HasRegionData { get; set; }
}

public class AnalyticsProductDetailDto
{
    public AnalyticsProductDto Product { get; set; } = new();
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Grain { get; set; } = "day";
    public AnalyticsSummaryDto Summary { get; set; } = new();
    public List<AnalyticsPointDto> Series { get; set; } = [];
    public List<string> ChannelOrder { get; set; } = [];
    public List<AnalyticsBreakdownDto> Channels { get; set; } = [];
    public List<AnalyticsGroupDto> Groups { get; set; } = [];
    public List<AnalyticsBreakdownDto> Devices { get; set; } = [];
    public List<AnalyticsBreakdownDto> Shops { get; set; } = [];
    public List<List<int>> Heatmap { get; set; } = [];
}

/// <summary>Funil da página pública: quem olhou, quem converteu um link e quem foi entrar num grupo. Robôs ficam de fora.</summary>
public class AnalyticsFunnelDto
{
    public int Views { get; set; }
    public int UniqueViews { get; set; }
    public int Conversions { get; set; }
    public int JoinClicks { get; set; }
    public int PreviousViews { get; set; }
    public int PreviousConversions { get; set; }
    public int PreviousJoinClicks { get; set; }
    public List<AnalyticsBreakdownDto> JoinByChannel { get; set; } = [];
    public bool HasData => Views > 0 || Conversions > 0 || JoinClicks > 0;
}
