using Microsoft.Extensions.FileProviders;

namespace TecFlow.WebUi.Extensions;

public static class ProductUploadsStaticFilesExtensions
{
    public static WebApplication UseTecFlowProductUploads(this WebApplication app)
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "api", "wwwroot", "uploads")),
            Path.GetFullPath(@"C:\inetpub\tecflow\api\wwwroot\uploads"),
            Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "TecFlow.API", "wwwroot", "uploads"))
        };

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
            break;
        }

        return app;
    }
}
