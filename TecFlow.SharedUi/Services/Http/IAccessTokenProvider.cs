namespace TecFlow.SharedUi.Services.Http;

public interface IAccessTokenProvider
{
    string? GetAccessToken();

    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(GetAccessToken());
}
