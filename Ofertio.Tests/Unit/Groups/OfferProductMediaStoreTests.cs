using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Ofertio.Business.Service.Groups;
using Ofertio.Infrastructure.Services.Groups;

namespace Ofertio.Tests.Unit.Groups;

public class OfferProductMediaStoreTests
{
    [Fact]
    public async Task SaveFromStreamAsync_ShouldCreateTenantFolderAndConfirmFileBeforeReturningUrl()
    {
        var (store, webRoot) = CreateStore();
        var photo = await CreatePngAsync();

        var relative = await store.SaveFromStreamAsync(
            7,
            "45892",
            (stream, token) => stream.WriteAsync(photo, token).AsTask());

        var utc = DateTime.UtcNow;
        Assert.Equal($"/uploads/products/7/{utc:yyyy}/{utc:MM}/45892.webp", relative);
        var physical = ProductImageStorageRules.TryResolvePhysicalPath(webRoot, relative)!;
        Assert.True(File.Exists(physical));
        Assert.True(store.ExistsOnDisk(relative));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(physical)!, "*.tmp"));
    }

    [Fact]
    public async Task SaveFromStreamAsync_ShouldReturnNull_WhenDownloadFails()
    {
        var (store, webRoot) = CreateStore();

        var relative = await store.SaveFromStreamAsync(
            7,
            "45893",
            (_, _) => throw new IOException("conexão MTProto caiu"));

        Assert.Null(relative);
        Assert.False(Directory.Exists(Path.Combine(webRoot, "uploads", "products", "7")));
    }

    [Fact]
    public async Task SaveFromStreamAsync_ShouldReturnNullAndLeaveNoFile_WhenConversionFails()
    {
        var (store, webRoot) = CreateStore();

        var relative = await store.SaveFromStreamAsync(
            7,
            "45894",
            (stream, token) => stream.WriteAsync("isto não é uma imagem"u8.ToArray(), token).AsTask());

        Assert.Null(relative);
        var tenantFolder = Path.Combine(webRoot, "uploads", "products", "7");
        Assert.True(!Directory.Exists(tenantFolder)
            || Directory.GetFiles(tenantFolder, "*", SearchOption.AllDirectories).Length == 0);
    }

    [Fact]
    public void BuildSaveTarget_ShouldUseYearAndMonthSegments_RegardlessOfCultureDateSeparator()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var utc = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

            var (dir, file, url) = ProductImageStorageRules.BuildSaveTarget(@"C:\site\wwwroot", 3, "9.webp", utc);

            Assert.Equal(Path.Combine(@"C:\site\wwwroot", "uploads", "products", "3", "2026", "10"), dir);
            Assert.Equal(Path.Combine(dir, "9.webp"), file);
            Assert.Equal("/uploads/products/3/2026/10/9.webp", url);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task SaveFromStreamAsync_ShouldWriteIntoExternalUploadsFolder()
    {
        var (store, mediaRoot) = CreateStore();

        var relative = await store.SaveFromStreamAsync(
            1,
            "77",
            async (stream, token) => await stream.WriteAsync(await CreatePngAsync(), token));

        var utc = DateTime.UtcNow;
        var expected = Path.Combine(mediaRoot, "uploads", "products", "1", utc.ToString("yyyy"), utc.ToString("MM"), "77.webp");
        Assert.Equal($"/uploads/products/1/{utc:yyyy}/{utc:MM}/77.webp", relative);
        Assert.True(File.Exists(expected));
        Assert.EndsWith(Path.Combine("ofertio", "uploads", "products", "1", utc.ToString("yyyy"), utc.ToString("MM"), "77.webp"), expected);
    }

    [Fact]
    public void ResolveUploadsRoot_ShouldPreferConfiguredExternalFolder()
    {
        var uploads = UploadStorageRules.ResolveUploadsRoot(@"C:\inetpub\ofertio\uploads\", @"C:\inetpub\ofertio\api\wwwroot");

        Assert.Equal(@"C:\inetpub\ofertio\uploads", uploads);
        Assert.Equal(@"C:\inetpub\ofertio", UploadStorageRules.ResolveMediaRoot(uploads));
        Assert.Equal(
            @"C:\inetpub\ofertio\uploads\products\7\2026\10\9.webp",
            ProductImageStorageRules.TryResolvePhysicalPath(UploadStorageRules.ResolveMediaRoot(uploads), "/uploads/products/7/2026/10/9.webp"));
    }

    [Fact]
    public void ResolveUploadsRoot_ShouldFallBackToWebRoot_WhenNotConfigured()
    {
        Assert.Equal(
            @"C:\site\wwwroot\uploads",
            UploadStorageRules.ResolveUploadsRoot(null, @"C:\site\wwwroot"));
    }

    [Fact]
    public void ResolveUploadsRoot_ShouldRejectFolderNotNamedUploads()
    {
        Assert.Throws<InvalidOperationException>(() => UploadStorageRules.ResolveUploadsRoot(@"D:\media", null));
    }

    private static (OfferProductMediaStore Store, string MediaRoot) CreateStore()
    {
        var root = Path.Combine(Path.GetTempPath(), "ofertio-media-" + Guid.NewGuid().ToString("N"), "ofertio");
        var uploads = new UploadStorage(Path.Combine(root, "uploads"));
        var optimizer = new ImageOptimizationService(uploads, NullLogger<ImageOptimizationService>.Instance);
        return (new OfferProductMediaStore(uploads, optimizer, NullLogger<OfferProductMediaStore>.Instance), uploads.MediaRoot);
    }

    private static async Task<byte[]> CreatePngAsync()
    {
        using var image = new Image<Rgba32>(640, 480, Color.ParseHex("336699"));
        await using var buffer = new MemoryStream();
        await image.SaveAsPngAsync(buffer);
        return buffer.ToArray();
    }
}
