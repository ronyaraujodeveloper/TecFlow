using Ofertio.Business.Dto;
using Ofertio.Database.Entity;

namespace Ofertio.Business.Interfaces.Services;

/// <summary>Fallback da Shopee Open API (credenciais AppKey/AppSecret da conta conectada).</summary>
public interface IShopeeService
{
    Task<ProductMetadataDto?> TryGetAffiliateItemDetailsAsync(
        string productUrl,
        IntegracaoLoja store,
        CancellationToken cancellationToken = default);
}
