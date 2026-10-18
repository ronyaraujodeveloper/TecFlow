using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

public sealed class MasterAuditService : IMasterAuditService
{
    private const int MaxDetailsLength = 4000;

    private readonly AppDbContext _context;

    public MasterAuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        MasterActor actor,
        string action,
        string targetType,
        string? targetId,
        string? details,
        CancellationToken cancellationToken = default)
    {
        _context.MasterAuditLogs.Add(new MasterAuditLog
        {
            ActorUserId = actor.UserId,
            ActorEmail = Truncate(actor.Email, 256) ?? string.Empty,
            ActorRole = Truncate(actor.Role, 32) ?? string.Empty,
            Action = Truncate(action, 64) ?? string.Empty,
            TargetType = Truncate(targetType, 64) ?? string.Empty,
            TargetId = Truncate(targetId, 128),
            Details = Truncate(details, MaxDetailsLength),
            IpAddress = Truncate(actor.IpAddress, 64)
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MasterAuditLogDto>> QueryAsync(MasterAuditQueryDto query, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(query.Take, 1, 1000);
        var items = _context.MasterAuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            items = items.Where(item => item.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(query.Actor))
        {
            var actor = query.Actor.Trim();
            items = items.Where(item => item.ActorEmail.Contains(actor));
        }

        if (query.From.HasValue)
        {
            items = items.Where(item => item.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            items = items.Where(item => item.CreatedAt <= query.To.Value);
        }

        return await items
            .OrderByDescending(item => item.CreatedAt)
            .Take(take)
            .Select(item => new MasterAuditLogDto
            {
                Id = item.Id,
                CreatedAt = item.CreatedAt,
                ActorEmail = item.ActorEmail,
                ActorRole = item.ActorRole,
                Action = item.Action,
                TargetType = item.TargetType,
                TargetId = item.TargetId,
                Details = item.Details,
                IpAddress = item.IpAddress
            })
            .ToListAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
