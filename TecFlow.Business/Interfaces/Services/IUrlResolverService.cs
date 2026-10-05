using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IUrlResolverService
{
    Task<UrlResolverResultDto> ResolveCanonicalAsync(
        string capturedUrl,
        CancellationToken cancellationToken = default);
}
