using System.Globalization;

namespace Ofertio.SharedUi.Helpers;

/// <summary>Opção de um combo multisseleção; <paramref name="Count"/> aparece como contador ao lado do rótulo.</summary>
public sealed record MultiSelectOption(string Value, string Label, int? Count = null, string? Hint = null);

public static class MultiSelectRules
{
    /// <summary>A lista de opções só é filtrada a partir desta quantidade de caracteres digitados.</summary>
    public const int MinSearchLength = 3;

    /// <summary>O campo de busca interno só aparece com mais opções do que isto (padrão de combos do README).</summary>
    public const int SearchThreshold = 6;

    public static bool ShowSearch(int optionCount) => optionCount > SearchThreshold;

    /// <summary>Próximo item ativo ao navegar com as setas: sem item ativo (-1) a seta para baixo vai ao primeiro; para nos extremos.</summary>
    public static int MoveActive(int current, int count, int step) =>
        count == 0 ? -1 : Math.Clamp(current + step, 0, count - 1);

    public static bool IsSearchActive(string? term) =>
        (term?.Trim().Length ?? 0) >= MinSearchLength;

    /// <summary>Termo digitado, mas ainda curto demais para filtrar (mostra a dica de 3 caracteres).</summary>
    public static bool IsSearchTooShort(string? term) =>
        !string.IsNullOrWhiteSpace(term) && !IsSearchActive(term);

    /// <summary>Opções cujo rótulo contém o termo, sem diferenciar maiúsculas nem acentos ("eletro" acha "Eletrônicos").</summary>
    public static IReadOnlyList<MultiSelectOption> Filter(IReadOnlyList<MultiSelectOption> options, string? term)
    {
        if (!IsSearchActive(term))
        {
            return options;
        }

        var normalized = term!.Trim();
        var compare = CultureInfo.InvariantCulture.CompareInfo;
        return options
            .Where(option => compare.IndexOf(option.Label, normalized, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)
            .ToList();
    }

    /// <summary>Rótulo do botão: título puro sem seleção, ou "Título (n)" com n opções marcadas.</summary>
    public static string Summary(string title, int selectedCount) =>
        selectedCount > 0 ? $"{title} ({selectedCount})" : title;

    public static IReadOnlyList<string> Toggle(IReadOnlyCollection<string> selected, string value, bool isChecked)
    {
        var result = selected.Where(item => !string.Equals(item, value, StringComparison.OrdinalIgnoreCase)).ToList();
        if (isChecked)
        {
            result.Add(value);
        }

        return result;
    }

    /// <summary>Marca todas as opções visíveis (respeitando o filtro digitado) sem desmarcar as já escolhidas.</summary>
    public static IReadOnlyList<string> SelectAll(IReadOnlyCollection<string> selected, IEnumerable<MultiSelectOption> visible)
    {
        var result = selected.ToList();
        foreach (var option in visible)
        {
            if (!result.Contains(option.Value, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(option.Value);
            }
        }

        return result;
    }

    /// <summary>Desmarca as opções visíveis; com filtro digitado, as escolhidas fora dele continuam marcadas.</summary>
    public static IReadOnlyList<string> ClearVisible(IReadOnlyCollection<string> selected, IEnumerable<MultiSelectOption> visible)
    {
        var visibleValues = visible.Select(option => option.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return selected.Where(item => !visibleValues.Contains(item)).ToList();
    }
}
