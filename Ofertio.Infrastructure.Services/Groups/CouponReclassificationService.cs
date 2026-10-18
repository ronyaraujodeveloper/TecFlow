using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Groups;

public sealed class CouponReclassificationService : ICouponReclassificationService
{
    public const int BatchSize = 500;

    private readonly AppDbContext _context;
    private readonly ILogger<CouponReclassificationService> _logger;

    public CouponReclassificationService(AppDbContext context, ILogger<CouponReclassificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ReclassifyLegacyCouponsAsync(CancellationToken cancellationToken = default)
    {
        var reclassified = 0;
        var lastId = 0;
        while (true)
        {
            var batch = await _context.GroupCapturedMessages
                .AsNoTracking()
                .Where(item => item.Id > lastId
                    && item.Type == OfferType.Product
                    && (item.RawText.Contains("cupo")
                        || item.RawText.Contains("CUPO")
                        || item.RawText.Contains("digo")
                        || item.RawText.Contains("DIGO")
                        || item.RawText.Contains("OFF")
                        || item.RawText.Contains("off")))
                .OrderBy(item => item.Id)
                .Select(item => new { item.Id, item.RawText, item.HasDirectProductUrl })
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            lastId = batch[^1].Id;
            var couponIds = batch
                .Where(item => IsLegacyCoupon(item.RawText, item.HasDirectProductUrl))
                .Select(item => item.Id)
                .ToList();
            if (couponIds.Count == 0)
            {
                continue;
            }

            var entities = await _context.GroupCapturedMessages
                .Where(item => couponIds.Contains(item.Id))
                .ToListAsync(cancellationToken);
            foreach (var entity in entities)
            {
                entity.Type = OfferType.Coupon;
                if (string.IsNullOrWhiteSpace(entity.CouponCode))
                {
                    entity.CouponCode = CouponMessageRules.ExtractCouponCode(entity.RawText);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            reclassified += entities.Count;
        }

        _logger.LogInformation("Reclassificação de cupons legados concluída. Reclassificados={Count}", reclassified);
        return reclassified;
    }

    /// <summary>
    /// Itens sem link direto nunca apareceram no feed de produtos; só viram cupom quando a mensagem
    /// é claramente um anúncio de cupons, para não lotar a aba Cupons com produtos de links encurtados.
    /// </summary>
    public static bool IsLegacyCoupon(string rawText, bool hasDirectProductUrl) =>
        hasDirectProductUrl
            ? CouponMessageRules.Classify(rawText, hasDirectProductUrl: true) == OfferType.Coupon
            : CouponMessageRules.HasCouponSignal(rawText) && CouponMessageRules.IsCouponAnnouncement(rawText);
}

public sealed class CouponReclassificationHost
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CouponReclassificationHost> _logger;

    public CouponReclassificationHost(IServiceScopeFactory scopeFactory, ILogger<CouponReclassificationHost> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(InitialDelay, cancellationToken);
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICouponReclassificationService>();
            await service.ReclassifyLegacyCouponsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reclassificação de cupons legados falhou.");
        }
    }
}
