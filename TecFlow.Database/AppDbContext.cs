using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Entities;
using TecFlow.Database.Entity;
using TecFlow.Database.MultiTenancy;
using TecFlow.Util.Security;
using TecFlow.Util.Security.EntityFramework;

namespace TecFlow.Database;

public class AppDbContext : DbContext
{
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentTenantService _currentTenant;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IEncryptionService encryptionService,
        ICurrentTenantService currentTenant)
        : base(options)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<MarketplaceAccount> MarketplaceAccounts { get; set; } = null!;
    public DbSet<Affiliate> Affiliates { get; set; } = null!;
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<Metric> Metrics { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Content> Contents { get; set; } = null!;
    public DbSet<Conversion> Conversions { get; set; } = null!;
    public DbSet<UserAccount> UserAccounts { get; set; } = null!;
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; } = null!;
    public DbSet<MarketplaceToken> MarketplaceTokens { get; set; } = null!;
    public DbSet<MarketplaceOrder> MarketplaceOrders { get; set; } = null!;
    public DbSet<MarketplaceOrderLine> MarketplaceOrderLines { get; set; } = null!;
    public DbSet<UserDeviceToken> UserDeviceTokens { get; set; } = null!;
    public DbSet<GlobalAdvertisingProduct> GlobalAdvertisingProducts { get; set; } = null!;
    public DbSet<MarketplaceAffiliateLink> MarketplaceAffiliateLinks { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; } = null!;
    public DbSet<Inventory> Inventories { get; set; } = null!;
    public DbSet<InventoryMovement> InventoryMovements { get; set; } = null!;
    public DbSet<IntegracaoLoja> IntegracaoLojas { get; set; } = null!;
    public DbSet<ShortAffiliateLink> ShortAffiliateLinks { get; set; } = null!;
    public DbSet<ShortAffiliateLinkAccount> ShortAffiliateLinkAccounts { get; set; } = null!;
    public DbSet<LinkClickLog> LinkClickLogs { get; set; } = null!;
    public DbSet<PublicConverterPage> PublicConverterPages { get; set; } = null!;
    public DbSet<WhatsAppIntegration> WhatsAppIntegrations { get; set; } = null!;
    public DbSet<WhatsAppGroup> WhatsAppGroups { get; set; } = null!;
    public DbSet<WhatsAppBroadcastCampaign> WhatsAppBroadcastCampaigns { get; set; } = null!;
    public DbSet<TelegramIntegration> TelegramIntegrations { get; set; } = null!;
    public DbSet<TelegramBroadcastCampaign> TelegramBroadcastCampaigns { get; set; } = null!;
    public DbSet<GroupCapturedMessage> GroupCapturedMessages { get; set; } = null!;

    /// <summary>Usuários oficiais do ecossistema TecFlow (tabela users).</summary>
    public DbSet<UserEntity> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSensitiveData(modelBuilder);
        ApplyTenantQueryFilters(modelBuilder);

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Name);

        modelBuilder.Entity<MarketplaceAccount>(entity =>
        {
            entity.ToTable("MarketplaceAccounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.UserId).HasMaxLength(128);
            entity.Property(account => account.FriendlyName).HasMaxLength(256);
            entity.Property(account => account.ShopId).HasMaxLength(128);
            entity.Property(account => account.ShopName).HasMaxLength(256);
            entity.Property(account => account.TrackingId).HasMaxLength(64);
            entity.Property(account => account.AffiliateTrackingId).HasMaxLength(64);
            entity.Property(account => account.AppKey).HasMaxLength(256);
            entity.Property(account => account.AppSecret).HasMaxLength(512);
            entity.Property(account => account.IsActive).HasDefaultValue(true);
            entity.Property(account => account.MarketplaceType).HasConversion<int>();
            entity.HasIndex(account => new { account.TenantId, account.ShopId, account.MarketplaceType })
                .IsUnique();
            entity.HasIndex(account => new { account.MarketplaceType, account.TrackingId })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [TrackingId] IS NOT NULL AND [TrackingId] <> N''");
            entity.HasOne(account => account.Tenant)
                .WithMany(tenant => tenant.MarketplaceAccounts)
                .HasForeignKey(account => account.TenantId);
        });

        modelBuilder.Entity<UserAccount>()
            .HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId);

        modelBuilder.Entity<UserExternalLogin>(entity =>
        {
            entity.ToTable("AspNetUserLogins");
            entity.HasKey(login => new { login.LoginProvider, login.ProviderKey });
            entity.HasIndex(login => login.UserId);
            entity.Property(login => login.LoginProvider).HasMaxLength(128);
            entity.Property(login => login.ProviderKey).HasMaxLength(128);
            entity.Property(login => login.ProviderDisplayName).HasMaxLength(256);
            entity.HasOne(login => login.User)
                .WithMany(user => user.ExternalLogins)
                .HasForeignKey(login => login.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegracaoLoja>(entity =>
        {
            entity.ToTable("IntegracaoLoja");
            entity.HasIndex(i => new { i.UserId, i.ShopId, i.PlatformType }).IsUnique();
            entity.Property(i => i.AffiliateTrackingId).HasMaxLength(64);
            entity.HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.Tenant)
                .WithMany()
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Campaign>()
            .HasIndex(c => c.LojaId);

        modelBuilder.Entity<Metric>()
            .HasIndex(m => m.LojaId);

        modelBuilder.Entity<Campaign>()
            .HasOne<IntegracaoLoja>()
            .WithMany()
            .HasForeignKey(c => c.LojaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Metric>()
            .HasOne<IntegracaoLoja>()
            .WithMany()
            .HasForeignKey(m => m.LojaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Conversion>().Property(c => c.SaleAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Affiliate>().Property(a => a.Commission).HasPrecision(18, 2);
        modelBuilder.Entity<Campaign>().Property(c => c.Budget).HasPrecision(18, 2);
        modelBuilder.Entity<Content>().Property(c => c.Budget).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.SalesVolume).HasPrecision(18, 2);
        modelBuilder.Entity<Metric>().Property(m => m.Investment).HasPrecision(18, 2);
        modelBuilder.Entity<Metric>().Property(m => m.Revenue).HasPrecision(18, 2);

        modelBuilder.Entity<Metric>()
            .HasOne(m => m.Campaign)
            .WithMany()
            .HasForeignKey(m => m.CampaignId);

        modelBuilder.Entity<Metric>()
            .HasOne<Metric>()
            .WithMany(m => m.ChildMetrics)
            .HasForeignKey(m => m.ParentMetricId);

        modelBuilder.Entity<Affiliate>()
            .HasOne(a => a.Campaign)
            .WithMany(c => c.Affiliates)
            .HasForeignKey(a => a.CampaignId);

        modelBuilder.Entity<Affiliate>()
            .HasOne(a => a.Content)
            .WithMany(c => c.Affiliates)
            .HasForeignKey(a => a.ContentId);

        modelBuilder.Entity<MarketplaceToken>()
            .HasIndex(t => new { t.TenantId, t.ShopId, t.MarketplaceType })
            .IsUnique();

        modelBuilder.Entity<MarketplaceOrder>()
            .HasIndex(o => new { o.TenantId, o.ExternalOrderId, o.MarketplaceType, o.ShopId })
            .IsUnique();

        modelBuilder.Entity<MarketplaceOrder>()
            .HasMany(o => o.Lines)
            .WithOne(l => l.MarketplaceOrder)
            .HasForeignKey(l => l.MarketplaceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.TenantId, p.SkuCode, p.MarketplaceSource, p.MarketplaceShopId })
            .HasFilter(Database.IsSqlServer()
                ? "[SkuCodigo] IS NOT NULL AND [MarketplaceOrigem] IS NOT NULL"
                : "\"SkuCodigo\" IS NOT NULL AND \"MarketplaceOrigem\" IS NOT NULL");

        modelBuilder.Entity<UserDeviceToken>()
            .HasIndex(t => new { t.TenantId, t.OwnerId, t.Token })
            .IsUnique();

        modelBuilder.Entity<UserDeviceToken>()
            .Property(t => t.Token)
            .HasMaxLength(512);

        modelBuilder.Entity<UserDeviceToken>()
            .Property(t => t.Platform)
            .HasMaxLength(32);

        modelBuilder.Entity<UserDeviceToken>()
            .Property(t => t.DeviceId)
            .HasMaxLength(128);

        modelBuilder.Entity<GlobalAdvertisingProduct>()
            .Property(p => p.AveragePrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<GlobalAdvertisingProduct>()
            .HasIndex(p => new { p.TenantId, p.GlobalProductUid })
            .IsUnique();

        modelBuilder.Entity<GlobalAdvertisingProduct>()
            .HasMany(p => p.MarketplaceLinks)
            .WithOne(l => l.GlobalProduct)
            .HasForeignKey(l => l.GlobalProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MarketplaceAffiliateLink>()
            .HasIndex(l => new { l.GlobalProductId, l.MarketplaceType })
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.TenantId, c.DocumentNumber });

        modelBuilder.Entity<SalesOrder>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesOrder>()
            .Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesOrder>()
            .Property(o => o.FreightAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesOrder>()
            .HasIndex(o => new { o.TenantId, o.OrderNumber })
            .IsUnique();

        modelBuilder.Entity<SalesOrder>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId);

        modelBuilder.Entity<SalesOrder>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SalesOrderItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesOrderItem>()
            .Property(i => i.TotalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesOrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId);

        modelBuilder.Entity<Inventory>()
            .HasIndex(i => new { i.TenantId, i.ProductId })
            .IsUnique();

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId);

        modelBuilder.Entity<InventoryMovement>()
            .HasIndex(m => new { m.TenantId, m.SalesOrderId, m.MovementType });

        modelBuilder.Entity<InventoryMovement>()
            .HasOne(m => m.Inventory)
            .WithMany(i => i.Movements)
            .HasForeignKey(m => m.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShortAffiliateLink>(entity =>
        {
            entity.HasIndex(link => link.ShortCode).IsUnique();
            entity.HasIndex(link => link.AffiliateLinkId).IsUnique();
            entity.HasIndex(link => new { link.UserId, link.CreatedAt });
            entity.HasIndex(link => link.LinkGroupId);
            entity.Property(link => link.AffiliateUrl).HasMaxLength(2048);
            entity.Property(link => link.Source).HasMaxLength(32);
            entity.HasOne<MarketplaceAccount>()
                .WithMany()
                .HasForeignKey(link => link.MarketplaceAccountId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(link => link.AccountLinks)
                .WithOne(account => account.AffiliateLink)
                .HasForeignKey(account => account.ShortAffiliateLinkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShortAffiliateLinkAccount>(entity =>
        {
            entity.ToTable("ShortAffiliateLinkAccounts");
            entity.Property(account => account.IsActive).HasDefaultValue(true);
            entity.HasIndex(account => account.LinkGroupId);
            entity.HasIndex(account => new { account.LinkGroupId, account.IntegracaoLojaId }).IsUnique();
        });

        modelBuilder.Entity<PublicConverterPage>(entity =>
        {
            entity.ToTable("PublicConverterPages");
            entity.Property(page => page.Slug).HasMaxLength(64).IsRequired();
            entity.Property(page => page.DisplayName).HasMaxLength(128);
            entity.Property(page => page.IsActive).HasDefaultValue(true);
            entity.HasIndex(page => page.Slug).IsUnique();
            entity.HasIndex(page => page.PublicCode);
            entity.HasIndex(page => new { page.UserId, page.IsActive });
        });

        modelBuilder.Entity<WhatsAppIntegration>(entity =>
        {
            entity.ToTable("WhatsAppIntegrations");
            entity.Property(item => item.InstanceName).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ConnectionStatus).HasMaxLength(32).IsRequired();
            entity.Property(item => item.PhoneNumber).HasMaxLength(32);
            entity.Property(item => item.ProfileName).HasMaxLength(128);
            entity.Property(item => item.ProfilePictureUrl).HasMaxLength(512);
            entity.Property(item => item.IsActive).HasDefaultValue(true);
            entity.Property(item => item.EnableAutoConvertBot).HasDefaultValue(true);
            entity.Property(item => item.ReplyToPrivateMessages).HasDefaultValue(true);
            entity.Property(item => item.ReplyToGroupMessages).HasDefaultValue(false);
            entity.Property(item => item.Token).HasColumnType("nvarchar(max)");
            entity.Property(item => item.ApiKey).HasColumnType("nvarchar(max)");
            entity.Property(item => item.SessionData).HasColumnType("nvarchar(max)");
            entity.HasIndex(item => item.UserId);
            entity.HasIndex(item => item.InstanceName).IsUnique();
        });

        modelBuilder.Entity<TelegramIntegration>(entity =>
        {
            entity.ToTable("TelegramIntegrations");
            entity.Property(item => item.Token).HasColumnType("nvarchar(max)");
            entity.Property(item => item.ApiKey).HasColumnType("nvarchar(max)");
            entity.Property(item => item.SessionData).HasColumnType("nvarchar(max)");
            entity.Property(item => item.ChatId).HasMaxLength(64);
            entity.Property(item => item.BotUsername).HasMaxLength(128);
            entity.Property(item => item.IsActive).HasDefaultValue(true);
            entity.HasIndex(item => item.UserId);
        });

        modelBuilder.Entity<TelegramBroadcastCampaign>(entity =>
        {
            entity.ToTable("TelegramBroadcastCampaigns");
            entity.Property(item => item.Title).HasMaxLength(128).IsRequired();
            entity.Property(item => item.MessageText).IsRequired();
            entity.Property(item => item.ImageUrl).HasMaxLength(512);
            entity.Property(item => item.TargetChatId).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(item => new { item.Status, item.ScheduledAt });
            entity.HasIndex(item => item.UserId);
        });

        modelBuilder.Entity<GroupCapturedMessage>(entity =>
        {
            entity.ToTable("GroupCapturedMessages");
            entity.Property(item => item.Channel).HasMaxLength(16).IsRequired();
            entity.Property(item => item.GroupKey).HasMaxLength(160).IsRequired();
            entity.Property(item => item.GroupName).HasMaxLength(256).IsRequired();
            entity.Property(item => item.ExternalMessageId).HasMaxLength(128);
            entity.Property(item => item.MediaUrl).HasMaxLength(500);
            entity.Property(item => item.ProductImageUrl).HasMaxLength(500);
            entity.Property(item => item.OriginalUrl).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.ProductName).HasMaxLength(255);
            entity.Property(item => item.PlatformName).HasMaxLength(64);
            entity.Property(item => item.OfferStatus).HasMaxLength(32).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.ReceivedAt });
            entity.HasIndex(item => new { item.UserId, item.GroupKey, item.ReceivedAt });
            entity.HasIndex(item => new { item.UserId, item.Channel, item.ExternalMessageId, item.OriginalUrl });
        });

        modelBuilder.Entity<WhatsAppGroup>(entity =>
        {
            entity.ToTable("WhatsAppGroups");
            entity.Property(item => item.Jid).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(256).IsRequired();
            entity.Property(item => item.IsActive).HasDefaultValue(true);
            entity.HasIndex(item => new { item.UserId, item.Jid }).IsUnique();
        });

        modelBuilder.Entity<WhatsAppBroadcastCampaign>(entity =>
        {
            entity.ToTable("WhatsAppBroadcastCampaigns");
            entity.Property(item => item.Title).HasMaxLength(128).IsRequired();
            entity.Property(item => item.MessageText).IsRequired();
            entity.Property(item => item.ImageUrl).HasMaxLength(512);
            entity.Property(item => item.TargetGroupJidsJson).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired();
            entity.Property(item => item.IntervalSeconds).HasDefaultValue(30);
            entity.HasIndex(item => new { item.Status, item.ScheduledAt });
            entity.HasIndex(item => item.UserId);
        });

        modelBuilder.Entity<LinkClickLog>(entity =>
        {
            entity.HasIndex(log => log.AffiliateLinkId);
            entity.HasIndex(log => log.ClickedAt);
            entity.HasIndex(log => log.CreatedAt);
            entity.HasIndex(log => log.TenantId);
            entity.Property(log => log.EventKind).HasMaxLength(32).IsRequired();
            entity.Property(log => log.Platform).HasMaxLength(32).IsRequired();
            entity.Property(log => log.ShopId).HasMaxLength(128).IsRequired();
            entity.HasOne(log => log.AffiliateLink)
                .WithMany()
                .HasForeignKey(log => log.AffiliateLinkId)
                .HasPrincipalKey(link => link.AffiliateLinkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantIdentifiers();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantIdentifiers();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Valores lidos a cada query. O EF religa estes membros ao AppDbContext da requisição
    /// (não captura o ICurrentTenantService do primeiro request que montou o modelo).
    /// </summary>
    internal bool TenantFilterBypass => _currentTenant.BypassTenantFilters;

    internal Guid? CurrentTenantId => _currentTenant.TenantId;

    internal string? CurrentShopId => _currentTenant.ShopId;

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var tenantOnly = typeof(AppDbContext).GetMethod(
            nameof(ApplyTenantOnlyFilter),
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tenantAndShop = typeof(AppDbContext).GetMethod(
            nameof(ApplyTenantAndShopFilter),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == typeof(Tenant) || !typeof(ITenantScopedEntity).IsAssignableFrom(clrType))
            {
                continue;
            }

            if (clrType == typeof(Product))
            {
                ApplyProductFilter(modelBuilder);
                continue;
            }

            if (typeof(IShopScopedEntity).IsAssignableFrom(clrType))
            {
                tenantAndShop.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
                continue;
            }

            tenantOnly.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantOnlyFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScopedEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            TenantFilterBypass
            || CurrentTenantId == null
            || CurrentTenantId == e.TenantId);
    }

    private void ApplyTenantAndShopFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScopedEntity, IShopScopedEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            (TenantFilterBypass || CurrentTenantId == null || CurrentTenantId == e.TenantId)
            && (CurrentShopId == null || e.ShopId == CurrentShopId));
    }

    private void ApplyProductFilter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasQueryFilter(e =>
            (TenantFilterBypass || CurrentTenantId == null || CurrentTenantId == e.TenantId)
            && (CurrentShopId == null || e.MarketplaceShopId == null || e.MarketplaceShopId == CurrentShopId));
    }

    private void ApplyTenantIdentifiers()
    {
        if (_currentTenant.BypassTenantFilters || _currentTenant.TenantId is null)
        {
            return;
        }

        var tenantId = _currentTenant.TenantId.Value;

        foreach (var entry in ChangeTracker.Entries<ITenantScopedEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.State == EntityState.Added || entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = tenantId;
                }
            }
        }
    }

    private void ConfigureSensitiveData(ModelBuilder modelBuilder)
    {
        var encryptedString = EncryptedStringConverter.Create(_encryptionService);
        var encryptedNullableString = EncryptedStringConverter.CreateNullable(_encryptionService);

        var userAccount = modelBuilder.Entity<UserAccount>();

        userAccount.Property(u => u.PasswordHash)
            .HasConversion(encryptedString);

        userAccount.Property(u => u.TikTokShopAccessToken)
            .HasConversion(encryptedNullableString);

        userAccount.Property(u => u.TikTokRefreshToken)
            .HasConversion(encryptedNullableString);

        var marketplaceToken = modelBuilder.Entity<MarketplaceToken>();

        marketplaceToken.Property(t => t.AccessToken)
            .HasConversion(encryptedString);

        marketplaceToken.Property(t => t.RefreshToken)
            .HasConversion(encryptedNullableString);

        var marketplaceAccount = modelBuilder.Entity<MarketplaceAccount>();

        marketplaceAccount.Property(a => a.AccessToken)
            .HasConversion(encryptedNullableString);

        marketplaceAccount.Property(a => a.AppSecret)
            .HasConversion(encryptedNullableString);

        marketplaceAccount.Property(a => a.RefreshToken)
            .HasConversion(encryptedNullableString);

        var integracaoLoja = modelBuilder.Entity<IntegracaoLoja>();

        integracaoLoja.Property(i => i.AccessToken)
            .HasConversion(encryptedNullableString);

        integracaoLoja.Property(i => i.RefreshToken)
            .HasConversion(encryptedNullableString);

        var user = modelBuilder.Entity<UserEntity>();

        user.Property(u => u.PasswordHash)
            .HasConversion(encryptedString);

        user.HasIndex(u => u.Email)
            .IsUnique();

        var whatsApp = modelBuilder.Entity<WhatsAppIntegration>();
        whatsApp.Property(item => item.Token).HasConversion(encryptedNullableString);
        whatsApp.Property(item => item.ApiKey).HasConversion(encryptedNullableString);
        whatsApp.Property(item => item.SessionData).HasConversion(encryptedNullableString);

        var telegram = modelBuilder.Entity<TelegramIntegration>();
        telegram.Property(item => item.Token).HasConversion(encryptedNullableString);
        telegram.Property(item => item.ApiKey).HasConversion(encryptedNullableString);
        telegram.Property(item => item.SessionData).HasConversion(encryptedNullableString);
    }
}
