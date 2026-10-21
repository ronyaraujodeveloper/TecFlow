using System.Net;
using Ofertio.Business.Dto;
using Ofertio.Core.Enums;
using Ofertio.Database.Filter;

namespace Ofertio.Business.Service.Radar;

public static class OfficialCatalogSearchRules
{
    public const int MinKeywordLength = 2;
    public const int DefaultLimit = 20;
    public const int MaxLimit = 20;

    public static bool IsValidQuery(string? keyword) =>
        !string.IsNullOrWhiteSpace(keyword) && keyword.Trim().Length >= MinKeywordLength;

    public static int ClampLimit(int limit) =>
        limit <= 0 ? DefaultLimit : Math.Clamp(limit, 1, MaxLimit);

    public static string EscapeGraphQl(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    public static bool HasRealAffiliateCredentials(string? appId, string? secret)
    {
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var id = appId.Trim();
        var key = secret.Trim();
        if (id.Equals("100000", StringComparison.Ordinal))
        {
            return false;
        }

        return !key.Contains("homolog", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("sandbox", StringComparison.OrdinalIgnoreCase);
    }

    public const string ShopeeMissingApiKeyMessage = "Chave de API não configurada";

    public const string AmazonMissingPaApiMessage = "PA-API não configurada";

    public const string MercadoLivreMissingAccountMessage =
        "API Mercado Livre ainda não autorizada pelo administrador da plataforma";

    public const string MercadoLivreSearchForbiddenMessage =
        "A API oficial do Mercado Livre recusou a busca (HTTP 403). A conexão é gerenciada pelo administrador da plataforma (Painel Master ➔ Integrações Globais); a vitrine pública não é consultada para evitar o bloqueio anti-robô.";

    /// <summary>401/403 ou erro de escopo de usuário na busca oficial (o token precisa ser renovado ou a conta reconectada).</summary>
    public static bool IsMercadoLivreSearchRefused(HttpStatusCode statusCode, string? body)
    {
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return true;
        }

        return statusCode != HttpStatusCode.OK
            && !string.IsNullOrWhiteSpace(body)
            && (body.Contains("scope", StringComparison.OrdinalIgnoreCase)
                || body.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                || body.Contains("invalid_token", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Desafio anti-bot da vitrine (account-verification / login) em vez da lista de produtos.</summary>
    public static bool IsMercadoLivreVerificationPage(string? finalUrl, string? html)
    {
        if (!string.IsNullOrWhiteSpace(finalUrl)
            && (finalUrl.Contains("account-verification", StringComparison.OrdinalIgnoreCase)
                || finalUrl.Contains("/gz/", StringComparison.OrdinalIgnoreCase)
                || finalUrl.Contains("/jms/", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(html)
            && VerificationMarkers.Any(marker => html.Contains(marker, StringComparison.OrdinalIgnoreCase))
            && !html.Contains("ui-search-layout", StringComparison.OrdinalIgnoreCase)
            && !html.Contains("poly-card", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly string[] VerificationMarkers =
        ["account-verification", "bot_challenge", "_bmstate", "micro-landing-container"];

    public static bool MatchesMercadoLivreAccount(
        MarketplaceType marketplaceType,
        string? friendlyName,
        string? shopName) =>
        marketplaceType == MarketplaceType.MercadoLivre
        || IsMercadoLivreLabel(friendlyName)
        || IsMercadoLivreLabel(shopName);

    public static bool IsMercadoLivreLabel(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return string.Equals(
            name.Replace(" ", string.Empty, StringComparison.Ordinal),
            "MercadoLivre",
            StringComparison.OrdinalIgnoreCase);
    }

    public static string? ResolveMercadoLivreAffiliateId(string? trackingId, string? affiliateTrackingId)
    {
        if (!string.IsNullOrWhiteSpace(trackingId))
        {
            return trackingId.Trim();
        }

        return string.IsNullOrWhiteSpace(affiliateTrackingId) ? null : affiliateTrackingId.Trim();
    }

    public static bool PassesOptionalFilters(
        OfficialCatalogProductDto item,
        OfficialCatalogSearchFilter filter) =>
        PassesPriceAndShipping(item, filter.MinPrice, filter.MaxPrice, filter.FreeShippingOnly == true)
        && (filter.HasCoupon != true || !string.IsNullOrWhiteSpace(item.CouponCode))
        && (filter.MainProductsOnly != true || !IsAccessoryOffer(item.ProductName, filter.Keyword));

    private static readonly HashSet<string> AccessoryWords = new(StringComparer.Ordinal)
    {
        "capa", "capinha", "case", "pelicula", "chaveiro", "carregador", "cabo", "fonte", "suporte",
        "adaptador", "protetor", "skin", "estojo", "bateria", "teclado", "tela", "carcaca", "dobradica",
        "cooler", "mochila", "pulseira", "controle", "refil"
    };

    /// <summary>
    /// Acessório do produto buscado: uma palavra de acessório aparece antes do termo no título
    /// ("Capa para iPhone 15", "Fonte Carregador Dell Inspiron"); "iPhone 15 + Capa" continua sendo produto principal.
    /// Quando o próprio termo é um acessório ("capa iphone"), nada é ocultado.
    /// </summary>
    public static bool IsAccessoryOffer(string? title, string? keyword)
    {
        var titleWords = NormalizeWords(title);
        if (titleWords.Count == 0)
        {
            return false;
        }

        var keywordWords = NormalizeWords(keyword).Where(word => word.Length >= 2).ToList();
        if (keywordWords.Any(IsAccessoryWord))
        {
            return false;
        }

        var firstKeyword = titleWords.FindIndex(word => keywordWords.Contains(word, StringComparer.Ordinal));
        var limit = firstKeyword >= 0 ? firstKeyword : Math.Min(2, titleWords.Count);
        return titleWords.Take(limit).Any(IsAccessoryWord);
    }

    private static bool IsAccessoryWord(string word) =>
        AccessoryWords.Contains(word)
        || (word.EndsWith("es", StringComparison.Ordinal) && AccessoryWords.Contains(word[..^2]))
        || (word.EndsWith('s') && AccessoryWords.Contains(word[..^1]));

    private static List<string> NormalizeWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var character in text.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>
    /// Filtros aplicados na tela sobre o que a última busca carregou (sem consultar as lojas):
    /// lojas marcadas, preço, frete grátis, cupom e categoria do Mercado Livre.
    /// </summary>
    public static IReadOnlyList<OfficialCatalogProductDto> FilterLoadedResults(
        IReadOnlyList<OfficialCatalogProductDto> items,
        OfficialCatalogSearchFilter filter)
    {
        var anyStore = filter.MercadoLivre || filter.Shopee || filter.Amazon;
        var category = filter.MercadoLivreCategory?.Trim();
        return items
            .Where(item => !anyStore || item.PlatformType switch
            {
                MarketplaceType.MercadoLivre => filter.MercadoLivre,
                MarketplaceType.Shopee => filter.Shopee,
                MarketplaceType.Amazon => filter.Amazon,
                _ => true
            })
            .Where(item => string.IsNullOrEmpty(category)
                || item.PlatformType != MarketplaceType.MercadoLivre
                || string.IsNullOrWhiteSpace(item.DomainId)
                || string.Equals(item.DomainId, category, StringComparison.OrdinalIgnoreCase))
            .Where(item => filter.OfficialStoreOnly != true || item.IsOfficialStore)
            .Where(item => filter.TrustedSellersOnly != true || item.SafetyScore >= TrustedSafetyScore)
            .Where(item => filter.BestSellersOnly != true || item.BestSellerPosition is not null)
            .Where(item => PassesOptionalFilters(item, filter))
            .ToList();
    }

    /// <summary>Faixa de preço e frete grátis: usados também durante a varredura do Mercado Livre.</summary>
    public static bool PassesPriceAndShipping(
        OfficialCatalogProductDto item,
        decimal? minPrice,
        decimal? maxPrice,
        bool freeShippingOnly)
    {
        if (minPrice is > 0 && (item.Price is null || item.Price < minPrice))
        {
            return false;
        }

        if (maxPrice is > 0 && (item.Price is null || item.Price > maxPrice))
        {
            return false;
        }

        return !freeShippingOnly || IsFreeShipping(item);
    }

    public static bool IsFreeShipping(OfficialCatalogProductDto item) =>
        !string.IsNullOrWhiteSpace(item.Shipping)
        && (item.Shipping.Contains("grátis", StringComparison.OrdinalIgnoreCase)
            || item.Shipping.Contains("gratis", StringComparison.OrdinalIgnoreCase)
            || item.Shipping.Contains("free", StringComparison.OrdinalIgnoreCase));

    public const string SortBestOpportunity = "melhor_oportunidade";
    public const string SortRelevance = "relevancia";
    public const string SortLowestPrice = "menor_preco";
    public const string SortBiggestDiscount = "maior_desconto";
    public const string SortBestSellers = "mais_vendidos";
    public const string SortHighestCommission = "maior_comissao";
    public const string SortBestRating = "melhor_avaliacao";

    public static IReadOnlyList<OfficialCatalogProductDto> ApplySort(
        IReadOnlyList<OfficialCatalogProductDto> items,
        string? sortBy) => sortBy?.Trim().ToLowerInvariant() switch
        {
            SortBestOpportunity => items
                .OrderByDescending(item => item.OpportunityScore)
                .ThenBy(item => item.Price ?? decimal.MaxValue)
                .ToList(),
            SortLowestPrice => items.OrderBy(item => item.Price ?? decimal.MaxValue).ToList(),
            SortBiggestDiscount => items
                .OrderByDescending(DiscountPercent)
                .ThenBy(item => item.Price ?? decimal.MaxValue)
                .ToList(),
            SortBestSellers => items
                .OrderBy(item => item.BestSellerPosition ?? int.MaxValue)
                .ThenByDescending(item => item.SoldQuantity ?? -1)
                .ThenByDescending(item => item.RatingCount ?? -1)
                .ToList(),
            SortHighestCommission => items
                .OrderByDescending(item => item.EstimatedCommission ?? -1m)
                .ThenByDescending(item => item.CommissionPercent ?? -1m)
                .ToList(),
            SortBestRating => items
                .OrderByDescending(item => item.RatingAverage ?? -1m)
                .ThenByDescending(item => item.RatingCount ?? -1)
                .ToList(),
            _ => items
        };

    public const int TrustedSafetyScore = 70;
    public const decimal MaxCommissionPercent = 90m;

    /// <summary>Taxa de comissão informada pelo usuário: vazia, zero ou acima de 90% vira "não configurada".</summary>
    public static decimal? NormalizeCommissionPercent(decimal? percent) =>
        percent is > 0 and <= MaxCommissionPercent ? decimal.Round(percent.Value, 2) : null;

    /// <summary>
    /// Comissão estimada: a Shopee informa a taxa por oferta; no Mercado Livre vale a tabela do afiliado pela categoria
    /// raiz do anúncio e, fora dela, a taxa do perfil do usuário; na Amazon, só a taxa do perfil (a API não expõe a comissão).
    /// </summary>
    public static void ApplyCommission(
        OfficialCatalogProductDto item,
        decimal? mercadoLivrePercent,
        decimal? amazonPercent)
    {
        if (item.CommissionPercent is null)
        {
            var categoryRate = item.PlatformType == MarketplaceType.MercadoLivre
                ? AffiliateCommissionRules.FindMercadoLivreRate(item.RootCategoryId, item.RootCategoryName)
                : null;
            if (categoryRate is not null)
            {
                item.CommissionPercent = categoryRate.Percent;
                item.CommissionSource = AffiliateCommissionRules.SourceMercadoLivreTable;
                item.CommissionCategory = categoryRate.Name;
            }
            else
            {
                var userRate = item.PlatformType switch
                {
                    MarketplaceType.MercadoLivre => NormalizeCommissionPercent(mercadoLivrePercent),
                    MarketplaceType.Amazon => NormalizeCommissionPercent(amazonPercent),
                    _ => null
                };
                if (userRate is null)
                {
                    return;
                }

                item.CommissionPercent = userRate;
                item.CommissionSource = AffiliateCommissionRules.SourceUserRate;
                item.CommissionCategory = item.RootCategoryName;
            }

            item.EstimatedCommission = null;
        }

        if (item.EstimatedCommission is null && item.Price is > 0 && item.CommissionPercent is >= 0)
        {
            item.EstimatedCommission = decimal.Round(item.Price.Value * item.CommissionPercent.Value / 100m, 2);
        }
    }

    /// <summary>Taxa recalculada na tela (ex.: o usuário mudou a sua taxa): a comissão da API da Shopee é mantida.</summary>
    public static void ResetEstimatedCommission(OfficialCatalogProductDto item)
    {
        if (item.CommissionSource == AffiliateCommissionRules.SourceShopeeApi)
        {
            return;
        }

        item.CommissionPercent = null;
        item.EstimatedCommission = null;
        item.CommissionSource = null;
        item.CommissionCategory = null;
    }

    /// <summary>
    /// Notas de 0 a 100. Segurança: loja oficial, reputação e MercadoLíder do vendedor, Full, garantia e condição.
    /// Vantagem: desconto, frete grátis, comissão, avaliação, ranking de mais vendidos e unidades vendidas.
    /// </summary>
    public static void ScoreOffer(OfficialCatalogProductDto item)
    {
        var highlights = new List<string>();
        var warnings = new List<string>();

        var safety = 30;
        if (item.IsOfficialStore)
        {
            safety += 30;
            highlights.Add("Loja oficial");
        }

        switch (item.SellerReputationLevel?.Trim().ToLowerInvariant())
        {
            case "5_green":
                safety += 25;
                break;
            case "4_light_green":
                safety += 15;
                break;
            case "3_yellow":
                warnings.Add("Reputação amarela");
                break;
            case "2_orange":
                safety -= 15;
                warnings.Add("Reputação laranja");
                break;
            case "1_red":
                safety -= 30;
                warnings.Add("Reputação vermelha");
                break;
        }

        switch (item.SellerPowerStatus?.Trim().ToLowerInvariant())
        {
            case "platinum":
                safety += 15;
                highlights.Add("MercadoLíder Platinum");
                break;
            case "gold":
                safety += 10;
                highlights.Add("MercadoLíder Gold");
                break;
            case "silver":
                safety += 5;
                highlights.Add("MercadoLíder");
                break;
            case "preferred":
                safety += 10;
                highlights.Add("Vendedor indicado");
                break;
        }

        if (item.SellerPositiveRate is >= 0.97m)
        {
            safety += 5;
        }
        else if (item.SellerPositiveRate is < 0.9m)
        {
            safety -= 10;
            warnings.Add("Muitas avaliações negativas do vendedor");
        }

        if (item.IsFulfillment)
        {
            safety += 10;
            highlights.Add("Envio Full");
        }

        if (!string.IsNullOrWhiteSpace(item.Warranty)
            && !item.Warranty.Contains("sem garantia", StringComparison.OrdinalIgnoreCase))
        {
            safety += 5;
        }

        if (string.Equals(item.Condition, "used", StringComparison.OrdinalIgnoreCase))
        {
            safety -= 10;
            warnings.Add("Produto usado");
        }

        if (!item.IsOfficialStore
            && string.IsNullOrWhiteSpace(item.SellerReputationLevel)
            && string.IsNullOrWhiteSpace(item.SellerPowerStatus))
        {
            warnings.Add("Sem dados do vendedor");
        }

        var advantage = 0;
        var discount = DiscountPercent(item);
        advantage += (int)Math.Min(35m, discount);
        if (discount >= 10m)
        {
            highlights.Add($"-{discount:0}%");
        }

        if (IsFreeShipping(item))
        {
            advantage += 10;
        }

        if (item.CommissionPercent is > 0)
        {
            advantage += (int)Math.Min(20m, item.CommissionPercent.Value * 2m);
        }

        if (item.RatingAverage is { } rating)
        {
            advantage += rating >= 4.7m ? 15 : rating >= 4.3m ? 10 : rating >= 4m ? 5 : 0;
            if (rating < 3.5m && item.RatingCount is >= 5)
            {
                warnings.Add("Avaliação baixa");
                safety -= 5;
            }
        }

        if (item.BestSellerPosition is { } position)
        {
            advantage += position <= 3 ? 20 : position <= 10 ? 15 : 10;
            highlights.Add($"{position}º mais vendido");
        }

        if (item.SoldQuantity is { } sold)
        {
            advantage += sold >= 1000 ? 10 : sold >= 100 ? 5 : 0;
        }

        item.SafetyScore = Math.Clamp(safety, 0, 100);
        item.AdvantageScore = Math.Clamp(advantage, 0, 100);
        item.OpportunityScore = (item.SafetyScore + item.AdvantageScore + 1) / 2;
        item.Highlights = highlights;
        item.Warnings = warnings;
    }

    public static string SafetyLabel(int score) =>
        score >= TrustedSafetyScore ? "Alta" : score >= 45 ? "Média" : "Baixa";

    public static string? ReputationLabel(string? level) => level?.Trim().ToLowerInvariant() switch
    {
        "5_green" => "reputação verde",
        "4_light_green" => "reputação verde-clara",
        "3_yellow" => "reputação amarela",
        "2_orange" => "reputação laranja",
        "1_red" => "reputação vermelha",
        _ => null
    };

    /// <summary>Resumo das ofertas visíveis: faixa e mediana de preço, frete, loja oficial, nota média e destaques.</summary>
    public static OfficialCatalogSearchMetricsDto BuildMetrics(IReadOnlyList<OfficialCatalogProductDto> items)
    {
        if (items.Count == 0)
        {
            return new OfficialCatalogSearchMetricsDto();
        }

        var prices = items.Where(item => item.Price is > 0).Select(item => item.Price!.Value).OrderBy(price => price).ToList();
        decimal? median = prices.Count == 0
            ? null
            : prices.Count % 2 == 1
                ? prices[prices.Count / 2]
                : (prices[(prices.Count / 2) - 1] + prices[prices.Count / 2]) / 2m;
        var rated = items.Where(item => item.RatingAverage is > 0).ToList();
        var sold = items.Where(item => item.SoldQuantity is > 0).ToList();
        return new OfficialCatalogSearchMetricsDto
        {
            Count = items.Count,
            MinPrice = prices.Count == 0 ? null : prices[0],
            MaxPrice = prices.Count == 0 ? null : prices[^1],
            MedianPrice = median,
            AveragePrice = prices.Count == 0 ? null : decimal.Round(prices.Average(), 2),
            FreeShippingPercent = Percent(items.Count(IsFreeShipping), items.Count),
            OfficialStorePercent = Percent(items.Count(item => item.IsOfficialStore), items.Count),
            TrustedSellerPercent = Percent(items.Count(item => item.SafetyScore >= TrustedSafetyScore), items.Count),
            AverageRating = rated.Count == 0 ? null : decimal.Round(rated.Average(item => item.RatingAverage!.Value), 1),
            TotalSoldKnown = sold.Count == 0 ? null : sold.Sum(item => item.SoldQuantity!.Value),
            BestOpportunity = items.MaxBy(item => item.OpportunityScore),
            BestSeller = items
                .Where(item => item.BestSellerPosition is not null || item.SoldQuantity is > 0)
                .OrderBy(item => item.BestSellerPosition ?? int.MaxValue)
                .ThenByDescending(item => item.SoldQuantity ?? 0)
                .FirstOrDefault(),
            TopCommission = items.Where(item => item.EstimatedCommission is > 0).MaxBy(item => item.EstimatedCommission),
            Cheapest = items.Where(item => item.Price is > 0).MinBy(item => item.Price)
        };
    }

    private static int Percent(int part, int total) =>
        total == 0 ? 0 : (int)Math.Round(part * 100m / total, MidpointRounding.AwayFromZero);

    /// <summary>Pílulas de categoria ("Buscar só em") exibidas antes de "Outras categorias".</summary>
    public const int MaxVisibleCategoryChips = 6;

    /// <summary>
    /// Une as categorias já exibidas para o termo com as de uma nova busca do mesmo termo: a ordem original
    /// (relevância do Mercado Livre) é mantida e só as novas entram no fim. Buscar numa categoria devolve só ela,
    /// e as demais pílulas precisam continuar na tela.
    /// </summary>
    public static List<OfficialCatalogCategoryDto> MergeCategoryChips(
        IEnumerable<OfficialCatalogCategoryDto> known,
        IEnumerable<OfficialCatalogCategoryDto> incoming)
    {
        var merged = new List<OfficialCatalogCategoryDto>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in known.Concat(incoming))
        {
            if (!string.IsNullOrWhiteSpace(category.Id) && ids.Add(category.Id.Trim()))
            {
                merged.Add(category);
            }
        }

        return merged;
    }

    /// <summary>
    /// Pílulas visíveis: todas quando expandido; recolhido, as <paramref name="max"/> primeiras (mais relevantes)
    /// e também a selecionada, se ela estiver entre as "outras".
    /// </summary>
    public static List<OfficialCatalogCategoryDto> VisibleCategoryChips(
        IReadOnlyList<OfficialCatalogCategoryDto> all,
        string? selectedId,
        bool expanded,
        int max = MaxVisibleCategoryChips)
    {
        if (expanded || all.Count <= max)
        {
            return [.. all];
        }

        var visible = all.Take(max).ToList();
        var selected = all.Skip(max).FirstOrDefault(category =>
            string.Equals(category.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        if (selected is not null)
        {
            visible.Add(selected);
        }

        return visible;
    }

    public static decimal DiscountPercent(OfficialCatalogProductDto item) =>
        item.Price is > 0 && item.OriginalPrice > item.Price
            ? (item.OriginalPrice.Value - item.Price.Value) / item.OriginalPrice.Value * 100m
            : 0m;

    public const int MercadoLivreScanPageSize = 20;

    /// <summary>Teto de produtos de catálogo analisados por busca (cada um custa 1 a 4 chamadas à API).</summary>
    public const int MercadoLivreMaxScannedProducts = 100;

    /// <summary>Enquanto o termo não trouxe nenhuma oferta e há termo mais curto por tentar, ele não consome o teto inteiro.</summary>
    public const int MercadoLivreScanPerQueryWithFallback = 40;

    /// <summary>Variações (<c>children_ids</c>) consultadas por produto "pai" do catálogo; cada uma com oferta vira um card.</summary>
    public const int MercadoLivreMaxChildrenPerProduct = 4;

    /// <summary>"Dell Inspiron i7 16GB" ➔ "Dell Inspiron i7" ➔ "Dell Inspiron" ➔ "Dell".</summary>
    public static IReadOnlyList<string> BuildShorterQueries(string? query)
    {
        var words = (query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var shorter = new List<string>();
        for (var count = words.Length - 1; count >= 1; count--)
        {
            var candidate = string.Join(' ', words.Take(count));
            if (IsValidQuery(candidate))
            {
                shorter.Add(candidate);
            }
        }

        return shorter;
    }

    /// <summary><c>MLB-GAMING_HEADSETS</c> ➔ "Gaming headsets" (quando o ML não informa o nome do domínio).</summary>
    public static string HumanizeMercadoLivreDomain(string domainId)
    {
        var name = domainId.Trim();
        var dash = name.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
        {
            name = name[(dash + 1)..];
        }

        name = name.Replace('_', ' ').Trim().ToLowerInvariant();
        return name.Length == 0 ? domainId : char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>Selo do Mercado Livre: quantos produtos de catálogo foram analisados e quantos tinham oferta ativa.</summary>
    public static string DescribeMercadoLivreScan(int offers, int scanned, string? effectiveQuery, string? typedQuery)
    {
        var message = $"{offers} {(offers == 1 ? "oferta" : "ofertas")} · {scanned} produtos analisados";
        if (scanned > 0 && offers == 0)
        {
            message += " (sem anúncio ativo)";
        }

        return !string.IsNullOrWhiteSpace(effectiveQuery)
            && !string.Equals(effectiveQuery.Trim(), typedQuery?.Trim(), StringComparison.OrdinalIgnoreCase)
                ? $"{message} · buscado como \"{effectiveQuery.Trim()}\""
                : message;
    }

    /// <summary>Ofertas do canal que realmente chegam à tela (filtros opcionais + URLs únicas).</summary>
    public static int CountVisibleOffers(
        IEnumerable<OfficialCatalogProductDto> items,
        OfficialCatalogSearchFilter filter) =>
        items
            .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl) && PassesOptionalFilters(item, filter))
            .Select(item => item.SourceUrl.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    public static bool HasConnectedMercadoLivreStore(
        MarketplaceType marketplaceType,
        string? friendlyName,
        string? shopName,
        string? trackingId,
        string? affiliateTrackingId) =>
        MatchesMercadoLivreAccount(marketplaceType, friendlyName, shopName)
        && !string.IsNullOrWhiteSpace(ResolveMercadoLivreAffiliateId(trackingId, affiliateTrackingId));

    public static OfficialCatalogChannelStatusDto BuildChannelStatus(
        string key,
        string label,
        bool included,
        int productCount,
        bool missingCredentials,
        string missingMessage,
        string? errorMessage = null)
    {
        if (!included)
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = false,
                ProductCount = 0,
                State = "skipped",
                Message = "Não consultada"
            };
        }

        if (productCount > 0)
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = true,
                ProductCount = productCount,
                State = "ok",
                Message = $"{productCount} produtos"
            };
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = true,
                ProductCount = 0,
                State = "error",
                Message = errorMessage.Trim()
            };
        }

        if (missingCredentials)
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = true,
                ProductCount = 0,
                State = "missing",
                Message = missingMessage
            };
        }

        return new OfficialCatalogChannelStatusDto
        {
            Key = key,
            Label = label,
            Included = true,
            ProductCount = 0,
            State = "empty",
            Message = "0 produtos"
        };
    }
}
