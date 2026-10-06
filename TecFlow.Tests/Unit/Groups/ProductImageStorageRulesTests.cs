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
    public void BuildFileName_ShouldUseMessageIdAndGuid()
    {
        var name = ProductImageStorageRules.BuildFileName("987654");
        Assert.StartsWith("987654_", name, StringComparison.Ordinal);
        Assert.EndsWith(".jpg", name, StringComparison.Ordinal);
        Assert.Equal(43, name.Length);
    }

    [Fact]
    public void BuildSaveTarget_ShouldCreateWebRelativeUrlWithForwardSlashes()
    {
        var utc = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var (_, _, webUrl) = ProductImageStorageRules.BuildSaveTarget(@"C:\inetpub\tecflow\api\wwwroot", 7, "123_abcd.jpg", utc);
        Assert.Equal("/uploads/products/7/2026/10/123_abcd.jpg", webUrl);
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

    [Fact]
    public void ToWebRelativePath_ShouldStripWwwrootAndWindowsSeparators()
    {
        var path = ProductImageStorageRules.ToWebRelativePath(
            @"C:\inetpub\tecflow\api\wwwroot\uploads\products\7\2026\10\abc.jpg");
        Assert.Equal("/uploads/products/7/2026/10/abc.jpg", path);
    }

    [Fact]
    public void FileExistsOnDisk_ShouldDetectPhysicalFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-img-" + Guid.NewGuid().ToString("N"));
        var relative = "/uploads/products/1/2026/10/foto.jpg";
        var physical = ProductImageStorageRules.TryResolvePhysicalPath(root, relative)!;
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        File.WriteAllText(physical, "x");

        Assert.True(ProductImageStorageRules.FileExistsOnDisk(root, relative));
        Assert.False(ProductImageStorageRules.FileExistsOnDisk(root, "/uploads/products/1/2026/10/missing.jpg"));
    }
}
