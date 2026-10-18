using Microsoft.Extensions.FileProviders;

namespace Ofertio.WebUi.Extensions;

public static class ProductUploadsStaticFilesExtensions
{
    public const string RootPathSetting = "UploadStorage:RootPath";

    public static WebApplication UseOfertioProductUploads(this WebApplication app)
    {
        var configured = app.Configuration[RootPathSetting];
        var candidates = new[]
            {
                string.IsNullOrWhiteSpace(configured) ? null : Path.GetFullPath(configured.Trim()),
                Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "api", "wwwroot", "uploads")),
                Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "Ofertio.API", "wwwroot", "uploads"))
            }
            .OfType<string>();

        foreach (var root in candidates)
        {
            try
            {
                Directory.CreateDirectory(root);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            if (!Directory.Exists(root))
            {
                continue;
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(root),
                RequestPath = "/uploads"
            });
            app.Logger.LogInformation("Uploads servidos em /uploads a partir de {UploadsRoot}", root);
            break;
        }

        return app;
    }
}
