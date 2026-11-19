using System.Text.RegularExpressions;

namespace Ofertio.SharedUi.Components.Dashboard;

/// <summary>Classe de cor por canal (ch-whatsapp, ch-telegram...). As cores ficam em app.css.</summary>
public static class ChannelStyle
{
    public static string Css(string? channel) =>
        "ch-" + (string.IsNullOrWhiteSpace(channel) ? "desconhecido" : Regex.Replace(channel.ToLowerInvariant(), "[^a-z0-9]", string.Empty));
}
