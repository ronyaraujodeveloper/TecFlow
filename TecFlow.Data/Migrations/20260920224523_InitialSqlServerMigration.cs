using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServerMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StreetNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    City = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ShopId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MarketplaceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StockDeducted = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MarketplaceType = table.Column<int>(type: "int", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShortAffiliateLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AffiliateLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DestinationUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    PlatformType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IntegracaoLojaId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomNickname = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortAffiliateLinks", x => x.Id);
                    table.UniqueConstraint("AK_ShortAffiliateLinks_AffiliateLinkId", x => x.AffiliateLinkId);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Tenants] WHERE [Id] = 'a1000000-0000-4000-8000-000000000001')
                INSERT INTO [Tenants] ([Id], [Name], [IsActive], [CreatedAt])
                VALUES ('a1000000-0000-4000-8000-000000000001', N'TecFlow — Conta Padrão', 1, SYSUTCDATETIME());
                """);

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FreightAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketplaceOrderId = table.Column<int>(type: "int", nullable: false),
                    ExternalSkuId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SkuCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExternalProductId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceOrderLines_MarketplaceOrders_MarketplaceOrderId",
                        column: x => x.MarketplaceOrderId,
                        principalTable: "MarketplaceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LinkClickLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AffiliateLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ConvertedUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReferrerUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkClickLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkClickLog_ShortAffiliateLinks_AffiliateLinkId",
                        column: x => x.AffiliateLinkId,
                        principalTable: "ShortAffiliateLinks",
                        principalColumn: "AffiliateLinkId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MarketplaceType = table.Column<int>(type: "int", nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ShopId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ShopName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Cnpj = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            EnsureLegacyColumn(migrationBuilder, "Usuarios", "TenantId", "uniqueidentifier NOT NULL CONSTRAINT [DF_Usuarios_TenantId] DEFAULT 'a1000000-0000-4000-8000-000000000001'");
            EnsureLegacyColumn(migrationBuilder, "Usuarios", "TelefoneWhatsApp", "nvarchar(max) NULL");
            EnsureForeignKey(migrationBuilder, "FK_Usuarios_Tenants_TenantId", "Usuarios", "TenantId", "Tenants", "Id");

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            EnsureLegacyColumn(migrationBuilder, "Conteudos", "TenantId", "uniqueidentifier NOT NULL CONSTRAINT [DF_Conteudos_TenantId] DEFAULT 'a1000000-0000-4000-8000-000000000001'");

            EnsureLegacyColumn(migrationBuilder, "Conversaos", "TenantId", "uniqueidentifier NOT NULL CONSTRAINT [DF_Conversaos_TenantId] DEFAULT 'a1000000-0000-4000-8000-000000000001'");

            migrationBuilder.CreateTable(
                name: "IntegracaoLoja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformType = table.Column<int>(type: "int", nullable: false),
                    ShopId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegracaoLoja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegracaoLoja_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegracaoLoja_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            EnsureLegacyColumn(migrationBuilder, "Produtos", "TenantId", "uniqueidentifier NOT NULL CONSTRAINT [DF_Produtos_TenantId] DEFAULT 'a1000000-0000-4000-8000-000000000001'");
            EnsureLegacyColumn(migrationBuilder, "Produtos", "IdExterno", "nvarchar(128) NULL");
            EnsureLegacyColumn(migrationBuilder, "Produtos", "SkuCodigo", "nvarchar(128) NULL");
            EnsureLegacyColumn(migrationBuilder, "Produtos", "MarketplaceOrigem", "int NULL");
            EnsureLegacyColumn(migrationBuilder, "Produtos", "MarketplaceShopId", "nvarchar(128) NULL");

            migrationBuilder.CreateTable(
                name: "ProdutosPropagandaGlobal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GlobalProductUid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeAmigavel = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoriaGlobal = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UrlImagemPrincipal = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PrecoMedio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosPropagandaGlobal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosPropagandaGlobal_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            EnsureLegacyColumn(migrationBuilder, "Campanhas", "TenantId", "uniqueidentifier NOT NULL CONSTRAINT [DF_Campanhas_TenantId] DEFAULT 'a1000000-0000-4000-8000-000000000001'");
            EnsureLegacyColumn(migrationBuilder, "Campanhas", "LojaId", "int NULL");
            EnsureForeignKey(migrationBuilder, "FK_Campanhas_IntegracaoLoja_LojaId", "Campanhas", "LojaId", "IntegracaoLoja", "Id");

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    PhysicalQuantity = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    MinimumStock = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Produtos_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Produtos_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_SalesOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceAffiliateLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoGlobalId = table.Column<int>(type: "int", nullable: false),
                    Marketplace = table.Column<int>(type: "int", nullable: false),
                    UrlProdutoOriginal = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IdProdutoPlataforma = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LinkAfiliadoGerado = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ParametrosRastreio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceAffiliateLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceAffiliateLinks_ProdutosPropagandaGlobal_ProdutoGlobalId",
                        column: x => x.ProdutoGlobalId,
                        principalTable: "ProdutosPropagandaGlobal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            EnsureLegacyColumn(migrationBuilder, "Afiliados", "TenantId", "uniqueidentifier NOT NULL CONSTRAINT [DF_Afiliados_TenantId] DEFAULT 'a1000000-0000-4000-8000-000000000001'");

            EnsureLegacyColumn(migrationBuilder, "Metricas", "TenantId", "uniqueidentifier NOT NULL CONSTRAINT [DF_Metricas_TenantId] DEFAULT 'a1000000-0000-4000-8000-000000000001'");
            EnsureLegacyColumn(migrationBuilder, "Metricas", "LojaId", "int NULL");
            EnsureForeignKey(migrationBuilder, "FK_Metricas_IntegracaoLoja_LojaId", "Metricas", "LojaId", "IntegracaoLoja", "Id");

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_LojaId",
                table: "Campanhas",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_DocumentNumber",
                table: "Customers",
                columns: new[] { "TenantId", "DocumentNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegracaoLoja_TenantId",
                table: "IntegracaoLoja",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegracaoLoja_UserId_ShopId_PlatformType",
                table: "IntegracaoLoja",
                columns: new[] { "UserId", "ShopId", "PlatformType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_TenantId_ProductId",
                table: "Inventories",
                columns: new[] { "TenantId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_InventoryId",
                table: "InventoryMovements",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_SalesOrderId_MovementType",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "SalesOrderId", "MovementType" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_AffiliateLinkId",
                table: "LinkClickLog",
                column: "AffiliateLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_ClickedAt",
                table: "LinkClickLog",
                column: "ClickedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_CreatedAt",
                table: "LinkClickLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_TenantId",
                table: "LinkClickLog",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAccounts_TenantId_ShopId_MarketplaceType",
                table: "MarketplaceAccounts",
                columns: new[] { "TenantId", "ShopId", "MarketplaceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAffiliateLinks_ProdutoGlobalId_Marketplace",
                table: "MarketplaceAffiliateLinks",
                columns: new[] { "ProdutoGlobalId", "Marketplace" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrderLines_MarketplaceOrderId",
                table: "MarketplaceOrderLines",
                column: "MarketplaceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrders_TenantId_ExternalOrderId_MarketplaceType_ShopId",
                table: "MarketplaceOrders",
                columns: new[] { "TenantId", "ExternalOrderId", "MarketplaceType", "ShopId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceTokens_TenantId_ShopId_MarketplaceType",
                table: "MarketplaceTokens",
                columns: new[] { "TenantId", "ShopId", "MarketplaceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Metricas_LojaId",
                table: "Metricas",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_TenantId_SkuCodigo_MarketplaceOrigem_MarketplaceShopId",
                table: "Produtos",
                columns: new[] { "TenantId", "SkuCodigo", "MarketplaceOrigem", "MarketplaceShopId" },
                filter: "[SkuCodigo] IS NOT NULL AND [MarketplaceOrigem] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPropagandaGlobal_OwnerId",
                table: "ProdutosPropagandaGlobal",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPropagandaGlobal_TenantId_GlobalProductUid",
                table: "ProdutosPropagandaGlobal",
                columns: new[] { "TenantId", "GlobalProductUid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_OrderId",
                table: "SalesOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_ProductId",
                table: "SalesOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantId_OrderNumber",
                table: "SalesOrders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_AffiliateLinkId",
                table: "ShortAffiliateLinks",
                column: "AffiliateLinkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_ShortCode",
                table: "ShortAffiliateLinks",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_UserId_CreatedAt",
                table: "ShortAffiliateLinks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_TenantId_OwnerId_Token",
                table: "UserDeviceTokens",
                columns: new[] { "TenantId", "OwnerId", "Token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Afiliados");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "Conversaos");

            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "LinkClickLog");

            migrationBuilder.DropTable(
                name: "MarketplaceAccounts");

            migrationBuilder.DropTable(
                name: "MarketplaceAffiliateLinks");

            migrationBuilder.DropTable(
                name: "MarketplaceOrderLines");

            migrationBuilder.DropTable(
                name: "MarketplaceTokens");

            migrationBuilder.DropTable(
                name: "Metricas");

            migrationBuilder.DropTable(
                name: "SalesOrderItems");

            migrationBuilder.DropTable(
                name: "UserDeviceTokens");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "Conteudos");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "ShortAffiliateLinks");

            migrationBuilder.DropTable(
                name: "ProdutosPropagandaGlobal");

            migrationBuilder.DropTable(
                name: "MarketplaceOrders");

            migrationBuilder.DropTable(
                name: "Campanhas");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "IntegracaoLoja");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Tenants");
        }

        private static void EnsureLegacyColumn(
            MigrationBuilder migrationBuilder,
            string table,
            string column,
            string sqlType)
        {
            migrationBuilder.Sql($"""
                IF COL_LENGTH('dbo.{table}', '{column}') IS NULL
                    ALTER TABLE [{table}] ADD [{column}] {sqlType};
                """);
        }

        private static void EnsureForeignKey(
            MigrationBuilder migrationBuilder,
            string fkName,
            string table,
            string column,
            string principalTable,
            string principalColumn)
        {
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'dbo.{fkName}', N'F') IS NULL
                   AND COL_LENGTH('dbo.{table}', '{column}') IS NOT NULL
                   AND OBJECT_ID(N'dbo.{principalTable}', N'U') IS NOT NULL
                    ALTER TABLE [{table}] WITH NOCHECK
                    ADD CONSTRAINT [{fkName}] FOREIGN KEY ([{column}])
                    REFERENCES [{principalTable}] ([{principalColumn}]);
                """);
        }
    }
}

