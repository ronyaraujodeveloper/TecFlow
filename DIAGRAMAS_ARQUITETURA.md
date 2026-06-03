# 🏗️ DIAGRAMAS VISUAIS: ARQUITETURA ATUAL vs PROPOSTA

---

## 📊 DIAGRAMA 1: ESTRUTURA DE DIRETÓRIOS - ATUAL ❌

```
TecFlow.Automacao/
│
├── TecFlow.Core/ ✓ Bem estruturado
│   ├── Interfaces/
│   │   ├── Repositories/ (9 interfaces) ✓
│   │   ├── Services/ 
│   │   │   ├── ✓ IAIService.cs
│   │   │   ├── ✓ IGeminiService.cs
│   │   │   ├── ✓ IDataService.cs
│   │   │   ├── ✓ IScoreService.cs
│   │   │   ├── ✓ IRankingService.cs
│   │   │   ├── ✓ IAnaliseService.cs
│   │   │   ├── ❌ AnaliseService.cs (impl em pasta de interface!)
│   │   │   ├── ❌ CampanhaService.cs (impl em pasta de interface!)
│   │   │   ├── ❌ ProdutoService.cs (impl em pasta de interface!)
│   │   │   └── ❌ UsuarioService.cs (impl em pasta de interface!)
│   ├── Entities/ (11) ✓
│   ├── Exceptions/ (4) ✓
│   ├── Dto/ (2) ✓
│   └── Prompts/ (3) ✓
│
├── TecFlow.Application/ 🟡 Bom design, mas incomplete
│   ├── Services/
│   │   ├── ✓ 11 ApplicationServices
│   │   ├── ❌ ApplicationServiceCollectionExtensions (NUNCA é chamada!)
│   │   ├── ⚠️ GeminiApplicationService (2x - duplicada!)
│   │   └── ⚠️ PublicacaoApplicationService (2x - duplicada!)
│   └── Dto/ (bem organizado por agregado) ✓
│
├── TecFlow.Infrastructure/ 🟡 Parcialmente organizado
│   ├── Configuration/ (AppConfiguration, SerilogLogger)
│   ├── Data/
│   │   ├── AppDbContext.cs ✓
│   │   ├── DataService.cs ✓
│   │   └── Configurations/ (2 arquivos)
│   ├── API/
│   │   ├── Shopee/ ✓
│   │   └── TikTok/ ✓
│   ├── Security/ (vazia?)
│   ├── ❌ AIProvider.cs (solto!)
│   ├── ❌ ShopeeProductResult.cs (solto!)
│   └── Interfaces/ (3)
│
├── TecFlow.Infrastructure.Services/ 🔴 DESORGANIZADO
│   ├── ❌ ServiceRegistrationExtensions.cs (DUPLICATA #1)
│   ├── ❌ CoreServiceRegistrationExtensions.cs (DUPLICATA #2)
│   ├── ❌ ExternalServiceRegistrationExtensions.cs (DUPLICATA #3)
│   ├── ❌ InfrastructureDataServiceRegistrationExtensions.cs (DUPLICATA #4)
│   ├── Repositories/ (6 impls) ✓
│   ├── Service/
│   │   ├── ExternalServices/ (7 serviços)
│   │   └── (outros services)
│   ├── ❌ Interfaces/ (4 - DEVEM ESTAR EM CORE!)
│   │   ├── IShopeeApi.cs
│   │   ├── ITikTokShopApi.cs
│   │   ├── ITikTokAdsApiService.cs
│   │   └── ITikTokShopApiService.cs
│   └── Serilog.cs (solto)
│
├── TecFlow.API/ 🟢 Bem organizado
│   ├── Program.cs ✓ (mas incompleto: não chama ApplicationServices)
│   ├── Controllers/ (11) ✓
│   ├── Middleware/ (1) ✓
│   └── Properties/, appsettings, etc. ✓
│
├── TecFlow.Dashboard/ 🟡 Padrão MVC
│   ├── Controllers/, Pages/, Views/, wwwroot/
│   └── Program.cs
│
├── TecFlow.Orquestrador/ 🔴 CRÍTICO
│   ├── ❌ Program.cs ("Hello, World!" - não faz nada!)
│   ├── Configurator.cs (REPLICA TUDO MANUALMENTE - SEM SINCRONIZAR!)
│   ├── ❌ OrquestradorPrincipal.cs (ControllerBase híbrido - confunde!)
│   ├── Pipelines/ (4)
│   └── Interfaces/ (1)
│
├── TecFlow.Worker/ 🟡 Mínimo
│   └── WorkerService.cs
│
├── TecFlow.Tests/ 🟡 Básico
│   ├── Mock/, UnitTests/, Integration/
│   └── (6+ test classes)
│
└── TecFlow.Util/ 🟡 OK
    └── Dependências: AutoMapper, FluentValidation, Json
```

