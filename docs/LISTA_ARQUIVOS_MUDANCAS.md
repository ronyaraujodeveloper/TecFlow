# 📋 LISTA EXECUTIVA: ARQUIVOS A MOVER/CRIAR/DELETAR

**Última varredura:** 4 de junho de 2026 (Fase 7.1 — Multi-Tenant)  
**Workspace:** `c:\Programacao\Tecso.AutomacaoCusor` (pasta ainda com prefixo *Tecso*; projetos já renomeados para *Ofertio*)  
**Solution:** `Ofertio.sln` — 12 projetos `Ofertio.*` + `Tecso.LerArquivos` externo (Portal e Dashboard **removidos**)

> **Nota de varredura:** Esta lista deve ser usada para eliminar **resíduos das pastas antigas** (*Tecso* / camadas pré-refatoração) que ainda geram conflitos de compilação — por exemplo, cópias de `ExceptionMiddleware` na API, interfaces fantasma em `Infrastructure.Services/Interfaces`, artefatos `bin/`/`obj/` versionados e namespaces legados (`Ofertio.API.Middlewares` em arquivos do Core). Priorize itens da seção **🚨 Conflitos** antes de novas features.

**Navegação:** [« Índice Completo](./INDICE_COMPLETO.md) · [README principal](../README.md)

Use esta lista como painel de controle para garantir que nenhuma classe antiga ficou duplicada e que todos os namespaces estejam nos projetos corretos.

---

## 🚨 1. Conflitos e Arquivos Duplicados (Urgente)

### Resolvido recentemente

- [x] **ExceptionMiddleware.cs** — Havia cópia em `Ofertio.API/Middleware/` e implementação em `Ofertio.Core/Exceptions/`. **Ação concluída:** middleware legado removido do Core; `Ofertio.API/Middlewares/ExceptionHandlingMiddleware.cs` retorna `ProblemDetails` JSON.

### Interfaces duplicadas (compilam, mas confundem DI e manutenção)

- [ ] **ITikTokShopApi.cs** — `Ofertio.Business/Interfaces/Services/` (canônico, em uso) e `Ofertio.Infrastructure.Services/Interfaces/` (cópia legada, **excluída do compile** no `.csproj`). (Ação: deletar o arquivo em `Infrastructure.Services/Interfaces` e remover `<Compile Remove>` do csproj).

- [ ] **IShopeeApi.cs** — `Ofertio.Business/Interfaces/Services/` (canônico) e `Ofertio.Infrastructure.Services/Interfaces/` (legado, **Compile Remove**). (Ação: deletar cópia legada + limpar csproj).

- [ ] **ITikTokAdsApiService.cs** — `Ofertio.Business/Interfaces/Services/` (canônico) e `Ofertio.Infrastructure.Services/Interfaces/` (namespace `Ofertio.Core.Interfaces.Services`, **Compile Remove**). (Ação: deletar cópia legada; manter interface só em Business até mover para Core se for o alvo arquitetural).

- [ ] **ValidationHelper.cs** — `Ofertio.Business/Service/ValidationHelper.cs` e `Ofertio.Util/Validation/ValidationHelper.cs`. (Ação: unificar em `Ofertio.Util`; Business referencia Util ou deleta a cópia local.)

### Mesmo nome de classe, hosts diferentes (não é CS0436, mas exige disciplina)

- [ ] **CampaignsController.cs** — `Ofertio.API/Controllers/` e `Ofertio.Orquestrador/Controllers/`. (Ação: manter ambos se forem APIs distintas; documentar rotas; evitar lógica duplicada — extrair para `Ofertio.Business`).

- [ ] **MetricsController.cs** — API e Orquestrador. (Ação: idem.)

- [ ] **DashboardController.cs** — API e Orquestrador. (Ação: idem.)

- [ ] **UserAccountsController.cs** — API e Orquestrador. (Ação: idem.)

### DTOs espelhados (Portal vs Business)

- [x] **MetricDto.cs**, **CampaignDto.cs**, **DashboardSummaryDto.cs** — DTOs locais removidos de `Ofertio.Portal/Models/Responses/` e `Ofertio.WebUi/Models/Responses/`; UI consome `Ofertio.Business/Dto/` (`*ResponseDto`, `DashboardSummaryDto`) e entidades `Ofertio.Core.Entities` (`Campaign`, `Metric`).

### Migração Portal → WebUi (Fase 3)

