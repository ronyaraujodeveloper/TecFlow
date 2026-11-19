using Ofertio.Business.Dto;

namespace Ofertio.Business.Service.Attribution;

/// <summary>Um clique já com os dados do produto, como o serviço o lê do banco.</summary>
public sealed record AnalyticsClickRow(
    DateTime ClickedAtUtc,
    bool IsBot,
    string? Channel,
    string? GroupKey,
    string? LegacyGroup,
    string? DeviceType,
    string? Ip,
    string? UserAgent,
    Guid LinkGroupId,
    string? ProductName,
    string? ProductImageUrl,
    string? Platform,
    string? ShopId,
    string? OriginalUrl,
    string? Country = null,
    string? Region = null,
    string? City = null,
    string? CountryName = null);

/// <summary>Evento da página pública (visita, conversão ou entrada em grupo), como o serviço o lê do banco.</summary>
public sealed record PublicEventRow(
    DateTime CreatedAtUtc,
    string Kind,
    string? Channel,
    bool IsBot,
    string? IpMasked,
    string? UserAgent);

public sealed record AnalyticsGroupName(string Name, string Channel);

/// <summary>
/// Cálculos do painel sem acesso a banco: recebe os cliques (período atual e anterior) e devolve os DTOs.
/// Robôs de prévia ficam fora dos cliques e são contados à parte. Só totais: nenhum IP sai daqui.
/// </summary>
public static class LinkAnalyticsCalculator
{
    private const int TopProductsLimit = 10;
    private const int GroupsLimit = 20;

    public static AnalyticsOverviewDto BuildOverview(
        IReadOnlyList<AnalyticsClickRow> rows,
        DateOnly from,
        DateOnly to,
        AnalyticsGrain grain,
        int linksGenerated,
        bool hasAnyLink,
        IReadOnlyDictionary<string, AnalyticsGroupName> groupsByKey,
        IReadOnlyDictionary<string, AnalyticsGroupName> groupsByLegacyId,
        IReadOnlyList<PublicEventRow>? publicEvents = null)
    {
        var (current, previous) = Split(rows, from, to);
        var humans = current.Where(row => !row.IsBot).ToList();
        var series = BuildSeries(humans, from, to, grain);

        return new AnalyticsOverviewDto
        {
            Funnel = BuildFunnel(publicEvents ?? [], from, to),
            States = BuildRegions(humans, state: true),
            Cities = BuildRegions(humans, state: false),
            HasRegionData = humans.Exists(row => !string.IsNullOrWhiteSpace(row.Region) || !string.IsNullOrWhiteSpace(row.City) || IsAbroad(row)),
            From = from,
            To = to,
            Grain = AnalyticsPeriodRules.GrainName(grain),
            Summary = BuildSummary(current, previous, linksGenerated),
            Series = series,
            ChannelOrder = ChannelOrder(humans),
            Channels = Breakdown(humans, row => NormalizeChannel(row.Channel), LinkChannelRules.LabelFor),
            Devices = Breakdown(humans, row => NormalizeDevice(row.DeviceType), label => label),
            Groups = BuildGroups(humans, groupsByKey, groupsByLegacyId),
            TopProducts = BuildTopProducts(humans),
            Heatmap = BuildHeatmap(humans),
            HasAnyLink = hasAnyLink
        };
    }

    public static AnalyticsProductDetailDto BuildProductDetail(
        IReadOnlyList<AnalyticsClickRow> productRows,
        DateOnly from,
        DateOnly to,
        AnalyticsGrain grain,
        IReadOnlyDictionary<string, AnalyticsGroupName> groupsByKey,
        IReadOnlyDictionary<string, AnalyticsGroupName> groupsByLegacyId)
    {
        var (current, previous) = Split(productRows, from, to);
        var humans = current.Where(row => !row.IsBot).ToList();
        var allKnown = productRows.Where(row => !row.IsBot).ToList();

        return new AnalyticsProductDetailDto
        {
            Product = BuildTopProducts(allKnown.Count > 0 ? allKnown : productRows.ToList()).FirstOrDefault() ?? new AnalyticsProductDto(),
            From = from,
            To = to,
            Grain = AnalyticsPeriodRules.GrainName(grain),
            Summary = BuildSummary(current, previous, 0),
            Series = BuildSeries(humans, from, to, grain),
            ChannelOrder = ChannelOrder(humans),
            Channels = Breakdown(humans, row => NormalizeChannel(row.Channel), LinkChannelRules.LabelFor),
            Groups = BuildGroups(humans, groupsByKey, groupsByLegacyId),
            Devices = Breakdown(humans, row => NormalizeDevice(row.DeviceType), label => label),
            Shops = Breakdown(
                humans,
                row => string.IsNullOrWhiteSpace(row.ShopId) ? (row.Platform ?? "Loja") : $"{row.Platform} · {row.ShopId}",
                label => label),
            Heatmap = BuildHeatmap(humans)
        };
    }

