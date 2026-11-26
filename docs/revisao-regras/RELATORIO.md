# Revisão das regras de construção do Ofertio

- **Data:** 25/11/2026
- **Escopo:** branch `fix/seguranca-f-endpoints-anonimos` (base `main` em `2f04597`), com as alterações locais não commitadas presentes no momento.
- **Método:** leitura de todos os arquivos de regra, amostragem do código por busca e leitura, build dos projetos principais (`dotnet build`, Debug) e consulta à documentação oficial do Claude Code (https://code.claude.com/docs/en/memory, `/settings`, `/permissions`, `/hooks`, `/skills`).
- **Reaproveitamento:** a auditoria de segurança e performance do mesmo dia (`docs/auditoria/RELATORIO.md`, não versionada) já cobre os checklists de 27 e 31 itens com evidência. Este relatório cita os IDs dela (S1…S28, P1…P21, F1…F14) em vez de repetir a análise, e confere no código os pontos usados como evidência de regra.
- **Nenhum arquivo existente foi alterado.** Tudo o que foi produzido está em `docs/revisao-regras/`.
- **Segredos:** nenhum valor foi exibido; só os locais.

---

## 1. Resumo executivo

O Ofertio tem **regras de domínio e de interface muito boas** (menu por plano, ids para E2E, combos, OAuth, paridade de ambientes Docker) e elas são seguidas no código. O ponto fraco é o oposto do comum: faltam as **regras transversais de engenharia** (autorização padrão, autorização por objeto, formato de API, testes de integração, migrations, DoD) e **nenhuma regra é garantida por automação**. Não há CI, `.editorconfig`, `Directory.Build.props`, `CLAUDE.md` nem `.claude/`. As instruções para IA estão no `.cursorrules`, que o Claude Code não lê.

As regras que falharam na prática (segredos no git, endpoint anônimo, vazamento de `PasswordHash`) são justamente as que dependiam só de texto ou que **nem existiam**. Uma delas é causada pela própria regra: o padrão `ResponseDto` manda devolver a entidade em `Data`.

### 1.1 Maturidade das regras por categoria (0–10)

A nota mede a regra escrita **e** a garantia de que é cumprida, não a qualidade do código.

| Categoria | Nota | Comentário |
|---|---|---|
| 3.1 Arquitetura e organização | 5 | Camadas e padrão de 3 objetos descritos; sem dependências permitidas, padrões proibidos, passo a passo de feature nem ADRs |
| 3.2 Convenções C#/.NET | 4 | Idioma, nomes e async definidos; sem exceções/Result, DI, analisadores ou formatação automática |
| 3.3 Segurança | 3 | Criptografia, logs e OAuth bem definidos; faltam autorização padrão, IDOR, rate limit, CORS, webhooks, SSRF. Regra de segredos existe e foi ignorada |
| 3.4 Performance | 3 | Paginação e timeout HTTP; nada sobre projeção, N+1, `AsNoTracking`, índices, compressão ou quando não otimizar |
| 3.5 Banco de dados | 4 | Provider único e comandos bem definidos; nada sobre migration aplicada, destrutiva, concorrência, auditoria ou backup |
| 3.6 Design de API | 3 | Envelope e filtros definidos; rotas em dois idiomas, três formatos de erro, sem códigos HTTP |
| 3.7 Testes | 5 | Regra forte ("todo comportamento ganha teste") e E2E muito bem especificado; sem tipos, integração, fluxos críticos ou nomenclatura |
| 3.8 Logs e operação | 2 | Só "não logar segredos" e idioma; sem níveis, correlação, health checks ou alertas |
| 3.9 Configuração e ambientes | 7 | `ambientes-docker.md` é exemplar; falta separar segredos versionados e validar opções |
| 3.10 Fluxo Git | 4 | Conventional Commits (100% seguido); sem branches, PR, proteção da `main`, tags |
| 3.11 CI/CD | 0 | `.github/workflows` vazio |
| 3.12 Definição de Pronto | 2 | Pedaços no `.cursorrules` e no README; nada verificável |
| 3.13 Regras para IA | 2 | `.cursorrules` (só Cursor) e nota no README; nenhum `CLAUDE.md`, permissão, comando ou hook |
| 3.14 Documentação | 6 | Fluxo checklist → histórico e "regra vai para `docs/rules/`" claros; README mistura humano e IA; sem ADRs |
| Interface (extra) | 8 | Regras concretas, verificáveis e seguidas |

### 1.2 As 10 lacunas mais importantes

| # | Lacuna | Evidência | Por que importa |
|---|---|---|---|
| 1 | **Não há regra de autorização padrão** ("todo endpoint autenticado, `[AllowAnonymous]` só justificado") nem `FallbackPolicy` | Nenhuma regra em `docs/rules/`; `MasterAuthorizationExtensions.cs:10` sem FallbackPolicy; `UserAccountsController.cs:13-113` sem `[Authorize]` | Originou o incidente mais grave da auditoria (S1, A1) |
| 2 | **Não há regra de autorização por objeto (IDOR) nem de filtro de inquilino fechado** | Ausente nas regras; auditoria S7, F1 (`AppDbContext.cs:997-1000`) | Um usuário lê ou apaga dados de outro |
| 3 | **A regra do `ResponseDto` manda expor a entidade** | `backend-architecture.md:32` (`Data ([Nome]?)`); `UserAccountResponseDto.cs:9-10` devolve `UserAccount` | A regra produz vazamento de `PasswordHash` e tokens (S8) |
| 4 | **Nenhuma regra é garantida por automação**: sem CI, analisadores, `.editorconfig`, varredura de segredos ou de pacotes | `.github/workflows/` vazio; não há `.editorconfig`/`Directory.Build.props`; build gera NU1903 (ImageSharp) sem bloquear | Regra só em texto foi ignorada (segredos, pacotes vulneráveis) |
| 5 | **Regra de segredos existe, mas foi ignorada, e falta o procedimento de revogação** | Regra em `README.md:31`, `backend-architecture.md:60`; 10 `appsettings*.json` versionados com valores em chaves secretas; auditoria S21/S22 | Chaves JWT e de criptografia de produção expostas |
| 6 | **Sem regras para webhooks e operações de dinheiro** (assinatura, idempotência, valor calculado no servidor, concorrência de saldo) | Ausente; auditoria S9, S19, F4 | Créditos sem pagamento e renovação dupla |
| 7 | **Sem configuração para o Claude Code**: sem `CLAUDE.md`, sem `deny` de leitura de `.env`, sem comandos; as regras de IA estão no `.cursorrules` | `.cursorrules:1-9`; ausência de `CLAUDE.md` e `.claude/` | A IA trabalha sem as regras de IA e pode ler segredos |
| 8 | **Sem Definição de Pronto verificável** | Só fragmentos: `.cursorrules:3-9`, `README.md:96-100` | "Terminado" não inclui build sem warnings, segurança nem migration revisada |
| 9 | **Sem regras de migrations e backup** (aplicada é imutável, destrutiva em duas etapas, script idempotente, backup testado) | Só comandos (`backend-architecture.md:51`); backup pendente (`CHECKLIST_ATIVO.md:34`) | Perda de dados em deploy |
| 10 | **Regra de testes sem foco em fluxo crítico nem teste de integração** | `backend-architecture.md:89` ("todo comportamento"); 0 usos de `WebApplicationFactory`; 11 de 58 controllers com teste | Os bugs graves (autorização, pagamento) passaram por 176 arquivos de teste unitário |

---

## 2. Inventário (Fase 1)

### 2.1 Arquivos de regras e convenções

| Arquivo | Propósito | Linhas | Último commit | Público | Referenciado por |
|---|---|---|---|---|---|
| `README.md` | painel do projeto, regras de ouro, comandos, ambientes, fluxo de docs, nota para IA | 104 | 2026-11-20 | ambos (misturado) | `.cursorrules`, rules |
| `.cursorrules` | regras de documentação para a IA do Cursor | 9 | 2026-11-14 | IA (só Cursor) | `README.md:99` |
| `.cursorignore` | arquivos que o Cursor ignora | 8 | 2026-11-08 | IA (só Cursor) | não |
| `docs/rules/backend-architecture.md` | camadas, 3 objetos, controllers, banco, segurança, menu, uploads/IIS, testes | 92 | 2026-11-20 | ambos | README, rules |
| `docs/rules/frontend-ui.md` | ícones, combos, responsividade, modais, badges, sidebar, telas, ids E2E | 116 | 2026-11-19 | ambos | README, rules, `PLANO_RPA.md` |
| `docs/rules/marketplaces-oauth.md` | limites, OAuth ML/TikTok, tokens, busca, links, plataformas, Telegram | 87 | 2026-11-02 | ambos | README, rules |
| `docs/rules/encoding-standards.md` | BOM, cultura, idioma, nomenclatura, legado | 47 | 2026-11-14 | ambos | README, rules |
| `docs/rules/ambientes-docker.md` | paridade dev/hml/prod, overrides, segredos, volumes | 62 | 2026-11-20 | ambos | README, EXECUCAO_* |
| `docs/CHECKLIST_ATIVO.md` | pendências (contém algumas regras embutidas) | 119 | 2026-11-23 | ambos | README, rules |
| `docs/HISTORICO_FASES_CONCLUIDAS.md` | detalhe das entregas | 1767 | 2026-11-14 | consulta | README, checklist |
| `docs/DIAGRAMAS_ARQUITETURA.md` | diagramas Mermaid | 217 | 2026-11-15 | ambos | README, `.cursorrules` |
| `docs/EXECUCAO_DESENVOLVIMENTO.md` | roteiro dev (+ "O que não fazer") | 186 | 2026-11-20 | humano | README, rules |
| `docs/EXECUCAO_HOMOLOGACAO.md` | roteiro homologação | 122 | 2026-11-23 | humano | README, rules |
| `docs/EXECUCAO_PRODUCAO.md` | roteiro produção | 177 | 2026-11-23 | humano | README |
| `docs/CONFIGURACAO_PRODUCAO_DOCKER.md` | referência da produção | 158 | 2026-11-23 | humano | README, rules |
| `docs/PLANO_RPA.md` | plano E2E (fases 1–9 fechadas) | 81 | 2026-11-14 | ambos | README, rules |
| `docs/Ofertio_MOBILE_BUILD.md` | build mobile | 24 | 2026-11-14 | humano | README |
| `docs/Legal/RASCUNHO_CLAUSULA_TESTE_GRATIS.md` | rascunho jurídico | 77 | 2026-11-18 | humano | 1 |
| `docs/arquivo/*` (9 arquivos) | histórico congelado | — | 2026-11-23 | consulta | README (marcado "não é regra") |
| `docs/auditoria/*` (2 arquivos) | auditoria de 25/11 | — | não versionado | ambos | não |
| `.gitignore` | exclusões do git | 16 | 2026-11-20 | ferramenta | rules |
| `.dockerignore` | exclusões do build Docker | 13 | 2026-11-20 | ferramenta | não |
| `.env.example` | modelo de segredos | 39 | 2026-11-20 | humano | README, rules |
| `nuget.config` | fontes NuGet (verificação de assinatura desligada) | 11 | 2026-11-20 | ferramenta | rules |
| `comandos.txt` | anotações de comandos (contém segredos, auditoria F14) | 282 | 2026-10-18 | humano | 2 (arquivo) |
| `ProjectReference.txt` | lista antiga de referências | 42 | 2026-10-18 | humano | não; substituído por `DIAGRAMAS_ARQUITETURA.md` §1 |
| `Architecture.puml` | diagrama PlantUML antigo (`IDataService`, `Produto`) | 19 | 2026-10-18 | humano | não; desatualizado |
| `skills-lock.json` | trava de skill `swiftui-expert-skill` | 11 | não versionado | IA | não; sem relação com o projeto |
| Memória do Claude (`~/.claude/projects/.../memory/`, 7 arquivos) | preferências e 2 regras de projeto | — | fora do git | IA | `MEMORY.md` |
| `*.csproj` (17) | `Nullable` e `ImplicitUsings` em todos; `NoWarn` em 2 | — | — | ferramenta | — |

### 2.2 Arquivos procurados e ausentes

`CLAUDE.md` (raiz e subpastas), `AGENTS.md`, `.claude/` (settings, commands, skills, hooks), `.cursor/`, `.github/copilot-instructions.md`, `.github/pull_request_template.md`, templates de issue, qualquer workflow, `CONTRIBUTING.md`, `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `.globalconfig`, `stylecop.json`, `global.json`, `.gitleaks.toml`.

### 2.3 Mapa das regras por categoria

Legenda: **D** = duplicada · **C** = contraditória · **V** = vaga · **X** = desatualizada.

| Categoria | Regra (resumo) | Onde | Marca |
|---|---|---|---|
| Escopo | Altere **apenas** o arquivo e o trecho solicitados | `backend-architecture.md:7` | **C** com "teste no mesmo passo" (`backend-architecture.md:89-91`, `frontend-ui.md:109`) e "revise os roteiros no mesmo commit" (`ambientes-docker.md:38`), que exigem tocar outros arquivos |
| Escopo | Não remover validações/regras ao refatorar | `README.md:35`, `backend-architecture.md:8` | **D** |
| Escopo | Parta sempre da versão mais recente salva e validada no Git | `backend-architecture.md:9` | **V** |
| Encoding | UTF-8 com BOM em todo arquivo | `README.md:28`, `backend-architecture.md:10`, `encoding-standards.md:7` | **D** (3×); **X** no motivo ("para o IIS", `encoding-standards.md:3,7`) |
| Idioma | Código em inglês, **inclusive rotas de API** | `README.md:29`, `encoding-standards.md:24` | **D**; **C** com o envelope obrigatório `Descricao` (`backend-architecture.md:32`); ignorada nas rotas (ver Fase 2) |
| Idioma | Mensagens de log em português | `encoding-standards.md:25` | parcial (Fase 2) |
| Dados | 3 objetos por entidade | `README.md:30`, `backend-architecture.md:26-34`, `encoding-standards.md:29` | **D** (3×) |
| Dados | `ResponseDto.Data` é `[Nome]?` (a entidade) | `backend-architecture.md:32` | **C** com "segredos nunca vão para logs/telas" (`backend-architecture.md:55,61`): a entidade carrega hash e tokens |
| Dados | Listagem paginada, máximo 30 | `README.md:30`, `backend-architecture.md:39` | **D** |
| API | Retorno `ResponseDto` | `backend-architecture.md:38` | **C** com `ProblemDetails` (`ExceptionHandlingMiddleware.cs:80`) e com `ACCESS_DENIED` em HTTP 200 (`backend-architecture.md:77`): três formatos de erro sem regra de quando usar cada um |
| Segurança | Segredos fora do repositório | `README.md:31`, `backend-architecture.md:60`, `marketplaces-oauth.md:10`, `ambientes-docker.md:43-44`, `EXECUCAO_DESENVOLVIMENTO.md:184` | **D** (5×) |
| Segurança | Não logar tokens/segredos | `backend-architecture.md:61`, `marketplaces-oauth.md:9` | **D** |
| Banco | Nunca `docker compose down -v` | `README.md:82`, `ambientes-docker.md:55`, `EXECUCAO_DESENVOLVIMENTO.md:182` | **D** (3×) |
| Banco | PostgreSQL só como migrations legadas | `README.md:12`, `backend-architecture.md:50` | **X**: `RelationalDatabaseOptions.cs:35` ainda aceita `UseNpgsql`; `encoding-standards.md:20` ainda regra conexão PostgreSQL |
| Infra | Não criar subaplicação `/api` no site IIS | `backend-architecture.md:84`, `marketplaces-oauth.md:21` | **D**; **X** (IIS arquivado, `README.md:84`) |
| Infra | Seção "Arquivos, uploads e IIS"; HTTPS no IIS | `backend-architecture.md:80-86` | **X** em 84 e 86 |
| Integração | Sessão do Telegram em `C:\ProgramData\Ofertio\telegram-sessions` | `marketplaces-oauth.md:86` | **X**: `docker-compose.yml:85` usa `App_Data/telegram-sessions` no volume `api_appdata` |
| Integração | Redirect URI = `https://dev.ofertio.com.br/...` "na API, `appsettings.json`" | `marketplaces-oauth.md:14` | ambígua para produção (pergunta 11) |
| Integração | Tabela de plataformas com coluna "Status" | `marketplaces-oauth.md:63-74` | status de entrega dentro de regra; desatualiza |
| Testes | Comportamento novo → `Ofertio.Tests`; tela → `Ofertio.E2E` no mesmo passo | `README.md:33`, `backend-architecture.md:89-91`, `frontend-ui.md:109`, `PLANO_RPA.md:9` | **D** (os dois últimos já apontam para a fonte única; o README reescreve); **V** (não diz que tipo de teste nem o mínimo) |
| UI | Menu lateral com lista fixa de grupos | `frontend-ui.md:71` | **C** com o menu dinâmico definido pelo Master (`backend-architecture.md:68-70`) |
| UI | Controle por plano (Grupo → Tela → Componente) | `backend-architecture.md:71-78`, `frontend-ui.md:75-80`, memória `controle-itens-hierarquia-menu` | **D** (3×, uma fora do git) |
| UI | "contempla celular, tablet e desktop", "transição suave" | `frontend-ui.md:41,73` | **V** |
| Ambientes | Dev e homologação espelham a produção | `README.md:36`, `ambientes-docker.md`, memória `ambientes-espelham-producao` | **D** |
| Ambientes | Portas da WebUi no Visual Studio | `README.md:79` e `EXECUCAO_DESENVOLVIMENTO.md:169` (7002) vs `EXECUCAO_DESENVOLVIMENTO.md:112-122` (7012) | **C** leve (o texto explica; a tabela e o README não) |
| Git | Conventional Commits; histórico = `git log` | `encoding-standards.md:37`, `.cursorrules:5`, `README.md:99` | **D** (3×) |
| Documentação | Mudou fluxo → atualizar diagrama | `.cursorrules:6`, `README.md:99` | **D**; parcialmente seguida (Fase 2) |
| Documentação | Regra nova vai para `docs/rules/`, não para o README | `.cursorrules:8`, `README.md:100` | **D** |
| IA | Ler só a regra da camada; não ler `docs/arquivo/` | `README.md:3,104`, `.cursorrules:5` | instrução de IA dentro do README humano |

---

## 3. Aderência do código (Fase 2)

Amostragem: 58 controllers, 29 filtros, 105 DTOs, 66 classes `*Rules`, 176 arquivos de teste, 232 campos de formulário em `.razor`, 1380 arquivos de texto, 17 `.csproj`, 100 últimos commits.

| Regra | Onde está | Status | Evidência | Diagnóstico |
|---|---|---|---|---|
| UTF-8 com BOM | `encoding-standards.md:7` | **Cumprida** | 1379 de 1380 arquivos versionados com BOM (contagem por bytes, exceto migrations) | automação existe (`NormalizarEncodingPrecoce.ps1`) |
| `Nullable` habilitado | não escrita | **Cumprida** | `<Nullable>enable` nos 17 `.csproj` | boa prática sem regra; centralizar no `Directory.Build.props` |
| Sem `.Result`/`.Wait()` | `backend-architecture.md:43` | **Cumprida** | auditoria P6; nenhum uso em produção | — |
| Sem `DateTime.Now`/`ToLocalTime` | `frontend-ui.md:85` | **Cumprida** | 1 ocorrência, no template `WeatherForecastController.cs:26` | — |
| Sem `window.confirm/alert/prompt` | `frontend-ui.md:47` | **Cumprida** | 0 ocorrências em `.razor`/`.js` | — |
| Sem links fixos no `NavMenu` | `backend-architecture.md:68` | **Cumprida** | só links do portal anônimo (`NavMenu.razor:46`) | registrar a exceção |
| `id` estável em campo preenchível | `frontend-ui.md:97` | **Cumprida (~93%)** | 17 de 232 tags sem `id` na mesma linha (heurística); concentradas em `MultiSelectDropdown.razor`, `WhatsAppConexao.razor`, `BotConversor.razor` | regra boa; falta verificação automática (teste que varre `.razor`) |
| `Options` com `SectionName` | `encoding-standards.md:31` | **Cumprida** | 18 de 21; as 3 restantes não são lidas de configuração | — |
| Conventional Commits | `encoding-standards.md:37` | **Cumprida** | 100 de 100 commits | — |
| Migrations em PascalCase | `encoding-standards.md:36` | **Cumprida** | 47 migrations, todas no padrão | — |
| 3 objetos por entidade | `backend-architecture.md:26-34` | **Parcial** | 29 `Filter`, 105 DTOs; 3 DTOs `Update*`/`*Request` em `Business/Dto`; 22 classes `*Request/*Payload/*Command` fora (a maioria payload de integração, exceção legítima não documentada) | regra quase certa; falta listar as exceções |
| `ResponseDto` sem dado sensível | não existe | **Ignorada (por causa da regra)** | `UserAccountResponseDto.cs:9-10`; auditoria S8/P2 | **a regra está errada** |
| Controller sem `DbContext` | não escrita | **Parcial** | 2 de 58 injetam `AppDbContext` (`InventoryController`, `MarketplaceAuthController`) | falta a regra |
| Paginação máx. 30 | `backend-architecture.md:39` | **Parcial** | `PagedListHelper.cs:7-8` correto; listagens sem paginação na auditoria P5 | falta teste/revisão automática |
| `ValidationHelper` antes de persistir | `backend-architecture.md:42` | **Parcial** | auditoria S10 | falta padrão único de validação |
| `HttpClient` tipado com timeout | `backend-architecture.md:44` | **Parcial** | auditoria P7 (OK) e P9 (Asaas, Evolution, ML sem política) | — |
| Credenciais criptografadas no banco | `backend-architecture.md:55` | **Cumprida** | auditoria S28 (`AppDbContext.cs:1041-1060`) | — |
| Segredos fora do repositório | `backend-architecture.md:60` e mais 4 | **Ignorada** | 10 `appsettings*.json` versionados com valor preenchido em chave secreta (só o local; ver auditoria §5); `comandos.txt` | **falta automação** (gitleaks) e placeholder padrão |
| Logs sem tokens | `backend-architecture.md:61` | **Parcial** | auditoria S26 (`ExternalApiLoggingHandler.cs:23-51`) | falta teste do handler de log |
| Checagem de permissão no servidor | `backend-architecture.md:77` | **Parcial** | auditoria §2.7 (radar arbitragem sem `RequiresAccess`) | falta teste de integração |
| Toda rota em inglês | `encoding-standards.md:24` | **Ignorada** | a maioria das rotas é portuguesa: `api/Afiliados`, `api/Campanhas`, `api/conta`, `api/vendas/pedidos`, `api/integracoes`… ao lado de `api/affiliate-links`, `api/marketplace-auth`; `[Route("[controller]")]` no template | **a regra está errada ou incompleta**: precisa de decisão (pergunta 1) |
| Classes e métodos em inglês | `encoding-standards.md:24` | **Parcial** | 23 arquivos `.cs` com nome em português (`IntegracaoLoja*`, `GerarLinkAfiliadoDto`, `BuscarProdutosAsync`) | dívida antiga; analisador não resolve |
| Logs em português | `encoding-standards.md:25` | **Parcial** | amostra de 199 mensagens: ~90 em português, ~109 em inglês | regra sem verificação; decidir se fica (pergunta 13) |
| Teste para todo comportamento | `backend-architecture.md:89` | **Parcial** | 176 arquivos; 45 de 66 `*Rules` com teste; 11 de 58 controllers; **0** testes com `WebApplicationFactory` | regra vaga; falta o tipo de teste nos fluxos críticos |
| Estrutura de testes | não escrita | **Ignorada** | 7 pastas com papéis sobrepostos em `Ofertio.Tests/` (`Unit`, `UnitTests`, `Integration`, `IntegrationTests`, `APITests`, `Services`, `ServicesTests`) | falta a regra |
| Nomenclatura de teste | não escrita | **Parcial** | 887 métodos `A_B`, 382 `A_B_C`, 8 sem `_` | falta a regra |
| Diagramas atualizados com mudança de fluxo | `.cursorrules:6` | **Parcial** | localidades IBGE (`5a27e6b`) não aparecem em `DIAGRAMAS_ARQUITETURA.md` (0 menções); envio de e-mail (`20b7ba3`) com 1 menção | depende da memória da IA; o Claude não lê `.cursorrules` |
| Entidade com `CreatedAt`/`UpdatedAt` | não escrita | **Parcial** | `BaseEntity.cs` existe; 58 de 75 entidades herdam; preenchimento manual (`Touch()`) | falta regra e interceptor |
| Build sem warnings | não escrita | **Quase cumprida** | build atual: 0 erros, 14 warnings únicos (CS8601/CS8604 em `IntegracaoLojaService.cs:733,795` e testes, CS1573 em `PainelController.cs:35`, NU1902/NU1903 do ImageSharp) | barato ativar `TreatWarningsAsErrors` |

**Conclusão da Fase 2.** As regras concretas e locais (UI, encoding, nomes) são seguidas. As ignoradas caem em dois grupos:
- **Regra errada:** idioma das rotas e `ResponseDto.Data`.
- **Regra sem automação:** segredos, logs sensíveis, paginação e diagramas.

---

## 4. Matriz de lacunas (Fase 3)

Status: Coberta / Parcial / Ausente / N/A. "Proposta" aponta para o rascunho em `proposta/`.

| Categoria | Item | Status | Evidência | Proposta | Prioridade |
|---|---|---|---|---|---|
| 3.1 | Camadas e dependências permitidas | Parcial | camadas descritas (`backend-architecture.md:12-24`), dependências não; `Business`→`Database` (`Ofertio.Business.csproj:12`) | tabela "Depende de" no `CLAUDE.md` + "referência nova exige ADR" (`repo/docs/projeto/arquitetura.md` §2) | Média |
| 3.1 | Estrutura de pastas | Coberta | `backend-architecture.md:14-22`, `encoding-standards.md:33` | mantida em `arquitetura.md` | Baixa |
| 3.1 | Padrões proibidos | Ausente | nada sobre lógica em controller ou entidade exposta | `arquitetura.md` §2–3 | Alta |
| 3.1 | Feature de ponta a ponta | Ausente | só "nova plataforma" (`marketplaces-oauth.md:76-81`) | `arquitetura.md` §7 + `/nova-feature` | Média |
| 3.1 | ADRs | Ausente | decisões espalhadas em regras e histórico | `docs/adr/0000` a `0008` | Média |
| 3.2 | Nomenclatura e idioma | Parcial | `encoding-standards.md:22-37`; rotas ignoram | `codigo-csharp.md` §1 + pergunta 1 | Média |
| 3.2 | Nullable | Parcial | ativo nos 17 `.csproj`, sem regra | `Directory.Build.props` + `codigo-csharp.md` §3 | Baixa |
| 3.2 | async/CancellationToken | Parcial | `backend-architecture.md:43` não fala de `CancellationToken` | `codigo-csharp.md` §4 + CA2016 | Baixa |
| 3.2 | DI e tempo de vida | Ausente | — | `codigo-csharp.md` §5 | Média |
| 3.2 | Exceções vs Result, ProblemDetails | Ausente | três formatos coexistem | `codigo-csharp.md` §6, `api.md` §4 + pergunta 3 | Média |
| 3.2 | Analisadores e formatação | Ausente | sem `.editorconfig`/`Directory.Build.props` | `kit-base/.editorconfig`, `Directory.Build.props` | Alta |
| 3.3 | Autenticação padrão / `[AllowAnonymous]` justificado | Ausente | S1 | `seguranca.md` S1–S2 + teste global de autorização | **Alta** |
| 3.3 | Hash de senha | Parcial | BCrypt em uso (S2), sem regra | `seguranca.md` S3 | Baixa |
| 3.3 | Lockout e rate limit | Ausente | S3, S15 | S4, S20 | Alta |
| 3.3 | Autorização por papel | Parcial | `backend-architecture.md:77` (plano) | S9 | Média |
| 3.3 | IDOR e inquilino fechado | Ausente | S7, F1 | S10–S11, `seguranca-ofertio.md` §4 | **Alta** |
| 3.3 | DTO contra mass assignment | Parcial | 3 objetos (sem campos proibidos) | S13 + `arquitetura.md` §3 | Alta |
| 3.3 | Preço/pagamento no servidor | Parcial | `backend-architecture.md:62-63` (cartão, checkout) | S14, `seguranca-ofertio.md` §5 | **Alta** |
| 3.3 | Validação de entrada | Parcial | `ValidationHelper` (`backend-architecture.md:42`) | S15 | Média |
| 3.3 | Consultas parametrizadas | Ausente (cumprida no código, S11) | — | S16 + CA2100 como erro | Baixa |
| 3.3 | Upload seguro / SSRF | Parcial | `backend-architecture.md:82-83` (só armazenamento) | S17–S18 | Alta |
| 3.3 | CORS | Ausente | S16 (auditoria) | S21 + busca no CI | Média |
| 3.3 | CSRF | Ausente (cumprida, S17) | — | S22 | Baixa |
| 3.3 | Cabeçalhos de segurança | Ausente | S18 | S23 + item no roteiro de produção | Média |
| 3.3 | Webhooks com assinatura e idempotência | Ausente | S19 | S24 | **Alta** |
| 3.3 | Erros sem detalhes internos | Ausente (quase cumprida, S20) | — | S25 | Baixa |
| 3.3 | Segredos fora do código e do git | Parcial (escrita, ignorada) | S21–S22 | S26 + gitleaks (CI e pre-commit) + placeholders | **Alta** |
| 3.3 | Revogação de chaves vazadas | Ausente | `CHECKLIST_ATIVO.md:73` só cita o problema | S27 + `ROTEIRO_BACKUP_E_SEGREDOS.md` | **Alta** |
| 3.3 | Dependências vulneráveis | Ausente | S24; NU1903 no build | S28 + `NuGetAudit` + passo no CI | Alta |
| 3.3 | Logs sem dados sensíveis | Parcial | `backend-architecture.md:61` | `observabilidade.md` §2 | Média |
| 3.3 | LGPD | Parcial | termos e aceite (`backend-architecture.md:64`); sem exclusão de conta | S32, `seguranca-ofertio.md` §6 | Média |
| 3.4 | Projeção com `Select` | Ausente | P2 | P1 | Média |
| 3.4 | `AsNoTracking` | Ausente | P3 | P2 | Baixa |
| 3.4 | N+1 | Ausente | P1 | P3 | Média |
| 3.4 | Índices | Ausente | P4 | P5 + `/nova-migration` | Baixa |
| 3.4 | Paginação obrigatória | Coberta (parcialmente seguida) | `backend-architecture.md:39` | mantida | Média |
| 3.4 | Cache | Parcial | cache global de busca (`marketplaces-oauth.md:32`) | P13 | Baixa |
| 3.4 | Timeout/retry externo e `IHttpClientFactory` | Parcial | `backend-architecture.md:44` | P7–P8 | Média |
| 3.4 | Tarefas lentas em segundo plano | Ausente | P12 (e-mail na requisição) | P9 | Média |
| 3.4 | Paralelismo | Ausente | — | P10 (RECOMENDADO) | Baixa |
| 3.4 | Compressão | Ausente | P13 | P11 | Baixa |
| 3.4 | Frontend: skeleton, debounce, imagens, JS | Parcial | spinner (`frontend-ui.md:56`), `?v=` (`frontend-ui.md:92`) | P14–P17; divisão de JS N/A (Blazor Server) | Baixa |
| 3.4 | Quando NÃO otimizar | Ausente | — | `performance.md` §5 | Média |
| 3.5 | Nome de migration | Coberta | `encoding-standards.md:36` | mantida | Baixa |
| 3.5 | Revisão / nunca editar aplicada / destrutiva | Ausente | — | `banco-de-dados.md` §1 + permissão `ask` + `/nova-migration` | **Alta** |
| 3.5 | Convenções de tabela/coluna | Ausente | — | `banco-de-dados.md` §2 | Baixa |
| 3.5 | Transações e concorrência (saldo/estoque) | Ausente | F4 | `banco-de-dados.md` §3 | Alta |
| 3.5 | Soft delete | Ausente (não usado) | 0 ocorrências de `IsDeleted` | "não é padrão" (`banco-de-dados.md` §2) | Baixa |
| 3.5 | Auditoria `CreatedAt`/`UpdatedAt` | Ausente | `BaseEntity.cs`; 58/75 | `banco-de-dados.md` §2, `arquitetura.md` §5 | Baixa |
| 3.5 | Seeds | Parcial | carga IBGE na migration (`backend-architecture.md:40`); `scripts/carga-homolog-rpa.sql` | `banco-de-dados.md` §4 | Baixa |
| 3.5 | Backup | Parcial | volumes (`ambientes-docker.md:46-55`); rotina pendente (`CHECKLIST_ATIVO.md:34`) | `banco-de-dados.md` §5 + ajuste em `ambientes-docker.md` | **Alta** |
| 3.6 | Padrão de rotas | Parcial (regra ignorada) | `encoding-standards.md:24` | `api.md` §1 + pergunta 1 | Média |
| 3.6 | Versionamento | Ausente | só `api/v1/integrations/*` | RECOMENDADO só com cliente externo (`api.md` §6) | Baixa |
| 3.6 | Códigos HTTP | Ausente | — | `api.md` §2 | Média |
| 3.6 | Formato de erro | Parcial (contraditória) | ver §2.3 | `api.md` §4, `arquitetura.md` §4 + pergunta 3 | Média |
| 3.6 | Paginação e filtros | Coberta | `backend-architecture.md:39-41` | mantida | Baixa |
| 3.6 | Idempotência | Ausente | S19 | `api.md` §2 | Alta |
| 3.6 | OpenAPI/Swagger | Ausente | Swagger anônimo na API (auditoria §2.7) | `api.md` §6 | Baixa |
| 3.7 | O que é obrigatório testar | Parcial (vaga) | `backend-architecture.md:89` | `testes.md` §1 | **Alta** |
| 3.7 | Tipos de teste | Parcial | unitário + E2E; sem integração | `testes.md` §2 | Alta |
| 3.7 | Nomenclatura | Ausente | 3 estilos | `testes.md` §3 | Baixa |
| 3.7 | Dados de teste | Parcial | prefixo `E2E-` (`frontend-ui.md:111`) | `testes.md` §4 | Baixa |
| 3.7 | Cobertura nos fluxos críticos | Ausente | — | `testes.md` §5 | Média |
| 3.8 | Logs estruturados e níveis | Ausente | — | `observabilidade.md` §1 + CA2254 | Média |
| 3.8 | Correlação | Ausente | só no middleware de exceção | `observabilidade.md` §3 | Baixa |
| 3.8 | O que nunca logar | Parcial | `backend-architecture.md:61` | `observabilidade.md` §2 | Média |
| 3.8 | Health checks | Ausente | 0 `AddHealthChecks` | `observabilidade.md` §4 | Média |
| 3.8 | Métricas e alertas | Ausente | S27, F12 | `observabilidade.md` §5 | Média |
| 3.9 | Dev/hml/prod | Coberta | `ambientes-docker.md` | mantida | Baixa |
| 3.9 | Segredos por ambiente | Parcial (duplicada 5×) | ver §2.3 | tabela única em `seguranca-ofertio.md` §3 | Alta |
| 3.9 | Feature flags | N/A | permissões por plano cumprem esse papel (ADR 0003) | — | — |
| 3.9 | `appsettings` só com valores não sensíveis | Parcial (ignorada) | auditoria S21 | `seguranca-ofertio.md` §3 + gitleaks | **Alta** |
| 3.9 | Validação de opções na subida | Ausente | 0 `ValidateOnStart` | `codigo-csharp.md` §5 | Baixa |
| 3.10 | Branches | Ausente | prática `fix/seguranca-*` sem regra | `git.md` §1 | Média |
| 3.10 | Commits | Coberta | `encoding-standards.md:37` | mantida em `git.md` §2 | Baixa |
| 3.10 | Tamanho e checklist de PR | N/A (decisão 5: sem PR) | — | revisão local com `/revisar-pr` antes do merge (`git.md` §3) | Média |
| 3.10 | Proteção da `main` | N/A (decisão 5: sem PR) | — | CI em todo push; ativar se houver mais pessoas (`git.md` §5) | Baixa |
| 3.10 | Versão/changelog | Parcial | "histórico = git log" | tag por deploy (`git.md` §4) | Baixa |
| 3.11 | Build e testes no CI | Ausente | workflows vazio | `ci.yml` | **Alta** |
| 3.11 | `dotnet format` | Ausente | — | passo no CI (não bloqueante no início) | Média |
| 3.11 | Analisadores | Ausente | — | `Directory.Build.props` gradual | Média |
| 3.11 | Pacotes vulneráveis | Ausente | NU1903 | passo no CI + `NuGetAudit` | Alta |
| 3.11 | Varredura de segredos | Ausente | — | gitleaks no CI + `.gitleaks.toml` | **Alta** |
| 3.11 | Deploy | Parcial | manual e documentado (`EXECUCAO_PRODUCAO.md`) | manter manual; tag por deploy | Baixa |
| 3.12 | Definição de Pronto | Ausente | fragmentos | `definicao-de-pronto.md` + `/revisar-pr` | **Alta** |
| 3.13 | Como a IA trabalha | Parcial | `.cursorrules` (só Cursor), `README.md:104` | `trabalho-com-ia.md` §1–2 + `CLAUDE.md` | **Alta** |
| 3.13 | O que a IA nunca faz sem autorização | Ausente | — | `trabalho-com-ia.md` §3 + `settings.json` | **Alta** |
| 3.13 | Áreas sensíveis | Ausente | — | `CLAUDE.md` "Áreas sensíveis" | Alta |
| 3.13 | Permissões `.claude/settings.json` | Ausente | — | `repo/.claude/settings.json` | **Alta** |
| 3.13 | Slash commands | Ausente | — | 6 comandos em `kit-base/.claude/commands/` | Média |
| 3.13 | Hooks | Ausente | — | hook de BOM só em `.ps1` (barato); build por edição descartado (`trabalho-com-ia.md` §5) | Baixa |
| 3.14 | O que documentar e onde | Parcial | `README.md:96-100` | `documentacao.md` §1 | Média |
| 3.14 | Atualizar docs na mesma tarefa | Parcial | `.cursorrules:6-7`, `ambientes-docker.md:38` | `documentacao.md` §2 + DoD + `/atualizar-docs` | Média |
| 3.14 | README para humanos | Parcial | mistura regras de IA (`README.md:27-36,104`) | `repo/README.md` | Média |

---

## 5. Observações sobre o Claude Code (confirmadas na documentação oficial)

| Recurso | O que a documentação diz | Efeito na proposta |
|---|---|---|
| Imports `@arquivo` no `CLAUDE.md` | Caminho relativo ao arquivo que importa, até 4 níveis; **carregados sempre**, junto do `CLAUDE.md` ("are expanded and loaded into context alongside the CLAUDE.md"). Ignorados dentro de blocos de código. (`/memory`) | O índice usa **links**, não imports, para não carregar todos os padrões em toda sessão |
| Tamanho | "target under 200 lines per CLAUDE.md file" (`/memory`) | `repo/CLAUDE.md` tem 128 linhas |
| `.claude/rules/*.md` com `paths:` | Carregado só quando a IA mexe em arquivo que casa com o padrão (`/memory`) | Etapa opcional 9 do plano: substitui "abra só a regra da camada" por carregamento automático |
| Permissões | Ordem deny → ask → allow; `Bash(cmd *)`; `Read(./.env)`; `deny` de `Read` cobre `cat`/`head`/`sed`, **mas não** `grep -r` nem scripts; prefixo de Bash "não é barreira de segurança" (`/permissions`) | `deny` serve para evitar acidente; a proteção real é tirar segredo do disco versionado |
| Comandos | "Custom commands have been merged into skills"; `.claude/commands/*.md` continua funcionando; skills recomendadas para trabalho novo com arquivos de apoio (`/skills`) | Comandos em `.claude/commands/` como pedido; migração para skills fica opcional (pergunta 6) |
| Hooks | `PostToolUse` com `matcher: "Edit\|Write"`, JSON no stdin com `tool_input.file_path`, `timeout` em segundos; exit 2 bloqueia e devolve o stderr à IA (`/hooks`) | Hook de BOM sai sempre com 0 (nunca bloqueia) |
| `.cursorrules` | Não faz parte dos arquivos lidos pelo Claude Code (a página de memória lista só `CLAUDE.md`, `CLAUDE.local.md` e `.claude/rules/`) | As regras dele vão para o `CLAUDE.md` |

---

## 6. Itens fora do escopo desta revisão (registrados)

- Correção dos achados de segurança: já tratados nas branches `fix/seguranca-*` a partir da auditoria.
- Limpeza da raiz: `comandos.txt` (com segredos), `test_*.txt`, `temp_replace_orquestrador.ps1`, `Architecture.puml`, `ProjectReference.txt` e os scripts de IIS. A proposta só os retira das regras; a remoção depende de você (pergunta 10).
- Atualização do .NET 8 (fora de suporte desde 10/11/2026, auditoria F7): é decisão de plataforma, não de regra. Fica como item do plano de segurança.

---

## 7. Perguntas para você

Decisões que mudam o texto das regras. Para cada uma indico a minha recomendação.

### 7.1 Decisões registradas (25/11/2026)

| # | Decisão | Onde foi aplicada nos rascunhos |
|---|---|---|
| 1 | **Inglês só para rotas novas**; rotas existentes em português não são renomeadas | `repo/CLAUDE.md` regra 11; `repo/docs/projeto/encoding-e-legado.md` §3–4; `repo/docs/projeto/arquitetura.md` §7 |
| 2 | Recomendação adotada: **quarto objeto `[Nome]ReadDto`** para os dados devolvidos; `ResponseDto` nunca tem entidade | ADR 0009 (substitui a 0002); `arquitetura.md` §3; `CLAUDE.md` regras 3 e 8; PLANO etapa 16 |
| 3 | **Códigos HTTP corretos em tudo**; todo erro em `ProblemDetails` com `errorCode`; `ACCESS_DENIED` vira 403 | ADR 0010; `arquitetura.md` §4; `menu-e-permissoes.md` §3; `kit-base/docs/padroes/api.md` §2 e §4; `codigo-csharp.md` §6; `CLAUDE.md` regra 12; PLANO etapa 15 |
| 4 | Recomendação adotada: **escopo = código pedido + testes dele + documentação afetada**; melhoria fora disso vira sugestão; correção mínima para compilar é permitida e relatada | `arquitetura.md` §1 |
| 5 | **Sem PR obrigatório.** Branch por tarefa, `/revisar-pr` e merge local; CI em todo push; tag por deploy | `kit-base/docs/padroes/git.md` (substitui `git-e-pr.md`); template de PR removido; `ci.yml`; DoD; README; PLANO etapa 9 |
| 6 | Recomendação: **manter comandos** (`.claude/commands/`), um arquivo cada | sem mudança nos rascunhos |
| 7 | Recomendação: **teste obrigatório nos fluxos críticos, sem meta global** | já era a proposta (`testes.md` §1 e §5) |
| 8 | **Cursor não é mais usado**: `.cursorrules` e `.cursorignore` removidos | `AJUSTES-ARQUIVOS-MOVIDOS.md`; PLANO etapa 2 |
| 9 | **BOM só em `.ps1`** | `encoding-e-legado.md` §1; `.editorconfig`; hook só em `.ps1`; `CLAUDE.md` regra 15; ADR 0006 |
| 10 | **Limpeza da raiz aprovada** | PLANO etapa 14 (lista fechada) |
| 11 | **Pendente:** você vai verificar a Redirect URI de produção | ❓ em `AJUSTES-ARQUIVOS-MOVIDOS.md` |
| 12 | Recomendação: **imagens com versão fixa e containers sem root** | `AJUSTES-ARQUIVOS-MOVIDOS.md` (ambientes §6); PLANO etapa 18 |
| 13 | **Logs em inglês, como regra** (os antigos são traduzidos quando o arquivo for tocado) | `codigo-csharp.md` §1; `observabilidade.md` §1; `CLAUDE.md` regra 14 |
| 14 | **Fim do suporte a PostgreSQL**; Orquestrador, Application e Mobile continuam ativos | ADR 0001 reescrita; `arquitetura.md` §5; `CLAUDE.md` (mapa); PLANO etapas 3 (job Windows para o Mobile), 15 (Mobile publicado antes) e 17 |
| 15 | **`~/.claude/CLAUDE.md` aprovado**, criado na etapa 13; **kit fica neste repositório** em `docs/kit-base/`, sem cópias dos padrões | `kit-base/LEIA-ME.md`; `proposta/LEIA-ME.md`; PLANO etapas 2 e 13 |

### 7.2 Nova pergunta

16. **Onde o `Ofertio.Orquestrador` roda em produção?** Você confirmou que ele está ativo, mas ele não aparece no `docker-compose.yml` da produção (auditoria §2.3), e o compose aponta `OrquestradorApi__BaseUrl` para `http://api:8080/` (`docker-compose.yml:112`), ou seja, para a própria API. Se ele roda fora do Docker, as regras de ambiente e o CI precisam incluí-lo. Se a função dele foi absorvida pela API, o projeto é candidato a ADR de descontinuação.

### 7.3 Perguntas originais (respondidas em 25/11, exceto a 11)

1. **Idioma das rotas e identificadores.** A regra diz "rotas em inglês" (`encoding-standards.md:24`), mas a maioria das rotas é portuguesa (`api/Campanhas`, `api/conta`, `api/vendas/pedidos`) e o envelope usa `Descricao`. Opções: (a) oficializar **rotas em português** e código C# em inglês; (b) manter inglês só para rotas novas e conviver com as antigas; (c) migrar tudo para inglês. *Recomendo (b)*: renomear rota quebra o Mobile publicado, e a regra precisa valer para o que vem pela frente.
2. **O que vai em `ResponseDto.Data`.** Hoje é a entidade (`backend-architecture.md:32`), o que vazou `PasswordHash`. Opções: (a) permitir um quarto objeto `[Nome]ReadDto` (ou projeção) só para leitura; (b) manter 3 objetos e marcar campos sensíveis com `[JsonIgnore]` na entidade; (c) `Data` vira tipo anônimo/projeção montado no serviço. *Recomendo (a)*: é explícito, testável e não depende de lembrar o atributo.
3. **Formato de erro.** Convivem envelope com `Status=false`, `ProblemDetails` e `ACCESS_DENIED` com HTTP 200. Aceita a tabela proposta em `proposta/repo/docs/projeto/arquitetura.md` §4 (negócio = 200 + envelope; validação = 400; exceção = 500 ProblemDetails; plano negado continua 200 até o cliente distinguir 403 de sessão expirada)? Ou prefere ir para códigos HTTP corretos em tudo e ajustar o cliente?
4. **Escopo da tarefa.** "Altere apenas o arquivo e o trecho solicitados" (`backend-architecture.md:7`) conflita com teste e documentação no mesmo passo. Posso reescrever como "escopo = código pedido + testes dele + documentação afetada; melhorias fora disso viram sugestão"?
5. **Fluxo de PR.** Você trabalha sozinho e hoje usa branches locais (`fix/seguranca-*`), sem `gh` instalado. Quer adotar PR obrigatório com CI e proteção da `main` no GitHub, ou prefere merge local com o CI rodando só no push da `main`?
6. **Comandos ou skills.** A documentação oficial recomenda skills para trabalho novo; comandos em `.claude/commands/` continuam funcionando. Mantenho `.claude/commands/` (como você pediu, um arquivo por comando) ou já crio como skills?
7. **Cobertura nos fluxos críticos.** Aceita como regra bloqueante: "100% dos endpoints com teste de autorização e 100% dos fluxos de pagamento/crédito/webhook com teste de integração", sem meta global? Isso exige criar a base de testes de integração (`WebApplicationFactory`), que hoje não existe.
8. **`.cursorrules` e Cursor.** Você ainda usa o Cursor? Se não, o arquivo é removido e o conteúdo vai para o `CLAUDE.md`. Se sim, ele passa a ter uma linha: "Siga o `CLAUDE.md`".
9. **UTF-8 com BOM.** A regra nasceu para o IIS (`encoding-standards.md:3,7`), que saiu. Mantém para todos os tipos de arquivo (o hook garante sem custo) ou reduz a RECOMENDADO só para `.ps1` (PowerShell 5.1)?
10. **Arquivos soltos na raiz.** Posso propor, numa etapa própria, a remoção ou o arquivamento de `comandos.txt`, `test_*.txt`, `temp_replace_orquestrador.ps1`, `Architecture.puml`, `ProjectReference.txt`, `skills-lock.json` (skill de SwiftUI) e dos scripts de IIS?
11. **Redirect URI em produção.** `marketplaces-oauth.md:14` e `Ofertio.API/appsettings.json:98,124` usam `https://dev.ofertio.com.br/...` como valor base. A produção sobrescreve por variável de ambiente ou usa o domínio de homologação de propósito?
12. **Containers e imagens.** Viram regra "imagens de terceiros com versão fixa (sem `:latest`)" e "containers da aplicação sem root"? Hoje nenhuma das duas é cumprida (auditoria F8).
13. **Idioma dos logs.** A regra pede português (`encoding-standards.md:25`), mas ~55% das mensagens amostradas estão em inglês. Mantém português (e corrige aos poucos) ou libera inglês?
14. **Projetos ativos.** `Ofertio.Orquestrador` (fora do compose de produção), `Ofertio.Application`, `Ofertio.Mobile` (net9, sem CI possível em Linux) e o suporte a PostgreSQL (`UseNpgsql` ainda no código): quais continuam ativos e entram nas regras e no CI?
15. **`~/.claude/CLAUDE.md` e kit base.** Posso criar o seu `CLAUDE.md` de usuário a partir de `proposta/usuario/CLAUDE.md`? E onde quer guardar o kit base: num repositório próprio (recomendo, para ter uma fonte única) ou como pasta modelo neste?
