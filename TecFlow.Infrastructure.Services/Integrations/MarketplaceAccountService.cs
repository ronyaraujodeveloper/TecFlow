using TecFlow.Business.Dto;
using TecFlow.Business.Mappings;
using TecFlow.Core.Entities;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Integrations;

/// <summary>Mapeia MarketplaceAccounts (inclusive registros antigos com NULL) sem NullReferenceException.</summary>
public sealed class MarketplaceAccountService
{
    public MarketplaceAccountDto MapToDto(MarketplaceAccount? account, IntegracaoLoja? integration = null) =>
        MarketplaceAccountMapper.ToDto(account, integration);

    public ConvertLinkResponseDto MapToConvertLinkResponse(MarketplaceAccount? account, IntegracaoLoja? integration = null) =>
        MarketplaceAccountMapper.ToConvertLinkResponse(account, integration);
}
