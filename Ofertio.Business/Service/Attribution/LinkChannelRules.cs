using System.Text.RegularExpressions;

namespace Ofertio.Business.Service.Attribution;

/// <summary>Canal de envio de um link curto: a letra vai no caminho (/w/{loja}/{código}) e o nome é gravado no clique.</summary>
public sealed record LinkChannel(char Code, string Name, string Label);

/// <summary>
/// Tabela única de letras e canais. Para acrescentar um canal basta uma linha em <see cref="All"/>:
/// a rota (regex), os botões "Copiar para" e o painel leem daqui.
/// </summary>
public static class LinkChannelRules
{
    public static readonly IReadOnlyList<LinkChannel> All =
    [
        new('w', "WhatsApp", "WhatsApp"),
        new('t', "Telegram", "Telegram"),
        new('e', "Email", "E-mail"),
        new('p', "PaginaPublica", "Página pública"),
        new('i', "Instagram", "Instagram"),
        new('o', "Outro", "Outro")
    ];

    /// <summary>Canais que só são detectados pelo navegador ou pelo referer (não têm letra no caminho).</summary>
    public static readonly IReadOnlyList<LinkChannel> DetectedOnly =
    [
        new('\0', "Facebook", "Facebook"),
        new('\0', "TikTok", "TikTok"),
        new('\0', "Google", "Google"),
        new('\0', "X", "X (Twitter)")
    ];

    public const string Direct = "Direto";
    public const string Unknown = "Desconhecido";

    /// <summary>Expressão para a rota: só uma das letras da tabela.</summary>
    public static string RouteRegex => "^[" + string.Concat(All.Select(channel => channel.Code)) + "]$";

    public static bool TryFromCode(string? segment, out LinkChannel channel)
    {
        channel = default!;
        if (string.IsNullOrEmpty(segment) || segment.Length != 1)
        {
            return false;
        }

        var found = All.FirstOrDefault(item => item.Code == char.ToLowerInvariant(segment[0]));
        if (found is null)
        {
            return false;
        }

        channel = found;
        return true;
    }

    /// <summary>Aceita o nome canônico ou variações antigas ("WhatsApp", "E-mail", "pagina publica").</summary>
    public static LinkChannel? FromName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var key = Normalize(raw);
        return All.Concat(DetectedOnly).FirstOrDefault(item => Normalize(item.Name) == key || Normalize(item.Label) == key);
    }

    public static string LabelFor(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Unknown;
        }

        if (name == Direct || name == Unknown)
        {
            return name;
        }

        return FromName(name)?.Label ?? name;
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]", string.Empty);
}