---

## 📊 DIAGRAMA 2: ESTRUTURA DE DIRETÓRIOS - PROPOSTA ✅

```
TecFlow.Automacao/
│
├── TecFlow.Core/ ✓✓ Melhorado
│   ├── Interfaces/
│   │   ├── Repositories/ (9 interfaces) ✓
│   │   ├── Services/ 
│   │   │   ├── Business/ (8 interfaces) ✓
│   │   │   │   ├── IAIService.cs
│   │   │   │   ├── IGeminiService.cs
│   │   │   │   ├── IDataService.cs
│   │   │   │   ├── IAnaliseService.cs
│   │   │   │   ├── IScoreService.cs
│   │   │   │   ├── IRankingService.cs
│   │   │   │   ├── IPublicacaoService.cs
│   │   │   │   └── IOrquestradorService.cs
│   │   │   └── ExternalApis/ (4 interfaces - MOVIDAS) ✓✓
│   │   │       ├── IShopeeApi.cs
│   │   │       ├── ITikTokShopApi.cs
│   │   │       ├── ITikTokAdsApiService.cs
│   │   │       └── ITikTokShopApiService.cs
│   │   └── Infrastructure/ (3 interfaces)
│   │       ├── IAppConfiguration.cs
│   │       ├── IUserContextProvider.cs
│   │       └── ILoggerService.cs
│   ├── Entities/ (11) ✓
│   ├── Exceptions/ (4) ✓
│   ├── Dto/ (genéricos)
│   └── Prompts/ (3) ✓
│
├── TecFlow.Application/ ✓✓ Melhorado
│   ├── Services/
│   │   ├── ✓ 10 ApplicationServices (SEM DUPLICATAS)
│   │   │   ├── AIApplicationService.cs
│   │   │   ├── AnaliseApplicationService.cs
│   │   │   ├── CampanhasApplicationService.cs
│   │   │   ├── ConfiguracaoApplicationService.cs
│   │   │   ├── GeminiApplicationService.cs (1x)
│   │   │   ├── MetricasApplicationService.cs
│   │   │   ├── OrquestradorApplicationService.cs
│   │   │   ├── ProdutosApplicationService.cs
│   │   │   ├── PromocoesApplicationService.cs
│   │   │   └── PublicacaoApplicationService.cs (1x)
│   │   └── ✓✓ ServiceRegistrationExtensions.cs (CONSOLIDADO)
│   │       └── AddTecFlowApplicationServices()
│   └── Dto/ (bem organizado) ✓
│
├── TecFlow.Infrastructure/ ✓✓ Melhorado
│   ├── Configuration/
│   │   ├── AppConfiguration.cs ✓
│   │   ├── SerilogLogger.cs ✓
│   │   └── ✓ AIProvider.cs (MOVIDO)
│   ├── Data/
│   │   ├── AppDbContext.cs ✓
│   │   ├── DataService.cs ✓
│   │   └── Configurations/ (2)
│   ├── API/
│   │   ├── Shopee/
│   │   │   ├── ShopeeApi.cs ✓
│   │   │   └── Models/ (✓ ShopeeProductResult.cs MOVIDO)
│   │   └── TikTok/
│   │       ├── TikTokAdsApi.cs
│   │       ├── TikTokShopApi.cs
│   │       └── Models/
│   ├── Models/ (NOVO para organizados soltos)
│   │   └── ✓ AIProvider.cs (ORGANIZADO)
│   ├── Security/
│   └── Interfaces/ (3 - mantidos aqui para config)
│
├── TecFlow.Infrastructure.Services/ ✓✓ CONSOLIDADO
│   ├── ✓✓ ServiceRegistrationExtensions.cs (ÚNICO ARQUIVO)
│   │   └── AddTecFlowInfrastructureServices(config)
│   │       ├── DbContext
│   │       ├── IAppConfiguration
│   │       ├── 6 Repositories
│   │       ├── 4 Business Services
│   │       └── 5 External API HttpClients
│   ├── Repositories/ (6 impls) ✓
│   └── Service/
│       ├── ExternalServices/ (7 serviços) ✓
│       ├── Business/ (services implementados)
│       └── (com usings atualizados para Core.Interfaces.Services.ExternalApis)
│
├── TecFlow.API/ ✓✓ Melhorado
│   ├── ✓✓ Program.cs (ATUALIZADO)
│   │   ├── AddTecFlowInfrastructureServices(config) ✓
│   │   ├── AddTecFlowApplicationServices() ✓ NOVO
│   │   ├── AddTecFlowAuthentication(config) ✓ CONSOLIDADO
│   │   └── [resto ok]
│   ├── Controllers/ (11) ✓
│   ├── Middleware/ (1) ✓
│   └── Properties/, appsettings, etc. ✓
│
├── TecFlow.Dashboard/ 🟡 Sem mudanças
│   ├── Controllers/, Pages/, Views/, wwwroot/
│   └── Program.cs
│
├── TecFlow.Orquestrador/ ✓✓ REFATORADO
│   ├── ✓✓ Program.cs (EXECUTÁVEL)
│   │   └── await new Configurator().ConfigureAndRunAsync();
│   ├── ✓✓ Configurator.cs (SIMPLIFICADO)
│   │   ├── AddTecFlowInfrastructureServices(config)
│   │   └── AddTecFlowApplicationServices()
│   ├── ✓✓ Services/ (NOVO DIRETÓRIO)
│   │   └── OrquestradorService.cs (LÓGICA PURA)
│   ├── OrquestradorPrincipal.cs (REFATORADO - Controller ou eliminado)
│   ├── Pipelines/ (4) ✓
│   └── Interfaces/ (1) ✓
│
├── TecFlow.Worker/ 🟡 Sem mudanças
│   └── WorkerService.cs
│
├── TecFlow.Tests/ 🟡 Sem mudanças significativas
│   ├── Mock/, UnitTests/, Integration/
│   └── (6+ test classes)
│
└── TecFlow.Util/ 🟡 Sem mudanças
    └── Dependências OK
```

