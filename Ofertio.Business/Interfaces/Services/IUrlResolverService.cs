using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

public interface IUrlResolverService
{
    Task<UrlResolverResultDto> ResolveCanonicalAsync(
        string capturedUrl,
        CancellationToken cancellationToken = default);
}
