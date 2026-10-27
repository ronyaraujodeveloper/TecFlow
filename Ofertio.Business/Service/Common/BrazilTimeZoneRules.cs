using System.Collections.Concurrent;

namespace Ofertio.Business.Service.Common;

/// <summary>Estado brasileiro com o fuso usado para exibir e digitar horários.</summary>
public sealed record BrazilStateZone(string Code, string Name, string ZoneId, int OffsetHours, string ZoneName)
{
    public string Label => $"{ZoneName} (UTC{OffsetHours})";
}

/// <summary>
/// Horários são gravados em UTC; a tela converte pelo fuso do estado do cliente (Brasília quando não há estado).
/// O Brasil não tem horário de verão desde 2019, então o deslocamento fixo serve de reserva quando o sistema
/// operacional não reconhece a zona IANA.
/// </summary>
public static class BrazilTimeZoneRules
{
    public static readonly BrazilStateZone Brasilia = new(string.Empty, "Brasília", "America/Sao_Paulo", -3, "Brasília");

    public static readonly IReadOnlyList<BrazilStateZone> States =
    [
        new("AC", "Acre", "America/Rio_Branco", -5, "Rio Branco"),
        new("AL", "Alagoas", "America/Maceio", -3, "Brasília"),
        new("AP", "Amapá", "America/Belem", -3, "Brasília"),
        new("AM", "Amazonas", "America/Manaus", -4, "Manaus"),
        new("BA", "Bahia", "America/Bahia", -3, "Brasília"),
        new("CE", "Ceará", "America/Fortaleza", -3, "Brasília"),
        new("DF", "Distrito Federal", "America/Sao_Paulo", -3, "Brasília"),
        new("ES", "Espírito Santo", "America/Sao_Paulo", -3, "Brasília"),
        new("GO", "Goiás", "America/Sao_Paulo", -3, "Brasília"),
        new("MA", "Maranhão", "America/Fortaleza", -3, "Brasília"),
        new("MT", "Mato Grosso", "America/Cuiaba", -4, "Cuiabá"),
        new("MS", "Mato Grosso do Sul", "America/Campo_Grande", -4, "Campo Grande"),
        new("MG", "Minas Gerais", "America/Sao_Paulo", -3, "Brasília"),
        new("PA", "Pará", "America/Belem", -3, "Brasília"),
        new("PB", "Paraíba", "America/Fortaleza", -3, "Brasília"),
        new("PR", "Paraná", "America/Sao_Paulo", -3, "Brasília"),
        new("PE", "Pernambuco", "America/Recife", -3, "Brasília"),
        new("PI", "Piauí", "America/Fortaleza", -3, "Brasília"),
        new("RJ", "Rio de Janeiro", "America/Sao_Paulo", -3, "Brasília"),
        new("RN", "Rio Grande do Norte", "America/Fortaleza", -3, "Brasília"),
        new("RS", "Rio Grande do Sul", "America/Sao_Paulo", -3, "Brasília"),
        new("RO", "Rondônia", "America/Porto_Velho", -4, "Porto Velho"),
        new("RR", "Roraima", "America/Boa_Vista", -4, "Boa Vista"),
        new("SC", "Santa Catarina", "America/Sao_Paulo", -3, "Brasília"),
        new("SP", "São Paulo", "America/Sao_Paulo", -3, "Brasília"),
        new("SE", "Sergipe", "America/Maceio", -3, "Brasília"),
        new("TO", "Tocantins", "America/Araguaina", -3, "Brasília")
    ];

    private static readonly Dictionary<string, BrazilStateZone> ByCode =
        States.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<string, TimeZoneInfo?> Zones = new(StringComparer.Ordinal);

    /// <summary>UF em maiúsculas quando válida; null para vazio ou desconhecido.</summary>
    public static string? NormalizeStateCode(string? stateCode)
    {
        var code = stateCode?.Trim();
        return !string.IsNullOrEmpty(code) && ByCode.TryGetValue(code, out var state) ? state.Code : null;
    }

    public static BrazilStateZone Resolve(string? stateCode) =>
        NormalizeStateCode(stateCode) is { } code ? ByCode[code] : Brasilia;

    /// <summary>Valor em UTC: <c>Local</c> é convertido e <c>Unspecified</c> (lido do banco ou da API) já é UTC.</summary>
    public static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    /// <summary>Horário de parede do estado (Kind <c>Unspecified</c>), independente do fuso do servidor.</summary>
    public static DateTime ToLocal(DateTime utc, string? stateCode)
    {
        var value = NormalizeUtc(utc);
        var state = Resolve(stateCode);
        return FindZone(state.ZoneId) is { } zone
            ? DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(value, zone), DateTimeKind.Unspecified)
            : DateTime.SpecifyKind(value.AddHours(state.OffsetHours), DateTimeKind.Unspecified);
    }

    /// <summary>Horário digitado no estado (o Kind é ignorado) convertido para UTC.</summary>
    public static DateTime ToUtc(DateTime local, string? stateCode)
    {
        var wall = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var state = Resolve(stateCode);
        return FindZone(state.ZoneId) is { } zone
            ? TimeZoneInfo.ConvertTimeToUtc(wall, zone)
            : DateTime.SpecifyKind(wall.AddHours(-state.OffsetHours), DateTimeKind.Utc);
    }

    /// <summary>Início e fim (UTC) do dia do estado que contém <paramref name="utc"/>.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) DayRangeUtc(DateTime utc, string? stateCode)
    {
        var localDay = ToLocal(utc, stateCode).Date;
        return (ToUtc(localDay, stateCode), ToUtc(localDay.AddDays(1), stateCode));
    }

    private static TimeZoneInfo? FindZone(string zoneId) =>
        Zones.GetOrAdd(zoneId, static id =>
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        });
}
