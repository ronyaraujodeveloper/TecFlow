using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TecFlow.Business.Service.Groups;
using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.Tests.Unit.Groups;

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

    private static (OfferProductMediaStore Store, string WebRoot) CreateStore()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-media-" + Guid.NewGuid().ToString("N"));
        var environment = new FakeEnv(root);
        Directory.CreateDirectory(environment.WebRootPath);
        var optimizer = new ImageOptimizationService(environment, NullLogger<ImageOptimizationService>.Instance);
        return (new OfferProductMediaStore(environment, optimizer, NullLogger<OfferProductMediaStore>.Instance), environment.WebRootPath);
    }

    private static async Task<byte[]> CreatePngAsync()
    {
        using var image = new Image<Rgba32>(640, 480, Color.ParseHex("336699"));
        await using var buffer = new MemoryStream();
        await image.SaveAsPngAsync(buffer);
        return buffer.ToArray();
    }

    private sealed class FakeEnv : IWebHostEnvironment
    {
        public FakeEnv(string contentRoot)
        {
            ContentRootPath = contentRoot;
            WebRootPath = Path.Combine(contentRoot, "wwwroot");
        }

        public string ApplicationName { get; set; } = "tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Tests";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
