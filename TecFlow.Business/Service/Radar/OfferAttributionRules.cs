using System.Text.RegularExpressions;
using TecFlow.Business.Service.WhatsApp;

namespace TecFlow.Business.Service.Radar;

public static class OfferAttributionRules
{
    public const string SourceQuery = "tf_src";
    public const string GroupQuery = "tf_grp";
    public const string SubIdQuery = "sub_id";

    private static readonly Regex HttpUrlRegex = new(
        @"https?://[^\s<>""']+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string BuildSubId(string channel, string groupKey)
    {
        var src = Sanitize(channel, 12);
        var grp = Sanitize(groupKey, 24);
        if (string.IsNullOrWhiteSpace(src))
        {
            return grp;
        }

        return string.IsNullOrWhiteSpace(grp) ? src : $"{src}_{grp}";
    }

    public static string AppendTracking(string? url, string channel, string groupKey)
    {
        var working = (url ?? string.Empty).Trim();
        if (!Uri.TryCreate(working, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return working;
        }

        var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = part.IndexOf('=');
                var key = idx >= 0 ? Uri.UnescapeDataString(part[..idx]) : Uri.UnescapeDataString(part);
                var value = idx >= 0 ? Uri.UnescapeDataString(part[(idx + 1)..]) : string.Empty;
                pairs[key] = value;
            }
        }

        pairs[SourceQuery] = Sanitize(channel, 16);
        pairs[GroupQuery] = Sanitize(groupKey, 48);
        pairs[SubIdQuery] = BuildSubId(channel, groupKey);
        var query = string.Join('&', pairs.Select(item =>
            $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        var builder = new UriBuilder(uri) { Query = query };
        return builder.Uri.ToString();
    }

    public static string StampMessage(string? message, string channel, string groupKey)
    {
        var text = message ?? string.Empty;
        var match = HttpUrlRegex.Match(text);
        if (!match.Success)
        {
            return text;
        }

        var stamped = AppendTracking(match.Value, channel, groupKey);
        return string.Concat(text.AsSpan(0, match.Index), stamped, text.AsSpan(match.Index + match.Length));
    }

    public static (string? SourceChannel, string? SourceGroup, string? SubId) ReadTracking(HttpRequestQuery query)
    {
        var source = First(query, SourceQuery, "src");
        var group = First(query, GroupQuery, "grp");
        var subId = First(query, SubIdQuery);
        if (string.IsNullOrWhiteSpace(subId) && (!string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(group)))
        {
            subId = BuildSubId(source ?? string.Empty, group ?? string.Empty);
        }

        return (source, group, subId);
    }

    public static IReadOnlyList<string> ExtractUrls(string? text) =>
        WhatsAppBotRules.ExtractUrls(text ?? string.Empty);

    public static string Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(value.Trim(), @"[^A-Za-z0-9_\-@.]", "_");
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }

    private static string? First(HttpRequestQuery query, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (query.TryGet(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    public readonly struct HttpRequestQuery
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public HttpRequestQuery(IReadOnlyDictionary<string, string> values) =>
            _values = values;

        public static HttpRequestQuery FromPairs(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in pairs)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && !map.ContainsKey(pair.Key))
                {
                    map[pair.Key] = pair.Value;
                }
            }

            return new HttpRequestQuery(map);
        }

        public bool TryGet(string key, out string value) => _values.TryGetValue(key, out value!);
    }
}
