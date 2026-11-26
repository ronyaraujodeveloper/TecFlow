# Plano de implantação das novas regras

> Etapas pequenas, cada uma numa branch e num commit próprios (sem PR, decisão de 25/11), na ordem abaixo. Nenhuma etapa começa sem a sua aprovação.
> Rascunhos em [`proposta/`](./proposta/). Contexto e perguntas em [`RELATORIO.md`](./RELATORIO.md).
> Ponto de partida medido em 25/11/2026: build com 0 erros e 14 warnings únicos; 1379 de 1380 arquivos com BOM; nenhum CI.

## Visão geral

| # | Etapa | Depende de | Risco | Pode quebrar o build? |
|---|---|---|---|---|
| 0 | Responder às perguntas e ajustar os rascunhos | — | nenhum | não |
| 1 | `CLAUDE.md` + permissões do Claude Code | 0 | baixo | não |
| 2 | Reorganizar a documentação (`docs/padroes`, `docs/projeto`, `docs/adr`, README) | 0 | baixo (links) | não |
| 3 | CI mínimo: build + testes | — | baixo | não (só informa) |
| 4 | Varredura de segredos (gitleaks) | rotação dos segredos | médio | o job de segredos pode falhar |
| 5 | `Directory.Build.props` + `.editorconfig` neutros | 3 | baixo | não |
| 6 | Zerar os 14 warnings e ligar `TreatWarningsAsErrors` | 5 | médio | **sim, por desenho** |
| 7 | `dotnet format` geral + passo bloqueante no CI | 6, branches `fix/seguranca-*` mescladas | médio (diff grande) | não |
| 8 | Analisadores recomendados, código a código | 6 | médio | sim, gradual |
| 9 | Tags de deploy e revisão local antes do merge | 3 | baixo | não |
| 10 | Comandos do Claude Code + hook de BOM (`.ps1`) | 1, 2 | baixo | não |
| 11 | Base de testes de integração + teste global de autorização | 3 | médio | pode revelar endpoints abertos |
| 12 | (opcional) `.claude/rules/` com `paths:` | 2 | baixo | não |
| 13 | Nível pessoal (`~/.claude/CLAUDE.md`) e `docs/kit-base/` | 2, 10 | nenhum | não |
| 14 | Limpeza de arquivos soltos na raiz (aprovada) | 4 (rotação antes de apagar `comandos.txt`) | baixo | não |
| 15 | Códigos HTTP corretos e `ProblemDetails` (ADR 0010): cliente primeiro, depois controller a controller | 2, 11 | **alto** (contrato com a tela) | não, mas pode quebrar telas |
| 16 | `[Nome]ReadDto` (ADR 0009): `UserAccount` primeiro, depois os demais quando tocados | 2 | médio | não |
| 17 | Remover o suporte a PostgreSQL (ADR 0001) | 3 | baixo | pode, se algo ainda referenciar Npgsql |
| 18 | Containers sem root e imagens com versão fixa | 2 | médio (permissão de volume) | não |

> As etapas 15 e 16 mudam código de produção, não só regras. A parte urgente da 16 (`UserAccountResponseDto` sem `PasswordHash`) pode ir junto com as correções de segurança das branches `fix/seguranca-*`, sem esperar as etapas anteriores.

---

## Etapa 0: decisões

- **O que muda:** você responde às 15 perguntas do relatório (§7). Eu ajusto os rascunhos: removo os ❓ e troco pelas opções escolhidas.
- **Andamento (25/11):** perguntas 1 a 15 respondidas e aplicadas nos rascunhos, exceto a 11 (Redirect URI, você vai verificar). Surgiu a pergunta 16 (onde o Orquestrador roda em produção). As perguntas 6, 7 e 12 seguiram a minha recomendação; confirme ou corrija.
- **Risco:** nenhum.
- **Como validar:** `grep -r "❓" docs/revisao-regras/proposta` sem resultados.

## Etapa 1: `CLAUDE.md` e permissões

- **O que muda:**
  - cria `CLAUDE.md` na raiz (rascunho `proposta/repo/CLAUDE.md`, 128 linhas);
  - cria `.claude/settings.json` **só com `permissions`** (sem o bloco `hooks`, que entra na etapa 10).
