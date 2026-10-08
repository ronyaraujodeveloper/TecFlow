using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class ImageOptimizationServiceTests
{
    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldResizeStripExifAndWriteWebp()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-opt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "wwwroot"));
        var service = new ImageOptimizationService(new FakeEnv(root), NullLogger<ImageOptimizationService>.Instance);
        await using var raw = new MemoryStream();
        using (var source = new Image<Rgba32>(2000, 1200, Color.ParseHex("112233")))
        {
            source.Metadata.ExifProfile = new SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifProfile();
            await source.SaveAsPngAsync(raw);
        }

        raw.Position = 0;
        var relative = await service.ProcessAndSaveImageAsync(raw, "7", 45892);
        Assert.NotNull(relative);
        Assert.StartsWith("/uploads/products/7/", relative);
        Assert.EndsWith("/45892.webp", relative);

        var physical = Path.Combine(root, "wwwroot", relative!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physical));
        using var saved = await Image.LoadAsync(physical);
        Assert.True(saved.Width <= 1080);
        Assert.True(saved.Height <= 1080);
        Assert.Null(saved.Metadata.ExifProfile);
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldPadExtremeAspect()
    {
        var root = Path.Combine(Path.GetTempPath(), "tecflow-pad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "wwwroot"));
        var service = new ImageOptimizationService(new FakeEnv(root), NullLogger<ImageOptimizationService>.Instance);
        await using var raw = new MemoryStream();
        using (var source = new Image<Rgba32>(1600, 200, Color.ParseHex("AABBCC")))
        {
            await source.SaveAsBmpAsync(raw);
        }

        raw.Position = 0;
        var relative = await service.ProcessAndSaveImageAsync(raw, "3", 1);
        Assert.NotNull(relative);
        var physical = Path.Combine(root, "wwwroot", relative!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var saved = await Image.LoadAsync(physical);
        Assert.Equal(saved.Width, saved.Height);
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
