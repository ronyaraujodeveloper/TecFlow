using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

/// <summary>Conexões Master com APIs oficiais (Painel Master ➔ Integrações Globais), usadas por todos os clientes.</summary>
public interface IGlobalIntegrationService
{
    Task<GlobalIntegrationStatusDto> GetMercadoLivreStatusAsync(CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> SaveMercadoLivreSettingsAsync(
        MasterActor actor,
        GlobalIntegrationSettingsDto request,
        CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> DisconnectMercadoLivreAsync(
        MasterActor actor,
        CancellationToken cancellationToken = default);

    /// <summary>Troca o <c>code</c> do OAuth pelo token Master; só operadores com acesso às chaves globais.</summary>
    Task<MasterOperationResultDto> CompleteMercadoLivreAuthorizationAsync(
        int userId,
        string code,
        string? redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken = default);

    Task<GlobalIntegrationStatusDto> GetTikTokShopStatusAsync(CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> SaveTikTokShopSettingsAsync(
        MasterActor actor,
        GlobalIntegrationSettingsDto request,
        CancellationToken cancellationToken = default);

    Task<MasterOperationResultDto> DisconnectTikTokShopAsync(
        MasterActor actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Troca o <c>auth_code</c> do TikTok Shop pelo token Master; só operadores com acesso às chaves globais
    /// e só contas de criador (<c>user_type</c> = 1).
    /// </summary>
    Task<MasterOperationResultDto> CompleteTikTokShopAuthorizationAsync(
        int userId,
        string code,
        CancellationToken cancellationToken = default);
}