    /// <summary>Visitas, conversões e entradas em grupo da página pública (robôs fora; único = IP mascarado + navegador por dia).</summary>
    public static AnalyticsFunnelDto BuildFunnel(IReadOnlyList<PublicEventRow> events, DateOnly from, DateOnly to)
    {
        var (prevFrom, prevTo) = AnalyticsPeriodRules.Previous(from, to);
        var current = new List<PublicEventRow>();
        var previous = new List<PublicEventRow>();
        foreach (var item in events.Where(item => !item.IsBot))
        {
            var date = AnalyticsPeriodRules.ToLocalDate(item.CreatedAtUtc);
            if (date >= from && date <= to)
            {
                current.Add(item);
            }
            else if (date >= prevFrom && date <= prevTo)
            {
                previous.Add(item);
            }
        }

        var views = current.Where(item => item.Kind == "View").ToList();
        var joins = current.Where(item => item.Kind == "Join").ToList();
        var total = joins.Count;
        return new AnalyticsFunnelDto
        {
            Views = views.Count,
            UniqueViews = views
                .Select(item => (item.IpMasked, item.UserAgent, Day: AnalyticsPeriodRules.ToLocalDate(item.CreatedAtUtc)))
                .Distinct()
                .Count(),
            Conversions = current.Count(item => item.Kind == "Convert"),
            JoinClicks = total,
            PreviousViews = previous.Count(item => item.Kind == "View"),
            PreviousConversions = previous.Count(item => item.Kind == "Convert"),
            PreviousJoinClicks = previous.Count(item => item.Kind == "Join"),
            JoinByChannel = joins
                .GroupBy(item => string.IsNullOrWhiteSpace(item.Channel) ? LinkChannelRules.Unknown : item.Channel!)
                .Select(group => new AnalyticsBreakdownDto
                {
                    Key = group.Key,
                    Label = LinkChannelRules.LabelFor(group.Key),
                    Clicks = group.Count(),
                    Percent = total == 0 ? 0 : Math.Round(group.Count() * 100.0 / total, 1)
                })
                .OrderByDescending(item => item.Clicks)
                .ToList()
        };
    }

    /// <summary>
    /// Cliques por estado ou por cidade, pela região estimada do IP. Só totais; sem a base de localização a lista fica vazia.
    /// Fora do Brasil só o país é gravado: esses cliques aparecem na lista de estados como "Exterior · país".
    /// </summary>
    private static List<AnalyticsBreakdownDto> BuildRegions(List<AnalyticsClickRow> humans, bool state)
    {
        var located = humans
            .Where(row => state
                ? !string.IsNullOrWhiteSpace(row.Region) || IsAbroad(row)
                : !string.IsNullOrWhiteSpace(row.City))
            .ToList();
        var total = located.Count;
        return located
            .GroupBy(row => state
                ? string.IsNullOrWhiteSpace(row.Region)
                    ? $"Exterior · {(string.IsNullOrWhiteSpace(row.CountryName) ? row.Country!.Trim() : row.CountryName.Trim())}"
                    : Join(row.Region, row.Country)
                : Join(row.City, row.Region ?? row.Country))
            .Select(group => new AnalyticsBreakdownDto
            {
                Key = group.Key,
                Label = group.Key,
                Clicks = group.Count(),
                Percent = total == 0 ? 0 : Math.Round(group.Count() * 100.0 / total, 1)
            })
            .OrderByDescending(item => item.Clicks)
            .ThenBy(item => item.Label, StringComparer.CurrentCultureIgnoreCase)
            .Take(15)
            .ToList();

        static string Join(string? name, string? parent) =>
            string.IsNullOrWhiteSpace(parent) ? name!.Trim() : $"{name!.Trim()} · {parent.Trim()}";
    }

