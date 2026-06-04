namespace TecFlow.SharedUi.Extensions;

/// <summary>Helpers para envelopes ResponseDto consumidos pela UI.</summary>
public static class ResponseDtoExtensions
{
    public static bool IsApiSuccess<T>(this Models.ApiResult<T>? result, out T? payload)
        where T : class
    {
        payload = null;

        if (result is not { Success: true, Data: not null })
        {
            return false;
        }

        var statusProperty = typeof(T).GetProperty("Status");
        if (statusProperty?.PropertyType == typeof(bool)
            && statusProperty.GetValue(result.Data) is false)
        {
            return false;
        }

        payload = result.Data;
        return true;
    }

    public static string ResolveErrorMessage<T>(this Models.ApiResult<T>? result, string fallback)
        where T : class
    {
        if (result?.Data is not null)
        {
            var description = typeof(T).GetProperty("Descricao")?.GetValue(result.Data) as string;
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }
        }

        return result?.ErrorMessage ?? fallback;
    }
}
