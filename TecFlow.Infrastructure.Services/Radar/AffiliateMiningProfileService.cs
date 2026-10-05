using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class AffiliateMiningProfileService : IAffiliateMiningProfileService
{
    private readonly AppDbContext _context;
    private readonly IGroupCapturedMessagesService _captured;

    public AffiliateMiningProfileService(AppDbContext context, IGroupCapturedMessagesService captured)
    {
        _context = context;
        _captured = captured;
    }

    public async Task<AffiliateMiningProfileResponseDto> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await EnsureAsync(userId, cancellationToken);
        var platforms = await _captured.ListActivePlatformsAsync(userId, cancellationToken);
        return new AffiliateMiningProfileResponseDto
        {
            Status = true,
            Profile = Map(entity, platforms)
        };
    }

    public async Task<AffiliateMiningProfileResponseDto> SaveAsync(
        int userId,
        AffiliateMiningProfileDto request,
        CancellationToken cancellationToken = default)
    {
        var entity = await EnsureAsync(userId, cancellationToken);
        entity.NichesCsv = AffiliateMiningRules.JoinNiches(request.Niches);
        entity.MinTicket = request.MinTicket is > 0 ? request.MinTicket : null;
        entity.MaxTicket = request.MaxTicket is > 0 ? request.MaxTicket : null;
        entity.MinCommissionPercent = request.MinCommissionPercent is > 0 and <= 90
            ? decimal.Round(request.MinCommissionPercent, 2)
            : 5;
        entity.RestrictToActiveStores = request.RestrictToActiveStores;
        entity.AutoPilotEnabled = request.AutoPilotEnabled;
        entity.AutoPilotChannel = NormalizeChannel(request.AutoPilotChannel);
        entity.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var platforms = await _captured.ListActivePlatformsAsync(userId, cancellationToken);
        return new AffiliateMiningProfileResponseDto
        {
            Status = true,
            Descricao = "Perfil de mineração salvo.",
            Profile = Map(entity, platforms)
        };
    }

    internal async Task<AffiliateMiningProfile> EnsureAsync(int userId, CancellationToken cancellationToken)
    {
        var entity = await _context.AffiliateMiningProfiles
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (entity is not null)
        {
            return entity;
        }

        entity = new AffiliateMiningProfile
        {
            UserId = userId,
            NichesCsv = string.Join(", ", AffiliateMiningRules.DefaultNiches)
        };
        _context.AffiliateMiningProfiles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static string NormalizeChannel(string? channel) =>
        channel is not null && channel.Contains("telegram", StringComparison.OrdinalIgnoreCase)
            ? "Telegram"
            : "WhatsApp";

    private static AffiliateMiningProfileDto Map(
        AffiliateMiningProfile entity,
        IReadOnlyCollection<MarketplaceType> platforms) =>
        new()
        {
            Id = entity.Id,
            Niches = AffiliateMiningRules.ParseNiches(entity.NichesCsv).ToList(),
            MinTicket = entity.MinTicket,
            MaxTicket = entity.MaxTicket,
            MinCommissionPercent = entity.MinCommissionPercent,
            RestrictToActiveStores = entity.RestrictToActiveStores,
            AutoPilotEnabled = entity.AutoPilotEnabled,
            AutoPilotChannel = entity.AutoPilotChannel,
            ActivePlatforms = platforms.Select(item => item.ToString()).ToList()
        };
}
