using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Groups;

public sealed class ProductTagBackfillService : IProductTagBackfillService
{
    public const int BatchSize = 500;

    private readonly AppDbContext _context;
    private readonly ILogger<ProductTagBackfillService> _logger;

    public ProductTagBackfillService(AppDbContext context, ILogger<ProductTagBackfillService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> BackfillUntaggedOffersAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _context.ProductTags
            .Where(tag => tag.IsActive)
            .ToListAsync(cancellationToken);
        if (tags.Count == 0)
        {
            return 0;
        }

        var tagsBySlug = tags.ToDictionary(tag => tag.Slug, StringComparer.Ordinal);
        var tagged = 0;
        var lastId = 0;
        while (true)
        {
            var batch = await _context.GroupCapturedMessages
                .Where(item => item.Id > lastId && item.Type == OfferType.Product && !item.Tags.Any())
                .OrderBy(item => item.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            lastId = batch[^1].Id;
            var changed = 0;
            foreach (var offer in batch)
            {
                var matched = false;
                foreach (var slug in ProductTagClassifierRules.Classify(offer.ProductName, offer.RawText))
                {
                    if (tagsBySlug.TryGetValue(slug, out var tag))
                    {
                        offer.Tags.Add(tag);
                        matched = true;
                    }
                }

                if (matched)
                {
                    changed++;
                }
            }

            if (changed > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                tagged += changed;
            }

            // As tags precisam continuar rastreadas como existentes; soltas, o EF tentaria inseri-las de novo.
            _context.ChangeTracker.Clear();
            _context.ProductTags.AttachRange(tags);
        }

        _logger.LogInformation("Classificação de características concluída. OfertasComTags={Count}", tagged);
        return tagged;
    }
}

public sealed class ProductTagBackfillHost
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductTagBackfillHost> _logger;

    public ProductTagBackfillHost(IServiceScopeFactory scopeFactory, ILogger<ProductTagBackfillHost> logger)
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
            var service = scope.ServiceProvider.GetRequiredService<IProductTagBackfillService>();
            await service.BackfillUntaggedOffersAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Classificação de características das ofertas legadas falhou.");
        }
    }
}
