using System.Globalization;
using System.Text;

namespace Ofertio.Util.Address;

/// <summary>
/// Forma de comparação dos nomes de país, estado e cidade: sem acento, minúsculo e só com letras e números
/// separados por um espaço. "Alta Floresta D'Oeste", "alta floresta d’oeste" e "ALTA FLORESTA D OESTE" ficam iguais.
/// A mesma regra gera a coluna <c>NormalizedName</c> na carga das cidades e a busca em memória.
/// </summary>
public static class LocationNameNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;
        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (pendingSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingSpace = false;
            }
            else
            {
                pendingSpace = true;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
