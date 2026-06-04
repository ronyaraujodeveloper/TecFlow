namespace TecFlow.WebUi.Models;

public sealed class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public int? StatusCode { get; init; }
    public bool IsOffline { get; init; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResult<T> Fail(string message, int? statusCode = null, string? errorCode = null, bool isOffline = false) =>
        new()
        {
            Success = false,
            ErrorMessage = message,
            StatusCode = statusCode,
            ErrorCode = errorCode,
            IsOffline = isOffline
        };
}
