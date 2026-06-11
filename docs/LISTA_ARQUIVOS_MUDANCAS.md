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

- [x] **ExceptionMiddleware.cs** — Havia cópia em `TecFlow.API/Middleware/` e implementação em `TecFlow.Core/Exceptions/`. **Ação concluída:** removida a cópia da API; pipeline usa apenas `TecFlow.Core` via `Program.cs`.

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
- [x] **TecFlow.Core/Entities/MarketplaceAccount.cs** — vínculo Tenant + ShopId + tokens + CNPJ.
- [x] **TecFlow.Core/Abstractions/ITenantScopedEntity.cs**, **IShopScopedEntity.cs** — contratos de isolamento.
- [x] **TecFlow.Core/Security/TecFlowClaimTypes.cs** — claims `tenant_id`, `shop_id` (movido de SharedUi).
- [x] **TenantId** em: `UserAccount`, `Product`, `Campaign`, `Affiliate`, `Content`, `Conversion`, `Metric`, `MarketplaceToken`, `MarketplaceOrder`, `MarketplaceOrderLine`, `GlobalAdvertisingProduct`, `MarketplaceAffiliateLink`, `UserDeviceToken`.
- [x] **TecFlow.Database/MultiTenancy/** — `ICurrentTenantService`, `NullCurrentTenantService`, `TenantQueryFilterExtensions`, `TenantDbSetExtensions`.
- [x] **TecFlow.Database/AppDbContext.cs** — filtros globais, `SaveChanges` com `TenantId`, criptografia em `MarketplaceAccount`.
- [x] **TecFlow.Infrastructure/Security/CurrentTenantService.cs** — JWT + header `X-TecFlow-Shop-Id`.
- [x] **TecFlow.Infrastructure/Security/JwtTokenService.cs** — claim `TecFlow:tenant_id` no login.
- [x] **TecFlow.Infrastructure.Services/Tenancy/TenantProvisioningService.cs** — tenant automático no cadastro de usuário.
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
- [x] **Liberar-Logs-WebUi.ps1** — cria `C:\inetpub\tecflow\webui\logs\` e concede `FullControl` a `IIS_IUSRS` / app pools.

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
- [x] **TecFlow.WebUi/** — host fino: OAuth/cookies (`AuthCookieService`, `WebAccessTokenProvider`), `Routes.razor` com `AdditionalAssemblies` → SharedUi.
- [x] **TecFlow.WebUi/Program.cs** — repassa `builder.Environment` para `AddWebUiServices` (SSL bypass do HttpClient Orquestrador em dev/homolog).
- [x] **TecFlow.WebUi/Extensions/WebUiServiceCollectionExtensions.cs** — encaminha `IHostEnvironment` para `AddTecFlowClientServices`.
- [x] **TecFlow.Mobile/** — MAUI Blazor Hybrid (`MainPage.xaml` + `BlazorWebView` → `Routes` SharedUi).
- [x] **TecFlow.Mobile/MauiProgram.cs** — DI compartilhada + `MobileAuthenticationStateProvider` + `SessionAuthCookieService`.
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
- [x] **TecFlow.Business/Integrations/Shopee/** — `IShopeeIntegrationClient`, `ShopeeIntegrationOptions` (PartnerId/PartnerKey).
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
- [x] **TecFlow.API/Controllers/MarketplaceAuthController.cs** — `api/marketplace-auth/*`.
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

- [ ] **ExceptionMiddleware.cs** — Em `TecFlow.Core/Exceptions/`, namespace `TecFlow.API.Middlewares`. (Ação: `TecFlow.Core.Middlewares` ou `TecFlow.Core.Exceptions`; atualizar `using` em `TecFlow.API/Program.cs`.)

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
