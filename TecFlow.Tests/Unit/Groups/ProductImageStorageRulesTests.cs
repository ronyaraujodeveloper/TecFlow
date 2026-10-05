using TecFlow.Business.Service.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class ProductImageStorageRulesTests
{
    [Fact]
    public void BuildRelativeUrl_ShouldSegregateTenantAndMonth()
    {
        var utc = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        var url = ProductImageStorageRules.BuildRelativeUrl(1, utc, "123_abcd1234.jpg");
        Assert.Equal("/uploads/products/1/2026/10/123_abcd1234.jpg", url);
    }

    [Fact]
    public void BuildFileName_ShouldUseMessageIdAndShortGuid()
    {
        var name = ProductImageStorageRules.BuildFileName("987654");
        Assert.StartsWith("987654_", name, StringComparison.Ordinal);
        Assert.EndsWith(".jpg", name, StringComparison.Ordinal);
        Assert.Equal(19, name.Length);
    }

    [Fact]
    public void IsExpired_ShouldFlagRecordsOlderThanFifteenDays()
    {
        var now = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(ProductImageStorageRules.IsExpired(now.AddDays(-16), now));
        Assert.False(ProductImageStorageRules.IsExpired(now.AddDays(-2), now));
    }

    [Fact]
    public void TryResolvePhysicalPath_ShouldRejectTraversalOutsideProducts()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-www");
        var path = ProductImageStorageRules.TryResolvePhysicalPath(root, "/uploads/../secrets.txt");
        Assert.Null(path);
    }
}
