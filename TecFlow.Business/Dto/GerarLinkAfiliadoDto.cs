namespace TecFlow.Business.Dto;

/// <summary>Payload de entrada para geração omnichannel de link de afiliado.</summary>
public class GerarLinkAfiliadoDto
{
    /// <summary>URL bruta colada pelo usuário.</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>Escopo da loja selecionada no seletor global do Blazor.</summary>
    public Guid StoreId { get; set; }

    /// <summary>Escopos das contas marcadas no gerador (mesma plataforma).</summary>
    public List<Guid> StoreIds { get; set; } = [];

    public IReadOnlyList<Guid> ResolveStoreScopes()
    {
        var scopes = new List<Guid>();
        if (StoreIds is { Count: > 0 })
        {
            scopes.AddRange(StoreIds.Where(id => id != Guid.Empty));
        }

        if (StoreId != Guid.Empty && !scopes.Contains(StoreId))
        {
            scopes.Insert(0, StoreId);
        }

        return scopes;
    }

    /// <summary>Apelido opcional para rastreio ou identificação humana.</summary>
    public string? CustomNickname { get; set; }

    /// <summary>Tenant ativo no seletor global do painel (Blazor).</summary>
    public Guid? TenantId { get; set; }

    /// <summary>ShopId da loja ativa no seletor global do painel (Blazor).</summary>
    public string? ShopId { get; set; }
}
