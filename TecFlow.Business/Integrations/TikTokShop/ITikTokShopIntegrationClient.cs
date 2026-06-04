using TecFlow.Business.Integrations.Common;

namespace TecFlow.Business.Integrations.TikTokShop;

/// <summary>Cliente HTTP tipado para a TikTok Shop — fase 3.1 (transporte; OAuth na 3.2).</summary>
public interface ITikTokShopIntegrationClient : IExternalIntegrationClient
{
    TikTokShopIntegrationOptions Options { get; }
}
