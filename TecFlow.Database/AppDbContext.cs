using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Entities;
using TecFlow.Database.Entity;
using TecFlow.Util.Security;
using TecFlow.Util.Security.EntityFramework;

namespace TecFlow.Database;

public class AppDbContext : DbContext
{
    private readonly IEncryptionService _encryptionService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IEncryptionService encryptionService)
        : base(options)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public DbSet<Affiliate> Affiliates { get; set; } = null!;
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<Metric> Metrics { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Content> Contents { get; set; } = null!;
    public DbSet<Conversion> Conversions { get; set; } = null!;
    public DbSet<UserAccount> UserAccounts { get; set; } = null!;

    /// <summary>Usuários oficiais do ecossistema TecFlow (tabela users).</summary>
    public DbSet<UserEntity> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSensitiveData(modelBuilder);

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

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

        var user = modelBuilder.Entity<UserEntity>();

        user.Property(u => u.PasswordHash)
            .HasConversion(encryptedString);

        user.HasIndex(u => u.Email)
            .IsUnique();
    }
}