---

## 🔄 DIAGRAMA 3: FLUXO DE DEPENDENCY INJECTION - ATUAL ❌

```
┌─────────────────────────────────────────────────────────────────┐
│                      TecFlow.API                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                 ┌────────────┼────────────┐
                 ▼            ▼            ▼
    ┌──────────────────────────────────────────────────────┐
    │ Program.cs (INCOMPLETO - Não chama ApplicationServices!) 
    │                                                      │
    │ ✓ AddTecFlowInfrastructureData(connStr)              │
    │   └─ DbContext                                      │
    │      └─ Repositories (6)                            │
    │      └─ IShopeeApi ⚠️ DUPLICADO                    │
    │                                                      │
    │ ✓ AddTecFlowInfrastructureServices(config)           │
    │   └─ HttpClient<IShopeeApi> ⚠️ DUPLICADO          │
    │   └─ HttpClient<ITikTokShopApi> (2x) ⚠️ DUPLICADO │
    │   └─ HttpClient<ITikTokAdsApiService>              │
    │                                                      │
    │ ❌ AddTecFlowApplicationServices() - NÃO CHAMADO!    │
    │                                                      │
    │ Manual registrations:                               │
    │   ├─ JwtTokenService                                │
    │   ├─ IUsuarioRepository ⚠️ DUPLICADO               │
    │   └─ JWT Auth                                       │
    └──────────────────────────────────────────────────────┘
           Resulta em: Controllers sem acesso a ApplicationServices!


┌─────────────────────────────────────────────────────────────────┐
│                   TecFlow.ORQUESTRADOR                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                 ┌────────────┴────────────┐
                 ▼                         ▼
    ┌──────────────────────┐    ┌──────────────────────┐
    │ Program.cs           │    │ Configurator.cs      │
    │ "Hello, World!"❌    │    │ (REPLICA TUDO!)❌    │
    │                      │    │                      │
    │ (NÃO FAZ NADA!)      │    │ Manual Registration: │
    │                      │    │ ├─ DbContext         │
    │                      │    │ ├─ Repositories      │
    │                      │    │ ├─ HttpClients       │
    │                      │    │ ├─ Business Services │
    │                      │    │ └─ ...               │
    │                      │    │                      │
    │                      │    │ ❌ SE ALGO MUDAR NA  │
    │                      │    │    API, QUEBRA AQUI! │
    └──────────────────────┘    └──────────────────────┘
           ❌ NÃO É SINCRONIZADO COM API!
           ❌ SEM ENTRY POINT!
           ❌ SEM APPLICATION SERVICES!
```

