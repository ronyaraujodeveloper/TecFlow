using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Interfaces.Telemetry;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Analytics;

public class AffiliateAnalyticsService : IAffiliateAnalyticsService
{
    private const decimal DivergenceTolerance = 0.05m;

    private readonly AppDbContext _db;
    private readonly IMarketplaceAuthService _authService;
    private readonly IMarketplaceSignatureService _signatureService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;
    private readonly ITecFlowBusinessMetrics _metrics;
    private readonly ITelemetryRecentErrorRecorder _errorRecorder;
    private readonly ILogger<AffiliateAnalyticsService> _logger;

    public AffiliateAnalyticsService(
        AppDbContext db,
        IMarketplaceAuthService authService,
        IMarketplaceSignatureService signatureService,
        IHttpClientFactory httpClientFactory,
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions,
        ITecFlowBusinessMetrics metrics,
        ITelemetryRecentErrorRecorder errorRecorder,
        ILogger<AffiliateAnalyticsService> logger)
    {
        _db = db;
        _authService = authService;
        _signatureService = signatureService;
        _httpClientFactory = httpClientFactory;
        _shopeeOptions = shopeeOptions.Value;
        _tikTokOptions = tikTokOptions.Value;
        _metrics = metrics;
        _errorRecorder = errorRecorder;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MarketplaceCommissionLineDto>> FetchMarketplaceCommissionsAsync(
        int ownerId,
        string affiliateId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var affiliate = await ResolveAffiliateAsync(ownerId, affiliateId, cancellationToken);
        var shopIds = await _db.Products
            .Where(p => p.OwnerId == ownerId && p.MarketplaceShopId != null)
            .Select(p => p.MarketplaceShopId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var tokens = shopIds.Count == 0
            ? await _db.MarketplaceTokens.ToListAsync(cancellationToken)
            : await _db.MarketplaceTokens
                .Where(t => shopIds.Contains(t.ShopId))
                .ToListAsync(cancellationToken);

        var lines = new List<MarketplaceCommissionLineDto>();

        foreach (var token in tokens)
        {
            var imported = await TryFetchFromMarketplaceApiAsync(
                token,
                startDateUtc,
                endDateUtc,
                cancellationToken);

            if (imported.Count > 0)
            {
                lines.AddRange(imported);
            }
        }

        if (lines.Count == 0)
        {
            lines.AddRange(await BuildLinesFromLocalOrdersAsync(
                ownerId,
                startDateUtc,
                endDateUtc,
                cancellationToken));
        }

        if (affiliate is not null)
        {
            foreach (var line in lines)
            {
                line.TrackingCode = string.IsNullOrWhiteSpace(line.TrackingCode)
                    ? affiliate.AffiliateCode
                    : line.TrackingCode;
            }
        }

        return lines;
    }

    public async Task<(AffiliatePerformanceDto Performance, IReadOnlyList<CommissionDiscrepancyReportDto> Discrepancies)>
        ReconcileCommissionsAsync(
            int ownerId,
            string affiliateId,
            DateTime startDateUtc,
            DateTime endDateUtc,
            CancellationToken cancellationToken = default)
    {
        var affiliate = await ResolveAffiliateAsync(ownerId, affiliateId, cancellationToken)
            ?? throw new InvalidOperationException($"Afiliado '{affiliateId}' não encontrado para o utilizador {ownerId}.");

        var marketplaceLines = await FetchMarketplaceCommissionsAsync(
            ownerId,
            affiliateId,
            startDateUtc,
            endDateUtc,
            cancellationToken);

        var conversions = await _db.Conversions
            .Where(c => c.OwnerId == ownerId && c.AffiliateId == affiliate.Id)
            .Where(c => c.CreatedAt >= startDateUtc && c.CreatedAt <= endDateUtc)
            .ToListAsync(cancellationToken);

        var metrics = await _db.Metrics
            .Where(m => m.OwnerId == ownerId && m.CreatedAt >= startDateUtc && m.CreatedAt <= endDateUtc)
            .ToListAsync(cancellationToken);

        var products = await _db.Products
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

        var discrepancies = new List<CommissionDiscrepancyReportDto>();
        var lineId = 1;
        var commissionRate = NormalizeCommissionRate(affiliate.Commission);

        foreach (var mpLine in marketplaceLines)
        {
            var localConversion = conversions.FirstOrDefault(c =>
                mpLine.ExternalOrderId.Contains(c.Id.ToString(), StringComparison.OrdinalIgnoreCase) ||
                mpLine.TrackingCode.Equals(affiliate.AffiliateCode, StringComparison.OrdinalIgnoreCase));

            var orderAmount = mpLine.OrderAmount > 0
                ? mpLine.OrderAmount
                : localConversion?.SaleAmount ?? 0m;

            var expected = Math.Round(orderAmount * commissionRate, 2, MidpointRounding.AwayFromZero);
            var paid = mpLine.PaidCommission;
            var difference = paid - expected;

            var status = ClassifyCommission(mpLine, expected, paid, difference);
            var isDivergent = status is CommissionStatus.Retido
                or CommissionStatus.Cancelado
                || (expected > 0 && paid < expected * (1m - DivergenceTolerance));

            if (isDivergent)
            {
                discrepancies.Add(new CommissionDiscrepancyReportDto
                {
                    LineId = lineId++,
                    ExternalOrderId = mpLine.ExternalOrderId,
                    TrackingCode = mpLine.TrackingCode,
                    Marketplace = mpLine.Marketplace,
                    ExpectedCommission = expected,
                    PaidCommission = paid,
                    Difference = difference,
                    Status = status,
                    IsDivergent = true,
                    Reason = BuildDiscrepancyReason(status, expected, paid, mpLine.IsRetained),
                    ProductSku = mpLine.ProductSku,
                    OccurredAtUtc = mpLine.ReportedAtUtc
                });
            }
        }

        var totalClicks = metrics.Sum(m => (long)m.Clicks) + marketplaceLines.Sum(l => l.Clicks);
        var totalConversions = conversions.Sum(c => c.Sales) + marketplaceLines.Sum(l => l.Conversions);
        if (totalConversions == 0)
        {
            totalConversions = marketplaceLines.Count;
        }

        var estimated = marketplaceLines.Sum(l =>
            Math.Round(l.OrderAmount * commissionRate, 2, MidpointRounding.AwayFromZero));
        if (estimated == 0)
        {
            estimated = conversions.Sum(c => Math.Round(c.SaleAmount * commissionRate, 2, MidpointRounding.AwayFromZero));
        }

        var paidTotal = marketplaceLines.Sum(l => l.PaidCommission);
        var retained = marketplaceLines.Where(l => l.IsRetained || l.PaidCommission == 0).Sum(l =>
            Math.Round(l.OrderAmount * commissionRate, 2, MidpointRounding.AwayFromZero));

        var performance = new AffiliatePerformanceDto
        {
            AffiliateId = affiliate.AffiliateCode,
            OwnerId = ownerId,
            TotalClicks = totalClicks,
            TotalConversions = totalConversions,
            ConversionRate = totalClicks == 0
                ? 0
                : Math.Round((decimal)totalConversions / totalClicks * 100m, 2, MidpointRounding.AwayFromZero),
            EstimatedCommission = estimated,
            PaidCommission = paidTotal,
            RetainedAmount = retained,
            DiscrepancyCount = discrepancies.Count,
            PeriodStartUtc = startDateUtc,
            PeriodEndUtc = endDateUtc,
            PrimaryMarketplace = marketplaceLines.FirstOrDefault()?.Marketplace
        };

        return (performance, discrepancies);
    }

    private async Task<Affiliate?> ResolveAffiliateAsync(
        int ownerId,
        string affiliateId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(affiliateId))
        {
            return await _db.Affiliates
                .Where(a => a.OwnerId == ownerId)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await _db.Affiliates
            .FirstOrDefaultAsync(
                a => a.OwnerId == ownerId &&
                     (a.AffiliateCode == affiliateId || a.Id.ToString() == affiliateId),
                cancellationToken);
    }

    private async Task<List<MarketplaceCommissionLineDto>> TryFetchFromMarketplaceApiAsync(
        MarketplaceToken token,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = await _authService.GetValidTokenAsync(
                token.ShopId,
                token.MarketplaceType,
                cancellationToken);

            return token.MarketplaceType switch
            {
                MarketplaceType.Shopee => await FetchShopeeReportAsync(token.ShopId, accessToken, startUtc, endUtc, cancellationToken),
                MarketplaceType.TikTokShop => await FetchTikTokReportAsync(token.ShopId, accessToken, startUtc, endUtc, cancellationToken),
                _ => []
            };
        }
        catch (Exception ex)
        {
            _metrics.RecordConciliationError();
            _errorRecorder.Record("MarketplaceReport", ex.Message, token.ShopId);
            _logger.LogWarning(
                ex,
                "Relatório de afiliado indisponível para {Marketplace} shop {ShopId}; fallback local será usado.",
                token.MarketplaceType,
                token.ShopId);
            return [];
        }
    }

    private async Task<List<MarketplaceCommissionLineDto>> FetchShopeeReportAsync(
        string shopId,
        string accessToken,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_shopeeOptions.PartnerId))
        {
            return [];
        }

