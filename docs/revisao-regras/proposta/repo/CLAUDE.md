# CLAUDE.md: Ofertio

> Índice para a IA. Regras universais em `docs/padroes/`, regras deste sistema em `docs/projeto/`, decisões em `docs/adr/`. Abra só o documento da área da tarefa e o `docs/CHECKLIST_ATIVO.md`.
> Não leia `docs/arquivo/` (congelado) nem `docs/HISTORICO_FASES_CONCLUIDAS.md`, salvo para o contexto de uma entrega antiga.
> Itens marcados ❓ dependem de decisão pendente em `docs/revisao-regras/RELATORIO.md` §7 (perguntas 11 e 16) e serão ajustados antes da aplicação.

## O sistema

Plataforma SaaS multi-inquilino para afiliados: conversão de links de afiliado, busca de ofertas pela API oficial dos marketplaces, agendamento de envios para WhatsApp/Telegram, páginas públicas, painel de cliques e planos pagos (Asaas). Backend .NET 8 (API, Worker), WebUi Blazor Server, SQL Server, RabbitMQ, tudo em Docker atrás do Caddy. Produção numa VPS; homologação e desenvolvimento nesta máquina espelhando a produção.

## Comandos

PowerShell 5.1: encadeie com `;`, não com `&&`.

```powershell
dotnet build .\Ofertio.sln -c Release
dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj
dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj --filter "FullyQualifiedName~Radar"
dotnet format .\Ofertio.sln --verify-no-changes
dotnet list .\Ofertio.sln package --vulnerable --include-transitive

# Migrations (SQL Server). Gerar: permitido. Aplicar: só com autorização.
dotnet ef migrations add <Nome> --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
dotnet ef migrations script <Anterior> <Nome> --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext

# E2E (Playwright, contra homologação; credenciais em User Secrets)
dotnet test .\Ofertio.E2E\Ofertio.E2E.csproj
```

Ambientes, Docker e deploy: `docs/EXECUCAO_DESENVOLVIMENTO.md`, `docs/EXECUCAO_HOMOLOGACAO.md`, `docs/EXECUCAO_PRODUCAO.md`.

## Mapa da solução

Referências reais dos `.csproj` em nov/2026. Referência nova entre projetos exige ADR.

| Projeto | Responsabilidade | Depende de |
|---|---|---|
| `Ofertio.Core` | entidades e enums de domínio | nada |
| `Ofertio.Util` | utilitários (validação, criptografia) | nada |
| `Ofertio.Database` | `AppDbContext`, entidades de banco, `*Filter`, paginação | Core, Util |
| `Ofertio.Data` | migrations SQL Server | Database, Util |
| `Ofertio.Business` | `*Dto`/`*ResponseDto`, interfaces, `*Rules` (regras puras), opções | Core, Database (dívida, ver ADR 0002) |
| `Ofertio.Application` | casos de uso compartilhados por API, Worker e Orquestrador | Business, Core, Util |
| `Ofertio.Observability` | Serilog, OpenTelemetry, métricas | Business |
| `Ofertio.Infrastructure` | JWT e segurança (as migrations PostgreSQL saem: ADR 0001) | Business, Core, Database, Util |
| `Ofertio.Infrastructure.Services` | serviços com IO/HTTP, repositórios, integrações | Business, Infrastructure, Observability, Core, Util |
| `Ofertio.API` | controllers, middlewares, workers hospedados | Application, Business, Data, Database, Core, Infrastructure(.Services), Observability |
| `Ofertio.Worker` | processamento em fila | Application, Core, Data, Infrastructure(.Services), Observability |
| `Ofertio.Orquestrador` | host de orquestração (ativo; não está no `docker-compose.yml` de produção, ver pergunta 16) | igual à API |
| `Ofertio.SharedUi` | páginas e componentes Blazor (RCL) | Business, Core, Util, Database (dívida) |
| `Ofertio.WebUi` / `Ofertio.Mobile` | host Blazor + YARP / app MAUI (ativo, .NET 9) | SharedUi |
| `Ofertio.Tests` / `Ofertio.E2E` | xUnit + Moq / Playwright | qualquer um |

