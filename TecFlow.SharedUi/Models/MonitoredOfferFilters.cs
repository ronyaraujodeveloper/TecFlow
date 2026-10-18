namespace TecFlow.SharedUi.Models;

public sealed record MonitoredOfferFilters(
    int Hours,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Store,
    int PageSize)
{
    public const int DefaultHours = 24;

    public int ActiveCount(int defaultPageSize) =>
        (Hours != DefaultHours ? 1 : 0)
        + (MinPrice is > 0 ? 1 : 0)
        + (MaxPrice is > 0 ? 1 : 0)
        + (string.IsNullOrWhiteSpace(Store) ? 0 : 1)
        + (PageSize != defaultPageSize ? 1 : 0);
}
