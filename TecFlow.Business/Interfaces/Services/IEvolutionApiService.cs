using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IEvolutionApiService
{
    Task<bool> CreateInstanceAsync(string instanceName, CancellationToken cancellationToken = default);

    Task<string?> FetchQrCodeAsync(string instanceName, CancellationToken cancellationToken = default);

    Task<EvolutionConnectionStateDto> GetConnectionStateAsync(
        string instanceName,
        CancellationToken cancellationToken = default);

    Task<bool> SendTextMessageAsync(
        string instanceName,
        string remoteJid,
        string messageText,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvolutionWhatsAppGroupDto>> FetchUserGroupsAsync(
        string instanceName,
        string? ownerPhoneOrJid = null,
        CancellationToken cancellationToken = default);

    Task<bool> SendMediaMessageAsync(
        string instanceName,
        string remoteJid,
        string mediaUrl,
        string caption,
        CancellationToken cancellationToken = default);
}