---

## ✅ DIAGRAMA 4: FLUXO DE DEPENDENCY INJECTION - PROPOSTA

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
│ TecFlow.Application/                                              │
│   Services/ServiceRegistrationExtensions.cs (ÚNICO)             │
│   └─ AddTecFlowApplicationServices() ✓✓                           │
│      └─ 10 ApplicationServices (SEM DUPLICATAS)                 │
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

## 📂 DIAGRAMA 5: ESTRUTURA DE INTERFACES - ATUAL vs PROPOSTA

### ATUAL ❌
```
TecFlow.Core/Interfaces/
├── Repositories/ (9)
│   ├── IRepository.cs
│   ├── IAfiliadoRepository.cs
│   ├── ICampanhaRepository.cs
│   └── ... (6 mais)
│
├── Services/ (❌ MISTURADO!)
│   ├── ✓ IAIService.cs (interface ok)
│   ├── ✓ IGeminiService.cs (interface ok)
│   ├── ✓ IDataService.cs (interface ok)
│   ├── ... mais interfaces
│   ├── ❌ AnaliseService.cs (NÃO É INTERFACE!)
│   ├── ❌ CampanhaService.cs (NÃO É INTERFACE!)
│   ├── ❌ ProdutoService.cs (NÃO É INTERFACE!)
│   └── ❌ UsuarioService.cs (NÃO É INTERFACE!)
│
└── (sem subpasta ExternalApis)

TecFlow.Infrastructure.Services/Interfaces/
├── ❌ IShopeeApi.cs (DEVERIA ESTAR EM CORE!)
├── ❌ ITikTokShopApi.cs (DEVERIA ESTAR EM CORE!)
├── ❌ ITikTokAdsApiService.cs (DEVERIA ESTAR EM CORE!)
└── ❌ ITikTokShopApiService.cs (DEVERIA ESTAR EM CORE!)
```

### PROPOSTA ✓✓
```
TecFlow.Core/Interfaces/
│
├── Repositories/ (9)
│   ├── IRepository.cs (base)
│   ├── IAfiliadoRepository.cs
│   ├── ICampanhaRepository.cs
│   └── ... (6 mais)
│
├── Services/ (✓ LIMPO!)
│   ├── Business/ (8 interfaces)
│   │   ├── IAIService.cs
│   │   ├── IGeminiService.cs
│   │   ├── IDataService.cs
│   │   ├── IAnaliseService.cs
│   │   ├── IScoreService.cs
│   │   ├── IRankingService.cs
│   │   ├── IPublicacaoService.cs
│   │   └── IOrquestradorService.cs
│   │
│   └── ExternalApis/ (4 interfaces - MOVIDAS) ✓✓
│       ├── IShopeeApi.cs
│       ├── ITikTokShopApi.cs
│       ├── ITikTokAdsApiService.cs
│       └── ITikTokShopApiService.cs
│
└── Infrastructure/ (3)
    ├── IAppConfiguration.cs
    ├── IUserContextProvider.cs
    └── ILoggerService.cs

TecFlow.Infrastructure.Services/Interfaces/
└── (DELETADA - tudo movido para Core!)
```

---

## 🗺️ DIAGRAMA 6: MAPA DE DEPENDÊNCIAS - ATUAL ❌

