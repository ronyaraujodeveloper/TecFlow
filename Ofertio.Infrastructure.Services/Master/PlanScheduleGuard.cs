using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Limite diário de disparos (WhatsApp + Telegram somados) definido em Planos.</summary>
public static class PlanScheduleGuard
{
    public static async Task<string?> CheckAsync(
        IPlanLimitsService? planLimits,
        AppDbContext context,
        int userId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken)
    {
        if (planLimits is null)
        {
            return null;
        }

        var limits = await planLimits.GetForUserAsync(userId, cancellationToken);
        if (limits.IsUnlimited || limits.MaxDailySchedules <= 0)
        {
            return null;
        }

        var dayStart = scheduledAtUtc.Date;
        var dayEnd = dayStart.AddDays(1);
        var whatsApp = await context.WhatsAppBroadcastCampaigns
            .CountAsync(item => item.UserId == userId
                                && item.ScheduledAt >= dayStart
                                && item.ScheduledAt < dayEnd
                                && item.Status != WhatsAppBroadcastStatuses.Cancelled, cancellationToken);
        var telegram = await context.TelegramBroadcastCampaigns
            .CountAsync(item => item.UserId == userId
                                && item.ScheduledAt >= dayStart
                                && item.ScheduledAt < dayEnd, cancellationToken);
        return PlanLimitRules.ExceedsCount(limits.MaxDailySchedules, whatsApp + telegram)
            ? PlanLimitRules.SchedulesLimitMessage(limits)
            : null;
    }
}
