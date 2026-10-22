using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Ofertio.SharedUi.Extensions;

/// <summary>Converte objetos Filter em query string para chamadas GET da API.</summary>
public static class FilterQueryStringExtensions
{
    /// <summary>Listas viram a chave repetida (<c>Categorias=a&amp;Categorias=b</c>), formato que o <c>[FromQuery]</c> do ASP.NET Core entende.</summary>
    public static string ToQueryString(this object filter)
    {
        var segments = filter.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(property =>
            {
                var value = property.GetValue(filter);
                IEnumerable<object> values = value switch
                {
                    null => [],
                    string text => [text],
                    IEnumerable sequence => sequence.Cast<object?>().OfType<object>(),
                    _ => [value]
                };

                return values
                    .Select(FormatQueryValue)
                    .Where(formatted => !string.IsNullOrEmpty(formatted))
                    .Select(formatted => $"{Uri.EscapeDataString(property.Name)}={Uri.EscapeDataString(formatted)}");
            });

        return string.Join('&', segments);
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
