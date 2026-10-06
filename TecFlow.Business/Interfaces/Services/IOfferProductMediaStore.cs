using System.IO;

namespace TecFlow.Business.Interfaces.Services;

public interface IOfferProductMediaStore
{
    Task<string?> SaveProductPhotoAsync(
        int userId,
        string? messageId,
        byte[] photoBytes,
        CancellationToken cancellationToken = default);

    Task<string?> SaveFromStreamAsync(
        int tenantId,
        string? messageId,
        Func<Stream, CancellationToken, Task> writeAsync,
        CancellationToken cancellationToken = default);

    bool ExistsOnDisk(string? webRelativeUrl);
}

public interface IProductImageCleanupService
{
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}

public interface IProductImageUrlLinkBackfillService
{
    Task<int> LinkExistingFilesOnceAsync(CancellationToken cancellationToken = default);
}
