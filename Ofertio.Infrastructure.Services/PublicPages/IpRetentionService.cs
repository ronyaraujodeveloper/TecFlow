using Microsoft.EntityFrameworkCore;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.PublicPages;

/// <summary>
/// Retenção curta do IP mascarado. Ele só serve para estimar visitantes únicos; depois do prazo (padrão 90 dias)
/// é apagado. A região estimada (país, estado, cidade) fica, porque não identifica a pessoa.
/// </summary>
public sealed class IpRetentionService
{
    public const int DefaultDays = 90;
    public const string MaskedPlaceholder = "***";

    private readonly AppDbContext _context;

    public IpRetentionService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Apaga o IP mascarado mais antigo que o prazo. Devolve quantos registros foram limpos.</summary>
    public async Task<int> PurgeAsync(int days = DefaultDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, days));

        var events = await _context.PublicPageEvents.IgnoreQueryFilters()
            .Where(item => item.CreatedAt < cutoff && item.IpMasked != null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IpMasked, (string?)null), cancellationToken);

        var clicks = await _context.LinkClickLogs.IgnoreQueryFilters()
            .Where(item => item.ClickedAt < cutoff && item.IpAddress != MaskedPlaceholder)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IpAddress, MaskedPlaceholder), cancellationToken);

        return events + clicks;
    }
}