- [x] **Ofertio.WebUi/** — projeto Blazor canônico na solution com referência a `Ofertio.Business`.
- [x] **DashboardApiService.cs** (WebUi) — deserializa `CampaignResponseDto` / `MetricResponseDto` e expõe `DataList` para os widgets.
- [x] **CampaignExtensions.cs** — helper `IsActive()` para entidade `Campaign`.
- [x] DTOs locais removidos: `CampaignDto.cs`, `MetricDto.cs`, `DashboardSummaryDto.cs` (WebUi).
- [x] **Ofertio.Portal/** — removido da solution e excluído do disco (substituído por WebUi).
- [x] **Ofertio.Dashboard/** — removido da solution e excluído do disco (scaffold MVC obsoleto).

### WebUi — arquitetura Filter / Dto / ResponseDto (Fase 3)

- [x] **Ofertio.WebUi.csproj** — referência explícita a `Ofertio.Database` (tipos `*Filter`).
- [x] **Extensions/FilterQueryStringExtensions.cs** — serializa `*Filter` em query string para GET.
- [x] **Extensions/ResponseDtoExtensions.cs** — valida `Status`/`Descricao` dos envelopes na UI.
- [x] **Services/Http/HttpService.cs** — `GetAsync(url, filter)`, `PutAsync` para POST/PUT com Dto; 401 sem corpo vira mensagem de sessão (não CORS).
- [x] **Services/Dashboard/DashboardApiService.cs** — `Get*ByFilterAsync(CampaignFilter|MetricFilter)`, `Create*Async(CampaignDto|MetricDto)`.
- [x] **Components/Dashboard/CampaignFilterForm.razor** — data binding → `CampaignFilter`.
- [x] **Components/Dashboard/MetricFilterForm.razor** — data binding → `MetricFilter`.
- [x] **Components/Dashboard/CampaignCreateForm.razor** — formulário POST com `CampaignDto`.
- [x] **Components/Dashboard/CampaignsWidget.razor** / **MetricsWidget.razor** — leem `*ResponseDto.DataList` diretamente.
- [x] **Components/Pages/Dashboard.razor** — orquestra filtros, listagens e criação padronizados.

### Fase 6.4 — Observabilidade e telemetria (jun/2026)

- [x] **Ofertio.Observability/** — `AddOfertioTelemetry`, `UseOfertioTelemetry`, `OfertioBusinessMetrics`, `TelemetryRecentErrorRecorder`, `TelemetryErrorRecordingMiddleware`.
- [x] **Pacotes:** OpenTelemetry (Hosting, AspNetCore, Http, Runtime), OTLP, Console, Prometheus.AspNetCore, Serilog.Sinks.Seq.
- [x] **Ofertio.API/Program.cs**, **Ofertio.Worker/Program.cs**, **Ofertio.Orquestrador/Program.cs** — telemetria ativada por host.
- [x] **appsettings.json** (API, Worker, Orquestrador) — seção `Telemetry`.
- [x] **Ofertio.Infrastructure.Services/Health/PlatformHealthService.cs** — health checks DB/RabbitMQ/Shopee/TikTok.
- [x] **Ofertio.Orquestrador/Controllers/HealthDashboardController.cs** — `GET /api/saude/dashboard`.
- [x] **Ofertio.SharedUi/Components/Pages/PainelSaude.razor**, **Components/Health/HealthStatusCard.razor**.
- [x] Instrumentação: **SocialMediaCommentConsumer**, **AffiliateAnalyticsService**.

### Fase 7.3 — Controle avançado de estoque físico (jun/2026)

- [x] **Ofertio.Core/Enums/InventoryMovementType.cs** — EntradaPorCompra, SaidaPorVenda, AjusteManual, Reserva, CancelamentoReserva.
- [x] **Ofertio.Core/Entities/Inventory.cs** — PhysicalQuantity, ReservedQuantity, AvailableQuantity (calculado), MinimumStock.
- [x] **Ofertio.Core/Entities/InventoryMovement.cs** — kardex com SalesOrderId.
- [x] **Ofertio.Business/Interfaces/Inventory/IInventoryService.cs** — ReserveStock, ConfirmStockDebit, ReleaseStockReservation.
- [x] **Ofertio.Business/Interfaces/Inventory/IInventoryAlertHook.cs** — gancho para push (Fase 4).
- [x] **Ofertio.Business/Domain/Inventory/InsufficientStockException.cs**.
- [x] **Ofertio.Infrastructure.Services/Stock/InventoryService.cs** — transações Serializable + retry de concorrência.
- [x] **Ofertio.Infrastructure.Services/Stock/LoggingInventoryAlertHook.cs** — log estruturado de estoque mínimo.
- [x] Integração: **SalesOrderService** (reserva na criação; débito em Pago; liberação em Cancelado), **InvoiceOrchestrator** (débito idempotente).
- [x] **Ofertio.API/Controllers/InventoryController.cs** — `api/estoque`.
- [x] **Ofertio.Infrastructure/Migrations/20260604141216_AddPhysicalInventory.cs**.

### Fase 7.2 — Core de Vendas, Faturamento e ERP Local (jun/2026)

- [x] **Ofertio.Core/Enums/OrderStatus.cs** — Pendente, Pago, Faturado, Enviado, Concluido, Cancelado.
- [x] **Ofertio.Core/Entities/Customer.cs** — cliente com endereço completo e `TenantId`.
- [x] **Ofertio.Core/Entities/SalesOrder.cs** — pedido de venda (`OrderNumber`, totais, `ShopId`, status).
- [x] **Ofertio.Core/Entities/SalesOrderItem.cs** — itens do pedido.
- [x] **Ofertio.Business/Domain/Sales/OrderStateMachine.cs** — transições rígidas de estado.
- [x] **Ofertio.Business/Interfaces/Sales/IInvoiceOrchestrator.cs** — `PrepareInvoiceAsync` + payload NF-e mockado.
- [x] **Ofertio.Infrastructure.Services/Sales/InvoiceOrchestrator.cs**, **SalesOrderService.cs**.
- [x] **Ofertio.API/Controllers/CustomersController.cs** — `api/vendas/clientes`.
- [x] **Ofertio.API/Controllers/SalesOrdersController.cs** — `api/vendas/pedidos`, `PUT .../status`, `POST .../faturar`.
- [x] **Ofertio.Database/Filter/** — `CustomerFilter`, `SalesOrderFilter`.
- [x] **Ofertio.Business/Dto/** — `CustomerDto`, `SalesOrderDto`, `InvoicePayloadDto`, envelopes `*ResponseDto`.
- [x] **Ofertio.Infrastructure/Migrations/20260604140643_AddSalesOrderCore.cs**.
- [x] **Ofertio.Tests/Unit/Sales/OrderStateMachineTests.cs**.

### Fase 7.1 — Multi-Tenant / Multi-Conta Marketplace (jun/2026)

- [x] **Ofertio.Core/Entities/Tenant.cs** — inquilino corporativo (assinante SaaS).
- [x] **Ofertio.Core/Entities/MarketplaceAccount.cs** — vínculo Tenant + ShopId + tokens; `TrackingId`/`ShopId`/`AppKey`/`AppSecret` anuláveis para registros antigos.
- [x] **Ofertio.Business/Mappings/MarketplaceAccountMapper.cs** — projeção nula-segura para `MarketplaceAccountDto` e `ConvertLinkResponseDto`.
- [x] **Ofertio.Infrastructure.Services/Integrations/IntegracaoLojaService.cs** — se o UserId do JWT não existir em `Usuarios`, usa o primeiro usuário do SQL Server (ou cria o demo de homologação) antes de gravar `MarketplaceAccount.UserId`.
- [x] **Ofertio.Infrastructure.Services/Integrations/MarketplaceAccountService.cs** — `PrepareForPersistAsync` grava `TenantId` de `dbo.Tenants` e `UserId` de `dbo.Usuarios`.
- [x] **Ofertio.Tests/Unit/MultiTenancy/TenantProvisioningServiceTests.cs** — cria `Tenant Principal` quando `Tenants` está vazia.
- [x] **Ofertio.Data/Migrations/20260922233000_AddMarketplaceAccountOptionalCredentials.cs** — colunas opcionais `TrackingId`/`AppKey`/`AppSecret`.
- [x] **Ofertio.Data/Migrations/20260922250000_AddShortAffiliateLinkAffiliateUrlAndAccount.cs** — `AffiliateUrl` e `MarketplaceAccountId` em `ShortAffiliateLinks` (SQL Server).
- [x] **Ofertio.Infrastructure.Services/ShortLinks/ShortLinkService.cs** — persiste `ShortAffiliateLink` com `SaveChangesAsync()` no SQLEXPRESS.
- [x] **Ofertio.Tests/Unit/LinkStrategies/ShortLinkPersistenceTests.cs** — garante `SaveChanges` de `OriginalUrl`/`AffiliateUrl`/`Code`/`Platform`/`MarketplaceAccountId`.
- [x] **appsettings.Homologacao.json** — `Database:Provider=SqlServer` / `Ofertio` (mesmo banco do `appsettings.json`).
- [x] **Ofertio.Infrastructure/Migrations/20260922240000_AddMarketplaceAccountOptionalCredentialsPg.cs** — mesmas colunas no PostgreSQL do IIS (`Homologacao`) para evitar Npgsql `42703`.
- [x] **docs/EXECUCAO_HOMOLOGACAO.md** / **Publicar-Homologacao.ps1** — `dotnet ef database update` no SQL Server (`Ofertio.Data`) para Development e Homologacao IIS.
- [x] **docs/HISTORICO_FASES_CONCLUIDAS.md** — registro das Fases 1 a 31 e entregas `[x]`; o `README.md` ficou só com regras permanentes e Fases 32–34.
- [x] **Ofertio.Infrastructure/Migrations/20260920215452_AddMarketplaceAccountsTable.cs** — colunas `UserId`, `FriendlyName` e `IsActive` em `MarketplaceAccounts`.
- [x] **Ofertio.Data/** — assembly de migrations SQL Server (`SqlServerMigrations`, `20260920224523_InitialSqlServerMigration`).
- [x] **Ofertio.Database/Data/RelationalDatabaseOptions.cs** — `UseSqlServer` / `UseNpgsql` conforme `Database:Provider`.
- [x] **Ofertio.Core/Abstractions/ITenantScopedEntity.cs**, **IShopScopedEntity.cs** — contratos de isolamento.
- [x] **Ofertio.Core/Security/OfertioClaimTypes.cs** — claims `tenant_id`, `shop_id` (movido de SharedUi).
- [x] **TenantId** em: `UserAccount`, `Product`, `Campaign`, `Affiliate`, `Content`, `Conversion`, `Metric`, `MarketplaceToken`, `MarketplaceOrder`, `MarketplaceOrderLine`, `GlobalAdvertisingProduct`, `MarketplaceAffiliateLink`, `UserDeviceToken`.
- [x] **Ofertio.Database/MultiTenancy/** — `ICurrentTenantService`, `NullCurrentTenantService`, `TenantQueryFilterExtensions`, `TenantDbSetExtensions`.
- [x] **Ofertio.Database/AppDbContext.cs** — filtros globais, `SaveChanges` com `TenantId`, criptografia em `MarketplaceAccount`.
- [x] **Ofertio.Infrastructure/Security/CurrentTenantService.cs** — JWT + header `X-Ofertio-Shop-Id`.
- [x] **Ofertio.Infrastructure/Security/JwtTokenService.cs** — claim `Ofertio:tenant_id`; emite `aud=OfertioClient`.
- [x] **Ofertio.Infrastructure.Services/Tenancy/TenantProvisioningService.cs** — `EnsurePersistedTenantAsync` cria `Tenant Principal` se `dbo.Tenants` estiver vazia; `EnsureTenantForUserAsync` corrige `Usuarios.TenantId` órfão.
- [x] **Ofertio.Infrastructure.Services/Repositories/MarketplaceAccountRepository.cs** — listagem consolidada / por loja.
- [x] Repositórios adaptados: **ProductRepository**, **MarketplaceTokenRepository**, **MarketplaceOrderRepository** (`ListConsolidated*`, `ListForShop*`).
- [x] **Ofertio.Infrastructure.Services/Integrations/Auth/MarketplaceAuthService.cs** — persiste `MarketplaceAccount` no OAuth.
- [x] **Ofertio.Infrastructure/Migrations/20260604135726_AddMultiTenantArchitecture.cs** — schema + tenant padrão para dados legados.
- [x] **Ofertio.Database/Filter/** — `TenantFilter`, `MarketplaceAccountFilter`; **ProductFilter** + `ShopId`/`TenantId`.
- [x] **Ofertio.Business/Dto/** — `TenantDto`, `TenantResponseDto`, `MarketplaceAccountDto`, `MarketplaceAccountResponseDto`.
- [x] **Ofertio.API/Controllers/MarketplaceAuthController.cs** — callback OAuth exige `[Authorize]`.
- [x] **Ofertio.API/Controllers/AuthController.cs** — `GET /api/auth/status` e `GET /api/auth/providers/status` (alias); login, register, vincular/desvincular provedores.
- [x] **Ofertio.SharedUi/Services/Auth/AccountSecurityApiService.cs** — consome `GET api/auth/status` via `OrquestradorApi:BaseUrl` (`https://localhost:7001/` em dev Kestrel; `http://localhost:5001/` no IIS Homologacao).
- [x] **Ofertio.WebUi/web.config** — `stdoutLogEnabled="true"`, `stdoutLogFile=".\logs\stdout"` (diagnóstico IIS).
- [x] **Ofertio.API/web.config** — `stdoutLogEnabled="true"`, `requestTimeout="20:00:00"` para o UserBot contínuo no IIS.
- [x] **Configurar-Logs-IIS.ps1** — cria `logs\` em `C:\inetpub\ofertio\api` e `webui`; `FullControl` para `IIS_IUSRS`, `DefaultAppPool` e app pools dedicados.
- [x] **Liberar-Logs-WebUi.ps1** — cria `C:\inetpub\ofertio\webui\logs\` e concede `FullControl` a `IIS_IUSRS` / app pools.
- [x] **HttpService.cs** / **WebAccessTokenProvider.cs** — 401 vazio vira mensagem de sessão; login sem JWT continua permitido.
- [x] **Ofertio.API/appsettings.json**, **appsettings.Homologacao.json** — seção `Serilog.MinimumLevel` (Information em homolog).
- [x] **Ofertio.WebUi/Program.cs** — Serilog Console + `logs/app-.txt`, `UseSerilogRequestLogging`, `Log.CloseAndFlush`.
- [x] **Ofertio.WebUi/Logging/BlazorCircuitLoggingHandler.cs** — log de abertura/fechamento/reconexão de circuitos SignalR.
- [x] **Ofertio.API/Middlewares/ExceptionHandlingMiddleware.cs** — captura 500, log contextual seguro, `application/problem+json`.
- [x] **Ofertio.API/Controllers/AuthController.cs** — `LogError` em login/registro inesperados.
- [x] **Ofertio.Infrastructure.Services/Security/PlatformAuthService.cs** — `LogError` em falhas não previstas de login.
- [x] **Ofertio.SharedUi/Services/Auth/AccountSecurityApiService.cs**, **UserRegistrationApiService.cs** — `ILogger` nos catches de auth.
- [x] **Ofertio.WebUi/Extensions/AuthEndpointExtensions.cs** — `LogError` no callback OAuth.

### Fase 6.3 — Produtos globais de propaganda (jun/2026)

- [x] **Ofertio.Core/Entities/GlobalAdvertisingProduct.cs** — FriendlyName, GlobalCategory, MainImageUrl, AveragePrice, `GlobalProductUid`.
- [x] **Ofertio.Core/Entities/MarketplaceAffiliateLink.cs** — vínculos Shopee/TikTok com links gerados e tracking JSON.
- [x] **Ofertio.Infrastructure/Migrations/*AddGlobalAdvertisingProducts*** — tabelas `ProdutosPropagandaGlobal`, `MarketplaceAffiliateLinks`.
- [x] **Ofertio.Database/AppDbContext.cs** — DbSets + índices/relacionamentos.
- [x] **Ofertio.Business/Dto/** — `GlobalAdvertisingProductDto`, `MarketplaceAffiliateLinkDto`, `OptimizedPostPayloadDto`, `GlobalAdvertisingProductResponseDto`.
- [x] **Ofertio.Business/Interfaces/Services/IAdvertisingProductService.cs**.
- [x] **Ofertio.Infrastructure.Services/Advertising/AdvertisingProductService.cs**.
- [x] **Ofertio.Orquestrador/Controllers/AdvertisingProductsController.cs**.
- [x] **Ofertio.SharedUi/Components/Pages/ProdutosPropaganda.razor** — formulário 1 col (mobile) / 2 cols (desktop), cards com copiar link.
- [x] **Ofertio.SharedUi/wwwroot/ofertio-clipboard.js** — área de transferência Web/Mobile.
- [x] **Ofertio.Tests/Unit/Advertising/AdvertisingProductServiceTests.cs**.

### Fase 6.2 — Painel de conciliação financeira de afiliado (jun/2026)

- [x] **Ofertio.Business/Dto/AffiliatePerformanceDto.cs** — cliques, conversões, CVR, comissão estimada/paga, retidos.
- [x] **Ofertio.Business/Dto/CommissionDiscrepancyReportDto.cs** — divergências marketplace vs. Ofertio.
- [x] **Ofertio.Business/Dto/MarketplaceCommissionLineDto.cs**, **AffiliateReconciliationResponseDto.cs**.
- [x] **Ofertio.Database/Filter/AffiliateReconciliationFilter.cs** — período, affiliateId, paginação.
- [x] **Ofertio.Business/Interfaces/Services/IAffiliateAnalyticsService.cs**.
- [x] **Ofertio.Infrastructure.Services/Analytics/AffiliateAnalyticsService.cs** — fetch APIs + reconciliação.
- [x] **Ofertio.Orquestrador/Controllers/AffiliateAnalyticsController.cs** — `api/afiliados/analytics/conciliacao`.
- [x] **Ofertio.SharedUi/Components/Pages/ConciliacaoFinanceira.razor** — painel KPI + lista responsiva.
- [x] **Ofertio.SharedUi/Components/Pages/ConciliacaoDetalhes.razor** — detalhe de linha divergente.
- [x] **Ofertio.SharedUi/Services/Analytics/** — `IAffiliateAnalyticsApiService`, `AffiliateAnalyticsApiService`.
- [x] **Ofertio.SharedUi/wwwroot/app.css** — `.conciliation-kpi-grid`, `.data-list-card--danger`.
- [x] **Ofertio.Tests/Unit/Analytics/AffiliateAnalyticsServiceTests.cs**.

### Fase 6.1 — Filas RabbitMQ e automação de engajamento (jun/2026)

- [x] **Ofertio.Business/Messaging/** — `SocialMediaCommentReceivedEvent`, `AffiliateLinkDeliveryRequestedEvent`, `RabbitMqOptions`, `EngagementKeywordTriageOptions`.
- [x] **Ofertio.Business/Interfaces/Messaging/** — `IEngagementEventPublisher`, `ICommentKeywordTriageService`, `IAffiliateLinkDeliveryNotifier`.
- [x] **Ofertio.Business/Dto/SocialMediaCommentWebhookRequest.cs** — payload do webhook.
- [x] **Ofertio.Infrastructure.Services/Messaging/** — `EngagementMessagingRegistrationExtensions`, `MassTransitEngagementEventPublisher`, `CommentKeywordTriageService`, `AffiliateLinkDeliveryNotifier`.
- [x] **Ofertio.Infrastructure.Services/Messaging/Consumers/SocialMediaCommentConsumer.cs** — triagem e disparo simulado de link.
- [x] **Ofertio.API/Controllers/SocialMediaWebhookController.cs** — `POST /api/webhooks/social-media/comments` → 202 + publicação na fila.
- [x] **Ofertio.API/Program.cs**, **Ofertio.Worker/Program.cs**, **Ofertio.Orquestrador/Program.cs** — DI MassTransit (Publisher / Consumer).
- [x] **appsettings.json** (API, Worker, Orquestrador) — seções `RabbitMq` e `EngagementTriage`.
- [x] **Ofertio.Orquestrador/docker-compose.yml** — serviço `rabbitmq` (5672 / 15672).
- [x] **Ofertio.Tests/Unit/Messaging/CommentKeywordTriageServiceTests.cs**, **SocialMediaWebhookControllerTests.cs**.

### Fase 5.0 — Domínio afiliados e contratos de orquestração (jun/2026)

- [x] **Ofertio.Core/Enums/SocialMediaType.cs** — Instagram, TikTok, YouTube, Facebook.
- [x] **Ofertio.Core/Enums/EngagementStatus.cs** — Pendente, Processado, LinkEnviado, Falhou.
- [x] **Ofertio.Core/Enums/CommissionStatus.cs** — Rastreado, Retido, Pago, Cancelado.
- [x] **Ofertio.Core/Entities/AffiliateLink.cs** — produto de divulgação, `OriginalUrl`, `ShopeeTrackedUrl`, `TikTokShopTrackedUrl`, IDs externos e `TrackingCode`.
- [x] **Ofertio.Business/Domain/Engagement/SocialEngagementEvent.cs** — evento de comentário/mensagem para triagem.
- [x] **Ofertio.Business/Domain/Engagement/EngagementOrchestrationResult.cs** — resultado do disparo de link.
- [x] **Ofertio.Business/Domain/Commission/CommissionAuditLine.cs** — linha de auditoria marketplace vs. local.
- [x] **Ofertio.Business/Domain/Commission/CommissionConciliationResult.cs** — envelope da conciliação.
- [x] **Ofertio.Business/Interfaces/Orchestration/IEngagementOrchestrator.cs** — contrato Orquestrador (engajamento).
- [x] **Ofertio.Business/Interfaces/Orchestration/ICommissionConciliator.cs** — contrato Orquestrador (comissões).
- [x] **Ofertio.Business/Dto/AffiliateLinkDto.cs**, **AffiliateLinkResponseDto.cs** — contratos API/UI futuros.
- [x] **Ofertio.Database** — `MarketplaceAffiliateLink` persistido (Fase 6.3); `AffiliateLink` legado permanece conceitual.
- [ ] **Ofertio.Orquestrador** — implementações concretas de `IEngagementOrchestrator` / `ICommissionConciliator` (Fase 6).

### Fase 4.3 — Push FCM/APNs + Deep Links (jun/2026)

- [x] **Ofertio.Core/Entities/UserDeviceToken.cs** — registo de tokens por utilizador.
- [x] **Ofertio.Infrastructure/Migrations/*AddUserDeviceTokens*** — tabela `UserDeviceTokens`.
- [x] **Ofertio.Business/** — `INotificationHubService`, DTOs `DeviceRegisterDto`, `PushNotificationDto`, `FirebaseOptions`.
- [x] **Ofertio.Infrastructure.Services/Integrations/Notifications/NotificationHubService.cs** — Firebase Admin SDK (FCM).
- [x] **Ofertio.API/Controllers/DevicesController.cs** — `POST /api/devices/register`.
- [x] **Ofertio.SharedUi/Navigation/DeepLinkRoutes.cs** — esquema `ofertio://` → rotas Blazor.
- [x] **Ofertio.SharedUi/Components/Shared/DeepLinkListener.razor** — navegação a partir de push/deep link.
- [x] **Ofertio.SharedUi/Components/Pages/EngajamentoFila.razor**, **ConciliacaoDetalhes.razor**.
- [x] **Ofertio.Mobile/Platforms/Android/OfertioFirebaseMessagingService.cs** — FCM foreground/background.
- [x] **Ofertio.Mobile/Platforms/iOS/AppDelegate.cs** — `UNUserNotificationCenter` + URL scheme.
- [x] **Ofertio.Mobile/Platforms/Android/MainActivity.cs** — intent-filter `ofertio://`.
- [x] **Ofertio.Tests/Unit/Controllers/DevicesControllerTests.cs**.

### Fase 4.2 — Shell MAUI Blazor Hybrid + RCL SharedUi (jun/2026)

- [x] **Ofertio.SharedUi/** — Razor Class Library: componentes (Layout, Dashboard, Pages, Auth), `wwwroot/app.css`, serviços HTTP/API, extensões Filter/ResponseDto.
- [x] **Ofertio.SharedUi/Extensions/ServiceCollectionExtensions.cs** — `AddOfertioClientServices()` (HttpClient Orquestrador; ignora SSL autoassinado em Development/Homologacao).
- [x] **Ofertio.SharedUi/Services/Http/IAccessTokenProvider.cs** — abstração de token para Web e MAUI.
- [x] **Ofertio.WebUi/** — host fino: OAuth/cookies (`AuthCookieService`, `WebAccessTokenProvider`), `Routes.razor` único no host com `AdditionalAssemblies` → SharedUi.
- [x] **Ofertio.SharedUi/Components/AppRoutes.razor** — router do MAUI/AppShell (nome distinto de `Routes` do WebUi).
- [x] **Ofertio.WebUi/Program.cs** — repassa `builder.Environment` para `AddWebUiServices` (SSL bypass do HttpClient Orquestrador em dev/homolog).
- [x] **Ofertio.WebUi/Extensions/WebUiServiceCollectionExtensions.cs** — encaminha `IHostEnvironment` para `AddOfertioClientServices`.
- [x] **Ofertio.Mobile/** — MAUI Blazor Hybrid (`MainPage.xaml` + `BlazorWebView` → `AppRoutes` SharedUi); TFM Windows sem iOS/Mac Catalyst.
- [x] **Ofertio.Mobile/MauiProgram.cs** — DI compartilhada + `MobileAuthenticationStateProvider` + `SessionAuthCookieService`.
- [x] **Ofertio.API/Program.cs** — política CORS `AllowAll` antes de `UseAuthentication`/`UseAuthorization`.
- [x] **Ofertio.Mobile/Platforms/Android/AndroidManifest.xml** — permissões `INTERNET` e `ACCESS_NETWORK_STATE`.
- [x] **Ofertio.Mobile/Platforms/iOS/Info.plist** — `NSAppTransportSecurity` / rede local.
- [x] **Ofertio.Mobile/appsettings.json** — URL do Orquestrador (emulador Android `10.0.2.2`).
- [x] **docs/Ofertio_MOBILE_BUILD.md** — comandos `dotnet publish` Android/iOS/Windows.

### Fase 4.1 — Mobile-First WebUi e contratos paginados (jun/2026)

- [x] **Ofertio.WebUi/Components/Layout/MainLayout.razor** — shell com sidebar colapsável (hambúrguer &lt; 992px), backdrop e navegação touch-friendly.
- [x] **Ofertio.WebUi/Components/Layout/MainLayout.razor.css** — ícone do menu.
- [x] **Ofertio.WebUi/wwwroot/app.css** — breakpoints mobile-first, `.btn-touch` (44px), `.responsive-data-table` (tabela ↔ cards), sidebar e toolbar do painel.
- [x] **Ofertio.WebUi/Components/Dashboard/CampaignsWidget.razor** — listagem em cards no mobile + rodapé de paginação.
- [x] **Ofertio.WebUi/Components/Dashboard/MetricsWidget.razor** — idem métricas/comissões.
- [x] **Ofertio.WebUi/Components/Dashboard/CampaignFilterForm.razor** — colunas `col-12` em mobile, botões touch.
- [x] **Ofertio.WebUi/Components/Dashboard/MetricFilterForm.razor** — idem.
- [x] **Ofertio.WebUi/Components/Dashboard/CampaignCreateForm.razor** — formulário responsivo.
- [x] **Ofertio.WebUi/Components/Pages/Dashboard.razor** — toolbar responsiva, âncoras `#campanhas` / `#metricas`.
- [x] **Ofertio.WebUi/Components/Pages/Home.razor** — botões de plataforma touch-friendly.
- [x] **Ofertio.Database/Filter/IPagedFilter.cs** — contrato `Page` / `PageSize` nos filtros de listagem.
- [x] **Ofertio.Database/Filter/CampaignFilter.cs**, **MetricFilter.cs**, **ProductFilter.cs**, **AffiliateFilter.cs** — implementam `IPagedFilter`.
- [x] **Ofertio.Database/Pagin/PagedListHelper.cs** — fatia listas (máx. 30 itens/página).
- [x] **Ofertio.Business/Dto/PagingInfoDto.cs** — metadados no envelope `*ResponseDto`.
- [x] **Ofertio.Business/Dto/CampaignResponseDto.cs**, **MetricResponseDto.cs**, **ProductResponseDto.cs**, **AffiliateResponseDto.cs** — propriedade `Paging`.
- [x] **Ofertio.API/Controllers/** — `CampaignsController`, `MetricsController`, `ProductsController`, `AffiliatesController` com paginação.
- [x] **Ofertio.Orquestrador/Controllers/CampaignsController.cs**, **MetricsController.cs** — alinhados ao padrão `*Filter` → `*ResponseDto` + `Paging` (WebUi).
- [x] **Ofertio.Tests/Unit/Database/PagedListHelperTests.cs**, **ProductsControllerPagingTests.cs**.

### Integrações TikTok Shop & Shopee — Fase 3.1 (Infraestrutura Core)

- [x] **Ofertio.Business/Integrations/Common/** — `IExternalIntegrationClient`, `IntegrationHttpClientNames`, `IntegrationResilienceOptions`.
- [x] **Ofertio.Business/Integrations/TikTokShop/** — `ITikTokShopIntegrationClient`, `TikTokShopIntegrationOptions` (AppKey/AppSecret).
- [x] **Ofertio.Business/Integrations/Shopee/** — `IShopeeIntegrationClient`, `ShopeeIntegrationOptions` (PartnerId/PartnerKey/AppKey/AppSecret/AppSignature).
- [x] **Ofertio.Infrastructure.Services/Integrations/Common/** — `ExternalApiLoggingHandler`, `IntegrationResiliencePolicies` (Polly retry + circuit breaker).
- [x] **Ofertio.Infrastructure.Services/Integrations/TikTokShop/TikTokShopIntegrationClient.cs** — implementação HTTP.
- [x] **Ofertio.Infrastructure.Services/Integrations/Shopee/ShopeeIntegrationClient.cs** — implementação HTTP.
- [x] **IntegrationHttpClientRegistrationExtensions.cs** — `AddOfertioIntegrationHttpClients()` registrado em `ServiceRegistrationExtensions`.
- [x] **appsettings.json** (API + Orquestrador) — seção `Integrations` com chaves de produção (vazias; usar User Secrets/env).

### Integrações TikTok Shop & Shopee — Fase 3.2 (OAuth2)

- [x] **Ofertio.Core/Entities/MarketplaceToken.cs** — persistência de tokens por loja (`ShopId`, `MarketplaceType`, `AccessToken`, `RefreshToken`, `ExpiresAt`, `RefreshExpiresAt`).
- [x] **Ofertio.Core/Enums/MarketplaceType.cs** — `Shopee`, `TikTokShop`.
- [x] **Ofertio.Business/Integrations/Auth/** — `IMarketplaceAuthService`, `IMarketplaceSignatureService`, `MarketplaceTokenResult`.
- [x] **Ofertio.Business/Integrations/Common/MarketplaceSignatureHelper.cs** — HMAC-SHA256 Shopee e TikTok Shop.
- [x] **Ofertio.Infrastructure.Services/Integrations/Auth/MarketplaceAuthService.cs** — authorize URL, callback, refresh automático.
- [x] **Ofertio.Infrastructure.Services/Repositories/MarketplaceTokenRepository.cs** — upsert por `ShopId` + `MarketplaceType`.
- [x] **MarketplaceAuthController** — `GET api/marketplace-auth/lojas` lê `MarketplaceAccounts` por `UserId`.
- [x] **IntegracaoLojaService.ListByUserAsync** — `AsNoTracking` em `MarketplaceAccounts`.
- [x] **IntegracaoLojaApiService.ListAsync** / **MinhasLojas.razor** — recarrega lojas após vínculo.
- [x] **Migration AddMarketplaceTokens** — tabela `MarketplaceTokens` com tokens criptografados no `AppDbContext`.

### Integrações TikTok Shop & Shopee — Fase 3.3 (Catálogo / Produtos)

- [x] **Ofertio.Core/Entities/Product.cs** — campos `[NotMapped]` `ExternalProductId`, `SkuCode`, `MarketplaceSource` para sincronização.
- [x] **Ofertio.Business/Integrations/Catalog/IMarketplaceProductService.cs** — `FetchProductsFromPlatformAsync`, `ConvertToInternalProductDto` (Shopee/TikTok).
- [x] **Ofertio.Business/Integrations/Shopee/Payloads/** — envelopes e DTOs `get_item_list`, `get_item_base_info`, `get_model_list`.
- [x] **Ofertio.Business/Integrations/TikTokShop/Payloads/** — envelope e DTOs `products/search` (categorias, attributes, skus, price, stock).
- [x] **ShopeeIntegrationOptions** / **TikTokShopIntegrationOptions** — paths de catálogo configuráveis.
- [x] **MarketplaceProductService.cs** — HTTP resiliente + token OAuth + assinatura HMAC + adapter → `ProductResponseDto`.
- [x] **MarketplaceProductMapper.cs** — conversão unificada (ID externo, SKU, nome, descrição, preço, estoque, origem).
- [x] **MarketplaceProductRegistrationExtensions.cs** — `AddOfertioMarketplaceCatalog()` em `ServiceRegistrationExtensions`.
- [x] **Ofertio.API/Controllers/MarketplaceProductsController.cs** — `GET api/marketplace-products/sync`.

### Integrações TikTok Shop & Shopee — Fase 3.4 (Pedidos & Estoque)

- [x] **Ofertio.Core/Entities/MarketplaceOrder.cs** / **MarketplaceOrderLine.cs** — pedidos externos com índice único (idempotência).
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

### Ofertio.Tests — Cobertura Fase 3 (Integrações & ResponseDto)

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
- [x] **Ofertio.Tests.csproj** — referências `Ofertio.API`, `Ofertio.Database`, `Microsoft.AspNetCore.Mvc.Testing`.

### Colisão semântica (nome enganoso)

- [ ] **AuthController.cs** — `Ofertio.Application/Controller/AuthController.cs` **não é controller** (classe DTO com `CampaignId`, `Revenue`). Conflito de nome com `Ofertio.Orquestrador/Controllers/AuthController.cs`. (Ação: renomear para `CampaignSummaryDto` ou deletar se obsoleto; remover projeto `Application` da cadeia se ficar vazio.)

### Registro DI fragmentado (não duplica tipo, duplica responsabilidade)

- [ ] **ServiceRegistrationExtensions.cs** + **CoreServiceRegistrationExtensions.cs** + **ExternalServiceRegistrationExtensions.cs** + **InfrastructureDataServiceRegistrationExtensions.cs** — todos em `Ofertio.Infrastructure.Services/`. (Ação: consolidar em um único `ServiceRegistrationExtensions.cs` conforme plano anterior.)

### API legada no Infrastructure (sobreposição com Business + Services)

- [ ] **TikTokShopApi.cs**, **TikTokAdsApi.cs**, **ShopeeApi.cs** — `Ofertio.Infrastructure/API/` vs implementações HttpClient em `Ofertio.Infrastructure.Services/Service/ExternalServices/`. (Ação: definir camada única de integração externa; deletar stubs antigos em `Infrastructure/API` se não forem registrados no DI.)

---

## 🚚 2. Arquivos Movidos com Sucesso (Apenas Ajustar Namespace)

Arquivos já no projeto físico correto, mas com `namespace` desalinhado da pasta/projeto:

- [ ] **ExceptionMiddleware.cs** — ~~Em `Ofertio.Core/Exceptions/`, namespace `Ofertio.API.Middlewares`~~ **Resolvido:** removido do Core; usar `Ofertio.API/Middlewares/ExceptionHandlingMiddleware.cs`.

- [ ] **CoreServiceRegistrationExtensions.cs** — Em `Ofertio.Infrastructure.Services/`, namespace `Ofertio.Infrastructure`. (Ação: `Ofertio.Infrastructure.Services`.)

- [ ] **Serilog.cs** — Em `Ofertio.Infrastructure.Services/`, namespace `Ofertio.Configuracao`, **excluído do compile**. (Ação: mover para `Ofertio.Infrastructure/Configuration` com namespace alinhado ou deletar.)

- [ ] **ITikTokShopApiService.cs** — Em `Ofertio.Infrastructure.Services/Interfaces/`, namespace `Ofertio.Core.Interfaces.Services`. (Ação: mover para `Ofertio.Business/Interfaces/Services` ou `Ofertio.Core` conforme regra de contratos.)

- [ ] **AnaliseCalculoService.cs**, **IAnaliseCalculoService.cs** — Pasta `Service/`, namespace `Ofertio.Infrastructure.Services.Services` (`.Services` duplicado). (Ação: `Ofertio.Infrastructure.Services.Service`.)

- [ ] **TikTokShopApiService.cs**, **TikTokAdsApiService.cs** — Pasta `Service/ExternalServices/`, namespace `Ofertio.Infrastructure.Services.ExternalServices` (falta segmento `.Service`). (Ação: alinhar com pasta ou renomear pasta.)

- [ ] **AnaliseService.cs** — Em `Ofertio.Business/Interfaces/Services/`, comentário assume `Ofertio.Core.Services`. (Ação: confirmar namespace `Ofertio.Business.Interfaces.Services` e remover comentário legado.)

- [ ] **OrquestradorPrincipalTests.cs** — `Ofertio.Tests/UnitTests/Core/`, namespace `Ofertio.Tests.UnitTests.Colore` (typo). (Ação: `Ofertio.Tests.UnitTests.Core`.)

- [ ] **CampanhaConfiguration.cs**, **AfiliadoConfiguration.cs** — `Ofertio.Infrastructure/Data/Configurations/`, nomes em português para entidades em inglês (`Campaign`, `Affiliate`). (Ação: renomear classes/arquivos para inglês ou mover configs para `Ofertio.Database`.)

### Movidos e com namespace correto (referência — sem ação)

- `Ofertio.Database`: `AppDbContext`, `Entity/`, `Filter/`, `Pagin/`, `Repositorio/` → namespaces `Ofertio.Database.*` ✓  
- `Ofertio.Business`: `Dto/`, `Interfaces/`, `Pipelines/`, `Service/Application/` → `Ofertio.Business.*` ✓  
- `Ofertio.API` controllers refatorados → `Ofertio.API.Controllers` ✓  

### Pendência arquitetural: Migrations vs DbContext

- [ ] **AppDbContext.cs** — em `Ofertio.Database/`.  
- [ ] **Migrations/** — ainda em `Ofertio.Infrastructure/Migrations/` (6 migrações + snapshot). (Ação: mover pasta de migrations para `Ofertio.Database` ou configurar `MigrationsAssembly` apontando para Infrastructure até concluir a mudança; hoje há risco de `dotnet ef` gerar no projeto errado.)

---

## 🧹 3. Resíduos e Arquivos Fantasmas a Deletar

### Artefatos de build com nome antigo *Tecso* (não versionar)

- [ ] **`**/bin/**` e `**/obj/**`** em todos os projetos — contêm `Tecso.API.deps.json`, `Tecso.Core.AssemblyInfo.cs`, `Tecso.Orquestrador.*`, etc. (Ação: `dotnet clean`; garantir `.gitignore` com `bin/`, `obj/`, `artifacts/`.)

- [ ] **`artifacts/orquestrador-publish/`** — publish com assembly `Tecso.Orquestrador`. (Ação: excluir pasta ou regenerar publish com nome Ofertio.)

- [ ] **`.vs/Tecso.Automacao/`** — cache Visual Studio com caminhos `Tecso.Infrastructure.Services\...`. (Ação: excluir do disco; não commitar.)

### Arquivos excluídos do compile mas ainda no disco (fantasmas)

- [ ] `Ofertio.Infrastructure.Services/Interfaces/ITikTokShopApi.cs`  
- [ ] `Ofertio.Infrastructure.Services/Interfaces/IShopeeApi.cs`  
- [ ] `Ofertio.Infrastructure.Services/Interfaces/ITikTokAdsApiService.cs`  
- [ ] `Ofertio.Infrastructure.Services/Serilog.cs`  
- [ ] `Ofertio.Infrastructure.Services/Service/ExternalServices/OrquestradorService.cs`  
(Ação: deletar arquivos **ou** reintegrar ao compile — não manter `<Compile Remove>` indefinidamente.)

### Templates / testes de scaffold

- [ ] `Ofertio.API/WeatherForecast.cs` + `Controllers/WeatherForecastController.cs`  
- [ ] `Ofertio.API/Controllers/TestController.cs`  
- [x] `Ofertio.Dashboard/` — projeto removido (WeatherForecast scaffold eliminado com a pasta).
(Ação API: excluir scaffolds se não usados em produção.)

### Projeto Application quase vazio

- [ ] `Ofertio.Application/` — apenas `Controller/AuthController.cs` (stub incorreto). Referenciado por `Ofertio.API`. (Ação: migrar serviços restantes para `Ofertio.Business` e remover referência de projeto, ou repopular Application com casos de uso reais.)

### Pasta API sem middleware (limpeza pós-merge)

- [ ] `Ofertio.API/Middleware/` — pasta vazia após remoção de `ExceptionMiddleware.cs`. (Ação: remover diretório vazio.)

### Arquivos de usuário IDE (opcional)

- [ ] `Ofertio.API/Tecso.API.csproj.user`  
- [ ] `Ofertio.API/Ofertio.API.csproj.user`  
(Ação: excluir e adicionar `*.csproj.user` ao `.gitignore`.)

### Duplicata de teste

- [ ] `Ofertio.Tests/Integration/OrquestradorPrincipalTests.cs` e `Ofertio.Tests/UnitTests/Core/OrquestradorPrincipalTests.cs` — mesmo nome de arquivo. (Ação: renomear um (ex.: `OrquestradorPrincipalIntegrationTests.cs`).)

---

## 🛠️ 4. Estrutura de Pastas Alvo (Arquitetura Atual)

```
Ofertio.sln
├── Ofertio.Core/                    # Domínio: entidades, exceções, middleware global
│   ├── Entities/                    # Campaign, Product, UserAccount, ...
│   └── Exceptions/                  # NotFoundException, ExceptionMiddleware ⚠ namespace API
│
├── Ofertio.Database/                # Persistência (isolado) ✓
│   ├── AppDbContext.cs
│   ├── Entity/                      # UserEntity
│   ├── Filter/                      # *Filter + FilterQueryExtensions
│   ├── Pagin/                       # PagedResult, QueryableExtensions
│   ├── Repositorio/                 # AppDbContextFactory
│   └── Prompts/
│
├── Ofertio.Business/                # Regras + contratos + DTOs (isolado) ✓
│   ├── Dto/                         # *Dto, *ResponseDto, ResponseDto
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   └── Services/
│   ├── Pipelines/
│   └── Service/
│       └── Application/             # *ApplicationService, DI extensions
│
├── Ofertio.Infrastructure/          # EF migrations, segurança, configs legadas
│   ├── Migrations/                  # ⚠ deveria alinhar com Ofertio.Database
│   ├── Data/                        # DataService, Configurations (PT-BR)
│   ├── API/                         # TikTok/Shopee stubs legados
│   ├── Security/
│   └── Services/Security/
│
├── Ofertio.Infrastructure.Services/ # Implementações: repos, APIs externas, DI
│   ├── Repositories/
│   ├── Service/
│   │   └── ExternalServices/
│   ├── Interfaces/                  # ⚠ fantasmas + namespace Core
│   └── *ServiceRegistrationExtensions.cs (4 arquivos)
│
├── Ofertio.Application/             # ⚠ quase vazio — 1 arquivo stub
├── Ofertio.API/                     # Host HTTP principal
├── Ofertio.Orquestrador/            # Host orquestração (controllers espelhados)
├── Ofertio.WebUi/                   # Blazor UI canônico → Business + Database (Filter)
│   ├── Components/Dashboard/        # FilterForm, CreateForm, Widgets (ResponseDto)
│   ├── Extensions/                  # FilterQueryString, ResponseDto, Campaign
│   └── Services/Dashboard/          # Filter → API → ResponseDto
├── Ofertio.Worker/
├── Ofertio.Tests/
├── Ofertio.Util/                    # ValidationHelper, Encryption, CEP
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

**Isolamento desejado:** `Ofertio.Business` e `Ofertio.Database` **não** devem referenciar `Infrastructure` — hoje **ok** no `.csproj`. Implementações ficam em `Infrastructure` + `Infrastructure.Services`.

### Fase 19.1.1 — Credenciais de afiliado Shopee (appsettings)

- [x] **Ofertio.API/appsettings.json**, **appsettings.Homologacao.json** — `Integrations:Shopee` com `PartnerId`, `PartnerKey`, `AppSecret`, `AppKey`, `AppSignature` (placeholders `""`).
- [x] **Ofertio.Business/Integrations/Shopee/ShopeeIntegrationOptions.cs** — bind das chaves de afiliado.
- [x] **Ofertio.Infrastructure.Services/LinkStrategies/ShopeeAffiliateLinkClient.cs** — fallback `AppKey`/`AppSecret`/`AppSignature`.
- [x] **Ofertio.WebUi** — sem seção Shopee (credenciais ficam só na API).

### Fase 19.1.2 — Fallback/Sandbox Shopee

- [x] **Ofertio.Business/Integrations/Shopee/ShopeeSandboxLinkBuilder.cs** — URL de homologação com `tracking_code`/`sub_id` (`tecflow_sandbox_subid`).
- [x] **ShopeeIntegrationClient** — modo sandbox quando credenciais vazias (não lança; não chama HTTP).
- [x] **ShopeeAffiliateLinkClient** / **ShopeeLinkStrategy** — geração local de URL rastreada sem exceção.
- [x] **Ofertio.Tests/Unit/LinkStrategies/ShopeeLinkConversionTests.cs** — credenciais, sandbox e tracking.

### Fase 19.2.1 — PlatformLinkResolver, unshorten e extração ShopId/ItemId

- [x] **ShopeeLinkHostMatcher.cs** — hosts `shopee.com.br`, `br.shp.ee`, `shp.ee`, `shope.ee`.
- [x] **ShopeeProductUrlParser.cs** — regex `i.(shopId).(itemId)` em URLs desktop, sanitização de `extraParams`/`sp_atk`/`xptdk` e HTTP 400 com alerta Blazor de formato não reconhecido.
- [x] **UrlExpansionService** — GET sem autoredirect, segue `Location` 301/302.
- [x] **ShopeeLinkStrategy** / **PlatformLinkResolver** — regex `i.(shopId).(itemId)` no desktop; Universal Link `https://shopee.com.br/universal-link/product/{shopId}/{itemId}?sub_id=...` sem Open API.
- [x] **ConnectStoreModal.razor** / **MinhasLojas.razor** — Shopee: plataforma, apelido e campo `ID do Afiliado (Affiliate ID / Tracking ID)` (ex.: `6512300000`); persistido em `MarketplaceAccounts.AffiliateTrackingId`.
- [x] **MarketplaceStoreCard.razor** — exibe `Shop ID` e `Affiliate ID` no card de Minhas Lojas.
- [x] **Ofertio.Data/Migrations/20260922220000_AddAffiliateTrackingId.cs** — coluna `AffiliateTrackingId` (`nvarchar(64)`) em `MarketplaceAccounts` e `IntegracaoLoja`.

### Fase 19.2.2 — URL rastreada de comissão

- [x] **ShopeeCommissionUrlBuilder.cs** — query `tracking_code`, `sub_id` (usuário/tenant), `universal_link` e `deep_link` com URL encoding.
- [x] **ShopeeOfficialShortUrl.cs** — resolve `https://br.shp.ee/...` a partir da API, da URL original ou dos IDs do produto.
- [x] **ShopeeLinkConversionTests.cs** — contrato de query string, deep links nativos e caracteres especiais.

### Fase 19.2.3 — Persistência de telemetria LinkClickLog

- [x] **LinkClickLog.cs** — TenantId, ShopId, OriginalUrl, ConvertedUrl, Platform, CreatedAt, EventKind e metadados de acesso.
- [x] **AffiliateLinkGenerationService** / **LinkClickTelemetryService.RecordGenerationAsync** — grava telemetria após conversão Shopee.
- [x] **20260916231111_AddLinkClickLogGenerationTelemetry** — colunas novas na tabela `LinkClickLog`.
- [x] **Ofertio.Tests/Unit/LinkStrategies/LinkClickLogTests.cs** — TenantId/ShopId da sessão, campos obrigatórios e FK do encurtador.

### Fase 19.3.1 — Integração GeradorLinks.razor

- [x] **GeradorLinks.razor** — POST com URL + StoreId/TenantId/ShopId da loja ativa; `_isLoading`, spinner, alerta vermelho e `StateHasChanged()` no sucesso.
- [x] **Ofertio.Util/Text/SlugHelper.cs** — `GenerateSlug` / `ShortLinkPublicUrl` (`@Achadinhos de Aaz` → `AchadinhosDeAaz`; URL `{host}/{storeSlug}/{code}`).
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
- [x] **Ofertio.Data/Migrations/20260923200000_AddShortAffiliateLinkAccounts.cs** — tabela e backfill no SQL Server.

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
- [x] **Ofertio.Tests/Unit/LinkStrategies/UrlUnshortenerServiceTests.cs** — `promoby.me/6nf9k3d5` → `amazon.com.br`; `ofertou.ai/drXB-Magalu` → `magazineluiza.com.br`.
- [x] **Ofertio.Business/Service/Application/ShortAffiliateLinkService.cs** — contas Magalu com alias flexível e Tracking ID.
- [x] **IntegracaoLojaScopeResolver.cs** — `AreSamePlatform` ao resolver a loja Magalu (ex.: Affiliate ID `5321952`).
- [x] **MagazineLuizaCommissionUrlBuilder** — ID numérico injeta `promoter_id` (remove tags do concorrente).
- [x] **ProductMetadataHtmlParser** — slug Magalu (`.../smart-tv-50-tcl.../p/{id}`) vira ProductName com `UrlDecode` + TitleCase/acrônimos.
- [x] **GeradorLinks.razor** — detecta `ofertou.ai/drXB-Magalu` e lista contas Magalu com comparação flexível.
- [x] **PlatformLinkResolverTests.cs** — `ofertou.ai/UjGXJ` (Kabum), `ofertou.ai/drXB-Magalu` (TV Magalu + `promoter_id=5321952`) e `promoby.me/6nf9k3d5` (Amazon).
- [x] **Ofertio.Business/Service/LinkStrategies/UniversalLinkResolverEngine.cs** — unshortener universal (10 saltos, JS/meta-refresh), mapa domínio→plataforma e injeção de comissão.
- [x] **ProductMetadataService.cs** / **PlatformLinkResolver.cs** — `ResolveFinalDestinationUrlAsync` no pipeline de metadados e de geração.
- [x] **Ofertio.Tests/Unit/LinkStrategies/UniversalResolverTests.cs** — ofertou Magalu `5321952`, promoby Amazon sem `promobit-d-20`, ofertou Kabum.
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
- [x] **GeradorLinks.razor** — opção Todas as lojas e CTA para conectar conta inexistente (`ActiveStoreScopeSelector.razor` removido em `UserProfileMenu`).
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

- [x] **ofertio-clipboard.js** — `copyText` com Clipboard API e fallback `execCommand`.
- [x] **LinkGeneratorResultPanel.razor** — três cards de cópia: afiliado longo, encurtado Shopee (`br.shp.ee`) e rastreio Ofertio.
- [x] **AffiliateShareLinkBuilder.cs** — URIs `api.whatsapp.com/send` e `t.me/share/url` com URL encoding.
- [x] **AffiliateShareLinkBuilderTests.cs** — encoding de espaços, `&` e query string.

### Login homologação — filtro de tenant

- [x] **AppDbContext.ApplyTenantQueryFilters** — lambdas de instância (`CurrentTenantId == null` visível no login).
- [x] **TenantQueryFilterExtensions.cs** — documentação: não capturar `ICurrentTenantService` no modelo EF.
- [x] **UserAccountRepository.GetByEmailAsync** — `IgnoreQueryFilters` na busca por e-mail.
- [x] **Ofertio.Tests/Unit/MultiTenancy/TenantQueryFilterTests.cs** — visível sem tenant; isolado com tenant.

### OAuth Minhas Lojas / Integrações

- [x] **ConnectStoreModal.razor** — `OnLojaVinculada` recarrega a lista após vínculo com sucesso.
- [x] **MinhasLojas.razor** — `ObterLojasAsync()` + `StateHasChanged()` e `StoreScope.RefreshStoresAsync()`.
- [x] **ActiveStoreScopeService** — `EnsureInitializedAsync` recarrega lojas; `OnStoreChanged` após cada leitura.
- [x] **MarketplaceAccountRepository** — listagens e `GetByShopAsync` com `AsNoTracking()`.
- [x] **MarketplaceOAuthConnectService.cs** — ticket pendente + GET `api/marketplace-auth/{plataforma}/authorize-url`.
- [x] **MarketplaceAuthController** — rota `{plataforma}/authorize-url` e DTO `authorizeUrl`.
- [x] **Ofertio.Tests/Unit/SharedUi/MarketplaceOAuthConnectServiceTests.cs** — path, DTO e erro da API.
- [x] **ShopeeAuthorizationUrlFactory.cs** — PartnerId/PartnerKey sandbox quando vazios (sem HTTP 500).
- [x] **MarketplaceAuthController** — try/catch em authorize-url; Shopee devolve 200 com URL sandbox.
- [x] **appsettings.json / appsettings.Homologacao.json** — `Integrations:Shopee` com PartnerId/PartnerKey padrão de homologação.
- [x] **HomologMarketplaceAuth.cs** — prefixo `code_*` ou ambiente Dev/Homologação pula a Shopee e persiste tokens stub.
- [x] **ConnectStoreModal.razor** — botão `type="button"` `@onclick="VincularManualmenteDirect"` chama a API direto; Shop ID/`code_teste` com fallback.
- [x] **IntegracaoLojaService / MarketplaceAuthService** — vínculo manual homologa sem HTTP remoto e devolve mensagem de sucesso de homologação.
- [x] **IntegracaoLojaApiService** — log do payload bruto se o envelope não deserializar; trata ProblemDetails.
- [x] **Ofertio.Tests/Unit/Integrations/IntegracaoLojaServiceTests.cs** — vínculo manual simulado.
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
- [x] **OfertioJsonOptions** — case-insensitive + números em string; log de JsonException.
- [x] **AuthControllerSecurityTests / AffiliateLinksControllerTests / DashboardControllerTests** — 401/500 e JSON de formulário.

### Fase 19.26 — Página pública de conversão (slug versionado)

- [x] **PublicConverterPage.cs** / **PublicConverterPages** — versionamento de slug (`IsActive=false` no antigo, novo registro com o mesmo `PublicCode`).
- [x] **PublicConverter.razor** — aceita slug ativo e inativo; badges `DistinctBy(PlatformType)` compactos (~35%); Converter + Limpar; resultados em duas linhas (direto e rastreio Ofertio) com Copiar/Abrir.
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
- [x] **WhatsAppBroadcastWorker.cs** — BackgroundService no Ofertio.API com `IntervalSeconds` ≥ 15s.
- [x] **WhatsAppAgendador.razor** — sincronizar grupos, multi-check, `[LINK_COMISSAO]`, agenda e tabela.
- [x] **WhatsAppBroadcastRulesTests.cs** — intervalo anti-bloqueio e tag de comissão.
- [x] **20261001003616_AddWhatsAppBroadcastCampaigns** — tabelas `WhatsAppGroups` e `WhatsAppBroadcastCampaigns`.

### Segurança defensiva Telegram/WhatsApp

- [x] **DataEncryptionService.cs** — AES-256 (`ENC1:`) em `Ofertio.Infrastructure/Security`; `IEncryptionService` no DbContext.
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
- [x] **AddSourceGroupToShortAffiliateLinks** — coluna `SourceGroup` no histórico de `/gerador-links`.
- [x] **TelegramUserMonitorRules.cs** / **TelegramUserMonitorRulesTests.cs** — regex HTTP ampla; filtro Shopee, ML, Amazon, AliExpress, Magalu, Casas Bahia, Pelando, Promobit e encurtadores; `api_id` numérico e PIN de 5 dígitos.
- [x] **UserBotHelpModal.razor** — tutorial de 7 passos, badges Bot Token/Chat ID e Copiar exemplo.
- [x] **Conexoes.razor** — badges amarelo/azul no bloco UserBot e abre `UserBotHelpModal`.
- [x] **TelegramUserBotSessionStore.cs** / **TelegramUserBotCodeStore.cs** — sessão em `C:\ProgramData\Ofertio\telegram-sessions` (sobrevive ao publish IIS; fallback TEMP) e código de verificação.
- [x] **TelegramUserBotSessionStoreTests.cs** — garante pasta `telegram-sessions` resolvida sem falhar o processo.
- [x] **TelegramUserMonitorWorker.cs** / **TelegramUserMonitorHost.cs** — catch-up por `chats` (Dialogs/Slice) + `GetHistory` (1500/48h), botões inline e persistência no sync; sessão em `ProgramData`.
- [x] **UserBotCatchUpResult.cs** — resultado do catch-up (offline vs persistidas) para o botão Sincronizar.
- [x] **TelegramIntegrationController.cs** — `POST userbot/solicitar-codigo` e `POST userbot/confirmar` (Login MTProto em duas etapas).
- [x] **GruposMonitorados.razor** — feed/ocultos, grid 2x2, Sem interesse / Restaurar e Clonar Oferta.
- [x] **GroupCapturedMessagesService.cs** / **IGroupCapturedMessagesService.cs** — filtro de lojas ativas, `HasDirectProductUrl` e `IsIgnored`.
- [x] **GroupCapturedMessageFilter.cs** — hours, groupKey, channel, skip/take e ignored.
- [x] **AddGroupCapturedMessageCuration** — colunas `HasDirectProductUrl`, `IsIgnored`, `IgnoredAt`.
- [x] **IUrlResolverService.cs** / **UrlResolverService.cs** — HEAD/GET com `AllowAutoRedirect` até a URL canônica da loja.
- [x] **IAffiliateLinkConverterService.cs** / **AffiliateLinkConverterService.cs** — conversão de comissão + título/preço/imagem.
- [x] **CloneOfferRules.cs** / **UrlResolverDto.cs** / **AffiliateLinkConverterDto.cs** — mensagens e envelopes do clone.
- [x] **UrlResolverServiceTests.cs** / **AffiliateLinkConverterServiceTests.cs** — HEAD até Shopee e persistência com SourceGroup.
- [x] **MonitoredGroupsController.cs** / **MonitoredGroupService.cs** — listagem paginada (`skip`/`take`, sem `Take(120)`).
- [x] **OfferProductMediaStore.cs** / **IOfferProductMediaStore.cs** — grava foto do Telegram em `wwwroot/uploads/products`.
- [x] **OfferValidationService.cs** / **OfferValidationDtos.cs** — `ValidateProductPageStatusAsync` com follow redirects e frases de esgotado.
- [x] **GroupOfferCaptureRules.cs** — regex de preço Telegram + fallback `og:title` / `product:price:amount`.
- [x] **GroupOfferCaptureService.cs** / **TelegramUserMonitorHost.cs** — download `MessageMediaPhoto` e scrape OpenGraph.
- [x] **AddGroupCapturedMessageAvailability** — coluna `IsAvailable` e índice por disponibilidade.
- [x] **GruposMonitorados.razor** — badge cinza/vermelho `Esgotado`; feed principal oculta `IsAvailable = false`.
- [x] **OfferValidationServiceTests.cs** / **GroupOfferCaptureRulesTests.cs** — frases de erro, regex de preço e meta tags.

### Módulo 28 — Radar e mineração de ofertas

- [x] **AffiliateMiningProfile.cs** / **ProductPriceSnapshot.cs** / **OfferRadarItem.cs** — perfil, histórico de preço e feed do radar.
- [x] **AffiliateMiningRules.cs** / **AffiliateMiningRulesTests.cs** — nichos, ticket, cupom, score e lojas ativas.
- [x] **AffiliateMiningProfileService.cs** / **ProductArbitrageService.cs** / **OfferRadarService.cs** / **OfferMiningEngine.cs**.
- [x] **OfferMiningHost.cs** / **OfferMiningWorker.cs** — ciclo 10 minutos no IIS.
- [x] **OfferRadarController.cs** — perfil, arbitragem, listagem, agendar e piloto.
- [x] **RadarPerfil.razor** / **RadarOfertas.razor** / **GeradorLinks.razor** — UI de mineração, feed e cards de menor preço.
- [x] **AddOfferRadarMining** — tabelas `AffiliateMiningProfiles`, `ProductPriceSnapshots` e `OfferRadarItems`.

### Módulo 29 — Saúde, SubID, Evergreen e mídia

- [x] **OfferHealthAlert.cs** / **EvergreenOffer.cs** / **AddOfferIntelligencePhase29** — alertas pós-disparo, campeãs e colunas `SourceChannel`/`SourceGroup`/`SubId` em `LinkClickLog`.
- [x] **OfferAttributionRules.cs** / **OfferAttributionRulesTests.cs** — injeção `tf_src`/`tf_grp`/`sub_id`.
- [x] **OfferHealthService.cs** / **GroupAttributionService.cs** / **EvergreenLibraryService.cs** / **OfferMediaStudio.cs**.
- [x] **OfferIntelligenceWorker.cs** / **OfferIntelligenceController.cs**.
- [x] **SaudeOfertas.razor** / **AtribuicaoGrupos.razor** / **BibliotecaEvergreen.razor** / **EstudioMidia.razor**.

### Módulo 30 — Pre-flight e saúde de agendamentos

- [x] **PreFlightNotification.cs** / **AddPreFlightPhase30** — pausas pré-envio e justificativas.
- [x] **PreFlightRules.cs** / **PreFlightRulesTests.cs** — janela de 15 min, alta de preço e troca de URL.
- [x] **PreFlightService.cs** / **PreFlightWorker.cs** / **PreFlightController.cs**.
- [x] **SaudeAgendamentos.razor** — central de notificações e substituição inteligente de link.

### Fase 31 README — Deal Intelligence (Achadinhos e histórico)

- [x] **ProductPriceHistory.cs** / **TenantCredit.cs** / **CreditTransaction.cs** / **GlobalTrendingDeal.cs** / **TenantDealUnlock.cs** / **AddDealIntelligencePhase31**.
- [x] **ProductSkuRules.cs** / **PriceHistoryRules.cs** / **DealCreditRules.cs** — SKU MLB/Shopee/Amazon, janelas 30/60/90 e cota diária.
- [x] **PriceHistoryTracker.cs** / **DealCreditsService.cs** / **GlobalTrendingDealsEngine.cs** / **GlobalTrendingDealsWorker.cs**.
- [x] **DealIntelligenceController.cs** — vitrine VIP, desbloqueio (1 crédito) e pacotes +10/+50/+100.
- [x] **Achadinhos.razor** / **GruposMonitorados.razor** — blur paywall e badges de menor preço / economia.

### Fase 32 README — Live Search e APIs oficiais

- [x] **LiveSearchRules.cs** / **LiveSearchRulesTests.cs** — filtro por palavra-chave, preço, cupom e loja.
- [x] **MercadoLivreApiService.cs** / **ShopeeAffiliateOfferService.cs** / **AmazonPaApiService.cs** — `/items`, HMAC `productOfferV2` e PA-API 5.0 GetItems.
- [x] **LiveCheckSearchService.cs** — ping na clonagem/validação com fallback HTML; atualiza preço e status no SQL.
- [x] **GruposMonitorados.razor** / **Achadinhos.razor** — barra de busca rápida no topo.
- [x] **OfficialCatalogSearchRules.cs** / **OfficialCatalogSearchService.cs** / **OfficialCatalogSearchController.cs** — `Task.WhenAll` ML/Shopee/Amazon + base interna.
- [x] **BuscaGlobal.razor** (`/busca`) / **NavMenu.razor** — item Buscar Ofertas e Converter & Gerar Link.
- [x] **appsettings.json** — nó `Integrations:AmazonPaApi`.
- [x] **InfrastructureDataServiceRegistrationExtensions.cs** — `AddDbContextFactory<AppDbContext>` scoped.
- [x] **OfficialCatalogSearchService.cs** / **ShopeeAffiliateOfferService.cs** / **AmazonPaApiService.cs** — contexto isolado por task no `WhenAll`.
- [x] **OfficialCatalogChannelStatusDto** / **BuscaGlobal.razor** — badges por loja (ML/Shopee/Amazon/local).
- [x] **MercadoLivreApiService.cs** — `GET https://api.mercadolibre.com/sites/MLB/search` sem Authorization.
- [x] **OfficialCatalogSearchService.cs** — log `Busca concluída para '{query}': ML=…`.
- [x] **OfficialCatalogSearchRulesTests.cs** / **MercadoLivreApiServiceTests.cs** / **OfficialCatalogSearchServiceTests.cs**.
- [x] **MercadoLivreSearchDtos.cs** — `results` / `title` / `price` / `permalink` / `thumbnail`.
- [x] **MercadoLivreApiService.cs** — User-Agent `OfertioApp/1.0`, `GET sites/MLB/search?q=&limit=20` e `Erro na API ML: {status}`.
- [x] **MercadoLivreApiService.cs** — Chrome/120, `SendAsync`, log do body e `OfficialCatalogChannelResult.Failed`.
- [x] **BuscaGlobal.razor** — badge `error` com ❌ e mensagem HTTP/JSON.
- [x] **MercadoLivreIntegrationOptions.cs** / **appsettings.json** — `Integrations:MercadoLivre` AppId/SecretKey/AccessToken.
- [x] **MercadoLivreApiService.cs** — Bearer da conta do tenant, client_credentials e 401/403 como conta ausente.
- [x] **BuscaGlobal.razor** — `⚠️ Requer conta conectada no painel`.
- [x] **BuscaGlobal.razor** / **MinhasLojas.razor** / **ConnectStoreModal.razor** — botão Conectar abre `/minhas-lojas?conectar=mercadolivre`.
- [x] **ConnectStoreModal.razor** — rótulo `Link de Afiliado ou ID do Mercado Livre` e placeholder para colar o link.
- [x] **HowToGetStoreIdModal.razor** — ajuda Desktop (`bi-laptop`) e Telemóvel (`bi-phone`) sem termos técnicos.
- [x] **ConnectStoreManualLinkForm.cs** — `TryExtractMercadoLivreTrackingId` (dígitos ou `matt_tool=`).
- [x] **MarketplaceAccountService.cs** / **IntegracaoLojaService.cs** — normaliza TrackingId ML e expande `meli.la`/`/sec/` com `UrlUnshortenerService`.
- [x] **AffiliateTrackingIdValidator.cs** — `TryParseMercadoLivreTrackingId` e `NeedsMercadoLivreUrlExpansion`.
- [x] **OfficialCatalogSearchRules.cs** — `MatchesMercadoLivreAccount` / `ResolveMercadoLivreAffiliateId` (espaço em "Mercado Livre").
- [x] **MercadoLivreApiService.cs** — loja ativa por enum ou nome compacto; Affiliate ID habilita busca sem OAuth e injeta `matt_tool`.
- [x] **BuscaGlobal.razor** — botão Conectar só no estado `missing`; badge 🟢 quando a loja cadastrada retorna produtos.
- [x] **MercadoLivreHtmlParser.cs** / **MercadoLivreHtmlParserTests.cs** — cards da vitrine `lista.mercadolivre.com.br`.
- [x] **MercadoLivreApiService.cs** — fallback web em HTTP 403 e `InjectMattTool` no permalink.
- [x] **OfficialCatalogSearchFilter** / **BuscaGlobal.razor** — envia `MercadoLivreTrackingId` da loja ativa.
- [x] **MercadoLivreHtmlParser.cs** — itera todos os `ui-search-layout__item` / `ui-search-result__content`.
- [x] **OfficialCatalogSearchService.cs** — `PassesOptionalFilters` (preço/cupom) sem exigir a keyword no título.
- [x] **BuscaGlobal.razor** — grid com `_items.Count > 0` e `InvokeAsync(StateHasChanged)`.
- [x] **MercadoLivreApiService.cs** — não devolve lista vazia em HTTP ≠ 200; `LogError` e `Failed("ML HTTP …")`.
- [x] **MercadoLivreHtmlParser.cs** — HtmlAgilityPack (`ui-search-layout__item` / `ui-search-result`) + regex de fallback.
- [x] **Ofertio.Business.csproj** — pacote `HtmlAgilityPack` 1.11.72.
- [x] **BuscaGlobal.razor** — alerta vermelho com `ex.Message` / mensagem do canal `error`; `StateHasChanged` no `finally`.
- [x] **MercadoLivreApiService.cs** — `HttpClient` `MercadoLivreLista` com cookies, descompressão e headers Chrome para a vitrine.
- [x] **WhatsAppEvolutionRegistrationExtensions.cs** — registro do named client `MercadoLivreLista`.
- [x] **MercadoLivreHtmlParser.cs** — seletor `poly-card` / `poly-component__title`.
- [x] **OfficialCatalogSearchRules.cs** — badge `ok` com `{n} produtos` mesmo se houver erro HTTP prévio.
- [x] **BuscaGlobal.razor** — `_error = null` quando a grid recebe ofertas.
- [x] **MercadoLivreApiService.cs** — busca MLB só com OAuth (`Bearer` + `access_token`); ID de afiliado não dispara API anônima nem HTML da vitrine.
- [x] **OfficialCatalogSearchRules.cs** — mensagens `Requer OAuth...` e `API MLB recusou a busca (HTTP 403)...`.
- [x] **MercadoLivreIntegrationOptions.cs** — `AuthorizeUrl` e `RedirectUri` (HTTPS cadastrada no app ML).
- [x] **MarketplaceOAuthExchangeResult.cs** (novo) / **IMarketplaceAuthService.cs** — `ExchangeMercadoLivreCodeAsync`.
- [x] **MarketplaceAuthService.cs** — URL `auth.mercadolivre.com.br/authorization` e troca `authorization_code` no `oauth/token`.
- [x] **IntegracaoLojaService.cs** — code ML grava token real na loja ML existente, preservando `TrackingId`.
- [x] **HomologMarketplaceAuth.cs** — `IsStubToken`; **MercadoLivreApiService.cs** ignora o placeholder e persiste refresh rotacionado.
- [x] **MarketplaceAccountDto.cs** / **MarketplaceAccountMapper.cs** — `HasOAuthToken` (sem expor token).
- [x] **IntegracaoLojaDto.cs** — `RedirectUri` enviada na troca do code.
- [x] **IIntegracaoLojaPendingLinkStore.cs** / **IntegracaoLojaPendingLinkStore.cs** — `CallbackUri` + `Peek`.
- [x] **IntegracoesEndpointExtensions.cs** — callback ML sem `shopId`; reenvia do túnel HTTPS para a origem autenticada.
- [x] **MarketplaceStoreCard.razor** / **MinhasLojas.razor** / **ConnectStoreModal.razor** — botão OAuth do Mercado Livre e status da API de busca.
- [x] **MarketplaceAuthController.cs** / **IntegracaoLojaApiService.cs** — erro de configuração ML exibido na tela.
- [x] **appsettings.json** / **appsettings.Homologacao.json** (API) — `AuthorizeUrl` e `RedirectUri` do Mercado Livre.
- [x] **MarketplaceAuthServiceTests.cs** / **IntegracaoLojaServiceTests.cs** / **MercadoLivreApiServiceTests.cs** / **MarketplaceOAuthConnectServiceTests.cs**.

### Fase 33 README — Expurgo e retenção de dados

- [x] **DataPurgeRules.cs** / **DataPurgeRulesTests.cs** — mídia 7 dias, mensagens 14 dias, lote 5000 e delay 03:00 UTC.
- [x] **IDataPurgeServices.cs** / **DataPurgeService.cs** / **DataPurgeHost** / **DataPurgeWorker.cs**.
- [x] **ColdStorageArchiver.cs** — extrai SKU/preço/cupom para `ProductPriceHistory` antes do hard delete.
- [x] **AddProductPriceHistoryCouponCode** — coluna `CouponCode` no histórico condensado.
- [x] **ProductImageCleanupService.cs** — retenção alinhada a 7 dias, JPG/PNG e órfãos em disco.

### Fase 34 README — Pipeline de mídia e compressão

- [x] **ImageOptimizationRules.cs** / **StrictImageIngestionPolicy** / **ImageOptimizationRulesTests.cs** — 1080px, qualidade 75–80, canvas `#F4F4F4`.
- [x] **IImageOptimizationService.cs** / **ImageOptimizationService.cs** / **ImageOptimizationServiceTests.cs** — WebP, EXIF strip e rejeição de formato inválido.
- [x] **OfferProductMediaStore.cs** / **TelegramUserMonitorHost.cs** / **OfferMediaStudio.cs** — interceptador único; sem `File.Create` cru em `uploads`.
- [x] **ProductImageStorageRules.cs** — nome `{messageId}.webp` e varredura jpg/png/webp.

### Módulo 31 — Pipeline de imagens capturadas

- [x] **ProductImageStorageRules.cs** / **ProductImageStorageRulesTests.cs** — pasta `{tenant}/{yyyy}/{MM}` e nome `{messageId}.webp`.
- [x] **OfferProductMediaStore.cs** — grava caminho relativo em `GroupCapturedMessages.ProductImageUrl`.
- [x] **ProductImageCleanupService.cs** / **ProductImageCleanupWorker.cs** — expurgo diário após 7 dias.
- [x] **GruposMonitorados.razor** — `<img src="@item.ProductImageUrl">` com placeholder se a foto foi expurgada.

### Módulo 32 — Sincronização assíncrona de grupos

- [x] **MonitoredGroupSyncRules.cs** / **MonitoredGroupSyncRulesTests.cs** — mensagem 202 e timeout mínimo de 3 min.
- [x] **TelegramUserMonitorHost.cs** — `EnqueueCatchUp` via `Channel` processado pelo worker.
- [x] **MonitoredGroupsController.cs** — `Accepted` (HTTP 202) na sincronização.
- [x] **GruposMonitorados.razor** — `_isSyncing` e badge animado em background.

### Módulo 33 — Resiliência do UserBot em background

- [x] **UserBotRuntimeRules.cs** / **UserBotRuntimeRulesTests.cs** — cria `wwwroot/uploads/products` e `App_Data/telegram-sessions`.
- [x] **IUserBotSyncStatusService.cs** / **UserBotSyncStatusService.cs** / **UserBotSyncStatusDto.cs** — status Idle/Running/Completed/Failed por usuário.
- [x] **TelegramUserMonitorWorker.cs** — `StartAsync` cria pastas; `ExecuteAsync` com try-catch e `LogError`.
- [x] **TelegramUserMonitorHost.cs** — falhas de catch-up/auth/pasta em `MarkFailed`.
- [x] **MonitoredGroupsController.cs** — `GET .../status` (`GetSyncStatus`).
- [x] **GruposMonitorados.razor** — poll do status e toast vermelho com a mensagem exata.

### Módulo 34 — Parser estruturado de ofertas Telegram

- [x] **OfferDataExtraction.cs** — título, preço, cupom, URL principal e plataforma.
- [x] **StructuredOfferParserService.cs** / **IStructuredOfferParserService.cs** / **StructuredOfferParserServiceTests.cs**.
- [x] **GroupCapturedMessage** — colunas `CouponCode` e `PrimaryProductUrl`.
- [x] **GroupOfferCaptureService.cs** — persiste extração na captura.
- [x] **GruposMonitorados.razor** — badge de cupom ao lado do preço/título.

### Módulo 35 — Renderização reativa e ImageUrl web

- [x] **ProductImageStorageRules.ToWebRelativePath** — remove `wwwroot` e troca `\` por `/`.
- [x] **GroupCapturedMessagesService.ApplyStructuredParse** — preenche título, preço, cupom, URL e sanitiza foto.
- [x] **GruposMonitorados.razor** — `Title`/`Price`/`ImageUrl`, badge de cupom e botão **Atualizar Feed**.

### Módulo 36 — Paginação e download de mídia da página visível

- [x] **GroupOfferCaptureRules** — page sizes 10/25/50/100 (padrão 25) e `Skip` por página.
- [x] **MonitoredGroupService.ListAsync** — `OrderByDescending(CreatedAt).Skip/Take`.
- [x] **TelegramUserMonitorHost** — fila alta/baixa de fotos; `POST .../midia/priorizar`.
- [x] **GruposMonitorados.razor** — paginação no topo/rodapé, skeleton "Carregando mídia..." e **Atualizar Feed**.

### Módulo 37 — Download físico e backfill de ImageUrl

- [x] **OfferProductMediaStore.SaveFromStreamAsync** — `File.Create` + stream do Telegram em `wwwroot/uploads/products/{tenant}/{yyyy}/{MM}/`.
- [x] **TelegramUserMonitorHost** — `DownloadMediaAsync` na leitura de `MessageMediaPhoto`, log `Imagem salva no caminho` e backfill (`ProductImageUrl` nulo ou `!File.Exists`).
- [x] **GroupCapturedMessagesService.SetProductImageUrlAsync** — força a coluna `ProductImageUrl` (alias `ImageUrl` no Blazor).
- [x] **GruposMonitorados.razor** — `<img src="@item.ImageUrl" />`.

### Módulo 38 — Vínculo SQL imediato e backfill de arquivos em disco

- [x] **GroupCapturedMessagesService.UpdateImageUrlAsync** — `ExecuteSqlInterpolatedAsync` em `ProductImageUrl`/`ExternalMessageId` sem carregar entidades.
- [x] **ProductImageUrlLinkBackfillService** — execução única no `StartAsync` mapeando `wwwroot/uploads/products` para o banco.
- [x] **MonitoredGroupService.ListAsync** — `AsNoTracking` + `OrderByDescending(CreatedAt)` + `Skip/Take`.
- [x] **GruposMonitorados.razor** — `<img src="@item.ImageUrl" />` ou placeholder "Processando mídia...".

### Módulo 39 — Vínculo disco → ProductImageUrl por ExternalMessageId

- [x] **BackfillMediaPathsService.cs** — na inicialização busca `ProductImageUrl` vazio e associa `{ExternalMessageId}_*.jpg` em `uploads/products`.
- [x] **GroupCapturedMessagesService.UpdateImageUrlAsync** — `ExecuteSqlRawAsync` em `ProductImageUrl`/`ExternalMessageId` (sem filtro de UserId).
- [x] **GruposMonitorados.razor** — `src` com barra inicial obrigatória.

### Módulo 40 — Nome `{telegramId}.jpg` e vínculo na carga da página 1

- [x] **ProductImageStorageRules.BuildFileName** — grava `{ExternalMessageId}.jpg` (sem GUID).
- [x] **GroupCapturedMessagesService.LinkExistingDownloadedImagesAsync** — `UPDATE ProductImageUrl` por `ExternalMessageId` a partir dos `.jpg` em disco.
- [x] **GruposMonitorados.razor** — `POST midia/vincular-disco` antes do `ListAsync` da página 1 e prioriza as 25 fotos visíveis.

### Módulo 41 — Vínculo da página visível por Id e `{msg.id}.jpg`

- [x] **TelegramUserMonitorHost.PersistTelegramPhotoAsync** — grava `{msg.id}.jpg` e `UPDATE ProductImageUrl WHERE ExternalMessageId`.
- [x] **GroupCapturedMessagesService.LinkDownloadedImagesForCurrentPageAsync** — para os 25 cards, busca `{id}.jpg`/`{id}_*.jpg` e atualiza por `Id`.
- [x] **MonitoredGroupService.ListAsync** — vincula após o `Skip/Take` e devolve `ImageUrl` já preenchido.
- [x] **GruposMonitorados.razor** — `StateHasChanged` após carregar a página.

### Módulo 42 — Empacotamento atômico de ofertas

- [x] **OfferPipelineProcessor.cs** — Parse ➔ filtro de loja ➔ foto `{msg.id}.jpg` ➔ validação ➔ `SaveValidatedOfferAsync`.
- [x] **GroupCapturedMessagesService.SaveValidatedOfferAsync** — INSERT único com título, preço, cupom, `ProductImageUrl` e plataforma.
- [x] **MonitoredGroupService.ListAsync** — `AsNoTracking` + pacote completo (Telegram exige imagem local).
- [x] **GruposMonitorados.razor** — lista paginada sem vínculo retroativo nem download sob demanda.

### Módulo 43 — Purge e ressincronização atômica

- [x] **ProductImageStorageRules.cs** — `TryResolveTenantProductsFolder` e `TryDeleteTenantProductFiles` (File.Delete por arquivo, ignora lock do IIS).
- [x] **IGroupCapturedMessagesService.cs** / **GroupCapturedMessagesService.cs** — `ResetAllCapturedMessagesAndMediaAsync` (`DELETE WHERE UserId` + exclusão segura em disco).
- [x] **IGroupOfferServices.cs** / **MonitoredGroupService.cs** — `ResetAndResyncAsync` só faz purge (`DELETE` + arquivos); não dispara `SyncAsync`.
- [x] **MonitoredGroupsController.cs** — `POST api/integracoes/grupos/monitorados/resetar` retorna HTTP 200.
- [x] **IMonitoredGroupsApiService.cs** — cliente `ResetAndResyncAsync`.
- [x] **GruposMonitorados.razor** — botão **Limpar** (`HandleResetAllAsync`); sincronização só no botão **Sincronizar grupos**.
- [x] **GroupOfferCaptureRules.cs** — `TemporaryLimitTenOffers = false`; paginação 10/25/50/100 (padrão 25).
- [x] **TelegramUserMonitorRules.cs** / **TelegramUserMonitorHost.cs** — catch-up para ao gravar 10 ofertas com foto.
- [x] **ProductUploadsStaticFilesExtensions.cs** — mapeia `/uploads` para `inetpub/ofertio/api/wwwroot/uploads`.
- [x] **GroupOfferCaptureRules.cs** — `DeduplicateOffers` / `IsSameCapturedOffer` por mensagem e URL.
- [x] **GroupCapturedMessagesService.cs** — `SaveValidatedOfferAsync` recusa duplicata de `ExternalMessageId` ou URL.
- [x] **MonitoredGroupService.ListAsync** / **GruposMonitorados.razor** — lista e tela só aceitam oferta nova se ainda não estiver visível.
- [x] **HomologDemoUserSeeder.cs** / **Program.cs** — seed de demo não aborta o start da API (evita IIS 500.30).

### Fases 35 a 41 — Backoffice & Gestão Master SaaS

**Core**
- [x] **UserAccount.cs** — `Role` (SuperAdmin / SuporteTecnico / FinanceiroSaaS / TenantUser) e `IsActive`.
- [x] **Tenant.cs** — `Status`, `StatusReason`, `PlanCode`, `BillingCycle`, `TrialEndsAt`, `RenewalAt`, `CanceledAt`, IDs do gateway.
- [x] **MasterAuditLog.cs** / **SystemSetting.cs** / **SaasPlan.cs** / **SaasPayment.cs** / **SystemBanner.cs** / **TenantNotification.cs** / **ProductChangelogEntry.cs** — novas entidades do backoffice.
- [x] **OfertioClaimTypes.cs** — claims de emulação (`ImpersonatorId`, `ImpersonatorAccessToken`, `ImpersonatedTenantName`, `ViewMode`).

**Business**
- [x] **Service/Master/MasterRbac.cs** — `MasterRoles`, `MasterPermissions`, `MasterPolicies`, `MasterPermissionMatrix`.
- [x] **Service/Master/TenantLifecycleRules.cs** — status, ações (pausar/suspender/reativar/cancelar/estender trial), carência de inadimplência.
- [x] **Service/Master/SaasMetricsRules.cs** — MRR, ARR, ARPU, churn 30d, conversão de trial, novos assinantes por mês.
- [x] **Service/Master/SystemSettingCatalog.cs** — catálogo de chaves globais (marketplaces, mensageria, cofre, ASAAS) e máscara.
- [x] **Service/Master/PlanLimitRules.cs** — `PlanLimits`, `SaasPlanCodes`, limites de lojas/agendamentos/radar/cota VIP.
- [x] **Dto/MasterDtos.cs** — DTOs do painel master, webhook ASAAS e comunicação.
- [x] **Interfaces/Services/IMasterServices.cs** — contratos dos serviços master e do gateway de pagamento.

**Database / Data**
- [x] **AppDbContext.cs** — DbSets master, `SystemSetting.Value` criptografado e `GuardImmutableAuditTrail`.
- [x] **Migrations/20261012234523_AddMasterBackofficePhase35to41.cs** — tabelas, índices, seed de planos e SuperAdmin.

**Infrastructure**
- [x] **Security/JwtTokenService.cs** — claim de papel e `GenerateImpersonationToken`.
- [x] **Security/PlatformAuthService.cs** — bloqueia usuário inativo e tenant pausado/suspenso/cancelado no login.
- [x] **Master/MasterAuditService.cs** / **MasterAuditActions.cs** — trilha imutável.
- [x] **Master/MasterOperatorService.cs** — CRUD de operadores internos.
- [x] **Master/DatabaseSettingsConfigurationProvider.cs** / **SystemSettingsService.cs** — cofre criptografado no banco como fonte de configuração.
- [x] **Master/SaasPlanService.cs** / **PlanScheduleGuard.cs** — planos e enforcement de limites.
- [x] **Master/TenantAdminService.cs** / **TenantAccessReader.cs** — Tenant 360°, ciclo de vida, créditos, armazenamento e purge.
- [x] **Master/AsaasPaymentGateway.cs** / **AsaasWebhookService.cs** — assinatura recorrente e baixa/bloqueio automático.
- [x] **Master/ImpersonationService.cs** — emulação auditada.
- [x] **Master/MasterMetricsService.cs** / **ApiRequestCounter.cs** — dashboard executivo e uso 24h.
- [x] **Master/SystemCommunicationService.cs** — banners, notificações segmentadas e changelog.
- [x] **Master/MasterBackofficeRegistrationExtensions.cs** — DI do backoffice.
- [x] **WhatsAppBroadcastService.cs** / **TelegramBroadcastService.cs** / **IntegracaoLojaService.cs** / **DealCreditsService.cs** — aplicam limites do plano.

**API**
- [x] **Controllers/Master/MasterControllerBase.cs** / **MasterAccessController.cs** / **MasterSettingsController.cs** / **MasterTenantsController.cs** / **MasterInsightsController.cs**.
- [x] **Controllers/SystemCommunicationController.cs** / **AsaasWebhookController.cs**.
- [x] **Security/MasterAuthorizationExtensions.cs** / **MasterScopeAttribute.cs** / **RadarPlanGateAttribute.cs**.
- [x] **Middlewares/TenantAccessGuardMiddleware.cs** / **ApiRequestCounterMiddleware.cs**.
- [x] **Workers/TenantBillingWorker.cs** — ciclo horário de trial vencido e suspensão por inadimplência.

**SharedUi / WebUi**
- [x] **Services/Auth/JwtPayloadReader.cs** / **MasterAccess.cs** — leitura de papel e emulação no cookie.
- [x] **Services/Master/MasterApiService.cs** / **SystemCommunicationApiService.cs**.
- [x] **Components/Master/** — `MasterPageBase`, `MasterAlerts`, `MasterProfileSwitcher`, `ImpersonationBanner`, `GlobalAnnouncements`.
- [x] **Pages/Master/** — `MasterDashboard`, `MasterTenants`, `MasterOperators`, `MasterAudit`, `MasterSettings`, `MasterPlans`, `MasterCommunication`.
- [x] **Pages/Novidades.razor** — changelog para clientes.
- [x] **MainLayout.razor** / **NavMenu.razor** — banner de emulação, alternância de perfil, avisos e grupo "Painel Master".
- [x] **WebUi/Extensions/MasterEndpointExtensions.cs** / **AuthCookieService.cs** — troca de visão, entrar/sair da emulação.

**Testes**
- [x] **Unit/Master/MasterRulesTests.cs** / **MasterInfrastructureTests.cs** / **MasterServicesTests.cs** — RBAC, ciclo de vida, métricas, limites, cofre, auditoria imutável.

### Fases 42 a 44 — Rotas Master, contas de desenvolvedor e menu por plano

**Core / Database / Data**
- [x] **MenuItem.cs** / **PlanMenuPermission.cs** — árvore de menus e visibilidade por plano (sem registro = visível).
- [x] **SystemDeveloperAccount.cs** — App Credentials e contas de teste em escopo de sistema (segredos criptografados).
- [x] **AppDbContext.cs** — DbSets `MenuItems`, `PlanMenuPermissions`, `SystemDeveloperAccounts`; `ClientSecret`/`TestPassword` com `EncryptedStringConverter`.
- [x] **Migrations/20261013012520_AddMasterRoutesDevAccountsMenuPhase42to44.cs** — criação das 3 tabelas.

**Business**
- [x] **Service/Master/MenuCatalog.cs** — `DefaultMenuCatalog` (árvore produtiva do cliente, Radar oculto no Starter) e `MenuVisibilityRules` (BuildTree / Flatten).
- [x] **Service/Master/DeveloperAccountRules.cs** — plataformas, tipos, ambientes, validade do segredo e mapeamento para o cofre global.
- [x] **Dto/MasterDtos.cs** — `ClientMenuNodeDto`, `MenuPermissionMatrixDto`, `MenuPermissionSaveDto`, `DeveloperAccountDto`, `DeveloperAccountCheckDto`.
- [x] **Interfaces/Services/IMasterServices.cs** — `IMenuPermissionService`, `IDeveloperAccountService`, `IDeveloperAccountHealthChecker`.

**Infrastructure**
- [x] **Master/MenuPermissionService.cs** — seed idempotente do catálogo, matriz por plano, menu do cliente com cache de 60s.
- [x] **Master/DeveloperAccountService.cs** — CRUD, publicação como credencial global (via `ISystemSettingsService`), diagnóstico por conta e das chaves em uso.
- [x] **Master/DeveloperAccountHealthChecker.cs** — testes reais: OAuth client_credentials (ML), GraphQL Afiliados (Shopee), SearchItems SigV4 (Amazon).
- [x] **Radar/AmazonPaApiService.cs** — assinatura SigV4 extraída para `CreateSignedRequest` (reuso no diagnóstico).
- [x] **Master/MasterBackofficeRegistrationExtensions.cs** / **MasterAuditActions.cs** — DI e ações auditadas novas.

**API**
- [x] **Controllers/Master/MasterDeveloperAccountsController.cs** — `api/master/marketplace-accounts` (CRUD, `/publicar`, `/testar`, `/saude-global`).
- [x] **Controllers/Master/MasterMenuPermissionsController.cs** — `api/master/menu-permissions` (GET/PUT).
- [x] **Controllers/SystemCommunicationController.cs** — `GET api/sistema/menu` (sidebar por plano).

**SharedUi / WebUi**
- [x] **Services/Navigation/MasterRoutes.cs** — convenção `/master/*`, links do console e isenção da saída de emulação.
- [x] **Components/Layout/MasterMainLayout.razor** + **wwwroot/app.css** (`.master-*`) — layout executivo do console.
- [x] **Pages/Master/MasterDeveloperAccounts.razor** (`/master/marketplace-accounts`) / **MasterMenuPermissions.razor** (`/master/menu-permissions`).
- [x] **Pages/Master/*.razor** — `@layout MasterMainLayout`; `MasterOperators` também em `/master/usuarios`.
- [x] **Components/Layout/NavMenu.razor** — sidebar dinâmica via API; "Próximos Desenvolvimentos" só na visão master.
- [x] **Services/Master/MasterApiService.cs** / **SystemCommunicationApiService.cs** — clientes das novas rotas.
- [x] **WebUi/Extensions/MasterRouteGuardExtensions.cs** / **Program.cs** — `UseMasterRouteGuard()` redireciona não-masters para `/`.

**Testes**
- [x] **Unit/Master/MasterPhase42To44RulesTests.cs** / **MasterPhase42To44ServicesTests.cs** — rotas, catálogo, visibilidade, matriz, contas de desenvolvedor e diagnóstico HTTP.

### Fase 45 — Navegação, hierarquia da sidebar, paletas e segregação do Vault

**SharedUi / WebUi**
- [x] **Components/Layout/NavigationScrollReset.razor** — ilha interativa que escuta `LocationChanged` e chama `ofertioAppInterop.resetScroll`.
- [x] **wwwroot/app-interop.js** — `resetScroll` (janela + `.main-content`) e fallback no evento `enhancedload`.
- [x] **Components/Layout/MainLayout.razor** / **MasterMainLayout.razor** — `.main-content`, `NavigationScrollReset`; Master com `data-bs-theme="dark"`.
- [x] **Components/Layout/NavMenu.razor** — renderização Categoria → Menu pai → Submenu.
- [x] **wwwroot/app.css** — paleta clara do cliente, hierarquia da sidebar e identidade slate/laranja do Master.
- [x] **Services/Navigation/MasterRoutes.cs** — `Vault` (`/master/vault`), `LegacySettings`, rótulos do console.
- [x] **Pages/Master/MasterSettings.razor** — `/master/vault` (+ alias `/master/configuracoes`) com "Testar chaves em uso".
- [x] **Pages/Master/MasterDeveloperAccounts.razor** — somente contas corporativas de teste/dev (sem publicar no cofre).
- [x] **Services/Master/MasterApiService.cs** — `CheckGlobalKeysAsync`; removido `PublishDeveloperAccountAsync`.

**Business / Infrastructure / API / Data**
- [x] **Service/Master/MenuCatalog.cs** — categoria "Canais" e menus pai de Integrações (Links & Ofertas, Conteúdo & Vitrines, Saúde & Resultados).
- [x] **Master/MenuPermissionService.cs** — `EnsureCatalogAsync` sincroniza pai/ordem/título/ícone/rota dos itens existentes.
- [x] **Master/GlobalCredentialHealthService.cs** (novo) + **IMasterServices.cs** (`IGlobalCredentialHealthService`) — diagnóstico das chaves do cofre.
- [x] **Master/DeveloperAccountService.cs** / **DeveloperAccountRules.cs** / **MasterAuditActions.cs** — removida a publicação no cofre global.
- [x] **Controllers/Master/MasterSettingsController.cs** — `POST api/master/configuracoes/saude-chaves`; **MasterDeveloperAccountsController.cs** sem `/publicar` e `/saude-global`.
- [x] **SystemDeveloperAccount.cs** / **MasterDtos.cs** — removido `IsGlobalDefault`.
- [x] **Migrations/20261013022329_SegregateVaultFromDeveloperAccountsPhase45.cs** — drop da coluna `IsGlobalDefault`.

**Testes**
- [x] **MasterPhase42To44RulesTests.cs** / **MasterPhase42To44ServicesTests.cs** — hierarquia do catálogo, migração de itens legados, contas de teste e `GlobalCredentialHealthService`.

### Fase 46 — Menu dinâmico com drag & drop e roadmap no banco

**Core / Database / Data**
- [x] **MenuItem.cs** — `OrderIndex` (antigo `Order`), `IsFolder`, `Badge`, `IsCustomized`.
- [x] **AppDbContext.cs** — `Badge` (30) e índice `(ParentId, OrderIndex)`.
- [x] **Migrations/20261013225515_MenuDragDropAndRoadmapPhase46.cs** — rename `Order`→`OrderIndex`, novas colunas e `IsFolder = 1` para itens sem rota.

**Business**
- [x] **Service/Master/MenuCatalog.cs** — pasta "Próximos Desenvolvimentos" + 6 subitens (`HiddenForAllPlans`, selo "EM BREVE"), `MenuTreeEditRules` (ciclos, reordenação, título de pasta).
- [x] **Dto/MasterDtos.cs** — `Badge`/`IsFolder` no nó do cliente, `ParentId`/`IsCustomFolder` na linha da matriz, `MenuMoveDto`, `MenuFolderCreateDto`, `MenuDropPositions`.
- [x] **Interfaces/Services/IMasterServices.cs** — `MoveAsync`, `CreateFolderAsync`, `DeleteFolderAsync`, `GetClientMenuAsync(clientView)`.

**Infrastructure / API**
- [x] **Master/MenuPermissionService.cs** — mover/agrupar/excluir pasta, sincronização do catálogo respeitando `IsCustomized`, visão cliente para masters.
- [x] **Master/MasterAuditActions.cs** — `menu.estrutura`.
- [x] **Controllers/Master/MasterMenuPermissionsController.cs** — `POST mover`, `POST pastas`, `DELETE pastas/{id}`.
- [x] **Controllers/SystemCommunicationController.cs** — `GET api/sistema/menu?visao=cliente`.

**SharedUi**
- [x] **Pages/Master/MasterMenuPermissions.razor** — linhas arrastáveis, modal "Nova pasta", exclusão de pasta vazia.
- [x] **Components/Layout/NavMenu.razor** — árvore do banco (sem accordion fixo), pastas aninhadas, selos, recarga por navegação.
- [x] **wwwroot/app-interop.js** — `ofertioMenuDnd` (zona antes/dentro/depois e indicadores visuais); **wwwroot/app.css** — estilos do drag & drop.
- [x] **Services/Master/MasterApiService.cs** / **SystemCommunicationApiService.cs** — clientes das novas rotas.
- [x] **Pages/Dashboard.razor**, **EngajamentoFila.razor**, **ConciliacaoFinanceira.razor**, **ProdutosPropaganda.razor**, **PainelSaude.razor** — rotas novas do roadmap (`/campanhas`, `/metricas`, `/fila-engajamento`, `/conciliacao-financeira`, `/produtos-propaganda`, `/saude-sistema`).

**Testes**
- [x] **MasterPhase42To44RulesTests.cs** (`MenuTreeEditRulesTests`, roadmap no catálogo) / **MasterPhase42To44ServicesTests.cs** (matriz com roadmap, visão cliente, mover, agrupar, excluir pasta).

### Fase 47 - Cofre de chaves: alterações em segredos mascarados

- [x] **Ofertio.Business/Service/Master/SystemSettingCatalog.cs** - `IsUnchangedMask` (comparação exata com a máscara atual) e `ContainsMaskCharacter`.
- [x] **Ofertio.Infrastructure.Services/Master/SystemSettingsService.cs** - ignora só a máscara idêntica, rejeita máscara editada sem gravar nada e salva o restante com AES + recarga da configuração.
- [x] **Ofertio.SharedUi/Components/Pages/Master/MasterSettings.razor** (`/master/vault`) - `@bind-value:event="oninput"`, `_hasChanges`, selo "Alterações não salvas", destaque do campo alterado, seleção da máscara ao focar e ícone de olho (`_showSecretMap` / `ToggleVisibility` / `IsSecretVisible`) para mostrar/ocultar segredos.
- [x] **Ofertio.Tests/Unit/Master/MasterRulesTests.cs** / **MasterServicesTests.cs** - regras da máscara e gravação/rejeição de segredos.

### MLBSearchOAuthFallback - Token de aplicação e vitrine na busca do Mercado Livre

- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreApiService.cs** - `IOptionsMonitor` (Vault ao vivo), token de usuário → token de aplicação `client_credentials` em cache → `Authorization: Bearer`; 401/403/escopo cai em `SearchViaListaAsync` (`lista.mercadolivre.com.br`) com `matt_tool`.
- [x] **Ofertio.Business/Service/Radar/OfficialCatalogSearchRules.cs** - `IsMercadoLivreSearchRefused`, `IsMercadoLivreVerificationPage`, `CountVisibleOffers` e nova mensagem de 403.
- [x] **Ofertio.Infrastructure.Services/Radar/OfficialCatalogSearchService.cs** - badge do Mercado Livre com a contagem de ofertas visíveis.
- [x] **Ofertio.Tests/Unit/Radar/MercadoLivreApiServiceTests.cs** / **OfficialCatalogSearchRulesTests.cs** - token de aplicação (e cache), fallback em 403/escopo, vitrine bloqueada e contagem do badge.
- [x] **Ofertio.Business/Integrations/Auth/OAuthRedirectUriNormalizer.cs** (novo) - `redirect_uri` limpa para túneis Ngrok (https, sem porta local, sem query/fragmento, caminho `/integracoes/oauth/callback`).
- [x] **Ofertio.Infrastructure.Services/Integrations/Auth/MarketplaceAuthService.cs** - `ResolveMercadoLivreRedirectUri` normaliza a URI no authorize e na troca do code.
- [x] **Ofertio.WebUi/Extensions/IntegracoesEndpointExtensions.cs** / **Ofertio.SharedUi/Components/Pages/MinhasLojas.razor** - callback pendente gravado já normalizado (retorno à origem sem porta).
- [x] **Ofertio.Tests/Unit/Integrations/OAuthRedirectUriNormalizerTests.cs** (novo) / **MarketplaceAuthServiceTests.cs** - Ngrok com `:7002`/`:5000`, origem sem caminho e hosts locais preservados.

### Callback do Mercado Livre sem sessão (state selado)

- [x] **Ofertio.Business/Integrations/Auth/MarketplaceOAuthState.cs** (novo) - `MarketplaceOAuthState`, `IOAuthStateProtector` e `MarketplaceOAuthStateRules` (prefixo `tfs1.`, validade, URL de retorno `/marketplace-accounts?status=`).
- [x] **Ofertio.Business/Dto/MarketplaceOAuthCallbackDtos.cs** (novo) - request (`Code`, `State`, `Error`) e resultado (`Success`, `Message`, `ReturnUrl`).
- [x] **Ofertio.Infrastructure.Services/Integrations/Auth/OAuthStateProtector.cs** (novo) - AES-256-GCM com chave derivada por HKDF de `Encryption:Key`; registrado em `MarketplaceAuthRegistrationExtensions`.
- [x] **Ofertio.API/Controllers/MarketplaceAuthController.cs** - `authorize-url` sela o state do ML para o usuário do JWT; `POST mercadolivre/callback` anônimo com nonce de uso único (`IMemoryCache`) e identidade do state para os filtros de tenant.
- [x] **Ofertio.WebUi/Extensions/IntegracoesEndpointExtensions.cs** - callback `.AllowAnonymous()`, repasse do state selado à API e redirecionamento ao `ReturnUrl`; `/oauth/start` envia JWT e apelido.
- [x] **Ofertio.SharedUi/Components/Pages/MinhasLojas.razor** - feedback de `?status=success|error` em `/marketplace-accounts`.
- [x] **Ofertio.Tests/Unit/Integrations/MarketplaceOAuthStateTests.cs** (novo) / **Controllers/MarketplaceAuthControllerTests.cs** - selagem, adulteração/expiração, retorno à origem, callback anônimo, replay e state forjado.

### MLBOAuthSearchTokenFix - Token OAuth da conta do tenant na busca MLB

- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreApiService.cs** - conta ML ativa filtrada pelo `TenantId` do usuário; `Bearer` da conta por requisição; HTTP 401/403 → `RefreshAccountTokenAsync` (`refresh_token` com `AppId`/`SecretKey` do Vault), persistência e nova tentativa antes da vitrine; log `[ML Busca]` com o nome da conta.
- [x] **Ofertio.Tests/Unit/Radar/MercadoLivreApiServiceTests.cs** - 403 com token vigente → refresh → retry com o novo Bearer e banco atualizado; falha no refresh → vitrine; isolamento por tenant.

### MLBScraperPrimary - Vitrine pública como motor primário da busca ML

- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreIntegrationOptions.cs** - `UseOfficialSearchApi` (padrão `false`).
- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreApiService.cs** - sem API oficial vai direto a `SearchViaListaAsync` (retorna ofertas + flag de bloqueio), headers de Chrome 129 (`ApplyDocumentHeaders`) e log `[ML Busca]` com a contagem.
- [x] **Ofertio.Infrastructure.Services/WhatsApp/WhatsAppEvolutionRegistrationExtensions.cs** - cliente do ML com `HttpClientHandler` (`GZip | Deflate | Brotli`).

### MLBPublicSearch - Busca pública leve do Mercado Livre

- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreApiService.cs** - `TrySearchPublicApiAsync` (`sites/MLB/search` anônimo, sem `access_token`) antes da vitrine; `SearchViaApiAsync` aceita token nulo.
- [x] **Ofertio.Tests/Unit/Radar/MercadoLivreApiServiceTests.cs** - API pública com sucesso (`matt_tool=97303724`, sem Authorization), 403 → vitrine, tudo bloqueado → mensagem amigável.
- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreHtmlParser.cs** - preço atual com centavos, preço original (`--previous` / `<s>`), link `poly-component__title` e imagem `data-src` em lazy-load.
- [x] **Ofertio.Business/Service/Radar/OfficialCatalogSearchRules.cs** - detecção do `bot_challenge` (`_bmstate`, `micro-landing-container`) e `MercadoLivreScraperBlockedMessage`.
- [x] **Ofertio.Infrastructure.Services/WhatsApp/WhatsAppEvolutionRegistrationExtensions.cs** - `SocketsHttpHandler` com `AutomaticDecompression` no cliente do ML.
- [x] **Ofertio.Tests/Unit/Radar/MercadoLivreApiServiceTests.cs** / **MercadoLivreHtmlParserTests.cs** - vitrine primária com `matt_tool`, desafio anti-robô, preços/imagem do poly-card.

### MarketplaceTokensDataProtection - Criptografia em repouso dos tokens de marketplace

- [x] **Ofertio.Util/Security/ISecurityEncryptionService.cs** (novo) - contrato `Protect` / `Unprotect`.
- [x] **Ofertio.Util/Security/EntityFramework/EncryptedStringConverter.cs** - `CreateProtected(ISecurityEncryptionService)`.
- [x] **Ofertio.Infrastructure/Security/SecurityEncryptionService.cs** (novo) - `IDataProtector` (prefixo `DP1:`, purpose `TecFlow.MarketplaceTokens.v1`), leitura do AES `ENC1:` e do texto puro legado.
- [x] **Ofertio.Infrastructure/Security/DataProtectionRegistrationExtensions.cs** (novo) - `AddOfertioDataProtection` (app legado `TecFlow`, chaves em `App_Data/OfertioDataProtectionKeys` via `DataProtection:KeysPath`, DPAPI, Singleton).
- [x] **Ofertio.Database/AppDbContext.cs** - `ISecurityEncryptionService` opcional no construtor; conversor protegido em `MarketplaceAccount.AccessToken` / `RefreshToken` / `AppSecret`.
- [x] **Ofertio.Infrastructure.Services/InfrastructureDataServiceRegistrationExtensions.cs** - chama `AddOfertioDataProtection` junto do `IEncryptionService`.
- [x] **Ofertio.API/Program.cs** - `AddOfertioDataProtection(builder.Configuration)`.
- [x] **Ofertio.Tests/Unit/Security/SecurityEncryptionServiceTests.cs** (novo) - round trip `DP1:`, idempotência, AES legado, texto puro, anel de chaves diferente e conversor EF.

### TelegramMediaPersistenceFix - Gravação confiável das fotos do Telegram

- [x] **Ofertio.Business/Service/Groups/ProductImageStorageRules.cs** - `BuildRelativeDirectory` (`uploads/products/{tenant}/{yyyy}/{MM}` invariante), usado por `BuildSaveTarget` / `BuildPhysicalDirectory`.
- [x] **Ofertio.Infrastructure.Services/Groups/ImageOptimizationService.cs** - `CreateDirectory` com log, WebP em `.tmp` + `Flush(true)` + `File.Move`, confirmação `File.Exists`, fallback JPEG sem arquivo órfão e logs de falha com caminho.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferProductMediaStore.cs** - `try/catch` separados para download e conversão, `LogWarning` detalhado e URL só com arquivo físico confirmado.
- [x] **Ofertio.Infrastructure.Services/Telegram/TelegramUserMonitorHost.cs** - `PersistTelegramPhotoAsync` sem `UPDATE` cru e com log em Warning; reprocessamento grava por `UserId` + `GroupKey` e reverte para nulo quando o download falha.
- [x] **Ofertio.Business/Interfaces/Services/IGroupCapturedMessagesService.cs** / **Ofertio.Infrastructure.Services/Groups/GroupCapturedMessagesService.cs** - `UpdateImageUrlAsync` filtra `UserId`/`GroupKey` e exige arquivo no disco; novo `ClearMissingImageUrlAsync`; `ILogger` injetado.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferPipelineProcessor.cs** - ignora `ProductImageUrl` local sem arquivo no disco (Warning).
- [x] **Ofertio.Tests/Unit/Groups/OfferProductMediaStoreTests.cs** (novo) / **GroupCapturedMessagesServiceTests.cs** - pasta criada e arquivo confirmado, falha de download, falha de conversão sem arquivo e caminho com cultura `de-DE`.

### ExternalUploadsFolder - Uploads em C:\inetpub\ofertio\uploads

- [x] **Ofertio.Business/Service/Groups/UploadStorageRules.cs** (novo) - `UploadStorage:RootPath`, `/uploads`, `ResolveUploadsRoot` (configurado ou `{WebRootPath}/uploads`, exige pasta `uploads`) e `ResolveMediaRoot`.
- [x] **Ofertio.Business/Interfaces/Services/IUploadStorage.cs** (novo) - `UploadsRoot` / `MediaRoot`.
- [x] **Ofertio.Infrastructure.Services/Groups/UploadStorage.cs** (novo) - implementação + `AddOfertioUploadStorage` (Singleton), registrada em `WhatsAppEvolutionRegistrationExtensions` e `MasterBackofficeRegistrationExtensions`.
- [x] **Ofertio.Infrastructure.Services/Groups/ImageOptimizationService.cs** / **OfferProductMediaStore.cs** / **GroupCapturedMessagesService.cs** / **ProductImageCleanupService.cs** - `IUploadStorage` no lugar de `IWebHostEnvironment`.
- [x] **Ofertio.Infrastructure.Services/Master/TenantAdminService.cs** / **Radar/OfferMediaStudio.cs** - disco por tenant e vídeos na pasta externa.
- [x] **Ofertio.API/Workers/TelegramUserMonitorWorker.cs** - cria `uploads/products` na pasta externa.
- [x] **Ofertio.API/Program.cs** - `UseStaticFiles` com `PhysicalFileProvider(UploadsRoot)` em `/uploads`.
- [x] **Ofertio.WebUi/Extensions/ProductUploadsStaticFilesExtensions.cs** - pasta configurada como primeira opção de `/uploads`.
- [x] **Ofertio.API/appsettings.json** / **Ofertio.WebUi/appsettings.json** - `UploadStorage:RootPath = C:\inetpub\ofertio\uploads`.
- [x] **Ofertio.Tests/Unit/Groups/OfferProductMediaStoreTests.cs** / **ImageOptimizationServiceTests.cs** - gravação na pasta externa, raiz configurada, fallback para `WebRootPath` e rejeição de pasta fora do padrão.

### OfferIntegrityFilters - Vitrine genérica, preço suspeito e revalidação periódica

- [x] **Ofertio.Core/Entities/GroupOfferStatuses.cs** - novos status `Inativo`, `VitrineGenerica`, `PrecoSuspeito`, rótulos e helpers `IsInactive` / `IsRevalidatable`.
- [x] **Ofertio.Business/Service/Groups/OfferIntegrityRules.cs** (novo) - `IsGenericStorefrontUrl`, `ContainsClosedListingPhrase`, `IsHighValueProduct`, `IsSuspiciousPrice` (< R$ 5,00) e `ApplyPriceSanity`.
- [x] **Ofertio.Business/Service/Groups/OfferRevalidationOptions.cs** (novo) - seção `OfferRevalidation` (`Enabled`, `IntervalHours`, `InitialDelayMinutes`, `BatchSize`, `DelayBetweenChecksMs`) com limites.
- [x] **Ofertio.Business/Dto/OfferValidationDtos.cs** - `FinalUrl` no resultado e `OfferRevalidationSummaryDto`.
- [x] **Ofertio.Business/Interfaces/Services/IGroupOfferServices.cs** - `IOfferValidationService.ValidateFromPageAsync` e `IOfferRevalidationService`.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferValidationService.cs** - vitrine antes/depois de resolver e após o GET (`VitrineGenerica`), anúncio encerrado/pausado (`Inativo`), status da página repassado ao resultado.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferPipelineProcessor.cs** - rejeição de vitrine na entrada com log e `PrecoSuspeito` no lugar de `Ativo`.
- [x] **Ofertio.Infrastructure.Services/Radar/LiveCheckSearchService.cs** - checagem da página antes das APIs oficiais, inativação imediata e trava de preço ao gravar.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - clonagem bloqueada para status inativos e `PrecoSuspeito`.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferRevalidationService.cs** (novo) - `OfferRevalidationService` (lote de ofertas ativas vencidas) + `OfferRevalidationHost` (laço com intervalo configurável).
- [x] **Ofertio.Infrastructure.Services/WhatsApp/WhatsAppEvolutionRegistrationExtensions.cs** - registro das opções, do serviço (Scoped) e do host (Singleton).
- [x] **Ofertio.API/Workers/OfferRevalidationWorker.cs** (novo) - `BackgroundService` registrado em `Program.cs`.
- [x] **Ofertio.API/appsettings.json** - seção `OfferRevalidation` (2h, lote 200).
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - selos de `Inativo`, `Vitrine Genérica` e `Preço Suspeito`.
- [x] **Ofertio.Tests/Unit/Groups/OfferIntegrityRulesTests.cs** (novo) / **OfferRevalidationServiceTests.cs** (novo) / **OfferValidationServiceTests.cs** - detecção de vitrine, preço suspeito, anúncio encerrado e seleção do lote de revalidação.

### AffiliateOpenLink - "Abrir Link Original" com matt_tool

- [x] **Ofertio.Business/Service/LinkStrategies/AffiliateUrlBuilderRules.cs** (novo) - detecta Mercado Livre, troca link curto pela URL direta e injeta `matt_tool`/`matt_word` via `MercadoLivreCommissionUrlBuilder.InjectMattTool`.
- [x] **Ofertio.Business/Interfaces/Services/IAffiliateUrlBuilderService.cs** (novo) - `ResolveMercadoLivreMattToolAsync` e `BuildAffiliateUrl`.
- [x] **Ofertio.Infrastructure.Services/LinkStrategies/AffiliateUrlBuilderService.cs** (novo) - `TrackingId` da conta ML ativa do tenant com fallback para `DefaultAffiliateTrackingId`; registrado (Scoped) em `WhatsAppEvolutionRegistrationExtensions`.
- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreIntegrationOptions.cs** / **Ofertio.API/appsettings.json** - `DefaultAffiliateTrackingId = 97303724`.
- [x] **Ofertio.Business/Dto/MonitoredGroupDtos.cs** - `GroupCapturedOfferDto.AffiliateUrl`.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - preenche `AffiliateUrl` na listagem.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - "Abrir Link Original" usa `ResolveOpenUrl` (`AffiliateUrl`) em nova aba.
- [x] **Ofertio.Tests/Unit/LinkStrategies/AffiliateUrlBuilderServiceTests.cs** (novo) - anexar/substituir `matt_tool`, link curto, outros marketplaces, conta do tenant e fallback.

### ShopeePriceIntegrity - Parcelamento, divergência de preço e esgotado

- [x] **Ofertio.Business/Service/Groups/OfferPriceTextRules.cs** (novo) - preço à vista ignorando parcelas e "De/Por", `IsInstallmentContext`, `IsPlausiblePrice` (< R$ 1.000.000) e `IsSignificantDivergence` (≥ 30%).
- [x] **Ofertio.Business/Integrations/Shopee/ShopeeProductPageRules.cs** (novo) - `IsSoldOut` (frases, JSON e classe) e `TryExtractActivePrice` (meta / JSON-LD).
- [x] **Ofertio.Business/Service/Groups/StructuredOfferParserService.cs** / **GroupOfferCaptureRules.cs** - extração de preço do Telegram via `OfferPriceTextRules`; fallback numérico pula multiplicador `12x`.
- [x] **Ofertio.Business/Service/LinkStrategies/ProductMetadataHtmlParser.cs** - primeiro preço plausível entre as fontes e `R$` do HTML sem parcelas.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferValidationService.cs** - Shopee esgotada vira `Esgotado`; preço ativo da Shopee prevalece e divergência é logada.
- [x] **Ofertio.Infrastructure.Services/Radar/LiveCheckSearchService.cs** - corrige `ExtractedPrice` em divergência significativa com log (`ILogger` injetado).
- [x] **Ofertio.Infrastructure.Services/Groups/OfferPipelineProcessor.cs** - preço da página substitui o da mensagem em divergência significativa.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - botão "Clonar" desabilitado para esgotado, inativo ou preço suspeito (`CanClone`).
- [x] **Ofertio.Tests/Unit/Groups/OfferPriceTextRulesTests.cs** (novo) / **OfferValidationServiceTests.cs** - parcelamento, De/Por, divergência, HTML com `price_min` bruto, esgotado e preço ativo da Shopee.

### CouponsTab - Aba de Cupons & Promoções por marketplace

- [x] **Ofertio.Core/Enums/OfferType.cs** (novo) - `Product = 0`, `Coupon = 1`.
- [x] **Ofertio.Core/Entities/GroupCapturedMessage.cs** - coluna `Type` (`OfferType`, padrão `Product`).
- [x] **Ofertio.Database/AppDbContext.cs** - índice `UserId, Type, IsIgnored, ReceivedAt` em `GroupCapturedMessages`.
- [x] **Ofertio.Data/Migrations/20261018001558_AddGroupCapturedMessageOfferType.cs** (novo, + Designer e snapshot) - coluna `Type int NOT NULL DEFAULT 0` e índice.
- [x] **Ofertio.Business/Service/Groups/CouponMessageRules.cs** (novo) - `Classify`, `ExtractCouponCode`, `ExtractDiscount`, `ExtractMinimumPurchase`, `ExtractValidity`, `ExtractRules`, `DetectPlatformFromText`, `ExtractHeadline` e filtros `Todos`/`Outros`.
- [x] **Ofertio.Business/Dto/OfferDataExtraction.cs** / **StructuredOfferParserService.cs** - `Type` classificado no parse; código de cupom pelo extrator estrito (CUPOM / CÓDIGO / USAR CUPOM) com fallback na regex antiga; `IsShowcaseUrl` público.
- [x] **Ofertio.Business/Dto/MonitoredGroupDtos.cs** - `GroupCapturedOfferDto.Type`, `CouponDiscount`, `CouponValidity`, `CouponRules`.
- [x] **Ofertio.Business/Service/Groups/GroupOfferCaptureRules.cs** - deduplicação de cupons por mensagem + grupo ou código + plataforma (nunca pela URL da loja).
- [x] **Ofertio.Business/Interfaces/Services/IGroupOfferServices.cs** / **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** / **Ofertio.API/Controllers/MonitoredGroupsController.cs** - `ListAsync` com `type` e `couponPlatform`; feed de produtos só `Type = Product`; campos do cupom lidos do `RawText`.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferPipelineProcessor.cs** - desvio de cupons (`SaveCouponAsync`) sem validação de página nem pacote atômico; foto extraída para `ResolveLocalPhotoAsync`.
- [x] **Ofertio.Infrastructure.Services/Groups/GroupCapturedMessagesService.cs** - `ApplyRelevanceFilter` mantém cupons sem link de produto ou loja conectada; `SaveCouponAsync` com deduplicação de 24h.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferRevalidationService.cs** - revalidação periódica ignora cupons.
- [x] **Ofertio.Infrastructure.Services/Telegram/TelegramUserMonitorHost.cs** - mensagens de cupom sem link também entram no pipeline.
- [x] **Ofertio.SharedUi/Services/Integrations/IMonitoredGroupsApiService.cs** - query `type` e `couponPlatform`.
- [x] **Ofertio.SharedUi/Components/Integrations/CouponCard.razor** (novo) - código em destaque com copiar, desconto, validade, regras e "Ir para a loja" (`AffiliateUrl`, nova aba).
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - aba `Cupons & Promoções` com pills Todos / Mercado Livre / Shopee / Amazon / Outros.
- [x] **Ofertio.Tests/Unit/Groups/CouponMessageRulesTests.cs** (novo) / **GroupCapturedMessagesServiceTests.cs** - classificação, extração, deduplicação e filtro de relevância de cupons.

### CompactOffersUi - Layout compacto de Grupos Monitorados

- [x] **Ofertio.SharedUi/Components/Integrations/OfferCard.razor** (novo) - card de oferta extraído da página: imagem 64px, título 2 linhas, badges `badge-sm` sutis e ações em grade 2x2 compacta (Abrir Link Original, Verificar anúncio, Clonar Oferta / Restaurar, Sem interesse).
- [x] **Ofertio.SharedUi/Components/Integrations/AdvancedOfferFiltersModal.razor** (novo) - modal "Filtros Avançados ⚙️" com Período, Preço mín./máx., Loja e Ofertas por página.
- [x] **Ofertio.SharedUi/Models/MonitoredOfferFilters.cs** (novo) - record dos filtros avançados com `ActiveCount`.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - cabeçalho compacto sem texto explicativo, busca + grupo + botão de filtros, toast de 5s no lugar do alerta de status, sem "Só com cupom", `#ofertas-container` com scroll suave ao paginar.
- [x] **Ofertio.SharedUi/Components/Integrations/CouponCard.razor** - mesmo padrão compacto (badge-sm, ações em grade).
- [x] **Ofertio.SharedUi/wwwroot/app.css** - `.badge-sm`, botões compactos do card, `.monitored-search`, `.monitored-toast`, `.monitored-subpills`, `scroll-margin-top` da lista.

### GlobalToasts - Notificações Toast globais

- [x] **Ofertio.SharedUi/Services/UI/IToastService.cs** (novo) - `ToastType`, `ToastMessage` (Id, Message, Type, Duration 10s, CreatedAt) e contrato `ShowSuccess/ShowError/ShowWarning/ShowInfo/Dismiss/Clear`.
- [x] **Ofertio.SharedUi/Services/UI/ToastService.cs** (novo) - fila thread-safe (máx. 5, sem duplicatas), auto-dismiss por `Task.Delay` e evento `OnChange`.
- [x] **Ofertio.SharedUi/Components/Shared/ToastContainer.razor** (novo) - container flutuante `InteractiveServer` com ícone, mensagem, botão fechar e barra de progresso.
- [x] **Ofertio.SharedUi/Extensions/ServiceCollectionExtensions.cs** - `AddScoped<IToastService, ToastService>()`.
- [x] **Ofertio.SharedUi/Components/Layout/MainLayout.razor** / **MasterMainLayout.razor** - `<ToastContainer />`.
- [x] **Ofertio.SharedUi/wwwroot/app.css** - `.monitored-toast` substituído por `.ofertio-toast-container` / `.ofertio-toast--success|error|warning|info`.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - alertas fixos e toast local trocados por `IToastService`.
- [x] **Ofertio.SharedUi/Components/Integrations/CouponCard.razor** - `OnCopied` removido; cópia do código notificada via `IToastService`.
- [x] **Ofertio.Tests/Unit/SharedUi/ToastServiceTests.cs** (novo) - duração padrão, tipos, mensagens vazias, duplicatas, limite, dismiss e auto-dismiss.

### CouponFeedCleanup - Cupons fora do feed de produtos

- [x] **Ofertio.Business/Service/Groups/CouponMessageRules.cs** - `IsCouponAnnouncement` (título de anúncio ou 2+ códigos), `ExtractCouponCodes`, remoção de "Limite de R$" / "OFF em R$" no cálculo de preço, URLs e palavras acentuadas ignoradas na leitura de códigos.
- [x] **Ofertio.Business/Interfaces/Services/IGroupOfferServices.cs** - `ICouponReclassificationService`.
- [x] **Ofertio.Infrastructure.Services/Groups/CouponReclassificationService.cs** (novo) - reclassificação em lotes dos itens `Product` legados + `CouponReclassificationHost` (execução única no startup).
- [x] **Ofertio.API/Workers/CouponReclassificationWorker.cs** (novo) - `BackgroundService` que dispara a reclassificação.
- [x] **Ofertio.API/Program.cs** / **Ofertio.Infrastructure.Services/WhatsApp/WhatsAppEvolutionRegistrationExtensions.cs** - registro do worker, host e serviço.
- [x] **Ofertio.Infrastructure.Services/Radar/OfferMiningEngine.cs**, **ProductArbitrageService.cs**, **GlobalTrendingDealsEngine.cs**, **OfficialCatalogSearchService.cs** - filtro `Type == OfferType.Product`.
- [x] **Ofertio.Tests/Unit/Groups/CouponMessageRulesTests.cs** / **CouponReclassificationServiceTests.cs** (novo) - anúncios reais de cupom, produtos com cupom único e idempotência da reclassificação.

### UserProfileMenu - Cabeçalho limpo com menu de perfil

- [x] **Ofertio.SharedUi/Components/Layout/UserProfileMenu.razor** (novo) - pílula com o nome do usuário e dropdown `<details>` (Minha Conta, Super Admin, Modo de Visão, Sair), funcional no layout SSR estático.
- [x] **Ofertio.SharedUi/Components/Layout/MainLayout.razor** - removidos "Todas as lojas", botão "Painel" e "Sessão ativa"; cabeçalho só com `UserProfileMenu`.
- [x] **Ofertio.SharedUi/Components/Layout/MasterMainLayout.razor** - `MasterProfileSwitcher` + `SessionStatusBadge` trocados por `UserProfileMenu`.
- [x] **Ofertio.SharedUi/Components/Shared/SessionStatusBadge.razor** (removido) - badge "Sessão ativa (plataforma)".
- [x] **Ofertio.SharedUi/Components/Shared/ActiveStoreScopeSelector.razor** (removido) - dropdown "Todas as lojas"; as páginas seguem usando `IActiveStoreScopeService` com a loja salva no navegador.
- [x] **Ofertio.SharedUi/wwwroot/app.css** - estilos `.active-store-scope*` trocados por `.ofertio-profile-*`.
- [x] **Ofertio.SharedUi/wwwroot/app-interop.js** - fecha o menu de perfil ao clicar fora, em um item ou com Esc.

### HeaderRestore - Botões de visão de volta ao cabeçalho

- [x] **Ofertio.SharedUi/Components/Master/MasterProfileSwitcher.razor** (restaurado) - grupo [Painel Master] [Visão Cliente] + badge de papel, no formato original.
- [x] **Ofertio.SharedUi/Components/Layout/MainLayout.razor** / **MasterMainLayout.razor** - `MasterProfileSwitcher` antes do `UserProfileMenu`.
- [x] **Ofertio.SharedUi/Components/Layout/UserProfileMenu.razor** - dropdown reduzido a Minha Conta, Super Admin e Sair.
- [x] **Ofertio.SharedUi/wwwroot/app.css** - `.portal-header` / `.master-header` com `position: relative; z-index: 1050`; removido `.ofertio-profile-hint`.

### ShowcaseBypass - Vitrines de divulgadores e matt_tool forçado (Mercado Livre)

- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreShowcaseRules.cs** (novo) - detecta vitrines `/social/` / `affiliate-profile` / `reco_client=home_affiliate-profile`, extrai o produto focado (`item_id`, `product_id`, `/p/MLB...`, `MLB-...`) como página direta e remove `matt_tool_id`, `matt_tool`, `matt_d2id`, `matt_tracing_id` e `matt_word` de terceiros.
- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreIntegrationOptions.cs** - constante `OfficialAffiliateTrackingId = "97303724"`, padrão de `DefaultAffiliateTrackingId`.
- [x] **Ofertio.Business/Service/LinkStrategies/AffiliateUrlBuilderRules.cs** - `ResolveMercadoLivreTarget` (bypass de vitrine no link original e no resolvido) + limpeza dos matt_* alheios + `matt_tool` sempre aplicado (oficial quando nenhum for informado).
- [x] **Ofertio.Infrastructure.Services/LinkStrategies/AffiliateUrlBuilderService.cs** - fallback do `matt_tool` cai na constante oficial quando a configuração estiver vazia.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - "Clonar Oferta" do Mercado Livre envia a URL higienizada ao conversor e a usa como link quando não houver loja ML conectada.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferValidationService.cs** - vitrine com produto focado é convertida antes da rejeição `VitrineGenerica` (link inicial, canônico e redirect final com novo GET do produto).
- [x] **Ofertio.Infrastructure.Services/Groups/OfferPipelineProcessor.cs** - ingestão valida o produto focado e grava em `PrimaryProductUrl` a página direta resolvida pelo redirect.
- [x] **Ofertio.Business/Dto/LiveSearchDtos.cs** / **Ofertio.Infrastructure.Services/Radar/LiveCheckSearchService.cs** - `ResolvedProductUrl` no snapshot; "Verificar anúncio" atualiza `PrimaryProductUrl` dos itens antigos (meli.la).
- [x] **Ofertio.Tests/Unit/LinkStrategies/MercadoLivreShowcaseRulesTests.cs** (novo) / **AffiliateUrlBuilderServiceTests.cs** / **Ofertio.Tests/Unit/Groups/OfferValidationServiceTests.cs** - cobertura do bypass, da limpeza dos matt_* e do `matt_tool=97303724` forçado.

### OffersPageSize100 - 100 ofertas por página por padrão

- [x] **Ofertio.Business/Service/Groups/GroupOfferCaptureRules.cs** - `OffersPageSize = 100` (antes 25); valor inválido de `take` cai em 100.
- [x] **Ofertio.Business/Interfaces/Services/IGroupOfferServices.cs** / **Ofertio.API/Controllers/MonitoredGroupsController.cs** / **Ofertio.SharedUi/Services/Integrations/IMonitoredGroupsApiService.cs** - default de `take` passa a usar `GroupOfferCaptureRules.OffersPageSize`.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - default de `take` pela constante e busca prévia para deduplicação sem o teto de 80 linhas (`take + 20` até 200), permitindo páginas de 100.
- [x] **Ofertio.SharedUi/Components/Integrations/AdvancedOfferFiltersModal.razor** - `selected` explícito na opção atual e fallback para o padrão (100) quando o valor não está na lista.
- [x] **Ofertio.Tests/Unit/Groups/GroupOfferCaptureRulesTests.cs** - default 100 e fallback de `take` inválido.

### MeliSocialCardReset - Vitrines /social/ resolvidas, card compacto e reset limpo

- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreShowcaseRules.cs** - IDs extraídos dos parâmetros da vitrine (`item_id`, `product_id`, `wid`) viram `https://www.mercadolivre.com.br/p/{ITEM_ID}`.
- [x] **Ofertio.Infrastructure.Services/LinkStrategies/UrlResolverService.cs** - após seguir os redirects, vitrine Meli Social com produto focado vira a página `/p/` antes da limpeza de rastreio (que descarta o fragmento).
- [x] **Ofertio.Infrastructure.Services/Groups/OfferValidationService.cs** / **OfferPipelineProcessor.cs** - links `/social/` do Mercado Livre não são mais rejeitados antes da resolução HTTP; viram `VitrineGenerica` só se a URL final continuar no perfil sem produto.
- [x] **Ofertio.SharedUi/Components/Integrations/OfferCard.razor** - data/hora da captura (`dd/MM/yyyy HH:mm`) em destaque no cabeçalho; botões "Abrir", "Verificar", "Clonar" e "Ocultar" com os ícones originais.
- [x] **Ofertio.SharedUi/wwwroot/app.css** - `.monitored-offer-captured` (pílula da data) e `.monitored-offer-actions` em uma linha com padding reduzido.
- [x] **ResetarOfertasLegadas.ps1** (novo) - reset limpo de `GroupCapturedMessages` e `uploads\products` (todos ou `-UserId`), com `-WhatIf`, confirmação "RESETAR", `-PararIis` e `-IncluirHistoricoPrecos`.
- [x] **Ofertio.Tests/Unit/LinkStrategies/UrlResolverServiceTests.cs** / **MercadoLivreShowcaseRulesTests.cs** / **AffiliateUrlBuilderServiceTests.cs** / **Ofertio.Tests/Unit/Groups/OfferValidationServiceTests.cs** - formato `/p/{ITEM_ID}`, redirect social ➔ produto e rejeição só na URL final.

### FeedChronologicalOrder - Feed em ordem decrescente da mensagem capturada

- [x] **Ofertio.Business/Service/Groups/CapturedMessageTimeRules.cs** (novo) - `OrderByCapturedAtDescending` (`ReceivedAt` desc + `Id` desc), `NormalizeUtc`, `FromUnixSeconds`, `ResolveReceivedAtUtc` e `ToBrasilia`.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - feed ordenado pela hora da mensagem (antes `CreatedAt`, hora da gravação) e `ReceivedAt` devolvido como UTC.
- [x] **Ofertio.Infrastructure.Services/Radar/OfficialCatalogSearchService.cs** / **Ofertio.Infrastructure.Services/Telegram/TelegramUserMonitorHost.cs** - consultas de `GroupCapturedMessages` com a mesma ordenação.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferPipelineProcessor.cs** - `ReceivedAt` normalizado em UTC com o horário de envio da mensagem.
- [x] **Ofertio.Business/Service/WhatsApp/WhatsAppBotRules.cs** / **Ofertio.Business/Service/Telegram/TelegramBotRules.cs** - `SentAtUtc` lido de `messageTimestamp` (número, string ou Long protobuf) e `message.date`.
- [x] **Ofertio.Infrastructure/Services/WhatsAppMessageProcessor.cs** / **TelegramMessageProcessor.cs** - capturam com o horário de envio em vez de `DateTime.UtcNow`.
- [x] **Ofertio.SharedUi/Components/Integrations/OfferCard.razor** / **CouponCard.razor** - data/hora exibida no horário de Brasília (`dd/MM/yyyy HH:mm`).
- [x] **Ofertio.Tests/Unit/Groups/CapturedMessageTimeRulesTests.cs** (novo) - ordem 09:44 ➔ 09:20 ➔ 09:14 ➔ 08:44 ➔ 08:14 ➔ 00:23 com dados reais, desempate, UTC/Brasília e timestamps dos webhooks.

### SoldOutFeedRemoval - Remoção imediata de produtos esgotados

- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreAvailabilityRules.cs** (novo) - anúncio pausado/encerrado (`Inativo`), sem estoque / só "Produtos relacionados" sem caixa de compra / redirect para `lista.mercadolivre.com.br` (`Esgotado`) e status da API `/items` (`available_quantity = 0`).
- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreShowcaseRules.cs** - `TryGetMercadoLivreUri` passou a `internal` para ser reaproveitado.
- [x] **Ofertio.Business/Service/Groups/OfferFeedVisibilityRules.cs** (novo) - `IsVisibleInFeed`, `RemoveFromFeed`, `ReplaceInFeed` e o toast "Produto esgotado. Oferta removida do feed.".
- [x] **Ofertio.Core/Entities/GroupOfferStatuses.cs** - `FeedVisibleStatuses` (lista branca do feed) e `IsFeedVisible`.
- [x] **Ofertio.Business/Dto/MonitoredGroupDtos.cs** - `MonitoredGroupsResponseDto.ValidatedOffer` e `OfferRemoved`.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferValidationService.cs** - detecção do Mercado Livre na verificação HTTP (botão Verificar e Worker de lote).
- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreItemParser.cs** - item pausado/encerrado ou com estoque zero marcado indisponível.
- [x] **Ofertio.Infrastructure.Services/Groups/GroupCapturedMessagesService.cs** - `ApplyRelevanceFilter` exige status da lista branca para produtos.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - `ValidateAsync` devolve a oferta revalidada e `OfferRemoved` sem recarregar o feed do grupo; `EnrichForFeedAsync` compartilhado com `ListAsync`.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferRevalidationService.cs** - Worker de lote conta como desativada a oferta que sai do feed.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - Verificar remove o card esgotado da lista em memória, ajusta o total, chama `StateHasChanged()` e mostra o toast de aviso.
- [x] **Ofertio.Tests/Unit/LinkStrategies/MercadoLivreAvailabilityRulesTests.cs** (novo) / **Ofertio.Tests/Unit/Groups/OfferFeedVisibilityRulesTests.cs** (novo) / **OfferValidationServiceTests.cs** / **GroupCapturedMessagesServiceTests.cs** / **Ofertio.Tests/Unit/Radar/MercadoLivreItemParserTests.cs** - cobertura da detecção, do filtro estrito e da remoção em memória.

### ProductTagsFilter - Filtro por características de produtos

- [x] **Ofertio.Core/Entities/ProductTag.cs** (novo) - entidade `ProductTag` e `ProductTagCatalog` (slugs, Ids fixos do seed e rótulo "Todas as Características").
- [x] **Ofertio.Core/Entities/GroupCapturedMessage.cs** - navegação `Tags` (N:N).
- [x] **Ofertio.Database/AppDbContext.cs** - `DbSet<ProductTag>`, tabela de junção `OfferTags` (cascata) e `HasData` do catálogo.
- [x] **Ofertio.Data/Migrations/20261018140443_AddProductTags.cs** (novo) / **.Designer.cs** / **AppDbContextModelSnapshot.cs** - tabelas `ProductTags` e `OfferTags` com seed.
- [x] **Ofertio.Business/Service/Groups/ProductTagClassifierRules.cs** (novo) - palavras-chave por característica, sem acento e por palavra inteira.
- [x] **Ofertio.Business/Dto/MonitoredGroupDtos.cs** / **LiveSearchDtos.cs** - `ProductTagDto`, `MonitoredGroupsResponseDto.Tags` e `LiveSearchFilterDto.TagId`.
- [x] **Ofertio.Business/Interfaces/Services/IGroupOfferServices.cs** - `IProductTagBackfillService`.
- [x] **Ofertio.Infrastructure.Services/Groups/GroupCapturedMessagesService.cs** - vincula as tags na gravação de cada oferta nova (Telegram e WhatsApp).
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - filtro `Tags.Any(t => t.Id == selectedTagId)` e lista de características ativas na resposta.
- [x] **Ofertio.Infrastructure.Services/Groups/ProductTagBackfillService.cs** (novo) - classifica em lotes as ofertas capturadas sem tags (`ProductTagBackfillHost`).
- [x] **Ofertio.Infrastructure.Services/WhatsApp/WhatsAppEvolutionRegistrationExtensions.cs** / **Ofertio.API/Program.cs** / **Ofertio.API/Workers/ProductTagBackfillWorker.cs** (novo) - registro do backfill na subida da API.
- [x] **Ofertio.API/Controllers/MonitoredGroupsController.cs** / **Ofertio.SharedUi/Services/Integrations/IMonitoredGroupsApiService.cs** - parâmetro `tagId` na listagem.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** / **Ofertio.SharedUi/wwwroot/app.css** - combo de características à direita de "Ocultos / Descartados" (`.monitored-tag-select`).
- [x] **Ofertio.Tests/Unit/Groups/ProductTagClassifierRulesTests.cs** (novo) / **ProductTagPersistenceTests.cs** (novo) - classificação, seed, vínculo na gravação, filtro por tag e backfill idempotente.

### AmazonUnitPriceFix - Preço total da Amazon sem valor por unidade

- [x] **Ofertio.Business/Service/Groups/OfferPriceTextRules.cs** - `IsUnitPriceContext` / `IsNonCashContext`; `TryExtractCashPrice` ignora "/ unidade", "por frasco", "unidade R$ X".
- [x] **Ofertio.Business/Service/LinkStrategies/ProductMetadataHtmlParser.cs** - leitura de valores monetários do HTML pula contextos de parcela e de preço unitário.
- [x] **Ofertio.Business/Integrations/Amazon/AmazonProductPageRules.cs** (novo) - total da buy box (`apexPriceToPay`, `priceToPay`, `corePrice_feature_div`) sem preço riscado/unitário, com fallback `og:price:amount`.
- [x] **Ofertio.Infrastructure.Services/Groups/OfferValidationService.cs** - páginas da Amazon usam o preço da buy box.
- [x] **Ofertio.Business/Dto/MonitoredGroupDtos.cs** / **Ofertio.Business/Service/Groups/OfferFeedVisibilityRules.cs** - `PriceUpdated`, `HasDisplayedPriceChanged` e toast "Preço atualizado para R$ X".
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - `ValidateAsync` compara o preço exibido antes/depois e sinaliza `PriceUpdated`.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - Verificar substitui o card, chama `StateHasChanged()` e mostra o toast de aviso com o novo preço.
- [x] **Ofertio.Tests/Unit/LinkStrategies/AmazonProductPageRulesTests.cs** (novo) / **OfferPriceTextRulesTests.cs** / **OfferValidationServiceTests.cs** / **OfferFeedVisibilityRulesTests.cs** - buy box, preço unitário, fallback og e toast.

### RebrandOfertio - Migração de marca TecFlow → Ofertio

- [x] **Ofertio.sln** (antes `TecFlow.sln`) e pastas/projetos **Ofertio.*/Ofertio.*.csproj** (antes `TecFlow.*`) - namespaces, assemblies, `ProjectReference` e `InternalsVisibleTo`.
- [x] **Ofertio.Core/Security/OfertioClaimTypes.cs** / **Ofertio.Infrastructure.Services/Security/OfertioPasswordHasher.cs** / **OfertioUserStore.cs** / **Ofertio.Observability/OfertioActivitySources.cs** / **OfertioBusinessMetrics.cs** / **Ofertio.Business/Interfaces/Telemetry/IOfertioBusinessMetrics.cs** / **Ofertio.SharedUi/Serialization/OfertioJsonOptions.cs** / **Ofertio.Mobile/Platforms/Android/OfertioFirebaseMessagingService.cs** / **Ofertio.SharedUi/wwwroot/ofertio-clipboard.js** / **Ofertio.API/Ofertio.API.http** / **docs/Ofertio_MOBILE_BUILD.md** - arquivos renomeados.
- [x] **Ofertio.Infrastructure/Migrations/20260603162948_InitialOfertio.cs** / **.Designer.cs** - arquivos renomeados mantendo o ID `20260603162948_InitialTecFlow` do histórico do PostgreSQL.
- [x] **Ofertio.Infrastructure/Security/DataProtectionRegistrationExtensions.cs** - nome de aplicação legado `TecFlow`; `DataProtection:KeysPath` relativo à raiz da aplicação (`App_Data/OfertioDataProtectionKeys`).
- [x] **Ofertio.Data/Migrations/20261018220917_RebrandOfertioSettings.cs** (novo) / **.Designer.cs** / **AppDbContextModelSnapshot.cs** - renomeia a chave `TecFlow:ShortLinks:PublicBaseUrl` em `SystemSettings`.
- [x] **Ofertio.API/appsettings*.json** / **Ofertio.WebUi/appsettings*.json** / **Ofertio.Worker/appsettings.json** / **Ofertio.Orquestrador/appsettings.json** / **web.config** - banco `Ofertio`, uploads `C:\inetpub\ofertio(_dev)\uploads`, chaves em `App_Data`, seção `Ofertio:*`, JWT `OfertioAPI`/`OfertioClient`.
- [x] **Ofertio.SharedUi/Components/Layout/MainLayout.razor** / **MasterMainLayout.razor** / **Pages/Home.razor** / **Ofertio.Mobile/wwwroot/index.html** - título "Ofertio — Inteligência em Ofertas".
- [x] **.gitignore** - ignora `App_Data/OfertioDataProtectionKeys/` e `App_Data/OfertioDevKeys/`.
- [x] **docs/EXECUCAO_HOMOLOGACAO.md** / **Publicar-Homologacao.ps1** / **AjustarPermissoesIIS.ps1** / **LiberarPermissoesWebUi.ps1** / **Configurar-Logs-IIS.ps1** / **Liberar-Logs-WebUi.ps1** / **ResetarOfertasLegadas.ps1** - pools, sites e pastas `Ofertio`; limpeza do deploy preserva `api\App_Data`.
- [x] **Ofertio.Tests/Unit/Security/DataProtectionKeysPathTests.cs** (novo) - caminho relativo/absoluto/padrão das chaves e nome legado do Data Protection.

