namespace TecFlow.Business.Interfaces.Services;

public interface IImageOptimizationService
{
    Task<string?> ProcessAndSaveImageAsync(
        Stream rawStream,
        string tenantId,
        long messageId,
        CancellationToken cancellationToken = default);

    Task<string?> ProcessAndSaveToRelativePathAsync(
        Stream rawStream,
        string webRelativePathWithoutExtension,
        CancellationToken cancellationToken = default);
}
