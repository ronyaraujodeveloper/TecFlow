using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Contrato Strategy para geração de deep links de afiliado por marketplace.
/// </summary>
public interface IPlatformLinkStrategy
{
    MarketplaceType PlatformType { get; }

    string PlatformName { get; }

    bool CanProcess(string url);

    Task<string> GenerateDeepLinkAsync(
        string originalUrl,
        Guid storeId,
        string affiliateId,
        CancellationToken cancellationToken = default);
}
