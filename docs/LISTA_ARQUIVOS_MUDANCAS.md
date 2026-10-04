# 📋 LISTA EXECUTIVA: ARQUIVOS A MOVER/CRIAR/DELETAR

**Última varredura:** 4 de junho de 2026 (Fase 7.1 — Multi-Tenant)  
**Workspace:** `c:\Programacao\Tecso.AutomacaoCusor` (pasta ainda com prefixo *Tecso*; projetos já renomeados para *TecFlow*)  
**Solution:** `TecFlow.sln` — 12 projetos `TecFlow.*` + `Tecso.LerArquivos` externo (Portal e Dashboard **removidos**)

> **Nota de varredura:** Esta lista deve ser usada para eliminar **resíduos das pastas antigas** (*Tecso* / camadas pré-refatoração) que ainda geram conflitos de compilação — por exemplo, cópias de `ExceptionMiddleware` na API, interfaces fantasma em `Infrastructure.Services/Interfaces`, artefatos `bin/`/`obj/` versionados e namespaces legados (`TecFlow.API.Middlewares` em arquivos do Core). Priorize itens da seção **🚨 Conflitos** antes de novas features.

**Navegação:** [« Índice Completo](./INDICE_COMPLETO.md) · [README principal](../README.md)

Use esta lista como painel de controle para garantir que nenhuma classe antiga ficou duplicada e que todos os namespaces estejam nos projetos corretos.

---

## 🚨 1. Conflitos e Arquivos Duplicados (Urgente)

### Resolvido recentemente

- [x] **ExceptionMiddleware.cs** — Havia cópia em `TecFlow.API/Middleware/` e implementação em `TecFlow.Core/Exceptions/`. **Ação concluída:** middleware legado removido do Core; `TecFlow.API/Middlewares/ExceptionHandlingMiddleware.cs` retorna `ProblemDetails` JSON.

### Interfaces duplicadas (compilam, mas confundem DI e manutenção)

- [ ] **ITikTokShopApi.cs** — `TecFlow.Business/Interfaces/Services/` (canônico, em uso) e `TecFlow.Infrastructure.Services/Interfaces/` (cópia legada, **excluída do compile** no `.csproj`). (Ação: deletar o arquivo em `Infrastructure.Services/Interfaces` e remover `<Compile Remove>` do csproj).

- [ ] **IShopeeApi.cs** — `TecFlow.Business/Interfaces/Services/` (canônico) e `TecFlow.Infrastructure.Services/Interfaces/` (legado, **Compile Remove**). (Ação: deletar cópia legada + limpar csproj).

- [ ] **ITikTokAdsApiService.cs** — `TecFlow.Business/Interfaces/Services/` (canônico) e `TecFlow.Infrastructure.Services/Interfaces/` (namespace `TecFlow.Core.Interfaces.Services`, **Compile Remove**). (Ação: deletar cópia legada; manter interface só em Business até mover para Core se for o alvo arquitetural).

- [ ] **ValidationHelper.cs** — `TecFlow.Business/Service/ValidationHelper.cs` e `TecFlow.Util/Validation/ValidationHelper.cs`. (Ação: unificar em `TecFlow.Util`; Business referencia Util ou deleta a cópia local.)

### Mesmo nome de classe, hosts diferentes (não é CS0436, mas exige disciplina)

- [ ] **CampaignsController.cs** — `TecFlow.API/Controllers/` e `TecFlow.Orquestrador/Controllers/`. (Ação: manter ambos se forem APIs distintas; documentar rotas; evitar lógica duplicada — extrair para `TecFlow.Business`).

- [ ] **MetricsController.cs** — API e Orquestrador. (Ação: idem.)

- [ ] **DashboardController.cs** — API e Orquestrador. (Ação: idem.)

- [ ] **UserAccountsController.cs** — API e Orquestrador. (Ação: idem.)

### DTOs espelhados (Portal vs Business)

- [x] **MetricDto.cs**, **CampaignDto.cs**, **DashboardSummaryDto.cs** — DTOs locais removidos de `TecFlow.Portal/Models/Responses/` e `TecFlow.WebUi/Models/Responses/`; UI consome `TecFlow.Business/Dto/` (`*ResponseDto`, `DashboardSummaryDto`) e entidades `TecFlow.Core.Entities` (`Campaign`, `Metric`).

### Migração Portal → WebUi (Fase 3)

