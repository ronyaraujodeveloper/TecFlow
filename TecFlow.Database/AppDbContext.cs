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

    /// <summary>Usuários oficiais do ecossistema TecFlow (tabela users).</summary>
    public DbSet<UserEntity> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSensitiveData(modelBuilder);
        modelBuilder.ApplyTenantQueryFilters(_currentTenant);

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Name);

        modelBuilder.Entity<MarketplaceAccount>()
            .HasIndex(a => new { a.TenantId, a.ShopId, a.MarketplaceType })
            .IsUnique();

        modelBuilder.Entity<MarketplaceAccount>()
            .HasOne(a => a.Tenant)
            .WithMany(t => t.MarketplaceAccounts)
            .HasForeignKey(a => a.TenantId);

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
            entity.HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.Tenant)
                .WithMany()
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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
            .HasFilter("\"SkuCodigo\" IS NOT NULL AND \"MarketplaceOrigem\" IS NOT NULL");

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
            .HasConversion(encryptedString);

        marketplaceAccount.Property(a => a.RefreshToken)
            .HasConversion(encryptedNullableString);

        var integracaoLoja = modelBuilder.Entity<IntegracaoLoja>();

        integracaoLoja.Property(i => i.AccessToken)
            .HasConversion(encryptedString);

        integracaoLoja.Property(i => i.RefreshToken)
            .HasConversion(encryptedNullableString);

        var user = modelBuilder.Entity<UserEntity>();

        user.Property(u => u.PasswordHash)
            .HasConversion(encryptedString);

        user.HasIndex(u => u.Email)
            .IsUnique();
    }
}