    /// <summary>Clique de fora do Brasil sem estado (só o país é gravado para o exterior).</summary>
    private static bool IsAbroad(AnalyticsClickRow row) =>
        string.IsNullOrWhiteSpace(row.Region)
        && !string.IsNullOrWhiteSpace(row.Country)
        && !string.Equals(row.Country.Trim(), "BR", StringComparison.OrdinalIgnoreCase);

    private static (List<AnalyticsClickRow> Current, List<AnalyticsClickRow> Previous) Split(
        IReadOnlyList<AnalyticsClickRow> rows,
        DateOnly from,
        DateOnly to)
    {
        var (prevFrom, prevTo) = AnalyticsPeriodRules.Previous(from, to);
        var current = new List<AnalyticsClickRow>();
        var previous = new List<AnalyticsClickRow>();
        foreach (var row in rows)
        {
            var date = AnalyticsPeriodRules.ToLocalDate(row.ClickedAtUtc);
            if (date >= from && date <= to)
            {
                current.Add(row);
            }
            else if (date >= prevFrom && date <= prevTo)
            {
                previous.Add(row);
            }
        }

        return (current, previous);
    }

    private static AnalyticsSummaryDto BuildSummary(
        List<AnalyticsClickRow> current,
        List<AnalyticsClickRow> previous,
        int linksGenerated)
    {
        var humans = current.Where(row => !row.IsBot).ToList();
        var byDay = humans
            .GroupBy(row => AnalyticsPeriodRules.ToLocalDate(row.ClickedAtUtc))
            .Select(group => (Day: group.Key, Count: group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Day)
            .FirstOrDefault();
        var previousHumans = previous.Where(row => !row.IsBot).ToList();

        return new AnalyticsSummaryDto
        {
            Clicks = humans.Count,
            UniqueClicks = CountUnique(humans),
            PreviewsFiltered = current.Count(row => row.IsBot),
            LinksGenerated = linksGenerated,
            PreviousClicks = previousHumans.Count,
            PreviousUniqueClicks = CountUnique(previousHumans),
            BestDay = byDay.Count > 0 ? byDay.Day : null,
            BestDayClicks = byDay.Count
        };
    }

    /// <summary>Visitante único = mesmo IP (já mascarado) e navegador no mesmo dia.</summary>
    private static int CountUnique(IEnumerable<AnalyticsClickRow> rows) =>
        rows.Select(row => (row.Ip, row.UserAgent, Day: AnalyticsPeriodRules.ToLocalDate(row.ClickedAtUtc)))
            .Distinct()
            .Count();

    private static List<AnalyticsPointDto> BuildSeries(
        List<AnalyticsClickRow> humans,
        DateOnly from,
        DateOnly to,
        AnalyticsGrain grain)
    {
        var points = AnalyticsPeriodRules.Buckets(from, to, grain)
            .ToDictionary(start => start, start => new AnalyticsPointDto
            {
                Start = start,
                Label = AnalyticsPeriodRules.BucketLabel(start, grain)
            });

        foreach (var row in humans)
        {
            var start = AnalyticsPeriodRules.BucketStart(AnalyticsPeriodRules.ToLocalDate(row.ClickedAtUtc), grain);
            if (!points.TryGetValue(start, out var point))
            {
                continue;
            }

            var channel = NormalizeChannel(row.Channel);
            point.Total++;
            point.ByChannel[channel] = point.ByChannel.GetValueOrDefault(channel) + 1;
        }

        return points.Values.OrderBy(point => point.Start).ToList();
    }

    private static List<string> ChannelOrder(List<AnalyticsClickRow> humans) =>
        humans.GroupBy(row => NormalizeChannel(row.Channel))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.Key)
            .ToList();