### OfertioVisualIdentity - Logotipo, favicon e título do portal

- [x] **Ofertio.WebUi/wwwroot/images/logo-ofertio.png** (novo) / **Ofertio.Mobile/wwwroot/images/logo-ofertio.png** (novo) - logotipo Ofertio recortado (490x160).
- [x] **Ofertio.WebUi/wwwroot/favicon.png** / **favicon.ico** (novos) / **Ofertio.Mobile/wwwroot/favicon.png** / **favicon.ico** (novos) - ícone das setas laranja com fundo transparente.
- [x] **Ofertio.SharedUi/Components/Layout/MainLayout.razor** / **MasterMainLayout.razor** - logotipo no cabeçalho com alt "Ofertio — Inteligência em Ofertas"; título padrão "Ofertio - Inteligência em Ofertas".
- [x] **Ofertio.SharedUi/Components/Pages/Home.razor** - logotipo na tela de login no lugar do selo de texto; título padrão atualizado.
- [x] **Ofertio.SharedUi/wwwroot/app.css** - classes `.brand-logo`, `.brand-logo-link` e `.brand-logo-login`.
- [x] **Ofertio.WebUi/Components/App.razor** / **Ofertio.SharedUi/Components/AppShell.razor** / **Ofertio.Mobile/wwwroot/index.html** - links de favicon (`favicon.png`/`favicon.ico`); título do Mobile "Ofertio - Inteligência em Ofertas".

