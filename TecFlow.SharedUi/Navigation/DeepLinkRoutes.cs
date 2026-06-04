namespace TecFlow.SharedUi.Navigation;

public static class DeepLinkRoutes
{
    public const string Scheme = "tecflow";

    public const string EngajamentoFila = "/engajamento/fila";
    public static string ConciliacaoDetalhes(int id) => $"/conciliacao/detalhes/{id}";

    public static bool TryMapToAppRoute(Uri uri, out string route)
    {
        route = string.Empty;

        if (!string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.Host + uri.AbsolutePath;
        path = path.Trim('/');

        route = path switch
        {
            "engajamento/fila" => EngajamentoFila,
            _ when path.StartsWith("conciliacao/detalhes/", StringComparison.OrdinalIgnoreCase)
                => "/" + path,
            _ => "/" + path
        };

        return !string.IsNullOrWhiteSpace(route);
    }

    public static bool TryMapFromNotificationData(IReadOnlyDictionary<string, string> data, out string route)
    {
        route = string.Empty;
        if (data.TryGetValue("route", out var r) && !string.IsNullOrWhiteSpace(r))
        {
            route = r.StartsWith('/') ? r : "/" + r;
            return true;
        }

        if (data.TryGetValue("deepLink", out var deepLink) && Uri.TryCreate(deepLink, UriKind.Absolute, out var uri))
        {
            return TryMapToAppRoute(uri, out route);
        }

        return false;
    }
}
