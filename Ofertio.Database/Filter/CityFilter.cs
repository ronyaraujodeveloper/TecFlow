namespace Ofertio.Database.Filter;

/// <summary>
/// Busca de cidades da tabela de referência. Volta a UF inteira, sem paginação, porque alimenta o combo de cidade
/// (no máximo 853 cidades, em Minas Gerais) e vem da memória, sem consulta ao banco.
/// </summary>
public class CityFilter
{
    /// <summary>Sigla da UF, obrigatória (ex.: SP).</summary>
    public string? StateCode { get; set; }

    /// <summary>Trecho do nome, sem diferenciar acento nem maiúsculas.</summary>
    public string? Name { get; set; }
}
