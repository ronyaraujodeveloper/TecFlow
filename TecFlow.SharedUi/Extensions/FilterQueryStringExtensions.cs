using System.Globalization;
using System.Reflection;

namespace TecFlow.SharedUi.Extensions;

/// <summary>Converte objetos Filter em query string para chamadas GET da API.</summary>
public static class FilterQueryStringExtensions
{
    public static string ToQueryString(this object filter)
    {
        var segments = filter.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property =>
            {
                var value = property.GetValue(filter);
                if (value is null)
                {
                    return null;
                }

                var formatted = FormatQueryValue(value);
                if (string.IsNullOrEmpty(formatted))
                {
                    return null;
                }

                return $"{Uri.EscapeDataString(property.Name)}={Uri.EscapeDataString(formatted)}";
            })
            .Where(segment => segment is not null);

        return string.Join('&', segments!);
    }

    public static string AppendQueryString(this string relativeUrl, object? filter)
    {
        if (filter is null)
        {
            return relativeUrl;
        }

        var query = filter.ToQueryString();
        if (string.IsNullOrEmpty(query))
        {
            return relativeUrl;
        }

        var separator = relativeUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{relativeUrl}{separator}{query}";
    }

    private static string FormatQueryValue(object value) => value switch
    {
        DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
        decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
        double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
        float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
}
