using TecFlow.Business.Dto;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Interfaces.Services;

/// <summary>Fallback da Shopee Open API (credenciais AppKey/AppSecret da conta conectada).</summary>
public interface IShopeeService
{
    Task<ProductMetadataDto?> TryGetAffiliateItemDetailsAsync(
        string productUrl,
        IntegracaoLoja store,
        CancellationToken cancellationToken = default);
}
