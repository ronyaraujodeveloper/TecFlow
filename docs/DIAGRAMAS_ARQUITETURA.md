# 🏗️ DIAGRAMAS VISUAIS: ARQUITETURA ATUAL vs PROPOSTA

[« Voltar para o Índice Completo](./INDICE_COMPLETO.md) · [README principal](../README.md) · [Lista de mudanças](./LISTA_ARQUIVOS_MUDANCAS.md)

> **Raiz da solution:** `c:\Programacao\Tecso.AutomacaoCusor\` (`TecFlow.sln`).  
> **Painel de controle:** [README.md](../README.md) · **Varredura física:** 3 de junho de 2026  
> **Divergências / limpeza:** [LISTA_ARQUIVOS_MUDANCAS.md](./LISTA_ARQUIVOS_MUDANCAS.md)

---

## 🔌 INTEGRAÇÕES EXTERNAS — TikTok Shop & Shopee (Fase 3)

> **Estratégia de acoplamento (jun/2026):** contratos e opções de configuração ficam em **`TecFlow.Business/Integrations/`**; implementações HTTP (HttpClient nomeado, Polly, logging handler) ficam em **`TecFlow.Infrastructure.Services/Integrations/`**. Os serviços legados em `Business/Interfaces/Services/` (`ITikTokShopApi`, `IShopeeApi`) serão gradualmente adaptados para delegar aos novos *Integration Clients* nas fases 3.2–3.4.

```
TecFlow.Business/Integrations/
├── Common/                          # Opções compartilhadas, nomes de HttpClient
├── TikTokShop/                      # ITikTokShopIntegrationClient + Options (AppKey/AppSecret)
└── Shopee/                          # IShopeeIntegrationClient + Options (PartnerId/PartnerKey)

TecFlow.Infrastructure.Services/Integrations/
├── Common/                          # ExternalApiLoggingHandler, políticas Polly
├── TikTokShop/                      # TikTokShopIntegrationClient
├── Shopee/                          # ShopeeIntegrationClient
└── IntegrationHttpClientRegistrationExtensions.cs  # DI: AddTecFlowIntegrationHttpClients()
```

```mermaid
flowchart LR
  API[TecFlow.API / Orquestrador]
  BUS[TecFlow.Business Integrations]
  INFSVC[TecFlow.Infrastructure.Services Integrations]
  TTS[TikTok Shop API]
  SHP[Shopee Open API]

  API --> BUS
  API --> INFSVC
  INFSVC --> BUS
  INFSVC -->|HttpClient + Polly + Logs| TTS
  INFSVC -->|HttpClient + Polly + Logs| SHP
```

**Configuração:** seção `Integrations` em `appsettings.json` (hosts API/Orquestrador/Worker) — chaves de produção via variáveis de ambiente ou User Secrets, nunca commitadas.

### Fluxo OAuth2 (Fase 3.2)

```mermaid
sequenceDiagram
  participant Lojista
  participant API as TecFlow.API
  participant Auth as MarketplaceAuthService
  participant MP as TikTok/Shopee OAuth
  participant DB as MarketplaceTokens

  Lojista->>API: GET /api/marketplace-auth/authorize-url
  API->>Auth: GenerateAuthorizationUrl + HMAC sign (Shopee)
  Auth-->>Lojista: authorizationUrl
  Lojista->>MP: Autoriza app TecFlow
  MP-->>API: GET /callback?code=&shopId=
  API->>Auth: CallbackAndGenerateTokensAsync
  Auth->>MP: POST token/get (assinado)
  Auth->>DB: Upsert Access/Refresh (criptografado)
  API->>Auth: GetValidTokenAsync (refresh se expirado)
```

### Fluxo de catálogo — Sincronização de produtos (Fase 3.3)

```mermaid
sequenceDiagram
  participant UI as WebUi / API
  participant Cat as MarketplaceProductService
  participant Auth as MarketplaceAuthService
  participant HC as HttpClient Shopee/TikTok
  participant MP as Marketplace API

  UI->>Cat: FetchProductsFromPlatformAsync(shopId, type, page)
  Cat->>Auth: GetValidTokenAsync
  Auth-->>Cat: access_token
  alt Shopee
    Cat->>HC: GET get_item_list (sign + token)
    HC->>MP: Lista item_id
    Cat->>HC: GET get_item_base_info (+ get_model_list se has_model)
  else TikTok Shop
    Cat->>HC: POST products/search (sign + body)
  end
  MP-->>Cat: Payload JSON (DTOs Payloads/)
  Cat->>Cat: ConvertToInternalProductDto → Product
  Cat-->>UI: ProductResponseDto (DataList padronizado)