### GruposMonitoradosButtonsFix - Limpar, spinners e toasts em Grupos Monitorados

- [x] **Ofertio.SharedUi/Components/Shared/ConfirmDialogHost.razor** - `@rendermode InteractiveServer`; o modal de confirmação passa a abrir nas páginas interativas (o "Limpar" ficava travado).
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - "Limpar" redefine todos os filtros e recarrega a lista padrão; `_isRefreshing` / `_isClearing` / `_isSyncing` / `_isBackgroundSyncing` com spinners; toasts de conclusão; ações de card por oferta.
- [x] **Ofertio.SharedUi/Components/Integrations/OfferCard.razor** / **CouponCard.razor** - parâmetro `PendingAction` com mini-spinner no botão em processamento.
- [x] **Ofertio.SharedUi/Models/OfferCardAction.cs** (novo) - ações de card (Validate, Clone, Ignore, Restore).
- [x] **Ofertio.SharedUi/Models/MonitoredOfferFilters.cs** - `Default(pageSize)` com os filtros padrão.
- [x] **Ofertio.Business/Service/Groups/MonitoredGroupSyncRules.cs** - `FiltersResetMessage`, `BuildCompletionMessage` e `IsStatusFromCurrentRun`.
- [x] **Ofertio.Tests/Unit/Groups/MonitoredGroupSyncRulesTests.cs** - mensagens de conclusão, status da execução atual e filtros padrão.