Detalhe, padrão de 3 objetos e passo a passo de feature nova: `docs/projeto/arquitetura.md`.

## Regras essenciais

Cada linha é a regra; o documento linkado traz exceções e como verificar.

**Segurança**
1. DEVE: todo endpoint exige autenticação por padrão (FallbackPolicy). `[AllowAnonymous]` só com justificativa em comentário e na lista de anônimos aprovados do teste global. → `docs/padroes/seguranca.md` S1–S2
2. DEVE: endpoint com `{id}` filtra pelo dono/inquilino em GET, PUT e DELETE; outro dono recebe 404. → S10
3. NÃO DEVE: resposta da API devolver entidade; os dados saem em `[Nome]ReadDto`, sem segredo, hash ou token. → `docs/projeto/arquitetura.md` §3
4. DEVE: preço, crédito, saldo e plano calculados no servidor; webhook autenticado e idempotente. → S14, S24
5. NÃO DEVE: segredo em arquivo versionado (`appsettings*.json`, compose, scripts, docs). Segredo vai no `.env` (Docker) ou em User Secrets (Visual Studio). → S26
6. NÃO DEVE: logar token, senha, `client_secret`, código OAuth, `code_verifier`, cartão ou URL com credencial. → `docs/padroes/observabilidade.md` §2
7. DEVE: credenciais gravadas no banco passam pelo conversor criptografado; identificadores `TecFlow` do Data Protection não mudam. → `docs/projeto/seguranca-ofertio.md`

**Dados e API**
8. DEVE: cada entidade usa `[Nome]Filter` (GET), `[Nome]Dto` (POST/PUT), `[Nome]ReadDto` (dados devolvidos) e `[Nome]ResponseDto` (envelope de sucesso). → `docs/projeto/arquitetura.md` §3
9. DEVE: toda listagem pagina via `PagedListHelper`, máximo 30 itens. Exceções listadas no documento. → `docs/projeto/arquitetura.md` §4
10. DEVE: banco único SQL Server `Ofertio`, provider `SqlServer`. Migration nova é sempre nova; NÃO DEVE editar migration existente. → `docs/padroes/banco-de-dados.md` §1
11. DEVE: rotas **novas** em inglês, kebab-case. NÃO DEVE: renomear rota existente em português (contrato com a WebUi e o Mobile). → `docs/padroes/api.md` §1
12. DEVE: código HTTP correto em toda resposta (400, 401, 403, 404, 409, 422, 500); erro sai como `ProblemDetails` com `errorCode`. NÃO DEVE: erro com HTTP 200. → `docs/projeto/arquitetura.md` §4
13. DEVE: só API oficial dos marketplaces, token no cabeçalho; NÃO DEVE raspar páginas nem contornar captcha/anti-robô. → `docs/projeto/integracoes-marketplaces.md` §1

**Código**
14. DEVE: código C# e mensagens de log em inglês; comentários, documentação e texto da tela em português. → `docs/padroes/codigo-csharp.md` §1
15. DEVE: scripts `.ps1` em UTF-8 com BOM (o hook do Claude Code garante); os demais arquivos em UTF-8, com ou sem BOM. → `docs/projeto/encoding-e-legado.md`
16. DEVE: async ponta a ponta com `CancellationToken`; NÃO DEVE `.Result`/`.Wait()`/`DateTime.Now`. → `docs/padroes/codigo-csharp.md` §4–5
17. DEVE: lógica de decisão em classes estáticas `*Rules` no Business, com teste unitário; serviços só orquestram. → `docs/projeto/arquitetura.md` §2

**Tela**
18. DEVE: tela nova registrada em `MenuItems`; "controlar por plano" é sempre a árvore Grupo → Tela → Componente, com checagem no servidor. → `docs/projeto/menu-e-permissoes.md`
19. DEVE: botão com ícone `bi bi-*`; campo preenchível com `id` `{tela}-{campo}` e `<label for>` igual; sem `window.confirm/alert/prompt`. → `docs/projeto/frontend-ui.md`
20. DEVE: horários gravados em UTC e exibidos com `IUserClock` (cliente) ou `MasterPageBase` (Master). → `docs/projeto/frontend-ui.md` §8

