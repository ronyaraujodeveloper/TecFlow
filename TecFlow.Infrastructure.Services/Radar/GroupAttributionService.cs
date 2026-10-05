using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class GroupAttributionService : IGroupAttributionService
{
    private readonly AppDbContext _context;

    public GroupAttributionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GroupAttributionResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default)
    {
        var linkIds = await _context.ShortAffiliateLinks
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.AffiliateLinkId)
            .ToListAsync(cancellationToken);

        var rows = await _context.LinkClickLogs
            .AsNoTracking()
            .Where(item => linkIds.Contains(item.AffiliateLinkId)
                && item.EventKind == LinkClickLog.EventKindClick
                && (item.SourceGroup != null || item.SubId != null))
            .GroupBy(item => new
            {
                Channel = item.SourceChannel ?? "—",
                Group = item.SourceGroup ?? "—",
                Sub = item.SubId ?? "—"
            })
            .Select(group => new GroupAttributionRowDto
            {
                Channel = group.Key.Channel,
                GroupKey = group.Key.Group,
                SubId = group.Key.Sub,
                Clicks = group.Count(),
                Links = group.Select(item => item.AffiliateLinkId).Distinct().Count()
            })
            .OrderByDescending(item => item.Clicks)
            .Take(80)
            .ToListAsync(cancellationToken);

        return new GroupAttributionResponseDto
        {
            Status = true,
            Descricao = rows.Count == 0
                ? "Ainda não há cliques com SubID de grupo. Faça um disparo para popular o relatório."
                : string.Empty,
            Rows = rows
        };
    }
}
