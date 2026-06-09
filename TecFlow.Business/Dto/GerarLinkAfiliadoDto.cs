namespace TecFlow.Business.Dto;

/// <summary>Payload de entrada para geração omnichannel de link de afiliado.</summary>
public class GerarLinkAfiliadoDto
{
    /// <summary>URL bruta colada pelo usuário.</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>Escopo da loja selecionada no seletor global do Blazor.</summary>
    public Guid StoreId { get; set; }

    /// <summary>Apelido opcional para rastreio ou identificação humana.</summary>
    public string? CustomNickname { get; set; }
}
