namespace TecFlow.Business.Interfaces.Services;

public interface IOfferProductMediaStore
{
    Task<string?> SaveProductPhotoAsync(
        int userId,
        string? messageId,
        byte[] photoBytes,
        CancellationToken cancellationToken = default);
}

public interface IProductImageCleanupService
{
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
