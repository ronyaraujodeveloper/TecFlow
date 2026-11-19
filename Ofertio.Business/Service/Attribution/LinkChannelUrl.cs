using System.Text;
using System.Text.RegularExpressions;

namespace Ofertio.Business.Service.Attribution;

/// <summary>Monta as variações do endereço do link curto por canal: {base}/{letra}/{loja}/{código}[?g=chave].</summary>
public static class LinkChannelUrl
{
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s<>""']+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Aplica a letra do canal (e a chave do grupo, se houver) a um link curto de 2 ou 3 segmentos.</summary>
    public static string Apply(string? shortUrl, char channelCode, string? groupKey = null)
    {
        var link = shortUrl?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !LinkChannelRules.TryFromCode(channelCode.ToString(), out var channel))
        {
            return link;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count < 2)
        {
            return link;
        }

        // Já tem letra de canal antes de {loja}/{código}: troca; senão insere.
        var channelIndex = segments.Count - 3;
        if (channelIndex >= 0 && LinkChannelRules.TryFromCode(segments[channelIndex], out _))
        {
            segments[channelIndex] = channel.Code.ToString();
        }
        else
        {
            segments.Insert(segments.Count - 2, channel.Code.ToString());
        }

        var builder = new UriBuilder(uri)
        {
            Path = "/" + string.Join('/', segments),
            Query = ReplaceQuery(uri.Query, GroupKeyRules.QueryKey, GroupKeyRules.IsKey(groupKey) ? groupKey : null)
        };
        return builder.Uri.ToString();
    }

    /// <summary>Aplica o mesmo canal a todos os links curtos de uma mensagem (só os do host do encurtador).</summary>
    public static string ApplyToMessage(string? message, string? shortBaseUrl, char channelCode, string? groupKey)
    {
        var text = message ?? string.Empty;
        if (!Uri.TryCreate(shortBaseUrl?.Trim(), UriKind.Absolute, out var baseUri))
        {
            return text;
        }

        var result = new StringBuilder(text.Length + 32);
        var cursor = 0;
        foreach (Match match in UrlRegex.Matches(text))
        {
            result.Append(text, cursor, match.Index - cursor);
            var url = match.Value;
            var trailing = string.Empty;
            while (url.Length > 0 && ".,;:!?)]}".Contains(url[^1]))
            {
                trailing = url[^1] + trailing;
                url = url[..^1];
            }

            // Do encurtador: mesmo host e porta, ou, em desenvolvimento, qualquer endereço de loopback (a tela reescreve
            // o host do link para o de quem está usando, ex.: https://localhost:7002 em vez de http://localhost:5001).
            var isOurs = Uri.TryCreate(url, UriKind.Absolute, out var candidate)
                && ((string.Equals(candidate.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) && candidate.Port == baseUri.Port)
                    || (candidate.IsLoopback && baseUri.IsLoopback));
            result.Append(isOurs ? Apply(url, channelCode, groupKey) : url).Append(trailing);
            cursor = match.Index + match.Length;
        }

        result.Append(text, cursor, text.Length - cursor);
        return result.ToString();
    }

    private static string ReplaceQuery(string existingQuery, string key, string? value)
    {
        var pairs = (existingQuery ?? string.Empty).TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.Split('=')[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!string.IsNullOrEmpty(value))
        {
            pairs.Add(key + "=" + Uri.EscapeDataString(value));
        }

        return string.Join('&', pairs);
    }
}
