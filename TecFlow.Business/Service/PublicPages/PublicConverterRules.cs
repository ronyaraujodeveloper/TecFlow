using System.Text.RegularExpressions;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.PublicPages;

/// <summary>Regras de slug, resolução ativa/inativa e deduplicação de plataformas na página pública.</summary>
public static class PublicConverterRules
{
    public const string GeradorSource = "Gerador";
    public const string PublicPageSource = "PublicPage";

    private static readonly Regex InvalidSlugChars = new("[^a-z0-9-]+", RegexOptions.Compiled);

    public static bool IsPublicPageSource(string? source) =>
        string.Equals(source, PublicPageSource, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeSlug(string? slug)
    {
        var raw = (slug ?? string.Empty).Trim().ToLowerInvariant();
        raw = raw.Replace(' ', '-');
        raw = InvalidSlugChars.Replace(raw, "-");
        raw = raw.Trim('-');
        while (raw.Contains("--", StringComparison.Ordinal))
        {
            raw = raw.Replace("--", "-", StringComparison.Ordinal);
        }

        if (raw.Length > 64)
        {
            raw = raw[..64].Trim('-');
        }

        return raw;
    }

    public static bool IsValidSlug(string? slug)
    {
        var normalized = NormalizeSlug(slug);
        return normalized.Length is >= 3 and <= 64;
    }

    public static PublicConverterPage? ResolveBySlug(
        IEnumerable<PublicConverterPage> pages,
        string? slug)
    {
        var normalized = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return pages.FirstOrDefault(page =>
            string.Equals(page.Slug, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<MarketplaceAccount> DistinctActivePlatforms(
        IEnumerable<MarketplaceAccount> accounts)
    {
        return accounts
            .Where(account => account.IsActive)
            .OrderBy(account => account.Id)
            .DistinctBy(account => account.MarketplaceType)
            .ToList();
    }

    public static MarketplaceAccount? FirstActiveForPlatform(
        IEnumerable<MarketplaceAccount> accounts,
        MarketplaceType platform)
    {
        return accounts
            .Where(account => account.IsActive && account.MarketplaceType == platform)
            .OrderBy(account => account.Id)
            .FirstOrDefault();
    }

    public static PublicConverterPage CreateVersionedSlug(
        PublicConverterPage currentActive,
        string newSlug)
    {
        ArgumentNullException.ThrowIfNull(currentActive);
        currentActive.IsActive = false;
        currentActive.Touch();

        return new PublicConverterPage
        {
            PublicCode = currentActive.PublicCode,
            UserId = currentActive.UserId,
            TenantId = currentActive.TenantId,
            DisplayName = currentActive.DisplayName,
            Slug = NormalizeSlug(newSlug),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
