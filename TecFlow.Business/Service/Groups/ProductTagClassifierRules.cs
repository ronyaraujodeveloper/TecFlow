using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Service.Groups;

/// <summary>
/// Vincula características (<see cref="ProductTagCatalog"/>) a uma oferta pelo nome do produto e texto da mensagem.
/// Comparação sem acento e por palavra inteira; URLs são descartadas antes para slugs de links não contarem.
/// </summary>
public static class ProductTagClassifierRules
{
    public static readonly IReadOnlyDictionary<string, string[]> Keywords = new Dictionary<string, string[]>
    {
        [ProductTagCatalog.Gamer] =
        [
            "gamer", "gaming", "playstation", "ps4", "ps5", "xbox", "nintendo", "console", "dualsense",
            "joystick", "gamepad", "headset", "rgb", "teclado mecanico", "placa de video", "rtx", "geforce", "radeon"
        ],
        [ProductTagCatalog.Eletronicos] =
        [
            "fone", "fones", "headphone", "earbuds", "caixa de som", "soundbar", "smart tv", "tv", "televisao",
            "televisor", "smartwatch", "relogio inteligente", "alexa", "echo dot", "camera", "drone", "carregador",
            "power bank", "powerbank", "kindle", "projetor", "chromecast", "fire tv", "bluetooth"
        ],
        [ProductTagCatalog.Informatica] =
        [
            "notebook", "laptop", "macbook", "chromebook", "monitor", "ssd", "hd externo", "pendrive", "pen drive",
            "memoria ram", "ddr4", "ddr5", "processador", "ryzen", "intel core", "placa mae", "teclado", "mouse",
            "roteador", "webcam", "impressora", "cartucho", "tablet", "ipad", "gabinete"
        ],
        [ProductTagCatalog.CasaCozinha] =
        [
            "air fryer", "airfryer", "fritadeira", "panela", "panelas", "frigideira", "liquidificador", "cafeteira",
            "micro-ondas", "microondas", "geladeira", "refrigerador", "fogao", "cooktop", "aspirador", "ventilador",
            "ar condicionado", "ar-condicionado", "lavadora", "maquina de lavar", "jogo de cama", "toalha", "toalhas",
            "travesseiro", "colchao", "cozinha", "garrafa termica", "mixer", "batedeira", "sanduicheira", "chaleira",
            "ferro de passar", "purificador"
        ],
        [ProductTagCatalog.Smartphones] =
        [
            "smartphone", "celular", "iphone", "galaxy", "redmi", "xiaomi", "motorola", "moto g", "moto e", "poco",
            "realme", "oneplus"
        ],
        [ProductTagCatalog.ModaBeleza] =
        [
            "perfume", "camiseta", "camisa", "tenis", "sapato", "sandalia", "chinelo", "bolsa", "vestido", "calca",
            "bermuda", "jaqueta", "moletom", "oculos", "maquiagem", "batom", "shampoo", "condicionador", "hidratante",
            "protetor solar", "secador de cabelo", "chapinha", "prancha alisadora", "barbeador", "depilador",
            "skincare", "serum", "cueca", "sutia", "biquini", "relogio"
        ]
    };

    private static readonly Regex UrlRegex = new(@"https?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyList<(string Slug, Regex Pattern)> Patterns = Keywords
        .Select(pair => (pair.Key, BuildWordRegex(pair.Value)))
        .ToList();

    /// <summary>Slugs das características encontradas, na ordem do catálogo e sem repetição.</summary>
    public static IReadOnlyList<string> Classify(string? productName, string? rawText)
    {
        var text = Normalize(UrlRegex.Replace($"{productName}\n{rawText}", " "));
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var slugs = new List<string>();
        foreach (var (slug, pattern) in Patterns)
        {
            if (pattern.IsMatch(text))
            {
                slugs.Add(slug);
            }
        }

        return slugs;
    }

    private static Regex BuildWordRegex(IEnumerable<string> keywords)
    {
        var pattern = string.Join("|", keywords.Select(keyword => Regex.Escape(Normalize(keyword))));
        return new Regex(
            $@"(?<![\p{{L}}\p{{N}}])(?:{pattern})(?![\p{{L}}\p{{N}}])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