### MonitoredPlatformFilter - Filtro por plataforma em Grupos Monitorados

- [x] **Ofertio.Business/Service/Groups/OfferPlatformFilterRules.cs** (novo) - opções do select, normalização (`Magalu`, `Ponto`, `Outros`) e filtro por `PlatformType`.
- [x] **Ofertio.Business/Dto/LiveSearchDtos.cs** - `LiveSearchFilterDto.Platform` (entra em `HasAny`).
- [x] **Ofertio.API/Controllers/MonitoredGroupsController.cs** - parâmetro `platform` na listagem.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - aplica `OfferPlatformFilterRules` no feed de produtos e nos ocultos.
- [x] **Ofertio.SharedUi/Services/Integrations/IMonitoredGroupsApiService.cs** - envia `platform` na query string.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - select "Todas as Plataformas", `_selectedPlatform`, `OnPlatformChanged` (página 1) e reset no "Limpar".
- [x] **Ofertio.SharedUi/wwwroot/app.css** - `.monitored-platform-select`.
- [x] **Ofertio.Tests/Unit/Groups/OfferPlatformFilterRulesTests.cs** (novo) - normalização, opções e filtro (inclusive "Outros").

### MonitoredPlatformFilterDynamic - Plataformas do select vindas das contas conectadas

