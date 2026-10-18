namespace Ofertio.Business.Dto;

/// <summary>Retorno bruto do Mercado Livre repassado pela WebUi (sem sessão) à API.</summary>
public sealed class MarketplaceOAuthCallbackRequestDto
{
    public string? Code { get; set; }

    /// <summary>State selado (prefixo <c>tfs1.</c>) gerado em authorize-url.</summary>
    public string? State { get; set; }

    public string? Error { get; set; }
}

public sealed class MarketplaceOAuthCallbackResultDto
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    /// <summary>Origem onde o OAuth começou + <c>/marketplace-accounts?status=success|error</c>.</summary>
    public string ReturnUrl { get; set; } = "/marketplace-accounts?status=success";
}
