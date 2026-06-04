# 📋 LISTA EXECUTIVA: ARQUIVOS A MOVER/CRIAR/DELETAR

**Última varredura:** 3 de junho de 2026  
**Workspace:** `c:\Programacao\Tecso.AutomacaoCusor` (pasta ainda com prefixo *Tecso*; projetos já renomeados para *TecFlow*)  
**Solution:** `TecFlow.sln` — 13 projetos (`TecFlow.*` + `Tecso.LerArquivos` externo)

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

- [ ] **MetricDto.cs**, **CampaignDto.cs**, **DashboardSummaryDto.cs** — `TecFlow.Business/Dto/` e `TecFlow.Portal/Models/Responses/`. (Ação: Portal consumir DTOs de `TecFlow.Business` (referência de projeto) ou mapear explicitamente; evitar divergência de contrato.)

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
- [ ] `TecFlow.Dashboard/WeatherForecast.cs` + `Controllers/WeatherForecastController.cs`  
(Ação: excluir se não usados em produção.)

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
├── TecFlow.Portal/                  # Blazor/UI — DTOs locais duplicados
├── TecFlow.Dashboard/               # Scaffold
├── TecFlow.Worker/
├── TecFlow.Tests/
├── TecFlow.Util/                    # ValidationHelper, Encryption, CEP
└── (externo) ../Tecso.LerArquivos/  # Utilitário fora do repo principal
```

### Grafo de referências (simplificado)

```
API / Orquestrador / Worker / Dashboard
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
| 8 | Portal usar DTOs de Business | Médio — contrato API |

---

*Gerado por varredura automatizada dos arquivos `.cs` e `.csproj` (excluindo `bin/` e `obj/`). Revisar checkboxes conforme cada item for concluído.*