    private static List<AnalyticsBreakdownDto> Breakdown(
        List<AnalyticsClickRow> humans,
        Func<AnalyticsClickRow, string> keySelector,
        Func<string, string> labelSelector)
    {
        var total = humans.Count;
        return humans.GroupBy(keySelector)
            .Select(group => new AnalyticsBreakdownDto
            {
                Key = group.Key,
                Label = labelSelector(group.Key),
                Clicks = group.Count(),
                Percent = total == 0 ? 0 : Math.Round(group.Count() * 100.0 / total, 1)
            })
            .OrderByDescending(item => item.Clicks)
            .ThenBy(item => item.Label, StringComparer.Ordinal)
            .ToList();
    }

    private static List<AnalyticsGroupDto> BuildGroups(
        List<AnalyticsClickRow> humans,
        IReadOnlyDictionary<string, AnalyticsGroupName> groupsByKey,
        IReadOnlyDictionary<string, AnalyticsGroupName> groupsByLegacyId)
    {
        var result = new List<AnalyticsGroupDto>();
        var grouped = humans
            .Select(row => (Row: row, Key: row.GroupKey ?? row.LegacyGroup))
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key!);

        foreach (var group in grouped)
        {
            var isKey = GroupKeyRules.IsKey(group.Key);
            AnalyticsGroupName? known = null;
            if (isKey)
            {
                groupsByKey.TryGetValue(group.Key, out known);
            }
            else
            {
                groupsByLegacyId.TryGetValue(group.Key, out known);
            }

            var channel = known?.Channel ?? NormalizeChannel(group.First().Row.Channel);
            result.Add(new AnalyticsGroupDto
            {
                // O identificador antigo (JID/ChatId) nunca sai daqui: só a chave opaca ou um rótulo genérico.
                Key = isKey ? group.Key : "legado",
                Name = known?.Name ?? (isKey ? "Grupo removido" : "Grupo antigo"),
                Channel = channel,
                Clicks = group.Count(),
                UniqueClicks = CountUnique(group.Select(item => item.Row))
            });
        }

        return result
            .OrderByDescending(item => item.Clicks)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(GroupsLimit)
            .ToList();
    }

    private static List<AnalyticsProductDto> BuildTopProducts(List<AnalyticsClickRow> humans) =>
        humans.Where(row => row.LinkGroupId != Guid.Empty)
            .GroupBy(row => row.LinkGroupId)
            .Select(group => new AnalyticsProductDto
            {
                LinkGroupId = group.Key,
                Name = ProductName(group),
                ImageUrl = group.Select(row => row.ProductImageUrl).FirstOrDefault(url => !string.IsNullOrWhiteSpace(url)),
                Platform = group.Select(row => row.Platform).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty,
                Clicks = group.Count(),
                UniqueClicks = CountUnique(group)
            })
            .OrderByDescending(item => item.Clicks)
            .ThenByDescending(item => item.UniqueClicks)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(TopProductsLimit)
            .ToList();

    private static string ProductName(IEnumerable<AnalyticsClickRow> rows)
    {
        var name = rows.Select(row => row.ProductName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        var original = rows.Select(row => row.OriginalUrl).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (Uri.TryCreate(original, UriKind.Absolute, out var uri))
        {
            var last = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return string.IsNullOrWhiteSpace(last) ? uri.Host : $"{uri.Host}/{last}";
        }

        return "Produto sem nome";
    }

    private static List<List<int>> BuildHeatmap(List<AnalyticsClickRow> humans)
    {
        var grid = Enumerable.Range(0, 7).Select(_ => new int[24]).ToArray();
        foreach (var row in humans)
        {
            var local = AnalyticsPeriodRules.ToLocalTime(row.ClickedAtUtc);
            grid[AnalyticsPeriodRules.WeekdayRow(local)][local.Hour]++;
        }

        return grid.Select(line => line.ToList()).ToList();
    }

    private static string NormalizeChannel(string? channel) =>
        string.IsNullOrWhiteSpace(channel) ? LinkChannelRules.Unknown : channel.Trim();

    private static string NormalizeDevice(string? device) =>
        string.IsNullOrWhiteSpace(device) ? "Desconhecido" : device.Trim();
}