        var path = _shopeeOptions.AffiliateCommissionReportPath;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sign = _signatureService.GenerateShopeeSign(
            _shopeeOptions.PartnerId,
            _shopeeOptions.PartnerKey,
            path,
            timestamp,
            accessToken,
            shopId);

        var query = $"partner_id={_shopeeOptions.PartnerId}&timestamp={timestamp}&sign={sign}&shop_id={shopId}&access_token={accessToken}" +
                    $"&start_time={startUtc:yyyy-MM-dd}&end_time={endUtc:yyyy-MM-dd}";

        var client = _httpClientFactory.CreateClient(IntegrationHttpClientNames.Shopee);
        var response = await client.GetAsync($"{path}?{query}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await ParseGenericCommissionPayloadAsync(
            await response.Content.ReadAsStringAsync(cancellationToken),
            MarketplaceType.Shopee,
            shopId);
    }

    private async Task<List<MarketplaceCommissionLineDto>> FetchTikTokReportAsync(
        string shopId,
        string accessToken,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_tikTokOptions.AppKey))
        {
            return [];
        }

        var path = _tikTokOptions.AffiliatePerformanceReportPath;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var body = JsonSerializer.Serialize(new
        {
            shop_id = shopId,
            start_time = startUtc.ToString("o"),
            end_time = endUtc.ToString("o")
        });

        var sign = _signatureService.GenerateTikTokShopSign(
            _tikTokOptions.AppKey,
            _tikTokOptions.AppSecret,
            path,
            timestamp,
            body);

        var client = _httpClientFactory.CreateClient(IntegrationHttpClientNames.TikTokShop);
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("x-tts-access-token", accessToken);
        request.Headers.Add("app_key", _tikTokOptions.AppKey);
        request.Headers.Add("timestamp", timestamp.ToString());
        request.Headers.Add("sign", sign);
        request.Content = JsonContent.Create(new { shop_id = shopId, start_time = startUtc, end_time = endUtc });

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await ParseGenericCommissionPayloadAsync(
            await response.Content.ReadAsStringAsync(cancellationToken),
            MarketplaceType.TikTokShop,
            shopId);
    }

    private static Task<List<MarketplaceCommissionLineDto>> ParseGenericCommissionPayloadAsync(
        string json,
        MarketplaceType marketplace,
        string shopId)
    {
        var lines = new List<MarketplaceCommissionLineDto>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(lines);
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("list", out var list) &&
            list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                lines.Add(new MarketplaceCommissionLineDto
                {
                    ExternalOrderId = item.TryGetProperty("order_id", out var oid) ? oid.GetString() ?? "" : "",
                    ShopId = shopId,
                    Marketplace = marketplace,
                    TrackingCode = item.TryGetProperty("tracking_code", out var tc) ? tc.GetString() ?? "" : "",
                    PaidCommission = item.TryGetProperty("commission", out var c) ? c.GetDecimal() : 0m,
                    OrderAmount = item.TryGetProperty("order_amount", out var oa) ? oa.GetDecimal() : 0m,
                    Clicks = item.TryGetProperty("clicks", out var cl) ? cl.GetInt32() : 0,
                    Conversions = item.TryGetProperty("conversions", out var cv) ? cv.GetInt32() : 1,
                    ProductSku = item.TryGetProperty("sku", out var sku) ? sku.GetString() : null,
                    ReportedAtUtc = DateTime.UtcNow,
                    IsRetained = item.TryGetProperty("status", out var st) &&
                                 st.GetString()?.Contains("retain", StringComparison.OrdinalIgnoreCase) == true
                });
            }
        }

        return Task.FromResult(lines);
    }

    private async Task<List<MarketplaceCommissionLineDto>> BuildLinesFromLocalOrdersAsync(
        int ownerId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var orders = await _db.MarketplaceOrders
            .Include(o => o.Lines)
            .Where(o => o.ProcessedAt >= startUtc && o.ProcessedAt <= endUtc)
            .ToListAsync(cancellationToken);

        var lines = new List<MarketplaceCommissionLineDto>();
        foreach (var order in orders)
        {
            var sku = order.Lines.FirstOrDefault()?.SkuCode;
            lines.Add(new MarketplaceCommissionLineDto
            {
                ExternalOrderId = order.ExternalOrderId,
                ShopId = order.ShopId,
                Marketplace = order.MarketplaceType,
                TrackingCode = order.ExternalOrderId,
                PaidCommission = 0,
                OrderAmount = order.Lines.Sum(l => l.Quantity) * 10m,
                Conversions = 1,
                ProductSku = sku,
                ReportedAtUtc = order.ProcessedAt,
                IsRetained = string.Equals(order.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)
            });
        }

        return lines;
    }

    private static decimal NormalizeCommissionRate(decimal commission) =>
        commission > 1m ? commission / 100m : commission;

    private static CommissionStatus ClassifyCommission(
        MarketplaceCommissionLineDto line,
        decimal expected,
        decimal paid,
        decimal difference)
    {
        if (line.IsRetained || (expected > 0 && paid == 0))
        {
            return CommissionStatus.Retido;
        }

        if (string.Equals(line.ExternalOrderId, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return CommissionStatus.Cancelado;
        }

        if (expected > 0 && paid < expected * (1m - DivergenceTolerance))
        {
            return CommissionStatus.Retido;
        }

        if (paid >= expected * (1m - DivergenceTolerance))
        {
            return CommissionStatus.Pago;
        }

        return CommissionStatus.Rastreado;
    }

    private static string BuildDiscrepancyReason(
        CommissionStatus status,
        decimal expected,
        decimal paid,
        bool retained) =>
        retained
            ? "Comissão retida pelo marketplace sem repasse no período."
            : status == CommissionStatus.Cancelado
                ? "Pedido cancelado após rastreio de conversão."
                : $"Repasse ({paid:C}) inferior ao esperado ({expected:C}) para a taxa acordada.";
}