```

```
TecFlow.Business/Integrations/
├── Catalog/                     # IMarketplaceProductService
├── Shopee/Payloads/             # get_item_list, get_item_base_info, get_model_list
└── TikTokShop/Payloads/         # products/search

TecFlow.Infrastructure.Services/Integrations/Catalog/
├── MarketplaceProductService.cs
├── MarketplaceProductMapper.cs
└── MarketplaceProductRegistrationExtensions.cs
```

### Fluxo Pedidos & Estoque (Fase 3.4)

```mermaid
sequenceDiagram
  participant MP as Shopee/TikTok
  participant WH as Webhook Controller
  participant Sig as WebhookSignatureVerifier
  participant Ord as MarketplaceOrderService
  participant DB as MarketplaceOrders + Produtos
  participant Stk as MarketplaceStockService
  participant API as Marketplace API

  MP->>WH: POST webhook (raw JSON + signature)
  WH->>Sig: Verify HMAC (PartnerKey/AppSecret)
  alt assinatura inválida
    WH-->>MP: 401 Unauthorized
  end
  WH->>Ord: ProcessWebhookOrderAsync
  Ord->>DB: Exists ExternalOrderId? (idempotência)
  alt já processado
    Ord-->>WH: AlreadyProcessed
  end
  Ord->>API: get_order_detail / orders/search (polling)
  Ord->>Stk: DeductLocalStockAsync (gate por SKU)
  Stk->>DB: AdjustStock FOR UPDATE (transação)
  Stk->>API: update_stock / products/stocks
  Ord->>DB: Insert MarketplaceOrder + Lines
  WH-->>MP: 200 OK
```

**Endpoints públicos (webhooks):**

| Método | Rota | Validação |
|--------|------|-----------|
| POST | `/api/webhooks/shopee` | Header `Authorization` = HMAC-SHA256(`WebhookCallbackUrl\|body`, PartnerKey) |
| POST | `/api/webhooks/tiktokshop` | Header `Webhook-Signature` ou `Tiktok-Signature` (HMAC com AppSecret/WebhookSecret) |

**Endpoints autenticados:**

| Método | Rota | Função |
|--------|------|--------|
| POST | `/api/marketplace-orders/poll` | Polling de contingência (`get_order_list` / `orders/search`) |
| PUT | `/api/Produtos/{id}` | Estoque local alterado → `UpdatePlatformStockAsync` se SKU vinculado |

**Concorrência:** `StockConcurrencyGate` serializa por `marketplace:shopId:sku`; `AdjustStockAsync` usa transação no PostgreSQL.

### Cobertura de testes automatizados (TecFlow.Tests)

```mermaid
flowchart TB
  TST[TecFlow.Tests xUnit + Moq]
  TST --> SIG[MarketplaceSignatureHelper / WebhookVerifier]
  TST --> AUTH[MarketplaceAuthService]
  TST --> CAT[MarketplaceProductService Convert]
  TST --> ORD[MarketplaceOrderService + StockService]
  TST --> API[Controllers: Products / Auth / Webhooks]
  TST --> FLT[ProductFilter → ProductResponseDto]

  SIG -.->|sem HTTP real| BUS[TecFlow.Business]
  AUTH -.->|StubHttpMessageHandler| INFSVC[TecFlow.Infrastructure.Services]
  CAT --> INFSVC
  ORD --> INFSVC
  API --> TecFlow.API
