using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

/// <summary>Envelope de resposta para geração de link de afiliado.</summary>
public class GerarLinkAfiliadoResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>URL enviada pelo usuário.</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>URL oficial de afiliado da Shopee (tag de rastreio).</summary>
    public string AffiliateUrl { get; set; } = string.Empty;

    /// <summary>URL interna TecFlow de telemetria (http://localhost:5001/{storeSlug}/{code}).</summary>
    public string ShortenedUrl { get; set; } = string.Empty;

    /// <summary>URL encurtada oficial da Shopee (https://br.shp.ee/...).</summary>
    public string ShortenedShopeeUrl { get; set; } = string.Empty;

    /// <summary>Alias de <see cref="AffiliateUrl"/> para o contrato `POST /api/links/convert`.</summary>
    public string ConvertedUrl { get; set; } = string.Empty;

    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string PlatformDetected { get; set; } = string.Empty;

    public Guid AffiliateLinkId { get; set; }

    public Guid LinkGroupId { get; set; }

    public int? SelectedStoreId { get; set; }

    public List<AffiliateLinkAccountVariantDto> Accounts { get; set; } = [];

    public string ResolvedShortUrl =>
        FirstNonEmpty(ShortenedUrl, LooksLikeTecFlowShort(ConvertedUrl) ? ConvertedUrl : null);

    public bool HasConvertedLink =>
        !string.IsNullOrWhiteSpace(AffiliateUrl)
        || !string.IsNullOrWhiteSpace(ResolvedShortUrl)
        || !string.IsNullOrWhiteSpace(ShortenedShopeeUrl)
        || Accounts.Exists(account => account.IsActive && (!string.IsNullOrWhiteSpace(account.AffiliateUrl) || !string.IsNullOrWhiteSpace(account.ShortenedUrl)));

    public void ApplySelectedAccount(int storeId)
    {
        var selected = Accounts.Find(account => account.StoreId == storeId && account.IsActive)
            ?? Accounts.Find(account => account.IsActive);
        if (selected is null)
        {
            return;
        }

        SelectedStoreId = selected.StoreId;
        AffiliateUrl = selected.AffiliateUrl;
        ConvertedUrl = selected.AffiliateUrl;
        ShortenedUrl = selected.ShortenedUrl;
        ShortenedShopeeUrl = selected.ShortenedShopeeUrl;
        AffiliateLinkId = selected.AffiliateLinkId;
    }

    public static GerarLinkAfiliadoResponseDto FromHistory(ShortAffiliateLinkDto item)
    {
        var dto = new GerarLinkAfiliadoResponseDto
        {
            Success = true,
            Status = true,
            OriginalUrl = item.OriginalUrl?.Trim() ?? string.Empty,
            AffiliateUrl = item.AffiliateUrl,
            ConvertedUrl = item.AffiliateUrl,
            ShortenedUrl = item.ShortenedUrl?.Trim() ?? string.Empty,
            ShortenedShopeeUrl = item.PlatformType == MarketplaceType.Shopee ? item.AffiliateUrl : string.Empty,
            PlatformDetected = string.IsNullOrWhiteSpace(item.PlatformName)
                ? item.PlatformType.ToString()
                : item.PlatformName,
            AffiliateLinkId = item.AffiliateLinkId,
            LinkGroupId = item.LinkGroupId,
            Message = "Link carregado do histórico.",
            Descricao = "Link carregado do histórico.",
            Accounts = (item.Accounts ?? [])
                .Where(account => account.IsActive)
                .ToList()
        };

        if (dto.Accounts.Count == 0 && !string.IsNullOrWhiteSpace(dto.AffiliateUrl))
        {
            dto.Accounts.Add(new AffiliateLinkAccountVariantDto
            {
                AffiliateLinkId = item.AffiliateLinkId,
                StoreId = 0,
                StoreName = item.PlatformName,
                AffiliateUrl = dto.AffiliateUrl,
                ShortenedUrl = dto.ShortenedUrl,
                ShortenedShopeeUrl = dto.ShortenedShopeeUrl,
                IsActive = true
            });
        }

        if (dto.Accounts.Count > 0)
        {
            dto.ApplySelectedAccount(dto.Accounts[0].StoreId);
        }

        return dto;
    }

    public void NormalizeHttp200()
    {
        if (!Success && Status)
        {
            Success = true;
        }

        if (string.IsNullOrWhiteSpace(AffiliateUrl) && !string.IsNullOrWhiteSpace(ConvertedUrl) && !LooksLikeTecFlowShort(ConvertedUrl))
        {
            AffiliateUrl = ConvertedUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(ConvertedUrl) && !string.IsNullOrWhiteSpace(AffiliateUrl))
        {
            ConvertedUrl = AffiliateUrl;
        }

        if (string.IsNullOrWhiteSpace(ShortenedUrl) && LooksLikeTecFlowShort(ConvertedUrl))
        {
            ShortenedUrl = ConvertedUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(ShortenedShopeeUrl) && LooksLikeShopeeOfficialShort(ConvertedUrl))
        {
            ShortenedShopeeUrl = ConvertedUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(ShortenedShopeeUrl) && LooksLikeShopeeOfficialShort(AffiliateUrl))
        {
            ShortenedShopeeUrl = AffiliateUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(ShortenedShopeeUrl)
            && !string.IsNullOrWhiteSpace(AffiliateUrl)
            && !PlatformDetected.Contains("TikTok", StringComparison.OrdinalIgnoreCase)
            && !PlatformDetected.Contains("Mercado", StringComparison.OrdinalIgnoreCase)
            && !PlatformDetected.Contains("Amazon", StringComparison.OrdinalIgnoreCase)
            && !PlatformDetected.Contains("Magazine", StringComparison.OrdinalIgnoreCase)
            && !PlatformDetected.Contains("Magalu", StringComparison.OrdinalIgnoreCase)
            && !PlatformDetected.Contains("Kabum", StringComparison.OrdinalIgnoreCase)
            && !PlatformDetected.Contains("Casas Bahia", StringComparison.OrdinalIgnoreCase)
            && !PlatformDetected.Contains("CasasBahia", StringComparison.OrdinalIgnoreCase))
        {
            ShortenedShopeeUrl = AffiliateUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(Message) && !string.IsNullOrWhiteSpace(Descricao))
        {
            Message = Descricao;
        }

        if (HasConvertedLink)
        {
            Success = true;
            Status = true;
        }
    }

    private static bool LooksLikeTecFlowShort(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.Contains("/r/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(
            url,
            @"/[A-Za-z][A-Za-z0-9]{0,79}/[A-Za-z0-9]{6,8}(?:[/?#]|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeShopeeOfficialShort(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && (url.Contains("br.shp.ee", StringComparison.OrdinalIgnoreCase)
            || url.Contains("shp.ee/", StringComparison.OrdinalIgnoreCase)
            || url.Contains("s.shopee.com", StringComparison.OrdinalIgnoreCase));

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
