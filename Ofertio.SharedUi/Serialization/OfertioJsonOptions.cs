using System.Text.Json;
using System.Text.Json.Serialization;
using Ofertio.Business.Dto;

namespace Ofertio.SharedUi.Serialization;

public static class OfertioJsonOptions
{
    public static JsonSerializerOptions Http { get; } = CreateHttp();

    public static JsonSerializerOptions CreateHttp()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new MarketplaceTypeJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));
        return options;
    }
}
