using TecFlow.Core.Entities;

namespace TecFlow.SharedUi.Extensions;

public static class CampaignExtensions
{
    public static bool IsActive(this Campaign campaign, DateTime? referenceUtc = null)
    {
        var now = referenceUtc ?? DateTime.UtcNow;
        return campaign.StartDate <= now && campaign.EndDate >= now;
    }
}