- **Risco:** baixo. Uma permissão `deny` ampla demais pode bloquear um comando útil; dá para ajustar na hora (as mudanças em `permissions` valem sem reiniciar, segundo a documentação oficial).
- **Como validar:**
  1. Abrir uma sessão nova e rodar `/memory`: o `CLAUDE.md` da raiz aparece carregado.
  2. Rodar `/permissions`: as regras aparecem.
  3. Pedir à IA para ler `.env`: deve ser negado.
  4. Pedir `dotnet build`: roda sem pedir confirmação.

## Etapa 2: reorganizar a documentação

- **O que muda:**
  - cria `docs/padroes/` (11 arquivos do kit), `docs/projeto/` (4 novos + 3 movidos com `git mv`, aplicando `proposta/repo/docs/projeto/AJUSTES-ARQUIVOS-MOVIDOS.md`), `docs/adr/` (0000 a 0010) e `docs/kit-base/` (LEIA-ME, `CLAUDE.modelo.md`, `settings.base.json`);
  - troca o `README.md` pelo rascunho voltado a humanos;
  - remove `docs/rules/backend-architecture.md` e `docs/rules/encoding-standards.md` (conteúdo redistribuído);
  - remove `.cursorrules` e `.cursorignore` (o Cursor não é mais usado);
  - muda o padrão do `NormalizarEncodingPrecoce.ps1` para `*.ps1` e tira do README e do roteiro de desenvolvimento a instrução de rodá-lo antes do build;
  - corrige links em `docs/EXECUCAO_*.md`, `docs/PLANO_RPA.md`, `docs/DIAGRAMAS_ARQUITETURA.md` e `docs/CHECKLIST_ATIVO.md`;
  - remove as frases duplicadas listadas no ajuste (ex.: `down -v` no README e no roteiro de desenvolvimento).
- **Risco:** baixo. Links quebrados e alguma regra perdida na redistribuição.
- **Como validar:**
  1. `git grep -n "docs/rules/"` só encontra ocorrências em `docs/arquivo/` e no histórico.
  2. Conferência cruzada: cada linha do mapa de regras (`RELATORIO.md` §2.3) tem destino num único arquivo novo. Eu entrego essa tabela "regra antiga → arquivo novo" no relatório da etapa.
  3. `git grep -n "cursorrules"` sem resultados fora de `docs/arquivo/`.

## Etapa 3: CI mínimo

- **O que muda:**
  - cria `.github/workflows/ci.yml`, disparado em todo push, com restore, build e testes de `Ofertio.Tests` em Linux, sobre um `.slnf` (filtro de solução) sem o Mobile e o E2E. Os passos de formato e de pacotes vulneráveis entram com `continue-on-error: true`. O job de segredos fica comentado até a etapa 4;
  - o `Ofertio.Mobile` está ativo (pergunta 14), então ganha um job separado em `windows-latest` com o workload MAUI, que roda só quando há mudança em `Ofertio.Mobile/**`, `Ofertio.SharedUi/**` ou `Ofertio.Business/**`. Motivo: MAUI não compila em Linux, e esse job é lento (~10 min).
