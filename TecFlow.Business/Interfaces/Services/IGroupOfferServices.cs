using TecFlow.Business.Dto;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Services;

public sealed class GroupOfferCaptureRequest
{
    public int UserId { get; init; }

    public string Channel { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;

    public string GroupName { get; init; } = string.Empty;

    public string? ExternalMessageId { get; init; }

    public string RawText { get; init; } = string.Empty;

    public string? MediaUrl { get; init; }

    public byte[]? PhotoBytes { get; init; }

    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}

public interface IGroupOfferCaptureService
{
    Task CaptureAsync(GroupOfferCaptureRequest request, CancellationToken cancellationToken = default);
}

public interface IOfferValidationService
{
    Task<OfferValidationResultDto> ValidateAsync(
        string originalUrl,
        decimal? capturedPrice,
        CancellationToken cancellationToken = default);

    Task<OfferPageStatusDto> ValidateProductPageStatusAsync(
        string originalUrl,
        CancellationToken cancellationToken = default);
}

public interface IMonitoredGroupService
{
    Task<MonitoredGroupsResponseDto> SyncAsync(
        int userId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<MonitoredGroupsResponseDto> ListAsync(
        int userId,
        int lookbackHours,
        string? groupKey,
        string? channel,
        int skip = 0,
        int take = 50,
        bool ignored = false,
        CancellationToken cancellationToken = default);

    Task<MonitoredGroupsResponseDto> SetIgnoredAsync(
        int userId,
        int offerId,
        bool ignored,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<MonitoredGroupsResponseDto> ValidateAsync(
        int userId,
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<MonitoredGroupsResponseDto> CloneAsync(
        int userId,
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default);
}
