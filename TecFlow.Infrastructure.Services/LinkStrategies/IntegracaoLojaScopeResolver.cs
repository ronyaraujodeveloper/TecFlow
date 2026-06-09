using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

/// <summary>Resolve IntegracaoLoja a partir do escopo global (Guid) e valida tokens.</summary>
public sealed class IntegracaoLojaScopeResolver : IIntegracaoLojaScopeResolver
{
    private readonly IIntegracaoLojaRepository _integracaoLojaRepository;
    private readonly ILogger<IntegracaoLojaScopeResolver> _logger;

    public IntegracaoLojaScopeResolver(
        IIntegracaoLojaRepository integracaoLojaRepository,
        ILogger<IntegracaoLojaScopeResolver> logger)
    {
        _integracaoLojaRepository = integracaoLojaRepository;
        _logger = logger;
    }

    public async Task<IntegracaoLoja> ResolveAsync(
        Guid storeScopeId,
        int userId,
        MarketplaceType expectedPlatform,
        CancellationToken cancellationToken = default)
    {
        IntegracaoLoja? store = null;

        var decodedId = IntegracaoLojaScopeHelper.TryDecodeStoreScope(storeScopeId);
        if (decodedId.HasValue)
        {
            store = await _integracaoLojaRepository.GetByIdAsync(decodedId.Value, cancellationToken);
        }

        if (store is null)
        {
            var stores = await _integracaoLojaRepository.ListByUserIdAsync(userId, cancellationToken);
            store = stores.FirstOrDefault(s => s.TenantId == storeScopeId);
        }

        if (store is null || store.UserId != userId)
        {
            _logger.LogWarning(
                "IntegracaoLoja não encontrada para StoreScope={StoreScopeId} e UserId={UserId}.",
                storeScopeId,
                userId);

            throw new AffiliateLinkGenerationException(
                "Loja não encontrada para o escopo selecionado. Verifique o seletor global no topo do painel.");
        }

        if (store.PlatformType != expectedPlatform)
        {
            throw new AffiliateLinkGenerationException(
                "A loja selecionada não corresponde à plataforma da URL informada.");
        }

        if (store.Status == MarketplaceIntegrationStatus.Inactive)
        {
            throw new AffiliateLinkGenerationException(
                "A loja selecionada está inativa. Reconecte-a em Minhas Lojas / Integrações.");
        }

        if (store.Status == MarketplaceIntegrationStatus.Expired || store.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AffiliateLinkGenerationException(
                "O token de acesso da loja expirou. Reconecte a conta em Minhas Lojas / Integrações.");
        }

        if (string.IsNullOrWhiteSpace(store.AccessToken))
        {
            throw new AffiliateLinkGenerationException(
                "Token de acesso da loja indisponível. Reconecte a integração em Minhas Lojas / Integrações.");
        }

        return store;
    }
}