- [x] **TecFlow.WebUi/** — projeto Blazor canônico na solution com referência a `TecFlow.Business`.
- [x] **DashboardApiService.cs** (WebUi) — deserializa `CampaignResponseDto` / `MetricResponseDto` e expõe `DataList` para os widgets.
- [x] **CampaignExtensions.cs** — helper `IsActive()` para entidade `Campaign`.
- [x] DTOs locais removidos: `CampaignDto.cs`, `MetricDto.cs`, `DashboardSummaryDto.cs` (WebUi).
- [x] **TecFlow.Portal/** — removido da solution e excluído do disco (substituído por WebUi).
- [x] **TecFlow.Dashboard/** — removido da solution e excluído do disco (scaffold MVC obsoleto).

### WebUi — arquitetura Filter / Dto / ResponseDto (Fase 3)

- [x] **TecFlow.WebUi.csproj** — referência explícita a `TecFlow.Database` (tipos `*Filter`).
- [x] **Extensions/FilterQueryStringExtensions.cs** — serializa `*Filter` em query string para GET.
- [x] **Extensions/ResponseDtoExtensions.cs** — valida `Status`/`Descricao` dos envelopes na UI.
- [x] **Services/Http/HttpService.cs** — `GetAsync(url, filter)`, `PutAsync` para POST/PUT com Dto.
- [x] **Services/Dashboard/DashboardApiService.cs** — `Get*ByFilterAsync(CampaignFilter|MetricFilter)`, `Create*Async(CampaignDto|MetricDto)`.
- [x] **Components/Dashboard/CampaignFilterForm.razor** — data binding → `CampaignFilter`.
- [x] **Components/Dashboard/MetricFilterForm.razor** — data binding → `MetricFilter`.
- [x] **Components/Dashboard/CampaignCreateForm.razor** — formulário POST com `CampaignDto`.
- [x] **Components/Dashboard/CampaignsWidget.razor** / **MetricsWidget.razor** — leem `*ResponseDto.DataList` diretamente.
- [x] **Components/Pages/Dashboard.razor** — orquestra filtros, listagens e criação padronizados.

### Fase 6.4 — Observabilidade e telemetria (jun/2026)

- [x] **TecFlow.Observability/** — `AddTecFlowTelemetry`, `UseTecFlowTelemetry`, `TecFlowBusinessMetrics`, `TelemetryRecentErrorRecorder`, `TelemetryErrorRecordingMiddleware`.
- [x] **Pacotes:** OpenTelemetry (Hosting, AspNetCore, Http, Runtime), OTLP, Console, Prometheus.AspNetCore, Serilog.Sinks.Seq.
- [x] **TecFlow.API/Program.cs**, **TecFlow.Worker/Program.cs**, **TecFlow.Orquestrador/Program.cs** — telemetria ativada por host.
- [x] **appsettings.json** (API, Worker, Orquestrador) — seção `Telemetry`.
- [x] **TecFlow.Infrastructure.Services/Health/PlatformHealthService.cs** — health checks DB/RabbitMQ/Shopee/TikTok.
- [x] **TecFlow.Orquestrador/Controllers/HealthDashboardController.cs** — `GET /api/saude/dashboard`.
- [x] **TecFlow.SharedUi/Components/Pages/PainelSaude.razor**, **Components/Health/HealthStatusCard.razor**.
- [x] Instrumentação: **SocialMediaCommentConsumer**, **AffiliateAnalyticsService**.

### Fase 7.3 — Controle avançado de estoque físico (jun/2026)

- [x] **TecFlow.Core/Enums/InventoryMovementType.cs** — EntradaPorCompra, SaidaPorVenda, AjusteManual, Reserva, CancelamentoReserva.
- [x] **TecFlow.Core/Entities/Inventory.cs** — PhysicalQuantity, ReservedQuantity, AvailableQuantity (calculado), MinimumStock.
- [x] **TecFlow.Core/Entities/InventoryMovement.cs** — kardex com SalesOrderId.
- [x] **TecFlow.Business/Interfaces/Inventory/IInventoryService.cs** — ReserveStock, ConfirmStockDebit, ReleaseStockReservation.
- [x] **TecFlow.Business/Interfaces/Inventory/IInventoryAlertHook.cs** — gancho para push (Fase 4).
- [x] **TecFlow.Business/Domain/Inventory/InsufficientStockException.cs**.
- [x] **TecFlow.Infrastructure.Services/Stock/InventoryService.cs** — transações Serializable + retry de concorrência.
- [x] **TecFlow.Infrastructure.Services/Stock/LoggingInventoryAlertHook.cs** — log estruturado de estoque mínimo.
- [x] Integração: **SalesOrderService** (reserva na criação; débito em Pago; liberação em Cancelado), **InvoiceOrchestrator** (débito idempotente).
- [x] **TecFlow.API/Controllers/InventoryController.cs** — `api/estoque`.
- [x] **TecFlow.Infrastructure/Migrations/20260604141216_AddPhysicalInventory.cs**.

### Fase 7.2 — Core de Vendas, Faturamento e ERP Local (jun/2026)

- [x] **TecFlow.Core/Enums/OrderStatus.cs** — Pendente, Pago, Faturado, Enviado, Concluido, Cancelado.
- [x] **TecFlow.Core/Entities/Customer.cs** — cliente com endereço completo e `TenantId`.
- [x] **TecFlow.Core/Entities/SalesOrder.cs** — pedido de venda (`OrderNumber`, totais, `ShopId`, status).
- [x] **TecFlow.Core/Entities/SalesOrderItem.cs** — itens do pedido.
- [x] **TecFlow.Business/Domain/Sales/OrderStateMachine.cs** — transições rígidas de estado.
- [x] **TecFlow.Business/Interfaces/Sales/IInvoiceOrchestrator.cs** — `PrepareInvoiceAsync` + payload NF-e mockado.
- [x] **TecFlow.Infrastructure.Services/Sales/InvoiceOrchestrator.cs**, **SalesOrderService.cs**.
- [x] **TecFlow.API/Controllers/CustomersController.cs** — `api/vendas/clientes`.
- [x] **TecFlow.API/Controllers/SalesOrdersController.cs** — `api/vendas/pedidos`, `PUT .../status`, `POST .../faturar`.
- [x] **TecFlow.Database/Filter/** — `CustomerFilter`, `SalesOrderFilter`.
- [x] **TecFlow.Business/Dto/** — `CustomerDto`, `SalesOrderDto`, `InvoicePayloadDto`, envelopes `*ResponseDto`.
- [x] **TecFlow.Infrastructure/Migrations/20260604140643_AddSalesOrderCore.cs**.
- [x] **TecFlow.Tests/Unit/Sales/OrderStateMachineTests.cs**.

### Fase 7.1 — Multi-Tenant / Multi-Conta Marketplace (jun/2026)

- [x] **TecFlow.Core/Entities/Tenant.cs** — inquilino corporativo (assinante SaaS).
- [x] **TecFlow.Core/Entities/MarketplaceAccount.cs** — vínculo Tenant + ShopId + tokens; `TrackingId`/`ShopId`/`AppKey`/`AppSecret` anuláveis para registros antigos.
- [x] **TecFlow.Business/Mappings/MarketplaceAccountMapper.cs** — projeção nula-segura para `MarketplaceAccountDto` e `ConvertLinkResponseDto`.
- [x] **TecFlow.Infrastructure.Services/Integrations/IntegracaoLojaService.cs** — se o UserId do JWT não existir em `Usuarios`, usa o primeiro usuário do SQL Server (ou cria o demo de homologação) antes de gravar `MarketplaceAccount.UserId`.
- [x] **TecFlow.Infrastructure.Services/Integrations/MarketplaceAccountService.cs** — `PrepareForPersistAsync` grava `TenantId` de `dbo.Tenants` e `UserId` de `dbo.Usuarios`.
- [x] **TecFlow.Tests/Unit/MultiTenancy/TenantProvisioningServiceTests.cs** — cria `Tenant Principal` quando `Tenants` está vazia.
- [x] **TecFlow.Data/Migrations/20260922233000_AddMarketplaceAccountOptionalCredentials.cs** — colunas opcionais `TrackingId`/`AppKey`/`AppSecret`.
- [x] **TecFlow.Data/Migrations/20260922250000_AddShortAffiliateLinkAffiliateUrlAndAccount.cs** — `AffiliateUrl` e `MarketplaceAccountId` em `ShortAffiliateLinks` (SQL Server).
- [x] **TecFlow.Infrastructure.Services/ShortLinks/ShortLinkService.cs** — persiste `ShortAffiliateLink` com `SaveChangesAsync()` no SQLEXPRESS.
- [x] **TecFlow.Tests/Unit/LinkStrategies/ShortLinkPersistenceTests.cs** — garante `SaveChanges` de `OriginalUrl`/`AffiliateUrl`/`Code`/`Platform`/`MarketplaceAccountId`.
- [x] **appsettings.Homologacao.json** — `Database:Provider=SqlServer` / `AutomacaoSociais` (mesmo banco do `appsettings.json`).
- [x] **TecFlow.Infrastructure/Migrations/20260922240000_AddMarketplaceAccountOptionalCredentialsPg.cs** — mesmas colunas no PostgreSQL do IIS (`Homologacao`) para evitar Npgsql `42703`.
- [x] **docs/EXECUCAO_HOMOLOGACAO.md** / **Publicar-Homologacao.ps1** — `dotnet ef database update` no SQL Server (`TecFlow.Data`) para Development e Homologacao IIS.
- [x] **TecFlow.Infrastructure/Migrations/20260920215452_AddMarketplaceAccountsTable.cs** — colunas `UserId`, `FriendlyName` e `IsActive` em `MarketplaceAccounts`.
- [x] **TecFlow.Data/** — assembly de migrations SQL Server (`SqlServerMigrations`, `20260920224523_InitialSqlServerMigration`).
- [x] **TecFlow.Database/Data/RelationalDatabaseOptions.cs** — `UseSqlServer` / `UseNpgsql` conforme `Database:Provider`.
- [x] **TecFlow.Core/Abstractions/ITenantScopedEntity.cs**, **IShopScopedEntity.cs** — contratos de isolamento.
- [x] **TecFlow.Core/Security/TecFlowClaimTypes.cs** — claims `tenant_id`, `shop_id` (movido de SharedUi).
- [x] **TenantId** em: `UserAccount`, `Product`, `Campaign`, `Affiliate`, `Content`, `Conversion`, `Metric`, `MarketplaceToken`, `MarketplaceOrder`, `MarketplaceOrderLine`, `GlobalAdvertisingProduct`, `MarketplaceAffiliateLink`, `UserDeviceToken`.
- [x] **TecFlow.Database/MultiTenancy/** — `ICurrentTenantService`, `NullCurrentTenantService`, `TenantQueryFilterExtensions`, `TenantDbSetExtensions`.
- [x] **TecFlow.Database/AppDbContext.cs** — filtros globais, `SaveChanges` com `TenantId`, criptografia em `MarketplaceAccount`.
- [x] **TecFlow.Infrastructure/Security/CurrentTenantService.cs** — JWT + header `X-TecFlow-Shop-Id`.
- [x] **TecFlow.Infrastructure/Security/JwtTokenService.cs** — claim `TecFlow:tenant_id`; emite `aud=TecFlowClient`.
- [x] **TecFlow.Infrastructure.Services/Tenancy/TenantProvisioningService.cs** — `EnsurePersistedTenantAsync` cria `Tenant Principal` se `dbo.Tenants` estiver vazia; `EnsureTenantForUserAsync` corrige `Usuarios.TenantId` órfão.
- [x] **TecFlow.Infrastructure.Services/Repositories/MarketplaceAccountRepository.cs** — listagem consolidada / por loja.
- [x] Repositórios adaptados: **ProductRepository**, **MarketplaceTokenRepository**, **MarketplaceOrderRepository** (`ListConsolidated*`, `ListForShop*`).
- [x] **TecFlow.Infrastructure.Services/Integrations/Auth/MarketplaceAuthService.cs** — persiste `MarketplaceAccount` no OAuth.
- [x] **TecFlow.Infrastructure/Migrations/20260604135726_AddMultiTenantArchitecture.cs** — schema + tenant padrão para dados legados.
- [x] **TecFlow.Database/Filter/** — `TenantFilter`, `MarketplaceAccountFilter`; **ProductFilter** + `ShopId`/`TenantId`.
- [x] **TecFlow.Business/Dto/** — `TenantDto`, `TenantResponseDto`, `MarketplaceAccountDto`, `MarketplaceAccountResponseDto`.
- [x] **TecFlow.API/Controllers/MarketplaceAuthController.cs** — callback OAuth exige `[Authorize]`.
- [x] **TecFlow.API/Controllers/AuthController.cs** — `GET /api/auth/status` e `GET /api/auth/providers/status` (alias); login, register, vincular/desvincular provedores.
- [x] **TecFlow.SharedUi/Services/Auth/AccountSecurityApiService.cs** — consome `GET api/auth/status` via `OrquestradorApi:BaseUrl` (`https://localhost:7001/` em dev Kestrel; `http://localhost:5001/` no IIS Homologacao).
- [x] **TecFlow.WebUi/web.config** — `stdoutLogEnabled="true"`, `stdoutLogFile=".\logs\stdout"` (diagnóstico IIS).
- [x] **TecFlow.API/web.config** — `stdoutLogEnabled="true"`, `requestTimeout="20:00:00"` para o UserBot contínuo no IIS.
- [x] **Configurar-Logs-IIS.ps1** — cria `logs\` em `C:\inetpub\tecflow\api` e `webui`; `FullControl` para `IIS_IUSRS`, `DefaultAppPool` e app pools dedicados.
- [x] **Liberar-Logs-WebUi.ps1** — cria `C:\inetpub\tecflow\webui\logs\` e concede `FullControl` a `IIS_IUSRS` / app pools.
- [x] **TecFlow.API/Program.cs** — `AddJwtBearer` com `ValidAudience`/`ValidAudiences` = `TecFlowClient`; `HostOptions` ignora falha de BackgroundService.
- [x] **TecFlow.API/appsettings.json**, **appsettings.Homologacao.json** — seção `Serilog.MinimumLevel` (Information em homolog).
- [x] **TecFlow.WebUi/Program.cs** — Serilog Console + `logs/app-.txt`, `UseSerilogRequestLogging`, `Log.CloseAndFlush`.
- [x] **TecFlow.WebUi/Logging/BlazorCircuitLoggingHandler.cs** — log de abertura/fechamento/reconexão de circuitos SignalR.
- [x] **TecFlow.API/Middlewares/ExceptionHandlingMiddleware.cs** — captura 500, log contextual seguro, `application/problem+json`.
- [x] **TecFlow.API/Controllers/AuthController.cs** — `LogError` em login/registro inesperados.
- [x] **TecFlow.Infrastructure.Services/Security/PlatformAuthService.cs** — `LogError` em falhas não previstas de login.
- [x] **TecFlow.SharedUi/Services/Auth/AccountSecurityApiService.cs**, **UserRegistrationApiService.cs** — `ILogger` nos catches de auth.
- [x] **TecFlow.WebUi/Extensions/AuthEndpointExtensions.cs** — `LogError` no callback OAuth.

### Fase 6.3 — Produtos globais de propaganda (jun/2026)

- [x] **TecFlow.Core/Entities/GlobalAdvertisingProduct.cs** — FriendlyName, GlobalCategory, MainImageUrl, AveragePrice, `GlobalProductUid`.
- [x] **TecFlow.Core/Entities/MarketplaceAffiliateLink.cs** — vínculos Shopee/TikTok com links gerados e tracking JSON.
- [x] **TecFlow.Infrastructure/Migrations/*AddGlobalAdvertisingProducts*** — tabelas `ProdutosPropagandaGlobal`, `MarketplaceAffiliateLinks`.
- [x] **TecFlow.Database/AppDbContext.cs** — DbSets + índices/relacionamentos.
- [x] **TecFlow.Business/Dto/** — `GlobalAdvertisingProductDto`, `MarketplaceAffiliateLinkDto`, `OptimizedPostPayloadDto`, `GlobalAdvertisingProductResponseDto`.
- [x] **TecFlow.Business/Interfaces/Services/IAdvertisingProductService.cs**.
- [x] **TecFlow.Infrastructure.Services/Advertising/AdvertisingProductService.cs**.
- [x] **TecFlow.Orquestrador/Controllers/AdvertisingProductsController.cs**.
- [x] **TecFlow.SharedUi/Components/Pages/ProdutosPropaganda.razor** — formulário 1 col (mobile) / 2 cols (desktop), cards com copiar link.
- [x] **TecFlow.SharedUi/wwwroot/tecflow-clipboard.js** — área de transferência Web/Mobile.
- [x] **TecFlow.Tests/Unit/Advertising/AdvertisingProductServiceTests.cs**.

### Fase 6.2 — Painel de conciliação financeira de afiliado (jun/2026)

- [x] **TecFlow.Business/Dto/AffiliatePerformanceDto.cs** — cliques, conversões, CVR, comissão estimada/paga, retidos.
- [x] **TecFlow.Business/Dto/CommissionDiscrepancyReportDto.cs** — divergências marketplace vs. TecFlow.
- [x] **TecFlow.Business/Dto/MarketplaceCommissionLineDto.cs**, **AffiliateReconciliationResponseDto.cs**.
- [x] **TecFlow.Database/Filter/AffiliateReconciliationFilter.cs** — período, affiliateId, paginação.
- [x] **TecFlow.Business/Interfaces/Services/IAffiliateAnalyticsService.cs**.
- [x] **TecFlow.Infrastructure.Services/Analytics/AffiliateAnalyticsService.cs** — fetch APIs + reconciliação.
- [x] **TecFlow.Orquestrador/Controllers/AffiliateAnalyticsController.cs** — `api/afiliados/analytics/conciliacao`.
- [x] **TecFlow.SharedUi/Components/Pages/ConciliacaoFinanceira.razor** — painel KPI + lista responsiva.
- [x] **TecFlow.SharedUi/Components/Pages/ConciliacaoDetalhes.razor** — detalhe de linha divergente.
- [x] **TecFlow.SharedUi/Services/Analytics/** — `IAffiliateAnalyticsApiService`, `AffiliateAnalyticsApiService`.
- [x] **TecFlow.SharedUi/wwwroot/app.css** — `.conciliation-kpi-grid`, `.data-list-card--danger`.
- [x] **TecFlow.Tests/Unit/Analytics/AffiliateAnalyticsServiceTests.cs**.

### Fase 6.1 — Filas RabbitMQ e automação de engajamento (jun/2026)

- [x] **TecFlow.Business/Messaging/** — `SocialMediaCommentReceivedEvent`, `AffiliateLinkDeliveryRequestedEvent`, `RabbitMqOptions`, `EngagementKeywordTriageOptions`.
- [x] **TecFlow.Business/Interfaces/Messaging/** — `IEngagementEventPublisher`, `ICommentKeywordTriageService`, `IAffiliateLinkDeliveryNotifier`.
- [x] **TecFlow.Business/Dto/SocialMediaCommentWebhookRequest.cs** — payload do webhook.
- [x] **TecFlow.Infrastructure.Services/Messaging/** — `EngagementMessagingRegistrationExtensions`, `MassTransitEngagementEventPublisher`, `CommentKeywordTriageService`, `AffiliateLinkDeliveryNotifier`.
- [x] **TecFlow.Infrastructure.Services/Messaging/Consumers/SocialMediaCommentConsumer.cs** — triagem e disparo simulado de link.
- [x] **TecFlow.API/Controllers/SocialMediaWebhookController.cs** — `POST /api/webhooks/social-media/comments` → 202 + publicação na fila.
- [x] **TecFlow.API/Program.cs**, **TecFlow.Worker/Program.cs**, **TecFlow.Orquestrador/Program.cs** — DI MassTransit (Publisher / Consumer).
- [x] **appsettings.json** (API, Worker, Orquestrador) — seções `RabbitMq` e `EngagementTriage`.
- [x] **TecFlow.Orquestrador/docker-compose.yml** — serviço `rabbitmq` (5672 / 15672).
- [x] **TecFlow.Tests/Unit/Messaging/CommentKeywordTriageServiceTests.cs**, **SocialMediaWebhookControllerTests.cs**.

### Fase 5.0 — Domínio afiliados e contratos de orquestração (jun/2026)

- [x] **TecFlow.Core/Enums/SocialMediaType.cs** — Instagram, TikTok, YouTube, Facebook.
- [x] **TecFlow.Core/Enums/EngagementStatus.cs** — Pendente, Processado, LinkEnviado, Falhou.
- [x] **TecFlow.Core/Enums/CommissionStatus.cs** — Rastreado, Retido, Pago, Cancelado.
- [x] **TecFlow.Core/Entities/AffiliateLink.cs** — produto de divulgação, `OriginalUrl`, `ShopeeTrackedUrl`, `TikTokShopTrackedUrl`, IDs externos e `TrackingCode`.
- [x] **TecFlow.Business/Domain/Engagement/SocialEngagementEvent.cs** — evento de comentário/mensagem para triagem.
- [x] **TecFlow.Business/Domain/Engagement/EngagementOrchestrationResult.cs** — resultado do disparo de link.
- [x] **TecFlow.Business/Domain/Commission/CommissionAuditLine.cs** — linha de auditoria marketplace vs. local.
- [x] **TecFlow.Business/Domain/Commission/CommissionConciliationResult.cs** — envelope da conciliação.
- [x] **TecFlow.Business/Interfaces/Orchestration/IEngagementOrchestrator.cs** — contrato Orquestrador (engajamento).
- [x] **TecFlow.Business/Interfaces/Orchestration/ICommissionConciliator.cs** — contrato Orquestrador (comissões).
- [x] **TecFlow.Business/Dto/AffiliateLinkDto.cs**, **AffiliateLinkResponseDto.cs** — contratos API/UI futuros.
- [x] **TecFlow.Database** — `MarketplaceAffiliateLink` persistido (Fase 6.3); `AffiliateLink` legado permanece conceitual.
- [ ] **TecFlow.Orquestrador** — implementações concretas de `IEngagementOrchestrator` / `ICommissionConciliator` (Fase 6).

### Fase 4.3 — Push FCM/APNs + Deep Links (jun/2026)

- [x] **TecFlow.Core/Entities/UserDeviceToken.cs** — registo de tokens por utilizador.
- [x] **TecFlow.Infrastructure/Migrations/*AddUserDeviceTokens*** — tabela `UserDeviceTokens`.
- [x] **TecFlow.Business/** — `INotificationHubService`, DTOs `DeviceRegisterDto`, `PushNotificationDto`, `FirebaseOptions`.
- [x] **TecFlow.Infrastructure.Services/Integrations/Notifications/NotificationHubService.cs** — Firebase Admin SDK (FCM).
- [x] **TecFlow.API/Controllers/DevicesController.cs** — `POST /api/devices/register`.
- [x] **TecFlow.SharedUi/Navigation/DeepLinkRoutes.cs** — esquema `tecflow://` → rotas Blazor.
- [x] **TecFlow.SharedUi/Components/Shared/DeepLinkListener.razor** — navegação a partir de push/deep link.
- [x] **TecFlow.SharedUi/Components/Pages/EngajamentoFila.razor**, **ConciliacaoDetalhes.razor**.
- [x] **TecFlow.Mobile/Platforms/Android/TecFlowFirebaseMessagingService.cs** — FCM foreground/background.
- [x] **TecFlow.Mobile/Platforms/iOS/AppDelegate.cs** — `UNUserNotificationCenter` + URL scheme.
- [x] **TecFlow.Mobile/Platforms/Android/MainActivity.cs** — intent-filter `tecflow://`.
- [x] **TecFlow.Tests/Unit/Controllers/DevicesControllerTests.cs**.

### Fase 4.2 — Shell MAUI Blazor Hybrid + RCL SharedUi (jun/2026)

- [x] **TecFlow.SharedUi/** — Razor Class Library: componentes (Layout, Dashboard, Pages, Auth), `wwwroot/app.css`, serviços HTTP/API, extensões Filter/ResponseDto.
- [x] **TecFlow.SharedUi/Extensions/ServiceCollectionExtensions.cs** — `AddTecFlowClientServices()` (HttpClient Orquestrador; ignora SSL autoassinado em Development/Homologacao).
- [x] **TecFlow.SharedUi/Services/Http/IAccessTokenProvider.cs** — abstração de token para Web e MAUI.
- [x] **TecFlow.WebUi/** — host fino: OAuth/cookies (`AuthCookieService`, `WebAccessTokenProvider`), `Routes.razor` único no host com `AdditionalAssemblies` → SharedUi.
- [x] **TecFlow.SharedUi/Components/AppRoutes.razor** — router do MAUI/AppShell (nome distinto de `Routes` do WebUi).
- [x] **TecFlow.WebUi/Program.cs** — repassa `builder.Environment` para `AddWebUiServices` (SSL bypass do HttpClient Orquestrador em dev/homolog).
- [x] **TecFlow.WebUi/Extensions/WebUiServiceCollectionExtensions.cs** — encaminha `IHostEnvironment` para `AddTecFlowClientServices`.
- [x] **TecFlow.Mobile/** — MAUI Blazor Hybrid (`MainPage.xaml` + `BlazorWebView` → `AppRoutes` SharedUi); TFM Windows sem iOS/Mac Catalyst.
- [x] **TecFlow.Mobile/MauiProgram.cs** — DI compartilhada + `MobileAuthenticationStateProvider` + `SessionAuthCookieService`.
- [x] **TecFlow.API/Program.cs** — política CORS `AllowAll` antes de `UseAuthentication`/`UseAuthorization`.
- [x] **TecFlow.Mobile/Platforms/Android/AndroidManifest.xml** — permissões `INTERNET` e `ACCESS_NETWORK_STATE`.
- [x] **TecFlow.Mobile/Platforms/iOS/Info.plist** — `NSAppTransportSecurity` / rede local.
- [x] **TecFlow.Mobile/appsettings.json** — URL do Orquestrador (emulador Android `10.0.2.2`).
- [x] **docs/TecFlow_MOBILE_BUILD.md** — comandos `dotnet publish` Android/iOS/Windows.

### Fase 4.1 — Mobile-First WebUi e contratos paginados (jun/2026)

- [x] **TecFlow.WebUi/Components/Layout/MainLayout.razor** — shell com sidebar colapsável (hambúrguer &lt; 992px), backdrop e navegação touch-friendly.
- [x] **TecFlow.WebUi/Components/Layout/MainLayout.razor.css** — ícone do menu.
- [x] **TecFlow.WebUi/wwwroot/app.css** — breakpoints mobile-first, `.btn-touch` (44px), `.responsive-data-table` (tabela ↔ cards), sidebar e toolbar do painel.
- [x] **TecFlow.WebUi/Components/Dashboard/CampaignsWidget.razor** — listagem em cards no mobile + rodapé de paginação.
- [x] **TecFlow.WebUi/Components/Dashboard/MetricsWidget.razor** — idem métricas/comissões.
- [x] **TecFlow.WebUi/Components/Dashboard/CampaignFilterForm.razor** — colunas `col-12` em mobile, botões touch.
- [x] **TecFlow.WebUi/Components/Dashboard/MetricFilterForm.razor** — idem.
- [x] **TecFlow.WebUi/Components/Dashboard/CampaignCreateForm.razor** — formulário responsivo.
- [x] **TecFlow.WebUi/Components/Pages/Dashboard.razor** — toolbar responsiva, âncoras `#campanhas` / `#metricas`.
- [x] **TecFlow.WebUi/Components/Pages/Home.razor** — botões de plataforma touch-friendly.
- [x] **TecFlow.Database/Filter/IPagedFilter.cs** — contrato `Page` / `PageSize` nos filtros de listagem.
- [x] **TecFlow.Database/Filter/CampaignFilter.cs**, **MetricFilter.cs**, **ProductFilter.cs**, **AffiliateFilter.cs** — implementam `IPagedFilter`.
- [x] **TecFlow.Database/Pagin/PagedListHelper.cs** — fatia listas (máx. 30 itens/página).
- [x] **TecFlow.Business/Dto/PagingInfoDto.cs** — metadados no envelope `*ResponseDto`.
- [x] **TecFlow.Business/Dto/CampaignResponseDto.cs**, **MetricResponseDto.cs**, **ProductResponseDto.cs**, **AffiliateResponseDto.cs** — propriedade `Paging`.
- [x] **TecFlow.API/Controllers/** — `CampaignsController`, `MetricsController`, `ProductsController`, `AffiliatesController` com paginação.
- [x] **TecFlow.Orquestrador/Controllers/CampaignsController.cs**, **MetricsController.cs** — alinhados ao padrão `*Filter` → `*ResponseDto` + `Paging` (WebUi).
- [x] **TecFlow.Tests/Unit/Database/PagedListHelperTests.cs**, **ProductsControllerPagingTests.cs**.

### Integrações TikTok Shop & Shopee — Fase 3.1 (Infraestrutura Core)

- [x] **TecFlow.Business/Integrations/Common/** — `IExternalIntegrationClient`, `IntegrationHttpClientNames`, `IntegrationResilienceOptions`.
- [x] **TecFlow.Business/Integrations/TikTokShop/** — `ITikTokShopIntegrationClient`, `TikTokShopIntegrationOptions` (AppKey/AppSecret).
- [x] **TecFlow.Business/Integrations/Shopee/** — `IShopeeIntegrationClient`, `ShopeeIntegrationOptions` (PartnerId/PartnerKey/AppKey/AppSecret/AppSignature).
- [x] **TecFlow.Infrastructure.Services/Integrations/Common/** — `ExternalApiLoggingHandler`, `IntegrationResiliencePolicies` (Polly retry + circuit breaker).
- [x] **TecFlow.Infrastructure.Services/Integrations/TikTokShop/TikTokShopIntegrationClient.cs** — implementação HTTP.
- [x] **TecFlow.Infrastructure.Services/Integrations/Shopee/ShopeeIntegrationClient.cs** — implementação HTTP.
- [x] **IntegrationHttpClientRegistrationExtensions.cs** — `AddTecFlowIntegrationHttpClients()` registrado em `ServiceRegistrationExtensions`.
- [x] **appsettings.json** (API + Orquestrador) — seção `Integrations` com chaves de produção (vazias; usar User Secrets/env).

### Integrações TikTok Shop & Shopee — Fase 3.2 (OAuth2)

- [x] **TecFlow.Core/Entities/MarketplaceToken.cs** — persistência de tokens por loja (`ShopId`, `MarketplaceType`, `AccessToken`, `RefreshToken`, `ExpiresAt`, `RefreshExpiresAt`).
- [x] **TecFlow.Core/Enums/MarketplaceType.cs** — `Shopee`, `TikTokShop`.
- [x] **TecFlow.Business/Integrations/Auth/** — `IMarketplaceAuthService`, `IMarketplaceSignatureService`, `MarketplaceTokenResult`.
- [x] **TecFlow.Business/Integrations/Common/MarketplaceSignatureHelper.cs** — HMAC-SHA256 Shopee e TikTok Shop.
- [x] **TecFlow.Infrastructure.Services/Integrations/Auth/MarketplaceAuthService.cs** — authorize URL, callback, refresh automático.
- [x] **TecFlow.Infrastructure.Services/Repositories/MarketplaceTokenRepository.cs** — upsert por `ShopId` + `MarketplaceType`.
- [x] **MarketplaceAuthController** — `GET api/marketplace-auth/lojas` lê `MarketplaceAccounts` por `UserId`.
- [x] **IntegracaoLojaService.ListByUserAsync** — `AsNoTracking` em `MarketplaceAccounts`.
- [x] **IntegracaoLojaApiService.ListAsync** / **MinhasLojas.razor** — recarrega lojas após vínculo.
- [x] **Migration AddMarketplaceTokens** — tabela `MarketplaceTokens` com tokens criptografados no `AppDbContext`.

### Integrações TikTok Shop & Shopee — Fase 3.3 (Catálogo / Produtos)

- [x] **TecFlow.Core/Entities/Product.cs** — campos `[NotMapped]` `ExternalProductId`, `SkuCode`, `MarketplaceSource` para sincronização.
- [x] **TecFlow.Business/Integrations/Catalog/IMarketplaceProductService.cs** — `FetchProductsFromPlatformAsync`, `ConvertToInternalProductDto` (Shopee/TikTok).
- [x] **TecFlow.Business/Integrations/Shopee/Payloads/** — envelopes e DTOs `get_item_list`, `get_item_base_info`, `get_model_list`.
- [x] **TecFlow.Business/Integrations/TikTokShop/Payloads/** — envelope e DTOs `products/search` (categorias, attributes, skus, price, stock).
- [x] **ShopeeIntegrationOptions** / **TikTokShopIntegrationOptions** — paths de catálogo configuráveis.
- [x] **MarketplaceProductService.cs** — HTTP resiliente + token OAuth + assinatura HMAC + adapter → `ProductResponseDto`.
- [x] **MarketplaceProductMapper.cs** — conversão unificada (ID externo, SKU, nome, descrição, preço, estoque, origem).
- [x] **MarketplaceProductRegistrationExtensions.cs** — `AddTecFlowMarketplaceCatalog()` em `ServiceRegistrationExtensions`.
- [x] **TecFlow.API/Controllers/MarketplaceProductsController.cs** — `GET api/marketplace-products/sync`.

### Integrações TikTok Shop & Shopee — Fase 3.4 (Pedidos & Estoque)

- [x] **TecFlow.Core/Entities/MarketplaceOrder.cs** / **MarketplaceOrderLine.cs** — pedidos externos com índice único (idempotência).
- [x] **Product.cs** — colunas persistidas `IdExterno`, `SkuCodigo`, `MarketplaceOrigem`, `MarketplaceShopId`.
- [x] **IMarketplaceOrderRepository** / **MarketplaceOrderRepository.cs**
- [x] **IProductRepository** — `GetByMarketplaceSkuAsync`, `AdjustStockAsync` (transação).
- [x] **Payloads Shopee/TikTok** — webhooks, `get_order_list`/`get_order_detail`, `orders/search`, `update_stock`/`products/stocks`.
- [x] **IMarketplaceWebhookSignatureVerifier** — HMAC Shopee (`callbackUrl|body`) e TikTok (`AppSecret` / `Webhook-Signature`).
- [x] **IMarketplaceOrderService** / **MarketplaceOrderService** — webhook + polling + idempotência.
- [x] **IMarketplaceStockService** / **MarketplaceStockService** — baixa local + push marketplace com `StockConcurrencyGate`.
- [x] **ShopeeWebhookController** — `POST /api/webhooks/shopee`
- [x] **TikTokShopWebhookController** — `POST /api/webhooks/tiktokshop`
- [x] **MarketplaceOrdersController** — `POST /api/marketplace-orders/poll`
- [x] **ProductsController** — propaga alteração de estoque para marketplace quando SKU vinculado.
- [x] **Migration AddMarketplaceOrdersAndProductSku**

### TecFlow.Tests — Cobertura Fase 3 (Integrações & ResponseDto)

- [x] **Helpers/StubHttpMessageHandler.cs** — mock HTTP sem chamadas externas.
- [x] **Helpers/MarketplaceTestOptionsFactory.cs** — opções Shopee/TikTok para testes.
- [x] **Unit/Integrations/MarketplaceSignatureHelperTests.cs** — HMAC e comparação de assinatura.
- [x] **Unit/Integrations/MarketplaceWebhookSignatureVerifierTests.cs** — webhooks válidos/inválidos/expirados.
- [x] **Unit/Integrations/MarketplaceAuthServiceTests.cs** — URL OAuth, token válido, refresh automático.
- [x] **Unit/Integrations/MarketplaceProductServiceTests.cs** — conversão Shopee/TikTok → `ProductResponseDto`.
- [x] **Unit/Integrations/MarketplaceOrderServiceTests.cs** — webhook, idempotência, baixa de estoque.
- [x] **Unit/Integrations/MarketplaceStockServiceTests.cs** — dedução local e push de estoque.
- [x] **Unit/Controllers/ProductsControllerResponseDtoTests.cs** — `Filter` + `ProductResponseDto` + sync estoque.
- [x] **Unit/Controllers/MarketplaceAuthControllerTests.cs** — authorize-url e callback com falha.
- [x] **Unit/Controllers/MarketplaceWebhookControllerTests.cs** — 401 assinatura inválida / 200 OK.
- [x] **Unit/Database/ProductFilterExtensionsTests.cs** — `ProductFilter` → `DataList` padronizado.
- [x] **TecFlow.Tests.csproj** — referências `TecFlow.API`, `TecFlow.Database`, `Microsoft.AspNetCore.Mvc.Testing`.

### Colisão semântica (nome enganoso)

- [ ] **AuthController.cs** — `TecFlow.Application/Controller/AuthController.cs` **não é controller** (classe DTO com `CampaignId`, `Revenue`). Conflito de nome com `TecFlow.Orquestrador/Controllers/AuthController.cs`. (Ação: renomear para `CampaignSummaryDto` ou deletar se obsoleto; remover projeto `Application` da cadeia se ficar vazio.)

### Registro DI fragmentado (não duplica tipo, duplica responsabilidade)

- [ ] **ServiceRegistrationExtensions.cs** + **CoreServiceRegistrationExtensions.cs** + **ExternalServiceRegistrationExtensions.cs** + **InfrastructureDataServiceRegistrationExtensions.cs** — todos em `TecFlow.Infrastructure.Services/`. (Ação: consolidar em um único `ServiceRegistrationExtensions.cs` conforme plano anterior.)

### API legada no Infrastructure (sobreposição com Business + Services)

- [ ] **TikTokShopApi.cs**, **TikTokAdsApi.cs**, **ShopeeApi.cs** — `TecFlow.Infrastructure/API/` vs implementações HttpClient em `TecFlow.Infrastructure.Services/Service/ExternalServices/`. (Ação: definir camada única de integração externa; deletar stubs antigos em `Infrastructure/API` se não forem registrados no DI.)

---

## 🚚 2. Arquivos Movidos com Sucesso (Apenas Ajustar Namespace)

Arquivos já no projeto físico correto, mas com `namespace` desalinhado da pasta/projeto:

- [ ] **ExceptionMiddleware.cs** — ~~Em `TecFlow.Core/Exceptions/`, namespace `TecFlow.API.Middlewares`~~ **Resolvido:** removido do Core; usar `TecFlow.API/Middlewares/ExceptionHandlingMiddleware.cs`.

- [ ] **CoreServiceRegistrationExtensions.cs** — Em `TecFlow.Infrastructure.Services/`, namespace `TecFlow.Infrastructure`. (Ação: `TecFlow.Infrastructure.Services`.)

- [ ] **Serilog.cs** — Em `TecFlow.Infrastructure.Services/`, namespace `TecFlow.Configuracao`, **excluído do compile**. (Ação: mover para `TecFlow.Infrastructure/Configuration` com namespace alinhado ou deletar.)

- [ ] **ITikTokShopApiService.cs** — Em `TecFlow.Infrastructure.Services/Interfaces/`, namespace `TecFlow.Core.Interfaces.Services`. (Ação: mover para `TecFlow.Business/Interfaces/Services` ou `TecFlow.Core` conforme regra de contratos.)

- [ ] **AnaliseCalculoService.cs**, **IAnaliseCalculoService.cs** — Pasta `Service/`, namespace `TecFlow.Infrastructure.Services.Services` (`.Services` duplicado). (Ação: `TecFlow.Infrastructure.Services.Service`.)

- [ ] **TikTokShopApiService.cs**, **TikTokAdsApiService.cs** — Pasta `Service/ExternalServices/`, namespace `TecFlow.Infrastructure.Services.ExternalServices` (falta segmento `.Service`). (Ação: alinhar com pasta ou renomear pasta.)

- [ ] **AnaliseService.cs** — Em `TecFlow.Business/Interfaces/Services/`, comentário assume `TecFlow.Core.Services`. (Ação: confirmar namespace `TecFlow.Business.Interfaces.Services` e remover comentário legado.)

- [ ] **OrquestradorPrincipalTests.cs** — `TecFlow.Tests/UnitTests/Core/`, namespace `TecFlow.Tests.UnitTests.Colore` (typo). (Ação: `TecFlow.Tests.UnitTests.Core`.)

- [ ] **CampanhaConfiguration.cs**, **AfiliadoConfiguration.cs** — `TecFlow.Infrastructure/Data/Configurations/`, nomes em português para entidades em inglês (`Campaign`, `Affiliate`). (Ação: renomear classes/arquivos para inglês ou mover configs para `TecFlow.Database`.)

### Movidos e com namespace correto (referência — sem ação)

- `TecFlow.Database`: `AppDbContext`, `Entity/`, `Filter/`, `Pagin/`, `Repositorio/` → namespaces `TecFlow.Database.*` ✓  
- `TecFlow.Business`: `Dto/`, `Interfaces/`, `Pipelines/`, `Service/Application/` → `TecFlow.Business.*` ✓  
- `TecFlow.API` controllers refatorados → `TecFlow.API.Controllers` ✓  

### Pendência arquitetural: Migrations vs DbContext

- [ ] **AppDbContext.cs** — em `TecFlow.Database/`.  
- [ ] **Migrations/** — ainda em `TecFlow.Infrastructure/Migrations/` (6 migrações + snapshot). (Ação: mover pasta de migrations para `TecFlow.Database` ou configurar `MigrationsAssembly` apontando para Infrastructure até concluir a mudança; hoje há risco de `dotnet ef` gerar no projeto errado.)

---

## 🧹 3. Resíduos e Arquivos Fantasmas a Deletar

### Artefatos de build com nome antigo *Tecso* (não versionar)

- [ ] **`**/bin/**` e `**/obj/**`** em todos os projetos — contêm `Tecso.API.deps.json`, `Tecso.Core.AssemblyInfo.cs`, `Tecso.Orquestrador.*`, etc. (Ação: `dotnet clean`; garantir `.gitignore` com `bin/`, `obj/`, `artifacts/`.)

- [ ] **`artifacts/orquestrador-publish/`** — publish com assembly `Tecso.Orquestrador`. (Ação: excluir pasta ou regenerar publish com nome TecFlow.)

- [ ] **`.vs/Tecso.Automacao/`** — cache Visual Studio com caminhos `Tecso.Infrastructure.Services\...`. (Ação: excluir do disco; não commitar.)

### Arquivos excluídos do compile mas ainda no disco (fantasmas)

- [ ] `TecFlow.Infrastructure.Services/Interfaces/ITikTokShopApi.cs`  
- [ ] `TecFlow.Infrastructure.Services/Interfaces/IShopeeApi.cs`  
- [ ] `TecFlow.Infrastructure.Services/Interfaces/ITikTokAdsApiService.cs`  
- [ ] `TecFlow.Infrastructure.Services/Serilog.cs`  
- [ ] `TecFlow.Infrastructure.Services/Service/ExternalServices/OrquestradorService.cs`  
(Ação: deletar arquivos **ou** reintegrar ao compile — não manter `<Compile Remove>` indefinidamente.)

### Templates / testes de scaffold

- [ ] `TecFlow.API/WeatherForecast.cs` + `Controllers/WeatherForecastController.cs`  
- [ ] `TecFlow.API/Controllers/TestController.cs`  
- [x] `TecFlow.Dashboard/` — projeto removido (WeatherForecast scaffold eliminado com a pasta).
(Ação API: excluir scaffolds se não usados em produção.)

### Projeto Application quase vazio

- [ ] `TecFlow.Application/` — apenas `Controller/AuthController.cs` (stub incorreto). Referenciado por `TecFlow.API`. (Ação: migrar serviços restantes para `TecFlow.Business` e remover referência de projeto, ou repopular Application com casos de uso reais.)

### Pasta API sem middleware (limpeza pós-merge)

- [ ] `TecFlow.API/Middleware/` — pasta vazia após remoção de `ExceptionMiddleware.cs`. (Ação: remover diretório vazio.)

### Arquivos de usuário IDE (opcional)

- [ ] `TecFlow.API/Tecso.API.csproj.user`  
- [ ] `TecFlow.API/TecFlow.API.csproj.user`  
(Ação: excluir e adicionar `*.csproj.user` ao `.gitignore`.)

### Duplicata de teste

- [ ] `TecFlow.Tests/Integration/OrquestradorPrincipalTests.cs` e `TecFlow.Tests/UnitTests/Core/OrquestradorPrincipalTests.cs` — mesmo nome de arquivo. (Ação: renomear um (ex.: `OrquestradorPrincipalIntegrationTests.cs`).)

---

## 🛠️ 4. Estrutura de Pastas Alvo (Arquitetura Atual)

```
TecFlow.sln
├── TecFlow.Core/                    # Domínio: entidades, exceções, middleware global
│   ├── Entities/                    # Campaign, Product, UserAccount, ...
│   └── Exceptions/                  # NotFoundException, ExceptionMiddleware ⚠ namespace API
│
├── TecFlow.Database/                # Persistência (isolado) ✓
│   ├── AppDbContext.cs
│   ├── Entity/                      # UserEntity
│   ├── Filter/                      # *Filter + FilterQueryExtensions
│   ├── Pagin/                       # PagedResult, QueryableExtensions
│   ├── Repositorio/                 # AppDbContextFactory
│   └── Prompts/
│
├── TecFlow.Business/                # Regras + contratos + DTOs (isolado) ✓
│   ├── Dto/                         # *Dto, *ResponseDto, ResponseDto
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   └── Services/
│   ├── Pipelines/
│   └── Service/
│       └── Application/             # *ApplicationService, DI extensions
│
├── TecFlow.Infrastructure/          # EF migrations, segurança, configs legadas
│   ├── Migrations/                  # ⚠ deveria alinhar com TecFlow.Database
│   ├── Data/                        # DataService, Configurations (PT-BR)
│   ├── API/                         # TikTok/Shopee stubs legados
│   ├── Security/
│   └── Services/Security/
│
├── TecFlow.Infrastructure.Services/ # Implementações: repos, APIs externas, DI
│   ├── Repositories/
│   ├── Service/
│   │   └── ExternalServices/
│   ├── Interfaces/                  # ⚠ fantasmas + namespace Core
│   └── *ServiceRegistrationExtensions.cs (4 arquivos)
│
├── TecFlow.Application/             # ⚠ quase vazio — 1 arquivo stub
├── TecFlow.API/                     # Host HTTP principal
├── TecFlow.Orquestrador/            # Host orquestração (controllers espelhados)
├── TecFlow.WebUi/                   # Blazor UI canônico → Business + Database (Filter)
│   ├── Components/Dashboard/        # FilterForm, CreateForm, Widgets (ResponseDto)
│   ├── Extensions/                  # FilterQueryString, ResponseDto, Campaign
│   └── Services/Dashboard/          # Filter → API → ResponseDto
├── TecFlow.Worker/
├── TecFlow.Tests/
├── TecFlow.Util/                    # ValidationHelper, Encryption, CEP
└── (externo) ../Tecso.LerArquivos/  # Utilitário fora do repo principal
```

### Grafo de referências (simplificado)

```
API / Orquestrador / Worker / WebUi
  → Application (quase vazio)
  → Business → Core, Database
  → Infrastructure.Services → Infrastructure → Business, Core, Database, Util
  → Infrastructure → Business, Core, Database, Util
```

**Isolamento desejado:** `TecFlow.Business` e `TecFlow.Database` **não** devem referenciar `Infrastructure` — hoje **ok** no `.csproj`. Implementações ficam em `Infrastructure` + `Infrastructure.Services`.

### Fase 19.1.1 — Credenciais de afiliado Shopee (appsettings)

- [x] **TecFlow.API/appsettings.json**, **appsettings.Homologacao.json** — `Integrations:Shopee` com `PartnerId`, `PartnerKey`, `AppSecret`, `AppKey`, `AppSignature` (placeholders `""`).
- [x] **TecFlow.Business/Integrations/Shopee/ShopeeIntegrationOptions.cs** — bind das chaves de afiliado.
- [x] **TecFlow.Infrastructure.Services/LinkStrategies/ShopeeAffiliateLinkClient.cs** — fallback `AppKey`/`AppSecret`/`AppSignature`.
- [x] **TecFlow.WebUi** — sem seção Shopee (credenciais ficam só na API).

### Fase 19.1.2 — Fallback/Sandbox Shopee

- [x] **TecFlow.Business/Integrations/Shopee/ShopeeSandboxLinkBuilder.cs** — URL de homologação com `tracking_code`/`sub_id` (`tecflow_sandbox_subid`).
- [x] **ShopeeIntegrationClient** — modo sandbox quando credenciais vazias (não lança; não chama HTTP).
- [x] **ShopeeAffiliateLinkClient** / **ShopeeLinkStrategy** — geração local de URL rastreada sem exceção.
- [x] **TecFlow.Tests/Unit/LinkStrategies/ShopeeLinkConversionTests.cs** — credenciais, sandbox e tracking.

### Fase 19.2.1 — PlatformLinkResolver, unshorten e extração ShopId/ItemId

- [x] **ShopeeLinkHostMatcher.cs** — hosts `shopee.com.br`, `br.shp.ee`, `shp.ee`, `shope.ee`.
- [x] **ShopeeProductUrlParser.cs** — regex `i.(shopId).(itemId)` em URLs desktop, sanitização de `extraParams`/`sp_atk`/`xptdk` e HTTP 400 com alerta Blazor de formato não reconhecido.
- [x] **UrlExpansionService** — GET sem autoredirect, segue `Location` 301/302.
- [x] **ShopeeLinkStrategy** / **PlatformLinkResolver** — regex `i.(shopId).(itemId)` no desktop; Universal Link `https://shopee.com.br/universal-link/product/{shopId}/{itemId}?sub_id=...` sem Open API.
- [x] **ConnectStoreModal.razor** / **MinhasLojas.razor** — Shopee: plataforma, apelido e campo `ID do Afiliado (Affiliate ID / Tracking ID)` (ex.: `6512300000`); persistido em `MarketplaceAccounts.AffiliateTrackingId`.
- [x] **MarketplaceStoreCard.razor** — exibe `Shop ID` e `Affiliate ID` no card de Minhas Lojas.
- [x] **TecFlow.Data/Migrations/20260922220000_AddAffiliateTrackingId.cs** — coluna `AffiliateTrackingId` (`nvarchar(64)`) em `MarketplaceAccounts` e `IntegracaoLoja`.

### Fase 19.2.2 — URL rastreada de comissão

- [x] **ShopeeCommissionUrlBuilder.cs** — query `tracking_code`, `sub_id` (usuário/tenant), `universal_link` e `deep_link` com URL encoding.
- [x] **ShopeeOfficialShortUrl.cs** — resolve `https://br.shp.ee/...` a partir da API, da URL original ou dos IDs do produto.
- [x] **ShopeeLinkConversionTests.cs** — contrato de query string, deep links nativos e caracteres especiais.

### Fase 19.2.3 — Persistência de telemetria LinkClickLog

- [x] **LinkClickLog.cs** — TenantId, ShopId, OriginalUrl, ConvertedUrl, Platform, CreatedAt, EventKind e metadados de acesso.
- [x] **AffiliateLinkGenerationService** / **LinkClickTelemetryService.RecordGenerationAsync** — grava telemetria após conversão Shopee.
- [x] **20260916231111_AddLinkClickLogGenerationTelemetry** — colunas novas na tabela `LinkClickLog`.
- [x] **TecFlow.Tests/Unit/LinkStrategies/LinkClickLogTests.cs** — TenantId/ShopId da sessão, campos obrigatórios e FK do encurtador.

### Fase 19.3.1 — Integração GeradorLinks.razor

- [x] **GeradorLinks.razor** — POST com URL + StoreId/TenantId/ShopId da loja ativa; `_isLoading`, spinner, alerta vermelho e `StateHasChanged()` no sucesso.
- [x] **TecFlow.Util/Text/SlugHelper.cs** — `GenerateSlug` / `ShortLinkPublicUrl` (`@Achadinhos de Aaz` → `AchadinhosDeAaz`; URL `{host}/{storeSlug}/{code}`).
- [x] **ShortLinkRedirectController.cs** — `GET /{storeSlug}/{code}` resolve `ShortAffiliateLinks` pelo hash e registra clique; `/r/{code}` permanece como alias.
- [x] **AffiliateLinkApiService** — `api/links/convert` (alias `api/afiliados/links/gerar`) via `HttpService`; 200 OK com `originalUrl`, `affiliateUrl` e `shortenedUrl`.
- [x] **GerarLinkAfiliadoResponseDto** — `OriginalUrl`, `AffiliateUrl` (longa), `ShortenedShopeeUrl` (`br.shp.ee`) e `ShortenedUrl` (`/{storeSlug}/{code}`).
- [x] **AffiliateLinksController** — rotas `api/afiliados/links` e `api/affiliate-links`.
- [x] **GeradorLinksServiceTests.cs** — mock HTTP POST e DTO de resposta com link convertido.

### Fase 19.3.4 — Múltiplas contas da mesma plataforma

- [x] **ShortAffiliateLinkAccount.cs** — associação loja/conta com `IsActive` (default true); desmarcar não apaga o registro.
- [x] **ShortAffiliateLink.LinkGroupId** — agrupa as conversões do mesmo produto.
- [x] **GeradorLinks.razor** — checkboxes e Selecionar todas; gera para todas as contas marcadas.
- [x] **LinkGeneratorResultPanel.razor** — combo "Conta selecionada" quando há mais de uma conta ativa.
- [x] **HistoricoLinks.razor** — ação Editar com modal de contas (inativação lógica).
- [x] **TecFlow.Data/Migrations/20260923200000_AddShortAffiliateLinkAccounts.cs** — tabela e backfill no SQL Server.

### Fase 19.4 — TikTok Shop Link Strategy

- [x] **TikTokShopLinkStrategy.cs** — hosts `tiktok.com` / `shop.tiktok.com` / `vt.tiktok.com`; extrai `productId`; `sub_id` com TrackingId ou FriendlyName. Substitui `TikTokLinkStrategy.cs`.
- [x] **TikTokShopProductUrlParser.cs** / **TikTokShopCommissionUrlBuilder.cs** — parse de path/query e URL `https://shop.tiktok.com/view/product/{id}?sub_id=`.
- [x] **LinkStrategyServiceCollectionExtensions.cs** — registra `TikTokShopLinkStrategy` no `PlatformLinkResolver`.
- [x] **ConnectStoreModal.razor** — cadastro TikTok Shop com apelido e Tracking ID (mesmo fluxo Universal Link da Shopee).
- [x] **TikTokShopLinkStrategyTests.cs** — parse de URLs, expansão de encurtador e injeção de `sub_id`.

### Fase 19.5 — Mercado Livre Link Strategy

- [x] **MercadoLivreLinkStrategy.cs** — hosts `mercadolivre.com.br` / `produto.mercadolivre.com.br` / `mercadolivre.com/sec` / `ml.com.br`; extrai MLB; `matt_tool` + `matt_word`.
- [x] **MercadoLivreProductUrlParser.cs** / **MercadoLivreCommissionUrlBuilder.cs** — parse MLB e preservação de query em `/sec/`.
- [x] **ConnectStoreModal.razor** — opção Mercado Livre (badge amarelo) com Nome Amigável e Matt Tool ID.
- [x] **GeradorLinks.razor** — chip Mercado Livre no seletor de marketplaces e detecção de URL MLB /sec/.
- [x] **MarketplaceUrlDetector.cs** — Mercado Livre `IsBackendReady`.
- [x] **MercadoLivreLinkStrategyTests.cs** — parse de URLs e injeção `matt_tool`/`matt_word`.

### Fase 19.6 — Amazon Link Strategy

- [x] **AmazonLinkStrategy.cs** — hosts `amazon.com.br` / `amazon.com` / `amzn.to` / `a.co`; extrai ASIN; injeta `tag`.
- [x] **AmazonProductUrlParser.cs** / **AmazonCommissionUrlBuilder.cs** — parse `/dp/` `/gp/product/` e expansão de encurtador.
- [x] **ConnectStoreModal.razor** — opção Amazon (badge laranja/preto) com Nome Amigável e Tag de Associado.
- [x] **GeradorLinks.razor** — chip Amazon no seletor de marketplaces (`IsBackendReady`).
- [x] **AmazonLinkStrategyTests.cs** — parse de URLs canônicas/curtas e injeção de `tag`.

### Fase 19.7 — Magazine Luiza Link Strategy

- [x] **MagazineLuizaLinkStrategy.cs** — hosts `magazineluiza.com.br` / `magazinevoce.com.br` / `magalu.me` / `mglz.ne`; extrai `/p/{id}`.
- [x] **MagazineLuizaProductUrlParser.cs** / **MagazineLuizaCommissionUrlBuilder.cs** — Magazine Você ou `?parceiro=` e expansão de encurtador.
- [x] **ConnectStoreModal.razor** — opção Magazine Luiza (badge azul) com Nome Amigável e loja parceira.
- [x] **GeradorLinks.razor** — chip Magalu no seletor (`IsBackendReady`).
- [x] **MagazineLuizaLinkStrategyTests.cs** — parse de URLs e montagem dos links de afiliado.

### Fase 19.8 — Kabum! Link Strategy

- [x] **KabumLinkStrategy.cs** — hosts `kabum.com.br` / `kb.um` / `kabum.me`; extrai `/produto/{id}`; injeta `sub_id` e `utm_source=afiliado`.
- [x] **KabumProductUrlParser.cs** / **KabumCommissionUrlBuilder.cs** — parse e expansão de encurtador.
- [x] **ConnectStoreModal.razor** — opção Kabum! (badge laranja/preto) com Nome Amigável e Tracking ID.
- [x] **GeradorLinks.razor** — chip Kabum! no seletor (`IsBackendReady`).
- [x] **KabumLinkStrategyTests.cs** — parse de URLs e injeção dos parâmetros de comissão.

### Fase 19.9 — Casas Bahia Link Strategy

- [x] **CasasBahiaLinkStrategy.cs** — hosts `casasbahia.com.br` / `cb.com.br` / `casasbahia.app.link`; extrai `/p/{id}` ou `{id}/p`.
- [x] **CasasBahiaProductUrlParser.cs** / **CasasBahiaCommissionUrlBuilder.cs** — `parceiro` + `sub_id` e expansão de encurtador.
- [x] **ConnectStoreModal.razor** — opção Casas Bahia (badge vermelho) com Nome Amigável e ID de Parceiro.
- [x] **GeradorLinks.razor** — chip Casas Bahia no seletor (`IsBackendReady`).
- [x] **CasasBahiaLinkStrategyTests.cs** — parse de URLs e injeção de rastreio.

### Fase 19.10 — Modal Conectar nova loja compacta

- [x] **ConnectStoreModal.razor** / **app.css** — grid `auto-fit` 3–4 colunas, badges 28px, modal sem scroll vertical.

### Fase 19.12 — Histórico de links por tenant (sem filtro da loja ativa)

- [x] **HistoricoLinks.razor** — lista todos os links ativos do tenant; abas de plataforma (inclui Mercado Livre); não usa `ActiveStoreId` como `LojaId`.
- [x] **GeradorLinks.razor** — troca de loja no topo não zera o resultado nem o histórico.
- [x] **AffiliateLinksControllerTests.cs** — histórico ignora `LojaId` e mantém `PlatformType`.
- [x] **AffiliateLinksController** / **AffiliateLinkHistoryService** / **ShortAffiliateLinkRepository** — `GET historico` ignora `LojaId`, sem inner join de loja e grupos com `IsActive`.
- [x] **ConnectStoreModal.razor** / **MinhasLojas.razor** — fechar o modal dispara `StateHasChanged` e recarrega o escopo de lojas.

### Fase 19.13 — Ajuda "Como pegar meu ID?" no cadastro de loja

- [x] **ConnectStoreModal.razor** / **app.css** — link com `bi-question-circle`, dica do formato e botão para o painel oficial (`target=_blank`).
- [x] **AffiliateTrackingIdSanitizer.cs** / **AffiliateTrackingIdHelp.cs** — extração de `tag`, `sub_id`, `an_id`, `matt_tool`/`matt_word`, `parceiro` e slug Magazine Você.
- [x] **AffiliateTrackingIdSanitizerTests.cs** — URLs, query solta, `an_id` Shopee e ID puro.
- [x] **ConnectStoreModal.razor** — `ExtractAffiliateIdFromUrl` em `@oninput`/`@onchange` e dica de colar link de teste.

### Fase 19.14 — Validação estrita do Tracking ID e expansão de encurtadores

- [x] **AffiliateTrackingIdValidator.cs** — rejeita URL residual (`://` ou `/`); extrai `tag`/`sub_id`/`affiliate_id`/`matt_tool`/`parceiro`.
- [x] **ConnectStoreModal.razor** — bloqueia Salvar conta com alerta de ID inválido; expande `br.shp.ee` / `amzn.to` / `magalu.me`.
- [x] **POST api/marketplace-auth/expand-affiliate-url** — HEAD/GET via `UrlExpansionService`.
- [x] **MarketplaceAccountRepository.SanitizeHttpTrackingIdsAsync** — contas com `TrackingId` contendo `http` são limpas ou inativadas na listagem.
- [x] **MarketplaceStoreCard** — alerta para corrigir ID inválido.

### Fase 19.15 — Unicidade de Tracking ID por plataforma

- [x] **IntegracaoLojaService** / **MarketplaceAccountRepository.ExistsActiveTrackingIdAsync** — bloqueia duplicata ativa na mesma plataforma.
- [x] **AppDbContext** / **20260924010000_UniqueMarketplaceAccountTrackingIdPerPlatform** — índice único filtrado `MarketplaceType + TrackingId`.
- [x] **ConnectStoreModal** / placeholders / testes — ID de exemplo padronizado `6512300000`.

### Fase 19.16 — Desconectar loja (inativação lógica)

- [x] **MarketplaceAccountService.InativarContaAsync** / **MarketplaceAccountRepository.SetInactiveByIdAsync** — `IsActive = false` e `SaveChanges` no SQL Server por Id.
- [x] **IntegracaoLojaService.UnlinkAsync** — resolve usuário persistível, inativa a conta e não usa `Upsert` por ShopId.
- [x] **DELETE api/marketplace-auth/lojas/{id}** — mesmo JWT (`sub` / NameIdentifier) da listagem; Minhas Lojas recarrega a grade.
- [x] **MinhasLojas.razor** / **MarketplaceStoreCard.razor** — `ConfirmarDesconexaoAsync(MarketplaceAccountDto)` (`async Task`), POST `lojas/{id}/desconectar`, `CarregarContasAsync` e `StateHasChanged`.
- [x] **MarketplaceAccountService.InativarContaAsync** — `IgnoreQueryFilters`, `IsActive = false` e `SaveChangesAsync` no `AppDbContext`.

### Fase 19.17 — Extração Shopee `mmp_pid=an_` / encurtadores

- [x] **AffiliateTrackingIdValidator** — regex `mmp_pid=an_`, `utm_source=an_`, `affiliate_id`, `sub_id`; hosts `shope.ee` / `s.shopee.com.br`.
- [x] **UrlExpansionService** / **PlatformLinkResolver.ExpandIfShortenedAsync** — HTTP unshorten com `AllowAutoRedirect`.
- [x] **UrlUnshortenerService.cs** — `ResolveToFinalSupportedMarketplaceAsync`: loop `while` de até 5 iterações; para em Amazon/Shopee/Magalu/ML/Kabum/TikTok/Casas Bahia; senão hop via Location, meta-refresh ou `window.location`.
- [x] **TecFlow.Tests/Unit/LinkStrategies/UrlUnshortenerServiceTests.cs** — `promoby.me/6nf9k3d5` → `amazon.com.br`; `ofertou.ai/drXB-Magalu` → `magazineluiza.com.br`.
- [x] **TecFlow.Business/Service/Application/ShortAffiliateLinkService.cs** — contas Magalu com alias flexível e Tracking ID.
- [x] **IntegracaoLojaScopeResolver.cs** — `AreSamePlatform` ao resolver a loja Magalu (ex.: Affiliate ID `5321952`).
- [x] **MagazineLuizaCommissionUrlBuilder** — ID numérico injeta `promoter_id` (remove tags do concorrente).
- [x] **ProductMetadataHtmlParser** — slug Magalu (`.../smart-tv-50-tcl.../p/{id}`) vira ProductName com `UrlDecode` + TitleCase/acrônimos.
- [x] **GeradorLinks.razor** — detecta `ofertou.ai/drXB-Magalu` e lista contas Magalu com comparação flexível.
- [x] **PlatformLinkResolverTests.cs** — `ofertou.ai/UjGXJ` (Kabum), `ofertou.ai/drXB-Magalu` (TV Magalu + `promoter_id=5321952`) e `promoby.me/6nf9k3d5` (Amazon).
- [x] **TecFlow.Business/Service/LinkStrategies/UniversalLinkResolverEngine.cs** — unshortener universal (10 saltos, JS/meta-refresh), mapa domínio→plataforma e injeção de comissão.
- [x] **ProductMetadataService.cs** / **PlatformLinkResolver.cs** — `ResolveFinalDestinationUrlAsync` no pipeline de metadados e de geração.
- [x] **TecFlow.Tests/Unit/LinkStrategies/UniversalResolverTests.cs** — ofertou Magalu `5321952`, promoby Amazon sem `promobit-d-20`, ofertou Kabum.
- [x] **PlatformLinkResolver.cs** / **ShortAffiliateLinkService.cs** — unshorten primeiro; valida domínio só na URL final; agregadores (`promoby.me`, `ofertou.ai`, `bit.ly`, `t.me`) não são rejeitados na cola.
- [x] **GeradorLinks.razor** — "Não reconhecemos este domínio" apenas se o destino final não for marketplace.
- [x] **PlatformLinkResolverTests.cs** — `ResolveFromInputAsync("https://promoby.me/6nf9k3d5")` chega em `amazon.com.br/dp/B0C4BW38R4` sem "Não reconhecemos este domínio"; remove `tag=promobit-d-20` e injeta a tag da conta.
- [x] **ConnectStoreModal.razor** — preenche Tracking ID e alerta verde de extração.

### Fase 19.18 — Encurtadores TikTok, Magalu e Mercado Livre

- [x] **AffiliateTrackingIdValidator** — hosts `vt.tiktok.com`, `vm.tiktok.com`, `magazineluiza.onelink.me`, `meli.la` e `/sec/`; extração `@handle` / `matt_tool` / `parceiro`.
- [x] **PlatformLinkResolverTests.cs** — parse e extração TikTok, Magazine Luiza e Mercado Livre.
- [x] **ConnectStoreModal.razor** — "✅ Credencial {ID} extraída com sucesso para {Plataforma}!".

### Fase 19.19 — Auto-detecção de plataforma e correção do unshorten

- [x] **MarketplaceUrlDetector** / **AffiliateTrackingIdValidator.TryDetectPlatformFromUrl** — domínio colado seleciona Shopee, TikTok, Magalu, ML, Amazon, Casas Bahia e Kabum.
- [x] **ConnectStoreModal.razor** — `SelectedPlatform` atualizado em `@oninput`/`@onchange`; ignora literal `true`.
- [x] **UrlExpansionService** — retorna string da URL final; `AllowAutoRedirect = true` e User-Agent de Chrome.
- [x] **AffiliateTrackingIdValidator** — `p=` só como query param; extrai `@username` / `tt_from`, Magazine Você / `parceiro`, `matt_tool` / `penn`.

### Fase 19.20 — Magalu `promoter_id`

- [x] **AffiliateTrackingIdValidator** / **PlatformLinkResolver** — prioriza `promoter_id` numérico e `utm_campaign` numérico; ignora `utm_source=divulgador`/`magalu`.
- [x] **ConnectStoreModal.razor** — auto-detect Magalu/ML/TikTok e Tracking ID `5321952` após expandir Onelink.
- [x] **PlatformLinkResolverTests** — `promoter_id=5321952` → `5321952`.

### Fase 19.21 — TikTok `unique_id`

- [x] **AffiliateTrackingIdValidator** / **PlatformLinkResolver** — regex `unique_id` / `user_id` / `sec_user_id`; depois fallback `@handle`.
- [x] **ConnectStoreModal.razor** — auto-detect `vt.tiktok.com` / `shop.tiktok.com` como TikTok Shop.
- [x] **PlatformLinkResolverTests** — `unique_id=amz.indica` → `amz.indica`.

### Fase 19.22 — TikTok login `redirect_url` e handle direto

- [x] **AffiliateTrackingIdValidator.UnwrapTikTokLoginRedirect** — `UrlDecode` recursivo (`HttpUtility`/`WebUtility`) em `tiktok.com/login?redirect_url=`.
- [x] **UrlExpansionService** — User-Agent Chrome e `Accept-Language: pt-BR,pt;q=0.9`.
- [x] **ConnectStoreModal.razor** — Tracking ID TikTok aceita `@amz.indica` / `amz.indica`.
- [x] **PlatformLinkResolverTests** — `redirect_url` codificado com `unique_id=amz.indica`.

### Fase 19.23 — Extração de metadados do produto (19.5)

- [x] **ShortAffiliateLink.cs** — `ProductName`, `ProductPrice`, `ProductImageUrl`.
- [x] **20260924233155_AddProductMetadataToLinks** — colunas em `ShortAffiliateLinks`.
- [x] **ProductMetadataHtmlParser.cs** / **ProductMetadataService.cs** / **IProductMetadataService** — unshorten + OpenGraph/JSON-LD + fallback de slug.
- [x] **AffiliateLinkGenerationService** — persiste metadados sem bloquear a conversão.
- [x] **LinkGeneratorResultPanel.razor** / **HistoricoLinks.razor** — card preview; colunas Produto e Preço (R$).
- [x] **ProductMetadataHtmlParserTests.cs** / **ProductMetadataServiceTests.cs**.

### Fase 19.24 — Resolução inteligente de loja no gerador

- [x] **IntegracaoLojaScopeResolver.cs** — `MarketplaceAccounts` ativas por plataforma; fallback quando o seletor é Todas ou a loja ativa é de outro marketplace.
- [x] **GeradorLinks.razor** / **ActiveStoreScopeSelector.razor** — opção Todas as lojas e CTA para conectar conta inexistente.
- [x] **IntegracaoLojaScopeResolverTests.cs**.

### Fase 19.25 — Nome e preço do produto (Shopee slug/OG)

- [x] **ProductMetadataHtmlParser.cs** — `UrlDecode` do slug, limpa `| Shopee Brasil` / `| Mercado Livre` e lê `"price": [0-9.]+`.
- [x] **ProductMetadataServiceTests.cs** — Lovito Casual Sutiã e `28.70` / `R$ 28,70`.
- [x] **ProductMetadataHtmlParser** — slug Shopee (`-i.shop.item`) como nome primário via `HttpUtility.UrlDecode`; descarta títulos anti-bot; HttpClient iPhone Safari.
- [x] **ProductMetadataHtmlParser** / **ProductMetadataService** — slug da URL expandida também para Magalu (`/p/`) e Mercado Livre; preço em `product:price:amount` / query `price_min`; preview e histórico via `FormatBrl` (`R$ 28,70`).
- [x] **ProductMetadataHtmlParser** — Regex `shopee.com.br/([^/]+)-i.\\d+.\\d+`; descarta slug só numérico (`25901538592`).
- [x] **ProductMetadataService** — blacklist `Opaanlp`/`Nsbo`/`captcha`; HttpClient iPhone Safari 17; unwrap `target=`/`redirect=`.
- [x] **ProductMetadataService** — API `shopee.com.br/api/v4/item/get?itemid&shopid`; preço micro-unidades `/ 100000000`; fallback slug.
- [x] **ProductMetadataService** — pipeline `ExtractMetadataAsync` por plataforma (`ExtractShopeeMetadata` inclui `/product/{shop}/{item}`).
- [x] **ProductMetadataService** — `CookieContainer` + warm-up `GET https://shopee.com.br/` e headers Chrome 122 antes da API de item.
- [x] **ProductMetadataHtmlParser** / **ProductMetadataService** — bloqueio rígido de títulos genéricos (`Produto`, `Shopee`, < 3 caracteres); persistência `NULL` em `ShortAffiliateLinks.ProductName`.
- [x] **ShopeeService.cs** / **IShopeeService** — fallback `product/get_item_base_info` com AppKey/AppSecret de `MarketplaceAccounts`.
- [x] **UpdateAffiliateProductMetadataDto.cs** — payload de edição manual de nome/preço.
- [x] **GeradorLinks.razor** — modal do lápis preservado (checkboxes de contas); `OnEditClick` pré-preenche `editingModel.ProductName`/`ProductPrice`; disquete persiste metadados e `StateHasChanged` no histórico.
- [x] **AffiliateLinksController** — `PUT/PATCH api/afiliados/links/{id}/metadata`.
- [x] **ShopeeServiceTests.cs** / **ProductMetadataServiceTests** — rejeição de `Produto` e Open API sem credenciais.

### Fase 19.11 — Cores institucionais dos marketplaces

- [x] **app.css** — tokens `--brand-*`, classes `.badge-*` / `.btn-*` e sombra de seleção na cor da marca.
- [x] **ConnectStoreModal.razor** / **GeradorLinks** chips — Shopee, TikTok, ML, Amazon, Magalu, Kabum e Casas Bahia padronizados.

### Fase 19.3.2 — Cópia e compartilhamento

- [x] **tecflow-clipboard.js** — `copyText` com Clipboard API e fallback `execCommand`.
- [x] **LinkGeneratorResultPanel.razor** — três cards de cópia: afiliado longo, encurtado Shopee (`br.shp.ee`) e rastreio TecFlow.
- [x] **AffiliateShareLinkBuilder.cs** — URIs `api.whatsapp.com/send` e `t.me/share/url` com URL encoding.
- [x] **AffiliateShareLinkBuilderTests.cs** — encoding de espaços, `&` e query string.

### Login homologação — filtro de tenant

- [x] **AppDbContext.ApplyTenantQueryFilters** — lambdas de instância (`CurrentTenantId == null` visível no login).
- [x] **TenantQueryFilterExtensions.cs** — documentação: não capturar `ICurrentTenantService` no modelo EF.
- [x] **UserAccountRepository.GetByEmailAsync** — `IgnoreQueryFilters` na busca por e-mail.
- [x] **TecFlow.Tests/Unit/MultiTenancy/TenantQueryFilterTests.cs** — visível sem tenant; isolado com tenant.

### OAuth Minhas Lojas / Integrações

- [x] **ConnectStoreModal.razor** — `OnLojaVinculada` recarrega a lista após vínculo com sucesso.
- [x] **MinhasLojas.razor** — `ObterLojasAsync()` + `StateHasChanged()` e `StoreScope.RefreshStoresAsync()`.
- [x] **ActiveStoreScopeService** — `EnsureInitializedAsync` recarrega lojas; `OnStoreChanged` após cada leitura.
- [x] **MarketplaceAccountRepository** — listagens e `GetByShopAsync` com `AsNoTracking()`.
- [x] **MarketplaceOAuthConnectService.cs** — ticket pendente + GET `api/marketplace-auth/{plataforma}/authorize-url`.
- [x] **MarketplaceAuthController** — rota `{plataforma}/authorize-url` e DTO `authorizeUrl`.
- [x] **TecFlow.Tests/Unit/SharedUi/MarketplaceOAuthConnectServiceTests.cs** — path, DTO e erro da API.
- [x] **ShopeeAuthorizationUrlFactory.cs** — PartnerId/PartnerKey sandbox quando vazios (sem HTTP 500).
- [x] **MarketplaceAuthController** — try/catch em authorize-url; Shopee devolve 200 com URL sandbox.
- [x] **appsettings.json / appsettings.Homologacao.json** — `Integrations:Shopee` com PartnerId/PartnerKey padrão de homologação.
- [x] **HomologMarketplaceAuth.cs** — prefixo `code_*` ou ambiente Dev/Homologação pula a Shopee e persiste tokens stub.
- [x] **ConnectStoreModal.razor** — botão `type="button"` `@onclick="VincularManualmenteDirect"` chama a API direto; Shop ID/`code_teste` com fallback.
- [x] **IntegracaoLojaService / MarketplaceAuthService** — vínculo manual homologa sem HTTP remoto e devolve mensagem de sucesso de homologação.
- [x] **IntegracaoLojaApiService** — log do payload bruto se o envelope não deserializar; trata ProblemDetails.
- [x] **TecFlow.Tests/Unit/Integrations/IntegracaoLojaServiceTests.cs** — vínculo manual simulado.
- [x] **HttpServiceVincularManualTests.cs** — POST `vincular-manual` com JSON real e falha de desserialização.
- [x] **AccountSecurityApiServiceTests.cs** — envelope inválido sem exceção.
- [x] **IntegracaoLojaDto.cs** / **MarketplaceTypeJsonConverter.cs** — JSON camelCase + ShopId string/long.
- [x] **IntegracoesController.cs** — log de ModelState no 400; homolog aceita `code_teste` / `123456`; `LinkAsync` devolve JSON 500 `ResponseDto`.
- [x] **MarketplaceAuthController** — `POST vincular-manual` com try/catch global; 500 JSON `ResponseDto.Fail("Erro do Servidor/SQL: ...")`.
- [x] **WebAccessTokenProvider / IntegracaoLojaApiService / HttpService** — Bearer JWT via cookie + AuthenticationStateProvider em `vincular-manual` e `lojas`.
- [x] **MarketplaceAuthController.vincular-manual** — `[AllowAnonymous]` + FallbackUserId=1 no IIS; JWT Bearer alinhado a Jwt:Key/Issuer.
- [x] **IntegracaoLojaApiService / HttpService** — HTML/500 bruto no modal; corpo vazio/`HttpRequestException` vira alerta CORS/porta 5001.
- [x] **MarketplaceAuthControllerTests.cs** — vínculo manual 400/500 JSON.
- [x] **MarketplaceAccountDto / MarketplaceAccountResponseDto** — envelope de vínculo sem entidade EF.
- [x] **TecFlowJsonOptions** — case-insensitive + números em string; log de JsonException.
- [x] **AuthControllerSecurityTests / AffiliateLinksControllerTests / DashboardControllerTests** — 401/500 e JSON de formulário.

### Fase 19.26 — Página pública de conversão (slug versionado)

- [x] **PublicConverterPage.cs** / **PublicConverterPages** — versionamento de slug (`IsActive=false` no antigo, novo registro com o mesmo `PublicCode`).
- [x] **PublicConverter.razor** — aceita slug ativo e inativo; badges `DistinctBy(PlatformType)` compactos (~35%); Converter + Limpar; resultados em duas linhas (direto e rastreio TecFlow) com Copiar/Abrir.
- [x] **app.css** — `.public-converter-platforms` (padding `0.4rem 0.6rem`, min/max-width e ícones menores).
- [x] **MinhasPaginasPublicas.razor** — histórico de slugs com badge Inativo.
- [x] **ShortAffiliateLinks.Source** — conversão pública grava `PublicPage` no `UserId` do afiliado e aparece no Histórico de Links.
- [x] **PublicConverterRulesTests.cs** — slug inativo e deduplicação de plataformas.

### Fase 23.1 — Sessão WhatsApp (Evolution API)

- [x] **WhatsAppIntegration.cs** / **WhatsAppIntegrations** — sessão por `UserId` (`InstanceName`, `ConnectionStatus`, telefone, perfil, `LastConnectedAt`).
- [x] **IEvolutionApiService** / **EvolutionApiService.cs** — `CreateInstanceAsync`, `FetchQrCodeAsync`, `GetConnectionStateAsync`.
- [x] **WhatsAppSessionService.cs** / **WhatsAppIntegrationController** — persistência + QR + polling de status.
- [x] **WhatsAppConexao.razor** (`/integracoes/whatsapp`) — card Desconectado / Aguardando Leitura / Conectado.
- [x] **NavMenu.razor** — dropdown Integrações com Minhas lojas e WhatsApp.
- [x] **WhatsAppSessionRulesTests.cs** — instância isolada por usuário e mapeamento open/close/connecting.
- [x] **20260930235426_AddWhatsAppIntegrations** — tabela `WhatsAppIntegrations` no SQL Server.

### Fase 23.2 — Bot WhatsApp (webhook e resposta)

- [x] **WhatsAppWebhookController.cs** — `POST /api/v1/integrations/whatsapp/webhook` (`MESSAGES_UPSERT`, ignora `fromMe`).
- [x] **WhatsAppMessageProcessor.cs** — regex de URLs, `PlatformLinkResolver`, `Source=WhatsAppBot`.
- [x] **EvolutionApiService.SendTextMessageAsync** — `POST /message/sendText/{instance}` em até 3s.
- [x] **WhatsAppIntegration** — flags `EnableAutoConvertBot`, `ReplyToPrivateMessages`, `ReplyToGroupMessages`.
- [x] **WhatsAppConexao.razor** — switches do bot persistidos no SQL Server.
- [x] **WhatsAppBotRulesTests.cs** / **WhatsAppWebhookControllerTests.cs** — URLs, fromMe e grupos.
- [x] **20261001001427_AddWhatsAppBotPreferences** — colunas de preferência do bot em `WhatsAppIntegrations`.

### Fase 23.3 — Disparo e agendamento para grupos

- [x] **WhatsAppGroup.cs** / **WhatsAppBroadcastCampaign.cs** — grupos (`Jid`, admin) e campanhas (`Pending/Processing/Completed/Failed`).
- [x] **EvolutionApiService** — `FetchUserGroupsAsync`, `SendMediaMessageAsync`.
- [x] **WhatsAppBroadcastWorker.cs** — BackgroundService no TecFlow.API com `IntervalSeconds` ≥ 15s.
- [x] **WhatsAppAgendador.razor** — sincronizar grupos, multi-check, `[LINK_COMISSAO]`, agenda e tabela.
- [x] **WhatsAppBroadcastRulesTests.cs** — intervalo anti-bloqueio e tag de comissão.
- [x] **20261001003616_AddWhatsAppBroadcastCampaigns** — tabelas `WhatsAppGroups` e `WhatsAppBroadcastCampaigns`.

### Segurança defensiva Telegram/WhatsApp

- [x] **DataEncryptionService.cs** — AES-256 (`ENC1:`) em `TecFlow.Infrastructure/Security`; `IEncryptionService` no DbContext.
- [x] **WhatsAppIntegration** / **TelegramIntegration** — `Token`, `ApiKey`, `SessionData` com `EncryptedStringConverter`.
- [x] **WebhookSecurityAttribute.cs** / **WebhookSecurityFilter** — header `X-Webhook-Secret` fail-closed.
- [x] **WhatsAppWebhookController** / **TelegramWebhookController** — `[WebhookSecurity]` em `POST .../webhook`.
- [x] **IntegrationOwnershipGuard** — `UserId` da integração vs usuário atual nos serviços de sessão, disparo e webhook.
- [x] **WhatsAppConexao.razor** / **TelegramConexao.razor** — tokens mascarados `****************`.
- [x] **MessagingIntegrationSecurityTests.cs** — round-trip AES, secret de webhook e isolamento.
- [x] **20261001005817_EncryptMessagingIntegrationSecrets** — colunas criptografáveis WhatsApp + tabela `TelegramIntegrations`.

### Fase 22 — Telegram BotFather, conversão e agendador

- [x] **TelegramApiService.cs** — `GetMeAsync` / `SetWebhookAsync` / `SendTextMessageAsync` (Telegram.Bot).
- [x] **TelegramConexao.razor** — BotToken + ChatId, validação e status `Conectado ✅`.
- [x] **TelegramWebhookController** / **TelegramMessageProcessor** — URLs, `PlatformLinkResolver`, `Source=TelegramBot`, resposta em até 2s.
- [x] **TelegramBroadcastCampaign** / **TelegramAgendador.razor** / **TelegramBroadcastWorker**.
- [x] **TelegramBotRulesTests** / **TelegramWebhookControllerTests** / **TelegramBroadcastRulesTests**.
- [x] **20261001012038_AddTelegramBroadcastCampaigns** — tabela `TelegramBroadcastCampaigns`.

### Painel unificado de conexões

- [x] **Conexoes.razor** (`/integracoes/conexoes`) — abas WhatsApp (Evolution) e Telegram (BotFather).
- [x] Modal de QR WhatsApp com recarga dinâmica e card de status (badge, foto, nome, número).
- [x] Formulário Telegram Bot Token + Chat ID com validação `GetMeAsync` e `X-Webhook-Secret` mascarado.
- [x] **WhatsAppIntegration.ProfilePictureUrl** — foto do perfil no card de status.
- [x] **20261001013152_AddWhatsAppProfilePictureUrl** — coluna `ProfilePictureUrl` em `WhatsAppIntegrations`.

### Navegação unificada

- [x] **NavMenu.razor** — Dashboard (`/`), Gerador de Links, **Minhas Lojas / Integrações** visível em `/marketplace-accounts`, Minhas Páginas Públicas; grupo **Mensageria & Bots** (Conexões, Bot de Conversão, Agendador) sem remover rotas antigas de WhatsApp/Telegram.
- [x] **BotConversor.razor** (`/integracoes/bot-conversor`) e **Agendador.razor** (`/integracoes/agendador`).
- [x] **MinhasLojas.razor** — aliases `/integracoes/lojas` e `/marketplace-accounts`.
- [x] **Conexoes.razor** — rota `/integracoes/conexoes` mantida.
- [x] **Conexoes.razor** — botão Validar e Conectar Bot com `_isLoading`/spinner, recusa token vazio/`*****` e modal BotFather.
- [x] **TelegramIntegrationService.cs** — webhook tolerante: salva **Conectado (Modo Disparo)** se `SetWebhookAsync` falhar e houver Chat ID.
- [x] **TelegramApiService.SetWebhookAsync** — relança falha do Telegram; o serviço captura, loga e persiste as credenciais.
- [x] **MinhasPaginasPublicas.razor** — `_isLoading` + try/catch em `OnInitializedAsync` para não ficar em branco sem registros.

### Resiliência Evolution API

- [x] **appsettings.json / Homologacao** — seção `EvolutionApi` (`BaseUrl`, `ApiKey`).
- [x] **EvolutionApiService** — try/catch em create/QR/state; log do body HTTP 40x/50x; instância já existente segue para `FetchQrCodeAsync`.
- [x] **WhatsAppSessionService** / **WhatsAppIntegrationController** — falha de conexão retorna mensagem amigável (sem 500 genérico).
- [x] **Conexoes.razor** / **WhatsAppConexao.razor** — alerta amigável quando a Evolution API está indisponível.
- [x] **WhatsAppSessionRules.ResolveConnectUiMessage** — mapeia 500 genérico para texto de UI.

### Agendador de grupos — formulário de campanha

- [x] **CommissionLinkPicker.razor** — typeahead com badges oficiais de plataforma (`PlatformBadgeHelper`).
- [x] **PlatformBadgeHelper.cs** — `GetPlatformBadgeClass` / `GetPlatformColor` centralizados para Amazon, ML, Shopee, Magalu, TikTok, Hotmart, Braip e padrão.
- [x] **WhatsAppAgendador.razor** — ordem Título → Mensagem → seletor → Link de Comissão; bloqueia URL no copy; concatena no disparo.
- [x] **WhatsAppBroadcastRules.ComposeDispatchMessage** — `"{Mensagem}\n\n{LinkUrl}"` na Evolution API.
- [x] **WhatsAppGroupPicker.razor** — MultiSelect com chips, atalhos 5/10/15/20 (LocalStorage) e filtro “sou Admin”.
- [x] **EvolutionApiService.DetectAdmin** — `IsAdmin` só se o número conectado for admin/superadmin do grupo.
- [x] **TelegramAgendador.razor** — mesmo seletor e reset após agendar.
- [x] **app-interop.js** — `insertAtCursor` e `scrollToTop` no formulário de campanha.

### Agendador WhatsApp — lista de disparos

- [x] **WhatsAppAgendador.razor** — Ações sempre clicáveis (`EditarAgendamento` / `ConfirmarExclusao`), sem `disabled`; scroll `scrollToTop`.
- [x] **IModalService.cs** / **ModalService.cs** — confirmação reutilizável sem `window.confirm`.
- [x] **ConfirmDialogHost.razor** — modal Bootstrap global no `MainLayout` / `PublicLayout`.
- [x] **ConfirmModal.razor** — modal de confirmação por parâmetros (`IsOpen`, `OnConfirm`, `OnCancel`) fora da tabela.
- [x] **UpdateAgendamentoCommand.cs** — comando PUT de atualização de agendamento pendente.
- [x] **WhatsAppBroadcastJobCoordinator.cs** — cancela job em processamento ao excluir/editar.
- [x] **WhatsAppBroadcastController** — `PUT /api/integracoes/whatsapp/campanhas/{id}`, `DELETE` cancela o worker.
- [x] **WhatsAppBroadcastService** — `UpdateAsync` / `DeleteCampaignAsync` com nomes dos grupos e ordenação por mais recente.
- [x] **WhatsAppBroadcastRules.SplitDispatchMessage** — separa copy e URL ao editar.
- [x] **WhatsAppBroadcastRules.ResolveEditScheduledAt** — na edição, preserva data futura e ajusta passada para +10 minutos.

### Imagem do produto (captura, edição e preview)

- [x] **ProductMetadataHtmlParser.cs** — `og:image`, `twitter:image`, JSON-LD e `itemprop=image`; `NormalizePersistedProductImageUrl`.
- [x] **UpdateAffiliateProductMetadataDto.cs** — `ProductImageUrl` persistido em `UpdateProductMetadataAsync`.
- [x] **ShortLinkService.cs** — grava/limpa `ProductImageUrl` na conversão e na edição manual.
- [x] **AffiliateLinksController.cs** — PUT/PATCH metadata devolve a URL da imagem.
- [x] **ProductImagePreview.razor** — input, thumbnail 96×96, placeholder e Remover Imagem.
- [x] **LinkGeneratorResultPanel.razor** / **GeradorLinks.razor** / **HistoricoLinks.razor** — preview e edição da imagem.
- [x] **WhatsAppAgendador.razor** — `OnCommissionLinkItemSelected` preenche `model.ImageUrl`.
- [x] **CommissionLinkPicker.razor** — miniatura nas sugestões do autocomplete.

### Menu lateral (NavMenu)

- [x] **NavMenu.razor** — remove duplicata de Minhas Lojas; grupos Mensageria, WhatsApp, Telegram, Integrações e accordion Próximos Desenvolvimentos; ícones Bootstrap e item ativo em cor primária.
- [x] **app.css** — destaque `active` com barra primária, chevron e badge “Em breve”.
- [x] **NavMenu.razor** — toggle `isProximosDesenvolvimentosOpen` com `@onclick:preventDefault`; abre só na rota do grupo, sem reabrir ao clicar para fechar.
- [x] **NavMenu.razor** — cabeçalho `button` + `ToggleProximosSubmenu`/`StateHasChanged`; 8 subitens em `@if (isProximosOpen)` com rotas reais.
- [x] **NavMenu.razor** — `InteractiveServer` (prerender off) para o `@onclick` dos accordions funcionar; sem `EventCallback` do layout estático.
- [x] **MainLayout.razor** / **app-interop.js** — hamburger e backdrop via `toggleSidebar`/`closeSidebar`.

### Módulo 27 — Grupos monitorados

- [x] **GroupCapturedMessage.cs** / **GroupOfferStatuses.cs** — persistência de ofertas capturadas.
- [x] **GroupOfferCaptureService.cs** / **OfferValidationService.cs** / **MonitoredGroupService.cs**.
- [x] **MonitoredGroupsController.cs** — listar, sincronizar, validar e clonar; try/catch + log + UserId fallback 1.
- [x] **InfrastructureDataServiceRegistrationExtensions.cs** — `IUserContextProvider` / `UserContextProvider` no DI.
- [x] **MonitoredGroupService.cs** — sync isolado WhatsApp/Telegram; lista inclui `TelegramGroups`.
- [x] **GruposMonitorados.razor** — `/integracoes/whatsapp/grupos/monitorados` e `/integracoes/telegram/grupos/monitorados`; legado redireciona ao WhatsApp.
- [x] **NavMenu.razor** — Grupos monitorados no submenu WhatsApp e no submenu Telegram.
- [x] **WhatsAppMessageProcessor.cs** / **TelegramMessageProcessor.cs** — captura em grupos.
- [x] **WhatsAppAgendador.razor** — query `cloneTitle/cloneMessage/cloneImage/cloneLink`.
- [x] **WhatsAppAgendador.razor** / **TelegramAgendador.razor** — badge oficial (ícone + nome) ao lado de “Disparo para grupos”.
- [x] **TelegramGroup.cs** / **TelegramBroadcastService.cs** — sync `GetUpdates`/`GetChat`, multi-select e disparo com intervalo.
- [x] **TelegramGroupPicker.razor** — chips com `GroupName` + Chat ID, busca e atalhos Selecionar Todos / Limpar.
- [x] **AddTelegramGroupsAndMultiChat** — tabela `TelegramGroups` e `TargetChatIdsJson` nas campanhas.
- [x] **AddTelegramUserBotCredentials** — `UserBotApiId`, `UserBotApiHash`, `UserBotPhone` em `TelegramIntegrations`.
- [x] **TelegramUserMonitorRules.cs** / **TelegramUserMonitorRulesTests.cs** — regex HTTP ampla; filtro Shopee, ML, Amazon, AliExpress, Magalu, Casas Bahia, Pelando, Promobit e encurtadores; `api_id` numérico e PIN de 5 dígitos.
- [x] **UserBotHelpModal.razor** — tutorial de 7 passos, badges Bot Token/Chat ID e Copiar exemplo.
- [x] **Conexoes.razor** — badges amarelo/azul no bloco UserBot e abre `UserBotHelpModal`.
- [x] **TelegramUserBotSessionStore.cs** / **TelegramUserBotCodeStore.cs** — sessão em `App_Data/telegram-sessions` (fallback TEMP) e código de verificação.
- [x] **TelegramUserBotSessionStoreTests.cs** — garante pasta `telegram-sessions` resolvida sem falhar o processo.
- [x] **TelegramUserMonitorWorker.cs** / **TelegramUserMonitorHost.cs** — catch-up por `chats.Values` + `GetHistory` paginado (200) e fila `Channel`; sync da tela dispara `CatchUpUserAsync`.
- [x] **TelegramIntegrationController.cs** — `POST userbot/solicitar-codigo` e `POST userbot/confirmar` (Login MTProto em duas etapas).
- [x] **GruposMonitorados.razor** — cards das ofertas de canais de terceiros, botão Clonar Oferta e “Carregar mais 50”.
- [x] **MonitoredGroupsController.cs** / **MonitoredGroupService.cs** — listagem paginada (`skip`/`take`, sem `Take(120)`).

---

## 📌 Prioridade sugerida (ordem de execução)

| # | Item | Risco |
|---|------|-------|
| 1 | Deletar interfaces fantasma em `Infrastructure.Services/Interfaces` | Alto — confusão em refactors |
| 2 | Corrigir namespace `ExceptionMiddleware` no Core | Médio |
| 3 | Renomear/remover `Application/AuthController.cs` stub | Médio |
| 4 | Consolidar DI (`*RegistrationExtensions`) | Médio |
| 5 | Alinhar Migrations com `TecFlow.Database` | Alto — schema/EF |
| 6 | `dotnet clean` + `.gitignore` para bin/obj/Tecso.* | Baixo — higiene Git |
| 7 | Unificar `ValidationHelper` | Baixo |
| 8 | WebUi usar DTOs de Business | Concluído |

---

*Gerado por varredura automatizada dos arquivos `.cs` e `.csproj` (excluindo `bin/` e `obj/`). Revisar checkboxes conforme cada item for concluído.*
