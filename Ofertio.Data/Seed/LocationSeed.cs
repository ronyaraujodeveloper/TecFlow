using System.Text;
using Ofertio.Util.Address;

namespace Ofertio.Data.Seed;

/// <summary>
/// Carga das tabelas de país, estado e cidade a partir dos CSVs embutidos (lista oficial do IBGE,
/// completada com os territórios que a localização por IP pode devolver). Gera T-SQL em lotes para a migration.
/// </summary>
public static class LocationSeed
{
    private const int BatchSize = 500;

    public sealed record CountryRow(string IsoCode, string Name);

    public sealed record StateRow(int IbgeCode, string Code, string Name);

    public sealed record CityRow(int IbgeCode, string StateCode, string Name);

    public static IReadOnlyList<CountryRow> Countries() =>
        ReadCsv("countries.csv").Select(cols => new CountryRow(cols[0], cols[1])).ToList();

    public static IReadOnlyList<StateRow> States() =>
        ReadCsv("states.csv").Select(cols => new StateRow(int.Parse(cols[0]), cols[1], cols[2])).ToList();

    public static IReadOnlyList<CityRow> Cities() =>
        ReadCsv("cities.csv").Select(cols => new CityRow(int.Parse(cols[0]), cols[1], cols[2])).ToList();

    /// <summary>Comandos INSERT (países, depois estados do Brasil, depois cidades ligadas ao estado pela sigla).</summary>
    public static IEnumerable<string> InsertStatements()
    {
        foreach (var batch in Countries().Chunk(BatchSize))
        {
            yield return "INSERT INTO [Countries] ([IsoCode], [Name]) VALUES\n"
                + string.Join(",\n", batch.Select(row => $"({Ascii(row.IsoCode)}, {Text(row.Name)})")) + ";";
        }

        yield return "INSERT INTO [States] ([CountryId], [Code], [Name], [IbgeCode])\n"
            + "SELECT c.[Id], v.[Code], v.[Name], v.[IbgeCode] FROM (VALUES\n"
            + string.Join(",\n", States().Select(row => $"({Ascii(row.Code)}, {Text(row.Name)}, {row.IbgeCode})"))
            + "\n) AS v([Code], [Name], [IbgeCode])\n"
            + "CROSS JOIN [Countries] AS c WHERE c.[IsoCode] = 'BR';";

        foreach (var batch in Cities().Chunk(BatchSize))
        {
            yield return "INSERT INTO [Cities] ([StateId], [Name], [NormalizedName], [IbgeCode])\n"
                + "SELECT s.[Id], v.[Name], v.[NormalizedName], v.[IbgeCode] FROM (VALUES\n"
                + string.Join(",\n", batch.Select(row =>
                    $"({Ascii(row.StateCode)}, {Text(row.Name)}, {Text(LocationNameNormalizer.Normalize(row.Name))}, {row.IbgeCode})"))
                + "\n) AS v([StateCode], [Name], [NormalizedName], [IbgeCode])\n"
                + "INNER JOIN [States] AS s ON s.[Code] = v.[StateCode];";
        }
    }

    private static string Text(string value) => "N'" + value.Replace("'", "''") + "'";

    private static string Ascii(string value) => "'" + value.Replace("'", "''") + "'";

    private static IEnumerable<string[]> ReadCsv(string fileName)
    {
        var resource = $"Ofertio.Data.Seed.Locations.{fileName}";
        using var stream = typeof(LocationSeed).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Recurso de carga não encontrado: {resource}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var rows = new List<string[]>();
        reader.ReadLine(); // cabeçalho
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                rows.Add(line.Split(';').Select(item => item.Trim()).ToArray());
            }
        }

        return rows;
    }
}