- **Risco:** baixo. É preciso conferir se algum teste depende de Windows (caminhos com `\`). Atenção: o Mobile usa .NET 9, fora de suporte desde 10/11/2026 (auditoria F7). Atualizar a plataforma não faz parte deste plano, mas o job vai acusar se uma imagem do runner deixar de ter o SDK.
- **Como validar:** um push de teste mostra o job `ci` verde; um commit com teste quebrado de propósito deixa o job vermelho; uma mudança em `Ofertio.Mobile` dispara o job Windows.

## Etapa 4: varredura de segredos

- **Pré-requisito:** rotação dos segredos versionados, conforme `docs/auditoria/ROTEIRO_BACKUP_E_SEGREDOS.md` (fora deste plano). Sem a rotação, limpar o arquivo não resolve: o valor continua no histórico.
- **O que muda:**
  - `.gitleaks.toml` na raiz;
  - placeholders vazios nas chaves secretas dos `appsettings*.json`;
  - job `segredos` ativo no CI, varrendo **só os commits novos do push**, com baseline do histórico atual (`gitleaks detect --report-path baseline.json` uma vez, e `--baseline-path` no CI);
  - gancho `pre-commit` local opcional (`gitleaks protect --staged`).
- **Risco:** médio. Falso positivo deixa o CI vermelho; ajusta-se o `allowlist`. Instalar o gitleaks na máquina exige a sua autorização.
- **Como validar:** um commit de teste com uma chave falsa em `appsettings.json` deixa o job vermelho; o commit sem ela fica verde.

## Etapa 5: `Directory.Build.props` e `.editorconfig` neutros

- **O que muda:**
  - `Directory.Build.props` com `Nullable`, `ImplicitUsings`, `LangVersion`, `NuGetAudit`, `TreatWarningsAsErrors=false`, **sem** `EnforceCodeStyleInBuild` e sem a linha `NU1903` em `WarningsAsErrors`;
  - `.editorconfig` com charset, indentação e nomes, todas as regras IDE/CA em `suggestion`, exceto as que já estão zeradas;
  - remove dos `.csproj` as propriedades que passaram a ser centralizadas (opcional; repetir não quebra).
- **Risco:** baixo. A severidade não muda, então o build fica igual.
- **Como validar:** `dotnet build` com o mesmo número de warnings (14) antes e depois. Abrir um `.cs` no Visual Studio e conferir que a formatação automática segue o `.editorconfig`.

## Etapa 6: zerar warnings e ligar `TreatWarningsAsErrors`

- **O que muda:**
  1. corrige CS8601/CS8604 em `IntegracaoLojaService.cs:733,795` e nos testes correspondentes (são arquivos alterados na branch atual);
  2. corrige CS1573 em `PainelController.cs:35`;
  3. atualiza `SixLabors.ImageSharp` para a versão sem as vulnerabilidades (pede a sua autorização para mexer em pacote);
  4. liga `TreatWarningsAsErrors=true` e a linha `NU1903;NU1904`.
- **Risco:** médio. A atualização do ImageSharp pode mudar a API usada em `IImageOptimizationService`; os testes de mídia cobrem parte disso. Uma vulnerabilidade nova publicada em qualquer pacote passa a quebrar o build: isso é intencional, mas pode acontecer num dia de deploy. A saída de emergência é `-p:TreatWarningsAsErrors=false` naquele build, registrada na mensagem do commit.
- **Como validar:** build com 0 warnings; testes verdes; `dotnet list package --vulnerable --include-transitive` sem High/Critical **diretos** (os transitivos da auditoria S24 entram na mesma etapa se a correção for só subir a versão).

## Etapa 7: formatação geral

- **Quando:** depois de mesclar as branches `fix/seguranca-*`, para não gerar conflito.
- **O que muda:**
  - um commit só com `dotnet format` da solução (`style: formatação geral pelo .editorconfig`), sem nenhuma outra mudança;
  - o hash desse commit vai para `.git-blame-ignore-revs`, para o `git blame` continuar útil;
  - o passo de formato no CI passa a bloquear (`continue-on-error` removido);
  - `EnforceCodeStyleInBuild=true`.
- **Risco:** médio. O diff é grande e conflita com branches abertas. Mitigação: nenhuma branch aberta no momento.
- **Como validar:** `dotnet format --verify-no-changes` limpo; build e testes verdes; a revisão do diff por amostragem não mostra mudança de lógica.

## Etapa 8: analisadores recomendados, código a código

- **O que muda:** medir com `dotnet build -p:AnalysisLevel=8.0-recommended -p:TreatWarningsAsErrors=false` e montar uma tabela "código → quantidade". Depois, em commits pequenos, um código ou projeto por vez: corrigir as ocorrências e subir aquele código para `warning` no `.editorconfig`. Prioridade: CA2016 (repassar `CancellationToken`), CA2254 (template de log), CA1849 e CA5394. Por último, `AnalysisLevel=8.0-recommended`.
- **Risco:** médio. Muitos warnings de uma vez desanimam e viram supressão em massa. Por isso vai um código por commit.
- **Como validar:** cada commit reduz a tabela e não acrescenta `#pragma warning disable` sem comentário.

## Etapa 9: tags de deploy e revisão antes do merge

Decisão: sem PR obrigatório (pergunta 5).

- **O que muda:**
  - tag `vAAAA.MM.DD` no commit de cada deploy, passo acrescentado em `docs/EXECUCAO_PRODUCAO.md`;
  - `docs/padroes/git.md` §3 passa a valer: `/revisar-pr` + Definição de Pronto antes do merge local na `main`, e o CI da `main` conferido depois do push.
- **Risco:** baixo. Sem proteção da `main`, nada impede um push com o CI vermelho. A garantia é o hábito de olhar o CI, mais o aviso por e-mail do GitHub Actions quando um job falha (ativo por padrão para quem fez o push).
- **Como validar:** o próximo deploy sai com tag; `git describe --tags` na VPS mostra a versão em produção.

## Etapa 10: comandos e hook do Claude Code

- **O que muda:**
  - `.claude/commands/` com os 6 comandos (pergunta 6: comandos, um arquivo cada);
  - `.claude/hooks/garantir-bom.ps1` (age só em `.ps1`);
  - bloco `hooks` no `.claude/settings.json`.
- **Risco:** baixo. Se o hook falhar, ele sai com 0 e não atrapalha a edição. O custo é uma chamada ao PowerShell por edição (~0,3–0,5 s), que encerra na hora quando o arquivo não é `.ps1`.
- **Como validar:**
  1. Pedir à IA para criar um `.ps1` de teste: o arquivo sai com BOM (`Format-Hex` mostra `EF BB BF`). Um `.md` sai sem alteração.
  2. Rodar `/revisar-pr` numa branch com uma mudança conhecida: o relatório aponta o item esperado.
  3. Rodar `/nova-migration Teste`: a IA para antes de `database update`.

## Etapa 11: base de testes de integração e autorização global

- **O que muda:**
  - `Ofertio.Tests/Integration/` com `WebApplicationFactory<Program>`, banco SQLite em memória (ou Testcontainers com SQL Server, se o tempo do CI permitir) e autenticação de teste;
  - um teste que lista todos os endpoints (`EndpointDataSource`), chama cada um sem token e falha se algum responder 2xx sem estar numa lista de anônimos aprovados (cada item com o motivo em comentário);
  - um modelo de teste de IDOR (usuário B acessando recurso de A) para os controllers com `{id}`.
- **Risco:** médio. Montar o host de teste pode exigir trocar serviços externos (RabbitMQ, workers) por dublês. O teste global pode falhar logo de início, revelando endpoints ainda abertos; nesse caso, eles entram na lista com um ❗ e um item no checklist, sem bloquear a etapa.
- **Como validar:** remover `[Authorize]` de um controller de teste faz o teste global falhar.

## Etapa 12 (opcional): regras por pasta para a IA

- **O que muda:** piloto com um único arquivo, `.claude/rules/frontend.md`, com `paths: ["Ofertio.SharedUi/**/*.razor", "Ofertio.WebUi/**/*.razor"]`, contendo só um link para `docs/projeto/frontend-ui.md` e as 5 regras de tela mais esquecidas.
- **Risco:** baixo. Se a regra duplicar o documento, vira manutenção dobrada. Por isso o arquivo fica só com link e um resumo mínimo.
- **Como validar:** numa sessão que edita um `.razor`, o `/memory` mostra a regra carregada; numa sessão que só mexe em `.cs`, ela não aparece. Se funcionar, estender para backend e integrações; se não agregar, apagar.

## Etapa 13: nível pessoal e kit base

- **O que muda:**
  - cria `~/.claude/CLAUDE.md` a partir de `proposta/usuario/CLAUDE.md`;
  - apaga da memória pessoal do Claude as duas entradas que duplicam regras versionadas (`controle-itens-hierarquia-menu`, `ambientes-espelham-producao`);
  - confere que `docs/kit-base/LEIA-ME.md` (criado na etapa 2) lista todos os arquivos do kit nos caminhos finais. O kit fica neste repositório (pergunta 15).
- **Aprovado (pergunta 15):** criar o `~/.claude/CLAUDE.md`. Fica para esta etapa, porque você pediu para não aplicar nada antes de aprovar a proposta.
- **Risco:** nenhum para o Ofertio.
- **Como validar:** numa pasta vazia sem `CLAUDE.md`, uma sessão nova responde em português e pergunta antes de comandos destrutivos.

## Etapa 14: limpeza da raiz

- **O que muda (aprovado, pergunta 10):**
  - remove `comandos.txt` (contém segredos; só depois da rotação da etapa 4, porque apagar não tira do histórico), `test_full_output.txt`, `test_full_output2.txt`, `test_prod_output.txt`, `test_results.txt`, `temp_replace_orquestrador.ps1`, `Architecture.puml`, `ProjectReference.txt` e `skills-lock.json`;
  - scripts de IIS (`AjustarPermissoesIIS.ps1`, `Configurar-Logs-IIS.ps1`, `Liberar-Logs-WebUi.ps1`, `LiberarPermissoesWebUi.ps1`): movidos para `docs/arquivo/scripts-iis/`, porque o roteiro IIS arquivado ainda os cita;
  - completa o `.gitignore` (auditoria S23: `secrets.json`, `.env.*` exceto `.env.example`, `*.pem`, `*.key`, `test_*.txt`).
- **Risco:** baixo. Antes de apagar, mostro a lista com `git grep` de quem cita cada arquivo.
- **Como validar:** `git ls-files` sem os arquivos; build e testes verdes.

## Etapa 15: códigos HTTP corretos e `ProblemDetails`

Decisão: pergunta 3, "códigos HTTP corretos em tudo" (ADR 0010).

- **O que muda, em cinco passos (um commit cada):**
  1. **Infraestrutura.** `ErrorCodes` em `Ofertio.Business/Errors/`; resultado tipado (`ServiceResult`/`Result<T>` com `ErrorCode`); extensão `ToActionResult()` na API que traduz código → HTTP + `ProblemDetails` com `errorCode`/`traceId`; `ExceptionHandlingMiddleware` passa a incluir `errorCode: "INTERNAL"`. Nenhum endpoint muda ainda.
  2. **Cliente.** O cliente HTTP da `SharedUi` passa a ler `ProblemDetails` (mensagem de `detail`) e a tratar 401 → login e 403 → "sem acesso", **aceitando também** o formato antigo (`Status=false` com 200). O `Ofertio.Mobile` está ativo (pergunta 14): o mesmo ajuste precisa estar **publicado nas lojas e adotado pelos usuários** antes do passo 3. Enquanto houver versão antiga do app em uso, a API mantém o formato antigo para ela (cabeçalho de versão do app) ou o passo 3 espera.
  3. **Permissões.** `RequiresAccessAttribute` e `RadarPlanGate` passam a responder 403 + `ACCESS_DENIED`.
  4. **Controllers, um módulo por commit** (Afiliados, Campanhas, Conta, Vendas, Integrações…): trocar `return Ok(new XResponseDto { Status = false, ... })` pelo resultado tipado, ajustar `[ProducesResponseType]` e os testes de controller para afirmar o código HTTP.
  5. Quando nenhum endpoint usar mais `Status=false`, remover do cliente o suporte ao formato antigo.
- **Risco:** alto. Tela que lia `Status=false` com HTTP 200 e não passou pelo novo cliente pode mostrar erro genérico ou ficar sem mensagem. Mitigação: o passo 2 vem antes de qualquer endpoint mudar; E2E das telas do módulo rodando em homologação a cada commit do passo 4.
- **Como validar:**
  - testes de controller afirmam `StatusCode` e `errorCode`;
  - um teste de arquitetura falha se algum action devolver `Ok(...)` com `Status = false` (busca no CI por `Status = false`, com lista de exceções que só diminui);
  - `Ofertio.E2E` verde em homologação;
  - com um plano sem a chave, a tela mostra "sem acesso" e **não** volta para o login.

## Etapa 16: `[Nome]ReadDto`

Decisão: pergunta 2, recomendação adotada (ADR 0009).

- **O que muda:**
  1. `UserAccountReadDto` (sem `PasswordHash` nem tokens) e `UserAccountResponseDto` passam a usar esse tipo. Pode ir junto das branches de segurança.
  2. Teste de arquitetura: reflete os tipos `*ResponseDto` do `Ofertio.Business` e falha se alguma propriedade for de tipo entidade, com uma lista de exceções inicial (as respostas atuais) que só pode diminuir.
  3. Demais entidades: o `ReadDto` é criado quando o controller for tocado por outra tarefa, ou em PRs dedicados por módulo, começando pelos que devolvem dados pessoais (Clientes, Pedidos, Produtos, Afiliados, segundo a auditoria P2).
- **Risco:** médio. A WebUi pode usar algum campo da entidade que não foi copiado para o `ReadDto`. Mitigação: o build da `SharedUi` acusa campo inexistente (o tipo é compartilhado), e o E2E pega o resto.
- **Como validar:** a lista de exceções do teste de arquitetura diminui a cada commit; resposta de `GET api/Usuarios/{id}` sem `passwordHash` (teste de integração da etapa 11).

## Etapa 17: remover o suporte a PostgreSQL

Decisão: pergunta 14 (ADR 0001).

- **O que muda:**
  - remove o ramo `UseNpgsql` e o `NpgsqlUtf8ClientEncodingInterceptor` de `Ofertio.Database/Data/RelationalDatabaseOptions.cs`, a constante `PostgreSqlMigrationsAssembly` e o valor `PostgreSql` do `Database:Provider`;
  - remove `Ofertio.Infrastructure/Migrations` (PostgreSQL) e os pacotes `Npgsql*` dos `.csproj`;
  - remove `scripts/seed-dashboard-demo.postgresql.sql`, `scripts/setup-postgresql.ps1` e `scripts/setup-postgresql.sql`, se nenhum roteiro vigente os citar;
  - limpa as menções nos documentos vigentes (o histórico fica como está).
- **Risco:** baixo. Algum teste ou ambiente ainda pode usar `Provider=PostgreSql`. Antes, confiro com `git grep -n "PostgreSql\|Npgsql"` e no `.env` da VPS (lido por você).
- **Como validar:** build e testes verdes; `git grep -in "npgsql"` sem resultados fora de `docs/arquivo/` e do histórico; a homologação sobe e lê o banco normalmente.

## Etapa 18: containers sem root e imagens com versão fixa

Decisão: pergunta 12, recomendação adotada.

- **O que muda:**
  - no `docker-compose.yml`, imagens de terceiros com versão fixa: SQL Server com CU, RabbitMQ `3.x-management`, Caddy `2.x`. Antes, anoto as versões que rodam hoje na VPS (`docker image ls`) para fixar exatamente o que já está em produção, sem atualizar nada junto;
  - nos `Dockerfile` da API e da WebUi, `USER $APP_UID` no estágio final;
  - roteiro de migração dos volumes `uploads` e `api_appdata` para o dono `1654` (o mesmo `chown` já documentado para o SQL Server em `ambientes-docker.md`), aplicado primeiro na homologação.
- **Risco:** médio. Se o volume continuar com dono `root`, a API não grava uploads nem chaves do Data Protection e cai na subida. Mitigação: homologação primeiro; o roteiro de produção traz o `chown` antes do `up`; backup do volume `api_appdata` antes.
- **Como validar:** `docker exec ofertio-api id` mostra `uid=1654`; um upload de avatar funciona; um token de marketplace gravado antes da troca continua legível; `docker compose config` sem `:latest`.

---

## Como medir se deu certo (após a etapa 11)

| Indicador | Hoje | Meta |
|---|---|---|
| Regras garantidas por automação | 1 (BOM, por script manual) | ≥ 12 (CI, gitleaks, analisadores, permissões, hook, teste global de autorização) |
| Warnings no build | 14 | 0, com `TreatWarningsAsErrors` |
| Regras duplicadas | 15 grupos marcados **D** (`RELATORIO.md` §2.3) | 0 |
| Linhas do `CLAUDE.md` | inexistente | ≤ 200 |
| Endpoints sem teste de autorização | 47 de 58 controllers sem teste | 0 endpoints fora do teste global |
| Erros devolvidos com HTTP 200 | todos os de negócio e `ACCESS_DENIED` | 0 (etapa 15) |
| `ResponseDto` com entidade em `Data` | regra atual manda | 0 (etapa 16) |
