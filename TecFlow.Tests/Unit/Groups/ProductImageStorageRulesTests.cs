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
    public void BuildFileName_ShouldUseTelegramMessageIdOnly()
    {
        Assert.Equal("987654.jpg", ProductImageStorageRules.BuildFileName("987654"));
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

    [Fact]
    public void TryParseMessageIdFromFileName_ShouldReadTelegramIdPrefix()
    {
        Assert.True(ProductImageStorageRules.TryParseMessageIdFromFileName("987654.jpg", out var exact));
        Assert.Equal(987654, exact);
        Assert.True(ProductImageStorageRules.TryParseMessageIdFromFileName("987654_abcdef0123456789.jpg", out var id));
        Assert.Equal(987654, id);
        Assert.False(ProductImageStorageRules.TryParseMessageIdFromFileName("msg_abcdef.jpg", out _));
    }

    [Fact]
    public void EnumerateExistingPhotos_ShouldMapTenantAndWebUrl()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-enum-" + Guid.NewGuid().ToString("N"));
        var physical = ProductImageStorageRules.TryResolvePhysicalPath(root, "/uploads/products/7/2026/10/321_abcd.jpg")!;
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        File.WriteAllBytes(physical, [1, 2, 3]);

        var photo = Assert.Single(ProductImageStorageRules.EnumerateExistingPhotos(root));
        Assert.Equal(321, photo.MessageId);
        Assert.Equal(7, photo.TenantId);
        Assert.Equal("/uploads/products/7/2026/10/321_abcd.jpg", photo.WebRelativeUrl);
    }

    [Fact]
    public void EnsureLeadingSlash_ShouldPrefixRelativePaths()
    {
        Assert.Equal("/uploads/products/1/2026/10/a.jpg", ProductImageStorageRules.EnsureLeadingSlash("uploads/products/1/2026/10/a.jpg"));
        Assert.Equal("/uploads/products/1/2026/10/a.jpg", ProductImageStorageRules.EnsureLeadingSlash("/uploads/products/1/2026/10/a.jpg"));
    }

    [Fact]
    public void TryFindPhotoForMessageId_ShouldMatchTelegramIdPrefix()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-find-" + Guid.NewGuid().ToString("N"));
        var physical = ProductImageStorageRules.TryResolvePhysicalPath(root, "/uploads/products/3/2026/10/555_zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz.jpg")!;
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        File.WriteAllBytes(physical, [1]);

        var url = ProductImageStorageRules.TryFindPhotoForMessageId(root, "555");
        Assert.Equal("/uploads/products/3/2026/10/555_zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz.jpg", url);
    }

    [Fact]
    public void TryDeleteTenantProductFiles_ShouldSkipLockedFilesAndDeleteTheRest()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-del-" + Guid.NewGuid().ToString("N"));
        var folder = ProductImageStorageRules.TryResolveTenantProductsFolder(root, 9)!;
        Directory.CreateDirectory(folder);
        var free = Path.Combine(folder, "ok.jpg");
        File.WriteAllBytes(free, [1, 2, 3]);

        Assert.Equal(1, ProductImageStorageRules.TryDeleteTenantProductFiles(folder));
        Assert.False(File.Exists(free));
        Assert.Equal(0, ProductImageStorageRules.TryDeleteTenantProductFiles(null));
    }

    [Fact]
    public void TryResolveTenantProductsFolder_ShouldStayUnderUploadsProducts()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-tenant-" + Guid.NewGuid().ToString("N"));
        var folder = ProductImageStorageRules.TryResolveTenantProductsFolder(root, 7);
        Assert.Equal(Path.GetFullPath(Path.Combine(root, "uploads", "products", "7")), folder);
        Assert.Null(ProductImageStorageRules.TryResolveTenantProductsFolder(root, 0));
    }

    [Fact]
    public void TryFindPhotoForMessageId_ShouldPreferExactTelegramIdFileName()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-exact-" + Guid.NewGuid().ToString("N"));
        var physical = ProductImageStorageRules.TryResolvePhysicalPath(root, "/uploads/products/1/2026/10/45892.jpg")!;
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        File.WriteAllBytes(physical, [1]);

        Assert.Equal("/uploads/products/1/2026/10/45892.jpg", ProductImageStorageRules.TryFindPhotoForMessageId(root, "45892"));
    }
}
