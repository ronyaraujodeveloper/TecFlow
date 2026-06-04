namespace TecFlow.Core.Abstractions;

/// <summary>Entidade vinculada a uma loja marketplace (ShopId) dentro do tenant.</summary>
public interface IShopScopedEntity
{
    string ShopId { get; set; }
}
