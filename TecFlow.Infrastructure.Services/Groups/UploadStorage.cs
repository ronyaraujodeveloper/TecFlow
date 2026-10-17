using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class UploadStorage : IUploadStorage
{
    public UploadStorage(string uploadsRoot)
    {
        UploadsRoot = UploadStorageRules.ResolveUploadsRoot(uploadsRoot, null);
        MediaRoot = UploadStorageRules.ResolveMediaRoot(UploadsRoot);
    }

    public string UploadsRoot { get; }

    public string MediaRoot { get; }

    public static UploadStorage Create(IWebHostEnvironment environment, IConfiguration configuration) =>
        new(UploadStorageRules.ResolveUploadsRoot(
            configuration[UploadStorageRules.RootPathSetting],
            environment.WebRootPath,
            environment.ContentRootPath));
}

public static class UploadStorageRegistrationExtensions
{
    public static IServiceCollection AddTecFlowUploadStorage(this IServiceCollection services)
    {
        services.TryAddSingleton<IUploadStorage>(provider => UploadStorage.Create(
            provider.GetRequiredService<IWebHostEnvironment>(),
            provider.GetRequiredService<IConfiguration>()));
        return services;
    }
}