**Testes**
21. DEVE: comportamento novo ou corrigido ganha teste em `Ofertio.Tests`; o que a pessoa vê ou clica também em `Ofertio.E2E`, no mesmo passo. → `docs/padroes/testes.md`, `docs/projeto/frontend-ui.md` §10
22. DEVE: endpoint novo tem teste de autorização (sem token, outro dono). → `docs/padroes/testes.md` §1

**Ambientes**
23. DEVE: dev e homologação seguem a produção; diferença só em override e registrada na tabela do roteiro. → `docs/projeto/ambientes-docker.md`
24. NÃO DEVE: `docker compose down -v`, `docker volume rm` ou `dotnet ef database update` em base vazia. → `docs/projeto/ambientes-docker.md` §5

## Áreas sensíveis: mostre o plano e espere aprovação antes de alterar

- Autenticação e autorização: `Ofertio.API/Program.cs`, `Ofertio.API/Security/`, `Ofertio.Infrastructure/Security/`, `PlatformAuthService`, `AuthCookieService`, `Ofertio.WebUi/Extensions/AuthenticationExtensions.cs`, filtro de inquilino no `AppDbContext`.
- Pagamento e plano: `MyPlanService`, `AsaasWebhookService`, `TenantAdminService` (billing), `DealCreditsService`, `IPlanPaymentGateway`.
- Criptografia e chaves: `ISecurityEncryptionService`, `EncryptedStringConverter`, `AddOfertioDataProtection`, `OAuthStateProtector`, `LegacyCredentialReEncryptService`.
- Webhooks: `Ofertio.API/Controllers/*Webhook*`, `WebhookSecurityAttribute`, `MarketplaceWebhookSignatureVerifier`.
- Migrations existentes: `Ofertio.Data/Migrations/`, `Ofertio.Infrastructure/Migrations/`.
- Infra de produção: `docker-compose.yml`, `Caddyfile`, `Dockerfile`s, `nuget.config`.
- Termos e aceite (LGPD): `TermsDocumentRules`, aceite de versão.

## Como trabalhar

Siga `docs/padroes/trabalho-com-ia.md` (ciclo entender → planejar → mudar pouco → testar → verificar → relatar) e a Definição de Pronto (`docs/padroes/definicao-de-pronto.md`). Preserve validações e regras existentes fora do escopo pedido. Comandos prontos: `/nova-feature`, `/revisar-pr`, `/auditoria-seguranca`, `/auditoria-performance`, `/nova-migration`, `/atualizar-docs`.

## Fluxo de documentação

1. Tarefa nova entra como `[ ]` em `docs/CHECKLIST_ATIVO.md`; ao concluir, o detalhe vai para `docs/HISTORICO_FASES_CONCLUIDAS.md` e o item sai do checklist.
2. Mudança de fluxo (controller → serviço novo, entidade, integração, referência entre projetos) atualiza `docs/DIAGRAMAS_ARQUITETURA.md` no mesmo PR.
3. Regra nova ou alterada vai para o arquivo da área em `docs/projeto/` ou `docs/padroes/`, nunca para o `README.md` nem para este arquivo (salvo se for essencial, com uma linha).
4. Decisão de arquitetura vira ADR em `docs/adr/`.

## Onde está cada regra

| Assunto | Documento |
|---|---|
| Camadas, 3 objetos, controllers, feature nova | `docs/projeto/arquitetura.md` |
| Segurança específica (criptografia, Data Protection, cartão, termos) | `docs/projeto/seguranca-ofertio.md` |
| Menu dinâmico e permissões por plano | `docs/projeto/menu-e-permissoes.md` |
| Telas Blazor, ícones, combos, modais, ids para E2E | `docs/projeto/frontend-ui.md` |
| Mercado Livre, TikTok Shop, links de afiliado, Telegram | `docs/projeto/integracoes-marketplaces.md` |
| Docker, `.env`, volumes, paridade com produção | `docs/projeto/ambientes-docker.md` |
| Encoding e identificadores legados | `docs/projeto/encoding-e-legado.md` |
| C#, segurança base, performance, banco, API, testes, logs, Git, DoD, IA, documentação | `docs/padroes/*.md` |
| Decisões já tomadas | `docs/adr/` |