```

| Área Fase 3 | Classes de teste | Caminho feliz | Caminho triste |
|-------------|------------------|---------------|----------------|
| Assinatura HMAC | `MarketplaceSignatureHelperTests`, `MarketplaceWebhookSignatureVerifierTests` | assinatura válida | header ausente / expirado |
| OAuth | `MarketplaceAuthServiceTests` | URL + token válido + refresh | redirect vazio / token ausente |
| Catálogo | `MarketplaceProductServiceTests` | Shopee item/variante, TikTok SKU | `shopId` vazio |
| Pedidos | `MarketplaceOrderServiceTests` | webhook + baixa estoque | JSON inválido / idempotência |
| Estoque | `MarketplaceStockServiceTests` | dedução local | SKU não vinculado |
| API 3 objetos | `ProductsControllerResponseDtoTests`, `ProductFilterExtensionsTests` | `DataList` / NotFound | filtro sem match |

---

## 📊 DIAGRAMA 1: ESTRUTURA FÍSICA ATUAL (fiel ao disco)

```
Tecso.AutomacaoCusor/
├── README.md                    # Painel principal (roadmap + regras)
├── TecFlow.sln
├── .gitignore
├── docs/                        # Documentação (este arquivo, LISTA, ANALISE, …)
│
├── TecFlow.Core/
│   ├── Entities/                # 11 entidades de domínio (Campaign, Product, …)
│   ├── Enums/                   # MarketplaceType (Shopee, TikTokShop)
│   └── Exceptions/              # BaseCustomException, NotFound, Unauthorized, ExceptionMiddleware
│
├── TecFlow.Business/
│   ├── Dto/                     # *Dto, *ResponseDto, ResponseDto, Auth/
│   ├── Integrations/            # Contratos + Options + Auth (TikTokShop, Shopee)
│   │   ├── Auth/                # IMarketplaceAuthService, IMarketplaceSignatureService
│   │   ├── Common/              # MarketplaceSignatureHelper, HttpClient names
│   │   ├── TikTokShop/
│   │   └── Shopee/
│   ├── Enum/                    # (pasta reservada — vazia no momento)
│   ├── Interfaces/
│   │   ├── Repositories/        # I*Repository (7 contratos)
│   │   └── Services/            # I*Service, ITikTok*, IShopee*, helpers legados
│   ├── Pipelines/               # ColetaDados, GeracaoConteudo, Publicacao
│   └── Service/
│       ├── Application/         # *ApplicationService + AddTecFlowApplicationServices()
│       ├── CryptographyHelper.cs
│       └── ValidationHelper.cs
│
├── TecFlow.Database/
│   ├── AppDbContext.cs          # DbContext principal
│   ├── Entity/                  # UserEntity
│   ├── Filter/                  # *Filter + FilterQueryExtensions
│   ├── Interface/               # (vazia — contratos em Business.Interfaces)
│   ├── Pagin/                   # PagedResult, QueryableExtensions
│   ├── Prompts/                 # GeracaoDescricao, Roteiro, Titulo
│   └── Repositorio/             # AppDbContextFactory
│
├── TecFlow.Application/         # ⚠️ Residual
│   └── Controller/              # AuthController.cs (stub — não é controller HTTP)
│
├── TecFlow.Infrastructure/
│   ├── API/                     # Shopee/, TikTok/ (+ Models/)
│   ├── Configuration/           # AppConfiguration, SerilogLogger
│   ├── Data/                    # DataService, Configurations/ (sem AppDbContext)
│   ├── Interfaces/              # IAppConfiguration, IUserContextProvider, ILoggerService
│   ├── Migrations/              # EF migrations (legado — alinhar com Database)
│   ├── Security/                # JwtTokenService, UserContextProvider
│   └── Services/Security/       # LegacyCredentialReEncrypt*
│
├── TecFlow.Infrastructure.Services/
│   ├── Integrations/            # HttpClients, Polly, ExternalApiLoggingHandler — Fase 3.1+
│   ├── Repositories/            # 6 repositórios (Affiliate, Campaign, Content, …)
│   ├── Service/ExternalServices/ # Gemini, OpenAI, Shopee, TikTok*, Ranking, …
│   ├── Interfaces/              # ⚠️ fantasmas (vários Compile Remove no csproj)
│   ├── ServiceRegistrationExtensions.cs
│   ├── CoreServiceRegistrationExtensions.cs      # ⚠️ ainda separado
│   ├── ExternalServiceRegistrationExtensions.cs
│   ├── InfrastructureDataServiceRegistrationExtensions.cs
│   └── DatabaseUrlConfiguration.cs
│
├── TecFlow.API/
│   ├── Controllers/             # 11 controllers (+ WeatherForecast scaffold)
│   ├── Program.cs               # DI: Core + Infra + Data + Application (Business)
│   ├── Properties/
│   └── (sem Middleware/*.cs — ExceptionMiddleware só no Core)
│
├── TecFlow.Orquestrador/
│   ├── Controllers/             # Auth, Campaigns, Dashboard, Metrics, UserAccounts
│   ├── Extensions/              # DatabaseExtensions, DemoDataSeeder
│   ├── Interfaces/
│   ├── Pipelines/
│   ├── Program.cs               # DI alinhado à API
│   └── OrquestradorPrincipal.cs
│
├── TecFlow.SharedUi/            # RCL UI compartilhada (Fase 4.2)
├── TecFlow.WebUi/               # Host Blazor Server + OAuth (Fase 3/4)
├── TecFlow.Mobile/              # MAUI Blazor Hybrid (Fase 4.2)
│   ├── Components/, Services/, Extensions/, Models/, wwwroot/, …
│   └── → TecFlow.Business (*ResponseDto, DashboardSummaryDto)
│
├── TecFlow.Worker/              # Program.cs, WorkerService.cs
├── TecFlow.Tests/               # Mock/, Integration/, Unit/, Services/, …
├── TecFlow.Util/                # Address/, Security/, Validation/
└── (externo na sln) ../Tecso.LerArquivos/
```

### Mapa de dependências entre projetos (referência de compilação)

```mermaid
flowchart TB
  subgraph hosts [Hosts]
    API[TecFlow.API]
    ORQ[TecFlow.Orquestrador]
    WEBUI[TecFlow.WebUi]
    WRK[TecFlow.Worker]
  end
  APP[TecFlow.Application]
  BUS[TecFlow.Business]
  DB[TecFlow.Database]
  CORE[TecFlow.Core]
  INF[TecFlow.Infrastructure]
  INFSVC[TecFlow.Infrastructure.Services]
  UTIL[TecFlow.Util]

  API --> APP
  API --> BUS
  API --> CORE
  API --> INF
  API --> INFSVC
  ORQ --> BUS
  ORQ --> INF
  ORQ --> INFSVC
  APP --> BUS
  APP --> CORE
  APP --> UTIL
  BUS --> CORE
  BUS --> DB
  DB --> CORE
  DB --> UTIL
  INF --> BUS
  INF --> CORE
  INF --> DB
  INF --> UTIL
  INFSVC --> CORE
  INFSVC --> INF
  INFSVC --> UTIL
  WEBUI --> BUS
  WEBUI --> DB
  WEBUI --> UTIL
  WRK --> INFSVC
```

---

## 📊 DIAGRAMA 2: DIVERGÊNCIAS (diagrama × disco)

| Item no diagrama antigo | Situação real (jun/2026) |
|-------------------------|---------------------------|
| `TecFlow.Core/Interfaces/` | **Removido** — contratos em `TecFlow.Business/Interfaces/` |
| `TecFlow.Core/Dto`, `Prompts` | **Movidos** — Dtos em Business; Prompts em Database |
| `TecFlow.Application/Services/*` | **Movidos** — `TecFlow.Business/Service/Application/` |
| `Infrastructure/Data/AppDbContext` | **Movido** — `TecFlow.Database/AppDbContext.cs` |
| `API/Middleware/ExceptionMiddleware` | **Removido da API** — único em `Core/Exceptions/` |
| `Orquestrador` "Hello World" | **Obsoleto** — `Program.cs` completo com DI |
| `TecFlow.Portal` / `TecFlow.Dashboard` | **Removidos** — UI canônica em `TecFlow.WebUi` |
| 4 arquivos `*RegistrationExtensions` | **Ainda separados** — consolidação pendente |
| `Infrastructure.Services/Interfaces/*.cs` | **Fantasmas** — excluídos do compile, ainda no disco |

---

## 🔄 DIAGRAMA 3: FLUXO DE DEPENDENCY INJECTION (jun/2026)

```
┌─────────────────────────────────────────────────────────────────┐
│ TecFlow.API / TecFlow.Orquestrador (mesmo padrão de DI)           │
└─────────────────────────────────────────────────────────────────┘
                              │
    ┌─────────────────────────────────────────────────────────────┐
    │ Program.cs                                                   │
    │ ✓ AddTecFlowCoreServices()                                   │
    │ ✓ AddTecFlowInfrastructureServices(config)                     │
    │ ✓ AddTecFlowInfrastructureData(config)  → DbContext + repos  │
    │ ✓ AddTecFlowApplicationServices()       → TecFlow.Business   │
    │ ✓ JWT (API) + Serilog + Controllers                        │
    │ ✓ UseMiddleware<ExceptionMiddleware>()  → TecFlow.Core       │
    └─────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
   Controllers          Business.AppServices    Infra.Services
   (Filter/Dto/         (Campaigns, Products,   (Repositories,
    ResponseDto)         Metrics, AI, …)          External APIs)
```

**Pendências de DI (ver LISTA):** consolidar 4 `*RegistrationExtensions` em um arquivo; remover interfaces fantasma em `Infrastructure.Services/Interfaces/`.

---

## ✅ DIAGRAMA 4: ALVOS ARQUITETURAIS (ainda não 100% no disco)

```
┌─────────────────────────────────────────────────────────────────┐
│            EXTENSION METHODS (COMPARTILHADO)                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ TecFlow.Infrastructure.Services/                                 │
│   ServiceRegistrationExtensions.cs (ÚNICO)                      │
│   └─ AddTecFlowInfrastructureServices(config) ✓✓                │
│      ├─ DbContext                                               │
│      ├─ 6 Repositories                                          │
│      ├─ 4 Business Services                                     │
│      └─ 5 External API HttpClients                              │
│                                                                  │
│ TecFlow.Business/Service/Application/                             │
│   ApplicationServiceCollectionExtensions.cs                     │
│   └─ AddTecFlowApplicationServices() ✓ (já em uso na API/Orq)    │
│      └─ 9 ApplicationServices                                   │
│                                                                  │
│ TecFlow.API/                                                      │
│   Authentication/ServiceRegistrationExtensions.cs               │
│   └─ AddTecFlowAuthentication(config) ✓✓                         │
│      ├─ JWT                                                     │
│      └─ IUserContextProvider                                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
     △                          △                        △
     │                          │                        │
     └──────────┬───────────────┼────────────┬──────────┘
                │               │            │
    ┌───────────┴────────┐      │   ┌────────┴──────────┐
    ▼                    ▼      ▼   ▼                   ▼
┌──────────────────┐  ┌──────────────────────────┐  ┌─────────────────┐
│  TecFlow.API       │  │  TecFlow.ORQUESTRADOR      │  │ TecFlow.DASHBOARD │
│                  │  │                          │  │ (se precisar)   │
│ Program.cs:      │  │ Program.cs: ✓✓           │  │                 │
│ ✓ AddTecFlowInfra  │  │ await Configurator       │  │ Program.cs:     │
│ ✓ AddTecFlowAppSvcs│  │   .ConfigureAndRunAsync()│  │ ✓ AddTecFlowInfra │
│ ✓ AddTecFlowAuth   │  │                          │  │ ✓ AddTecFlowAppSvcs
│                  │  │ Configurator.cs:         │  │ ✓ AddTecFlowAuth  │
│ Controllers      │  │ ✓ AddTecFlowInfra          │  │                 │
│ (Endpoints HTTP) │  │ ✓ AddTecFlowAppSvcs        │  │ Controllers/    │
│                  │  │ ✓ AddTecFlowAuth           │  │ Views/Pages     │
│                  │  │                          │  │                 │
└──────────────────┘  │ OrquestradorService ✓✓  │  └─────────────────┘
                      │ (Lógica pura)           │
                      │                          │
                      │ Pipelines                │
                      │ (ColetaDados, Conteúdo) │
                      └──────────────────────────┘

✓✓ TODOS USAM OS MESMOS EXTENSION METHODS!
✓✓ SINCRONIZADOS AUTOMATICAMENTE!
✓✓ FÁCIL ADICIONAR NOVOS SERVIÇOS!
```

---

## 📂 DIAGRAMA 5: ESTRUTURA DE INTERFACES (físico — jun/2026)

### Canônico (em uso)
```
TecFlow.Business/Interfaces/
├── Repositories/
│   ├── IRepository.cs
│   ├── IAffiliateRepository.cs
│   ├── ICampaignRepository.cs
│   ├── IContentRepository.cs
│   ├── IMetricRepository.cs
│   ├── IProductRepository.cs
│   ├── IUserAccountRepository.cs
│   └── IAIProvider.cs, IOrquestradorRepository.cs
│
└── Services/
    ├── IAIService, IGeminiService, IDataService, …
    ├── IShopeeApi, ITikTokShopApi, ITikTokAdsApiService
    └── AnaliseService.cs, ProdutoService.cs, UsuarioService.cs  # ⚠️ impl. com nome de serviço

TecFlow.Infrastructure/Interfaces/
├── IAppConfiguration.cs
├── IUserContextProvider.cs
└── ILoggerService.cs
```

### Legado no disco (limpar)
```
TecFlow.Infrastructure.Services/Interfaces/
├── IShopeeApi.cs, ITikTokShopApi.cs, ITikTokAdsApiService.cs  # Compile Remove
└── ITikTokShopApiService.cs  # namespace TecFlow.Core.Interfaces.Services
```

---

## 🗺️ DIAGRAMA 6: MAPA DE DEPENDÊNCIAS — REPOSITÓRIOS (jun/2026)

```
┌──────────────────────────────────────────────────────────────┐
│ CONTRATOS (TecFlow.Business.Interfaces.Repositories)          │
├──────────────────────────────────────────────────────────────┤
│ IRepository ◄─── BaseEntity (Core)                           │
│ IAffiliateRepository, ICampaignRepository, IContentRepository │
│ IMetricRepository, IProductRepository, IUserAccountRepository │
│ IOrquestradorRepository, IAIProvider                         │
└──────────────────────────────────────────────────────────────┘
                           △
                           │
        ┌──────────────────┴──────────────────┐
        │                                     │
┌───────┴──────────────────┐        ┌────────┴──────────────┐
│ IMPLEMENTAÇÕES           │        │ ONDE REGISTRADOS      │
│ (Infra.Services/         │        │ (API/Prog ou Orq)     │
│  Repositories)           │        │                       │
├──────────────────────────┤        ├──────────────────────┤
│ AffiliateRepository      │────────│ AddTecFlowInfrastructureData │
│ CampaignRepository       │────────│ (API + Orquestrador)         │
│ ContentRepository        │────────│                              │
│ MetricRepository         │────────│                              │
│ ProductRepository        │────────│                              │
│ UserAccountRepository    │────────│                              │
└──────────────────────────┘        └──────────────────────┘


┌──────────────────────────────────────────────────────────────┐
│ SERVICES (TecFlow.Business.Interfaces.Services)               │
├──────────────────────────────────────────────────────────────┤
│ Contratos ──► Implementações (Infrastructure.Services)       │
│   IAIService ──► OpenAIService                               │
│   IGeminiService ──► GeminiService                             │
│   IShopeeApi / ITikTokShopApi / ITikTokAdsApiService         │
│   IScoreService, IRankingService, …                          │
│                                                              │
│ Application (TecFlow.Business/Service/Application/)          │
│   AddTecFlowApplicationServices() ✓ chamado na API e Orq     │
└──────────────────────────────────────────────────────────────┘


┌──────────────────────────────────────────────────────────────┐
│ REGISTRATION (⚠️ ainda 4 arquivos + 1 em Business)           │
├──────────────────────────────────────────────────────────────┤
│ Infrastructure.Services: ServiceRegistrationExtensions,      │
│   Core*, External*, InfrastructureData*                      │
│ Business: ApplicationServiceCollectionExtensions ✓           │
└──────────────────────────────────────────────────────────────┘
```

---

## 🗺️ DIAGRAMA 7: ALVO DE DEPENDÊNCIAS (parcialmente implementado)

> **Nota:** Bloco abaixo descreve o estado **desejado**. Contratos já estão em `TecFlow.Business`; consolidação de DI e remoção de `Infrastructure.Services/Interfaces/` ainda pendente.

```
┌──────────────────────────────────────────────────────────────┐
│ INTERFACES (alvo: TecFlow.Business — hoje já é o canônico)   │
├──────────────────────────────────────────────────────────────┤
│ Interfaces/Repositories/ (9)                                 │
│ Interfaces/Services/Business/ (8)                            │
│ Interfaces/Services/ExternalApis/ (4) ← MOVIDAS               │
│ Interfaces/Infrastructure/ (3)                               │
└──────────────────────────────────────────────────────────────┘
                           △
                           │
        ┌──────────────────┴──────────────────┐
        │                                     │
┌───────┴──────────────────┐        ┌────────┴──────────────┐
│ IMPLEMENTAÇÕES           │        │ EXTENSÕES           │
│ (Infra.Services,         │        │ (Extension Methods)  │
│  Infra/Data,             │        │                      │
│  Application)            │        ├──────────────────────┤
├──────────────────────────┤        │ TecFlow.Infrastructure │
│ AfiliadoRepository       │────┐   │ .Services            │
│ CampanhaRepository       │────┤   │ ServiceRegExtensions │
│ ... (6 repos total)      │    │   │ └─ AddTecFlowInfra  ✓✓ │
│                          │    │   │                      │
│ DataService              │    │   │ TecFlow.Business       │
│ OpenAIService            │────┼───│ Service/Application  │
│ GeminiService            │    │   │ └─ AddTecFlowAppSvcs ✓ │
│ ShopeeApiService         │    │   │                      │
│ TikTokAdsApiService      │    │   │                      │
│ TikTokShopApiService     │    │   │ TecFlow.API            │
│ RankingService           │    │   │ Authentication       │
│ ScoreService             │    │   │ ServiceRegExtensions │
│ ... (11+ total)          │    │   │ └─ AddTecFlowAuth   ✓✓ │
│                          │    │   │                      │
│ 11 ApplicationServices ──┼────┘   └──────────────────────┘
│ (sem duplicatas)         │
│                          │
└──────────────────────────┘
         │
         │
    ┌────┴────────────────────────┐
    │ USADOS EM TODOS OS PROJETOS │
    ├────────────────────────────┤
    │ TecFlow.API                   │
    │ TecFlow.Orquestrador          │
    │ TecFlow.WebUi                 │
    │ TecFlow.Worker (se usar)      │
    └────────────────────────────┘

✓✓ ÚNICA FONTE DE VERDADE
✓✓ FÁCIL SINCRONIZAR
✓✓ SEM DUPLICATAS
```

---

## 🔄 DIAGRAMA 8: CICLO DE IMPLEMENTAÇÃO

```
┌─────────────────┐
│   START         │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ FASE 1: Consolidar Registration    │
│ (1 hora - 4 arquivos → 1 arquivo) │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ FASE 2: Mover Arquivos             │
│ (1.5 horas - reorganizar)          │
│ ✓ Interfaces para Core             │
│ ✓ Impls fora de Interfaces         │
│ ✓ Arquivos soltos organizados      │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ FASE 3: Criar Novos Arquivos       │
│ (1 hora - novos extension methods) │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ FASE 4: Editar Existentes          │
│ (1.5 horas - Program.cs, etc.)    │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ FASE 5: Testes e Validação        │
│ (1 hora - compile, run, test)     │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────┐
│ ✅ COMPLETE    │
│ 4-6 horas      │
└─────────────────┘
```

---

## 📋 LEGENDA

| Símbolo | Significado |
|---------|------------|
| ✓ | Correto, sem problemas |
| ✓✓ | Muito bom, recomendado |
| ✓✓✓ | Otimizado, excelente |
| ⚠️ | Atenção, possível problema |
| ❌ | Erro, crítico |
| 🟢 | Status OK |
| 🟡 | Status em desenvolvimento/incompleto |
| 🔴 | Status crítico |
| △ | Fluxo ascendente |
| ▼ | Fluxo descendente |

---

## 🔔 DIAGRAMA 4d: Push + Deep Links (Fase 4.3)

```
Mobile App                          TecFlow.API
──────────                          ──────────
FCM/APNs token ──POST──► /api/devices/register ──► UserDeviceTokens (DB)
                              ▲
Worker/Webhook event ──► NotificationHubService (Firebase Admin)
                              │
                              └──► FCM data: { route, title, body }
                                        │
Mobile: TecFlowFirebaseMessagingService / iOS UNCenter
        └──► PushNotificationBridge ──► NavigationIntentService
                    └──► Blazor: /engajamento/fila | /conciliacao/detalhes/{id}

Deep link: tecflow://engajamento/fila
           tecflow://conciliacao/detalhes/42
```

---

## 📱 DIAGRAMA 4c: Multiplataforma WebUi + MAUI (Fase 4.2)

```
┌─────────────────────┐     ┌─────────────────────┐
│   TecFlow.WebUi     │     │   TecFlow.Mobile    │
│ Blazor Server host  │     │ MAUI BlazorWebView  │
│ OAuth + cookies     │     │ sessão em memória   │
└──────────┬──────────┘     └──────────┬──────────┘
           │  AddAdditionalAssemblies   │  Root: Routes (RCL)
           └────────────┬───────────────┘
                        ▼
           ┌────────────────────────────┐
           │     TecFlow.SharedUi       │
           │  Pages · Layout · Widgets  │
           │  AddTecFlowClientServices  │
           └────────────┬───────────────┘
                        │ HttpClient "Orquestrador"
                        ▼
           ┌────────────────────────────┐
           │ TecFlow.Orquestrador / API │
           └────────────────────────────┘
```

---

## 📱 DIAGRAMA 4b: WebUi Mobile-First (Fase 4.1)

```
┌─────────────────────────────────────────────────────────────────────────┐
│ viewport < 768px                                                         │
│  MainLayout: [☰] TecFlow ── SessionBadge                                 │
│  sidebar off-canvas (transform) + backdrop tap-to-close                  │
│  CampaignsWidget / MetricsWidget: .data-cards-mobile (stack)           │
│  breakpoint ≥ 768px: .data-table-desktop (tabela)                        │
│  botões/filtros: min-height 44px (.btn-touch)                            │
└─────────────────────────────────────────────────────────────────────────┘
```

| Breakpoint | Comportamento |
|------------|----------------|
| &lt; 768px | Tabelas ocultas; cards empilhados; menu lateral off-canvas |
| ≥ 768px | Tabelas visíveis; sidebar fixa (≥ 992px) |
| Filtros GET | `Page`, `PageSize` (máx. 30) → `PagingInfoDto` em `*ResponseDto` |

---

## 🖥️ DIAGRAMA 4: FLUXO WebUi — Filter / Dto / ResponseDto (Fase 3)

```
┌──────────────────────────────────────────────────────────────────────────┐
│ TecFlow.WebUi (Blazor Server)                                            │
│  CampaignFilterForm ──bind──► CampaignFilter                             │
│  MetricFilterForm   ──bind──► MetricFilter                               │
│  CampaignCreateForm ──bind──► CampaignDto                                  │
│  Dashboard.razor → IDashboardApiService → HttpService + query string    │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │ HTTP + Bearer
                                ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ TecFlow.Orquestrador / TecFlow.API                                       │
│  GET  api/Campanhas?[CampaignFilter&Page&PageSize] → CampaignResponseDto │
│       + PagingInfoDto (TotalCount, HasNextPage)                          │
│  GET  api/Metricas?[MetricFilter&Page&PageSize]    → MetricResponseDto   │
│  POST api/Campanhas (CampaignDto)     → CampaignResponseDto              │
│  POST api/Metricas  (MetricDto)       → MetricResponseDto                │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ Widgets leem ResponseDto.DataList (Campaign, Metric) + Status/Descricao   │
└──────────────────────────────────────────────────────────────────────────┘
```

```mermaid
sequenceDiagram
  participant Form as CampaignFilterForm
  participant Page as Dashboard.razor
  participant Svc as DashboardApiService
  participant HTTP as HttpService
  participant API as Orquestrador/API

  Form->>Page: @bind CampaignFilter
  Page->>Svc: GetCampaignsByFilterAsync(filter)
  Svc->>HTTP: GET api/Campanhas?Name=...&StartDate=...
  HTTP->>API: [FromQuery] CampaignFilter
  API-->>HTTP: CampaignResponseDto
  HTTP-->>Page: ApiResult CampaignResponseDto
  Page->>Page: CampaignsWidget(Response.DataList)
```

---

**FIM DOS DIAGRAMAS**

*Sincronizado com pastas físicas em 04/06/2026 (Fase 4.1 mobile-first).*  
*Próximo:* [README.md](../README.md) · [LISTA_ARQUIVOS_MUDANCAS.md](./LISTA_ARQUIVOS_MUDANCAS.md) · [INDICE_COMPLETO.md](./INDICE_COMPLETO.md)
