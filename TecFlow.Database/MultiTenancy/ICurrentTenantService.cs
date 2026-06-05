namespace TecFlow.Database.MultiTenancy;

/// <summary>Contexto do inquilino e loja ativos (JWT / cabeçalho HTTP).</summary>
public interface ICurrentTenantService
{
    Guid? TenantId { get; }

    /// <summary>ShopId selecionado no painel; null = visão consolidada de todas as lojas do tenant.</summary>
    string? ShopId { get; }

    /// <summary>Desativa filtros globais (migrations, jobs administrativos).</summary>
    bool BypassTenantFilters { get; set; }
}