```
┌──────────────────────────────────────────────────────────────┐
│ REPOSITÓRIOS (TecFlow.Core.Interfaces.Repositories)           │
├──────────────────────────────────────────────────────────────┤
│ IRepository ◄─── BaseEntity                                 │
│ IAfiliadoRepository                                          │
│ ICampanhaRepository                                          │
│ IConteudoRepository                                          │
│ IMetricaRepository                                           │
│ IProdutoRepository                                           │
│ IUsuarioRepository                                           │
│ IOrquestradorRepository (sem impl)                           │
│ IAIProvider                                                  │
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
│ AfiliadoRepository       │────────│ InfrastructureData ✓ │
│ CampanhaRepository       │────────│ InfrastructureData ✓ │
│ ConteudoRepository       │────────│ InfrastructureData ✓ │
│ MetricaRepository        │────────│ InfrastructureData ✓ │
│ ProdutoRepository        │────────│ InfrastructureData ✓ │
│ UsuarioRepository        │────────│ InfrastructureData ✓ │
│                          │        │ API/Program ❌ (DUP) │
│                          │        │ Orquestrador ❌ (DUP)│
└──────────────────────────┘        └──────────────────────┘


┌──────────────────────────────────────────────────────────────┐
│ SERVICES (❌ ESPALHADO)                                       │
├──────────────────────────────────────────────────────────────┤
│ TecFlow.Core.Interfaces.Services:                             │
│   ├─ IAIService                ◄─ OpenAIService            │
│   ├─ IGeminiService            ◄─ GeminiService            │
│   ├─ IDataService              ◄─ DataService              │
│   ├─ IAnaliseService           ◄─ AnaliseService (WRONG!)  │
│   ├─ IScoreService             ◄─ ScoreService             │
│   ├─ IRankingService           ◄─ RankingService           │
│   └─ IPublicacaoService        ◄─ ?                        │
│                                                              │
│ TecFlow.Infrastructure.Services.Interfaces: (❌ WRONG PLACE)  │
│   ├─ IShopeeApi               ◄─ ShopeeApiService         │
│   ├─ ITikTokShopApi            ◄─ TikTokShopApiService    │
│   ├─ ITikTokAdsApiService      ◄─ TikTokAdsApiService     │
│   └─ ITikTokShopApiService     ◄─ ? (talvez duplicata)     │
│                                                              │
│ TecFlow.Application: (❌ NUNCA REGISTRADA)                    │
│   ├─ AIApplicationService                                   │
│   ├─ AnaliseApplicationService                              │
│   ├─ CampanhasApplicationService                            │
│   ├─ ConfiguracaoApplicationService                         │
│   ├─ GeminiApplicationService (2x - duplicata!)             │
│   ├─ MetricasApplicationService                             │
│   ├─ OrquestradorApplicationService                         │
│   ├─ ProdutosApplicationService                             │
│   ├─ PromocoesApplicationService                            │
│   └─ PublicacaoApplicationService (2x - duplicata!)         │
└──────────────────────────────────────────────────────────────┘


┌──────────────────────────────────────────────────────────────┐
│ REGISTRATION (❌ 4 ARQUIVOS DESORGANIZADOS)                  │
├──────────────────────────────────────────────────────────────┤
│ Arquivo 1: ServiceRegistrationExtensions.cs                 │
│   └─ AddTecFlowInfrastructureServices()                       │
│      ├─ HttpClient<IShopeeApi> ⚠️                           │
│      ├─ HttpClient<ITikTokShopApi> (2x) ⚠️                  │
│      └─ HttpClient<ITikTokAdsApiService>                    │
│                                                              │
│ Arquivo 2: CoreServiceRegistrationExtensions.cs             │
│   └─ AddTecFlowCoreServices()                                 │
│      ├─ IAnaliseCalculoService                              │
│      └─ IScoreService ⚠️ (duplicado em Arquivo 1?)         │
│                                                              │
│ Arquivo 3: ExternalServiceRegistrationExtensions.cs         │
│   └─ AddTecFlowExternalServices()                             │
│      ├─ HttpClient<IGeminiService>                          │
│      ├─ HttpClient<IAIService>                              │
│      └─ HttpClient<IShopeeApi> ⚠️ (DUPLICADO em Arquivo 1)│
│                                                              │
│ Arquivo 4: InfrastructureDataServiceRegistrationExtensions  │
│   └─ AddTecFlowInfrastructureData()                           │
│      ├─ DbContext ⚠️ (DUPLICADO em API/Program)            │
│      ├─ Repositories                                        │
│      └─ IShopeeApi ⚠️ (DUPLICADO em Arquivo 1 & 3)        │
│                                                              │
│ Arquivo 5: ApplicationServiceCollectionExtensions.cs        │
│   └─ AddTecFlowApplicationServices() ⚠️ (NUNCA CHAMADO!)     │
└──────────────────────────────────────────────────────────────┘
```

---

## 🗺️ DIAGRAMA 7: MAPA DE DEPENDÊNCIAS - PROPOSTA ✓✓

```
┌──────────────────────────────────────────────────────────────┐
│ INTERFACES (TecFlow.Core - ÚNICA FONTE DE VERDADE)            │
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
│ DataService              │    │   │ TecFlow.Application    │
│ OpenAIService            │────┼───│ .Services            │
│ GeminiService            │    │   │ ServiceRegExtensions │
│ ShopeeApiService         │    │   │ └─ AddTecFlowAppSvcs✓✓ │
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
    │ TecFlow.Dashboard             │
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

**FIM DOS DIAGRAMAS**
*Próximo: Leia "LISTA_ARQUIVOS_MUDANCAS.md" para executar as mudanças*