- [x] **Ofertio.Business/Service/Groups/OfferPlatformFilterRules.cs** - `BuildOptions` (conectadas ➔ detectadas ➔ Mercado Livre/Shopee), rótulos via `GetDisplayName`, normalização por `TryCanonicalKey`; opções fixas e "Outros" removidos.
- [x] **Ofertio.Business/Dto/MonitoredGroupDtos.cs** - `PlatformFilterOptionDto` e `MonitoredGroupsResponseDto.Platforms`.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - `BuildPlatformOptionsAsync` a partir de `ListActivePlatformsAsync` e, sem conta ativa, do `Distinct` de `PlatformType` das ofertas do período.
- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - `@foreach` sobre `_platforms` vindo da API; mantém a plataforma selecionada visível se sair da lista.
- [x] **Ofertio.Tests/Unit/Groups/OfferPlatformFilterRulesTests.cs** - lista dinâmica, fallbacks e ordenação.

### UnifiedPlatformFilter - Filtro de plataforma único para todas as abas

- [x] **Ofertio.SharedUi/Components/Pages/Integracoes/GruposMonitorados.razor** - removidas as pílulas de marketplace dos cupons (`CouponPlatformTabs`, `_couponPlatform`, `SetCouponPlatformAsync`); "Todas as Plataformas" habilitado em todas as abas e preservado ao trocar de aba.
- [x] **Ofertio.Infrastructure.Services/Groups/MonitoredGroupService.cs** - `OfferPlatformFilterRules` também filtra cupons (o parâmetro legado `couponPlatform` da API continua aceito).
- [x] **Ofertio.SharedUi/wwwroot/app.css** - removido `.monitored-subpills`.

