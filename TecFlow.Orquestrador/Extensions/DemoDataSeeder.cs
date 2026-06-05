using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Orquestrador.Extensions;

public static class DemoDataSeeder
{
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantProvisioning = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        currentTenant.BypassTenantFilters = true;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("TecFlow.Seed");

        if (await db.UserAccounts.AnyAsync(u => u.Email == "demo@TecFlow.local"))
        {
            logger.LogInformation("Dados demo já existem — seed ignorado.");
            return;
        }

        logger.LogInformation("A inserir dados demo (demo@TecFlow.local / Test@123)...");

        var now = DateTime.UtcNow;
        var owner = new UserAccount
        {
            Name = "Utilizador Demo",
            Email = "demo@TecFlow.local",
            PasswordHash = "$2a$11$/vyYZV8TriN/IBQ8vlTbyeaoJJfTHkWy9T./mfHyefLAqo6FYmTmm",
            Plan = "Pro",
            WhatsAppPhone = "11987654321",
            CreatedAt = now
        };

        var tenant = await tenantProvisioning.EnsureTenantForUserAsync(owner);
        owner.TenantId = tenant.Id;
        db.UserAccounts.Add(owner);
        await db.SaveChangesAsync();

        var tikTokCampaign = new Campaign
        {
            Name = "TikTok Shop — Verão 2026",
            Description = "Campanha sazonal de afiliados TikTok Shop com foco em eletrónicos.",
            StartDate = now.AddDays(-30),
            EndDate = now.AddDays(60),
            Budget = 15000m,
            OwnerId = owner.Id,
            TenantId = tenant.Id,
            CreatedAt = now
        };

        var shopeeCampaign = new Campaign
        {
            Name = "Shopee — Flash Sale Maio",
            Description = "Promoções relâmpago e cupons para afiliados Shopee.",
            StartDate = now.AddDays(-7),
            EndDate = now.AddDays(23),
            Budget = 8500m,
            OwnerId = owner.Id,
            TenantId = tenant.Id,
            CreatedAt = now
        };

        var closedCampaign = new Campaign
        {
            Name = "TikTok Ads — Remarketing Q1",
            Description = "Campanha encerrada de remarketing (dados históricos no painel).",
            StartDate = now.AddDays(-120),
            EndDate = now.AddDays(-30),
            Budget = 5200m,
            OwnerId = owner.Id,
            TenantId = tenant.Id,
            CreatedAt = now
        };

        db.Campaigns.AddRange(tikTokCampaign, shopeeCampaign, closedCampaign);
        await db.SaveChangesAsync();

        db.Metrics.AddRange(
            new Metric
            {
                CampaignId = tikTokCampaign.Id,
                Views = 125000,
                Clicks = 8750,
                Sales = 420,
                Investment = 4500m,
                Revenue = 18900m,
                OwnerId = owner.Id,
                TenantId = tenant.Id,
                CreatedAt = now
            },
            new Metric
            {
                CampaignId = tikTokCampaign.Id,
                Views = 48000,
                Clicks = 3200,
                Sales = 155,
                Investment = 1800m,
                Revenue = 6200m,
                OwnerId = owner.Id,
                TenantId = tenant.Id,
                CreatedAt = now.AddDays(-14)
            },
            new Metric
            {
                CampaignId = shopeeCampaign.Id,
                Views = 89000,
                Clicks = 6100,
                Sales = 310,
                Investment = 2800m,
                Revenue = 11200m,
                OwnerId = owner.Id,
                TenantId = tenant.Id,
                CreatedAt = now
            },
            new Metric
            {
                CampaignId = closedCampaign.Id,
                Views = 210000,
                Clicks = 9200,
                Sales = 180,
                Investment = 5200m,
                Revenue = 9800m,
                OwnerId = owner.Id,
                TenantId = tenant.Id,
                CreatedAt = now.AddDays(-90)
            });

        await db.SaveChangesAsync();
        logger.LogInformation("Seed demo concluído. OwnerId={OwnerId}", owner.Id);
    }
}
