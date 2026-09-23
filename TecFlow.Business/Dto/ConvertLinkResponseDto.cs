namespace TecFlow.Business.Dto;

/// <summary>Resposta de conversão de link com identificadores nulos-seguros da loja.</summary>
public class ConvertLinkResponseDto : GerarLinkAfiliadoResponseDto
{
    public string ShopId { get; set; } = string.Empty;

    public string TrackingId { get; set; } = string.Empty;
}