### KestrelDevCertificate - Certificado .pfx no Kestrel (porta 7002)

- [x] **Ofertio.WebUi/appsettings.Development.json** - `Kestrel:Endpoints:Https` em `https://localhost:7002` com `C:/Programacao/certificado/dev.ofertio.com.br.pfx` (senha em User Secrets) e `Protocols: Http1AndHttp2`.
- [x] **Ofertio.WebUi/Ofertio.WebUi.csproj** - `UserSecretsId` para guardar a senha do certificado fora do repositório.
- [x] **.gitignore** - ignora `*.pfx`.

### MercadoLivreEndpoints - Rotas públicas do app Mercado Livre (OAuth e notificações)

- [x] **Ofertio.API/Controllers/MercadoLivreController.cs** - `[AllowAnonymous]` em `api/mercadolivre`: `GET callback` (code + state selado ➔ grava tokens ➔ redirect para `/marketplace-accounts` da origem) e `POST notifications` (lê o JSON e responde `Ok()` sempre).
- [x] **Ofertio.API/Security/MercadoLivreOAuthCallbackFlow.cs** - fluxo compartilhado do state selado (validação, nonce anti-replay, principal do state, `LinkAsync`) extraído do `MarketplaceAuthController`.
- [x] **Ofertio.API/Controllers/MarketplaceAuthController.cs** - `POST api/marketplace-auth/mercadolivre/callback` delega ao `MercadoLivreOAuthCallbackFlow` (mesmo contrato).
- [x] **Ofertio.Business/Dto/MercadoLivreNotificationDto.cs** - payload `_id`, `resource`, `user_id`, `topic`, `application_id`, `attempts`, `sent`, `received`.
- [x] **Ofertio.Tests/Unit/Controllers/MercadoLivreControllerTests.cs** - callback com redirect, state forjado (400 JSON), autorização negada e notificações sempre 200.

