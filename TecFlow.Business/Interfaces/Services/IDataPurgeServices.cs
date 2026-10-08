namespace TecFlow.Business.Interfaces.Services;

public interface IDataPurgeService
{
    Task<DataPurgeResult> RunCycleAsync(CancellationToken cancellationToken = default);
}

public sealed class DataPurgeResult
{
    public int MediaPurged { get; init; }

    public int PricesArchived { get; init; }

    public int MessagesDeleted { get; init; }

    public bool IndexesReorganized { get; init; }
}
