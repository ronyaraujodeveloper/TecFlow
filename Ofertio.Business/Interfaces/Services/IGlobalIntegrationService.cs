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
}