### MercadoLivreWebUiProxy - Rotas do ML pelo host público da WebUi

- [x] **Ofertio.WebUi/Extensions/MercadoLivreProxyExtensions.cs** - YARP: `AddHttpForwarder` + `MapForwarder` de `/api/mercadolivre/callback` e `/api/mercadolivre/notifications` para `OrquestradorApi:BaseUrl`, anônimos.
- [x] **Ofertio.WebUi/Program.cs** - `AddMercadoLivreProxy()` e `MapMercadoLivreProxy()`.
- [x] **Ofertio.WebUi/Ofertio.WebUi.csproj** - pacote `Yarp.ReverseProxy` 2.3.0.
- [x] **Ofertio.WebUi/Extensions/IntegracoesEndpointExtensions.cs** - removido o repasse manual (handler de callback e `Task.Run` das notificações) substituído pelo YARP.
- [x] **docs/EXECUCAO_HOMOLOGACAO.md** - passo 2.4 remove a subaplicação `/api` do site `Ofertio.WebUi` se existir.
- [x] **IIS** - removida a aplicação `/api` do site `Ofertio.WebUi` (dividia o `OfertioApiPool` com o site `Ofertio.API` e causava HTTP 500.35 na porta 5001); backup `antes-remover-api-webui-20261019211928`.

### MercadoLivreOfficialItems - Anúncios pela API oficial com OAuth e trava anti-robô

- [x] **Ofertio.Business/Integrations/MercadoLivre/MercadoLivreAntiBotGuard.cs** - trava singleton de 15 min após desafio anti-robô; `IsChallengePage` separa desafio da página do anúncio (`ui-pdp`).
- [x] **Ofertio.Business/Interfaces/Services/ILiveSearchServices.cs** - `IMercadoLivreApiService.GetItemAsync(userId, itemId)`.
- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreApiService.cs** - `/items/{id}` com `Authorization: Bearer` (conta OAuth ➔ token fixo ➔ token do app), refresh antes da consulta e após 401/403, logs `[ML Item]` de conta não conectada; vitrine respeita e aciona a trava.
- [x] **Ofertio.Infrastructure.Services/Radar/LiveCheckSearchService.cs** - API oficial antes da página pública para links `MLB`; desafio anti-robô descarta o HTML, aciona a trava e mantém a oferta "Verificando".
- [x] **Ofertio.Infrastructure.Services/WhatsApp/WhatsAppEvolutionRegistrationExtensions.cs** - registro singleton de `MercadoLivreAntiBotGuard`.
- [x] **Ofertio.Tests/Unit/Radar/MercadoLivreApiServiceTests.cs** - Bearer da conta, refresh do token expirado, retry após 403, sem credencial não chama `/items`, vitrine com trava.
- [x] **Ofertio.Tests/Unit/Radar/LiveCheckSearchServiceTests.cs** - API antes da página, desafio aciona a trava, trava evita GET, expiração e detecção do desafio.

### MercadoLivreNoScraping - Resolvedor de links e metadados do ML sem scraping público

- [x] **Ofertio.Infrastructure.Services/Groups/OfferValidationService.cs** - links do ML não baixam a página: extrai o MLB (redirects só pelos cabeçalhos quando é encurtador/vitrine), consulta `IMercadoLivreApiService.GetItemAsync` e devolve Ativo/Esgotado/Inativo; sem MLB ou sem resposta da API fica "Verificando"; busca no lugar do produto continua Esgotado.
- [x] **Ofertio.Infrastructure.Services/LinkStrategies/ProductMetadataService.cs** - metadados do ML (título, preço, imagem) pela API oficial com o usuário do `IAffiliateLinkGenerationContext`; sem MLB ou sem resposta usa o slug da URL, nunca o HTML.
- [x] **Ofertio.Infrastructure.Services/LinkStrategies/UrlExpansionService.cs** - para ao chegar numa URL de produto do ML com MLB e não lê o corpo de páginas do ML.
- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreApiService.cs** - sem usuário (rotinas em segundo plano) não usa a conta de nenhum tenant; código de catálogo (`/p/MLB...`) com 404 em `/items` segue `/products/{id}` ➔ anúncio ganhador da buy box.
- [x] **Ofertio.Infrastructure.Services/Radar/MercadoLivreItemParser.cs** - `ParseCatalogProduct` (buy_box_winner, nome, foto).
- [x] **Ofertio.Tests/Unit/Groups/OfferValidationServiceTests.cs** - status do ML pela API sem nenhum GET na página (encerrado, sem estoque, ativo, API indisponível, vitrine focada).
- [x] **Ofertio.Tests/Unit/LinkStrategies/ProductMetadataServiceTests.cs** - metadados do ML pela API com o usuário da geração e sem requisição HTTP.
- [x] **Ofertio.Tests/Unit/LinkStrategies/AffiliateLinkInfrastructureTests.cs** - expansão para no produto do ML sem baixar a página.
- [x] **Ofertio.Tests/Unit/Radar/MercadoLivreApiServiceTests.cs** - catálogo via `/products` e token sem usuário não usa conta de tenant.

---

## 📌 Prioridade sugerida (ordem de execução)

| # | Item | Risco |
|---|------|-------|
| 1 | Deletar interfaces fantasma em `Infrastructure.Services/Interfaces` | Alto — confusão em refactors |
| 2 | Corrigir namespace `ExceptionMiddleware` no Core | Médio |
| 3 | Renomear/remover `Application/AuthController.cs` stub | Médio |
| 4 | Consolidar DI (`*RegistrationExtensions`) | Médio |
| 5 | Alinhar Migrations com `Ofertio.Database` | Alto — schema/EF |
| 6 | `dotnet clean` + `.gitignore` para bin/obj/Tecso.* | Baixo — higiene Git |
| 7 | Unificar `ValidationHelper` | Baixo |
| 8 | WebUi usar DTOs de Business | Concluído |

---

*Gerado por varredura automatizada dos arquivos `.cs` e `.csproj` (excluindo `bin/` e `obj/`). Revisar checkboxes conforme cada item for concluído.*
