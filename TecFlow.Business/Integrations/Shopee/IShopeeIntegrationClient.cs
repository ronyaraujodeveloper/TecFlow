using TecFlow.Business.Integrations.Common;

namespace TecFlow.Business.Integrations.Shopee;

/// <summary>Cliente HTTP tipado para a Shopee — fase 3.1 (transporte; OAuth na 3.2).</summary>
public interface IShopeeIntegrationClient : IExternalIntegrationClient
{
    ShopeeIntegrationOptions Options { get; }
}
