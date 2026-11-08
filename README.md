# 📝 Ofertio - Painel do Projeto

> Ponto de entrada do projeto (antigo `TODO.md`). As regras detalhadas ficam em [`docs/rules/`](./docs/rules/): abra só o arquivo da camada em que estiver trabalhando. Pendências em [`docs/CHECKLIST_ATIVO.md`](./docs/CHECKLIST_ATIVO.md).

## 🎯 Objetivo do Projeto
Plataforma de **automação e inteligência para afiliados de alta escala**: orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com marketplaces (Mercado Livre, Shopee, Amazon, TikTok Shop e outros). O backend em C# (`Ofertio.API`, `Ofertio.Worker`, `Ofertio.Orquestrador`) é operado pelo frontend **`Ofertio.WebUi`** para controle, auditoria e monitoramento em produção.

## 🛠️ Stack Tecnológica
- **Backend:** .NET 8.0 Web API (C#), Worker e Orquestrador.
- **Frontend:** Blazor WebApp `Ofertio.WebUi` com componentes compartilhados em `Ofertio.SharedUi` (Bootstrap 5 + Bootstrap Icons).
- **Banco de Dados:** SQL Server (`localhost\SQLEXPRESS` / `Ofertio`) via `Database:Provider=SqlServer` no Development e na Homologação IIS.
- **ORM:** Entity Framework Core 8 (`UseSqlServer`, migrations em `Ofertio.Data`); `Ofertio.Infrastructure` guarda só as migrations legadas de PostgreSQL.
- **Hospedagem:** IIS (`OfertioApiPool` e `OfertioWebUiPool`, em `C:\inetpub\ofertio`).

---

## 📚 Regras por Camada (consulte conforme a tarefa)

| Arquivo | Quando abrir |
| --- | --- |
| 🏗️ [Arquitetura Backend & Banco](./docs/rules/backend-architecture.md) | C#/.NET 8, camadas, Padrão 3 Objetos, SQL Server, Data Protection, LGPD e testes. |
| 🎨 [Interface & UI/UX](./docs/rules/frontend-ui.md) | Telas Blazor, ícones `bi bi-*`, combos, modais, Mobile First e badges. |
| 🔑 [Marketplaces & OAuth](./docs/rules/marketplaces-oauth.md) | OAuth do Mercado Livre e do TikTok Shop (App de Criador), token Master, busca oficial, sanitização de links e novas plataformas. |
| 🔤 [Encoding & Nomenclatura](./docs/rules/encoding-standards.md) | UTF-8 com BOM, cultura pt-BR, idioma, nomes e identificadores legados. |

### ⚡ Regras de ouro (resumo)
- **Encoding:** todo arquivo criado ou alterado vai em UTF-8 com BOM. ➔ [encoding-standards](./docs/rules/encoding-standards.md)
- **Idioma:** código em inglês; comentários e documentação em português. ➔ [encoding-standards](./docs/rules/encoding-standards.md)
- **Dados:** cada entidade tem só `Filter`, `Dto` e `ResponseDto`; listagens paginadas em no máximo 30. ➔ [backend-architecture](./docs/rules/backend-architecture.md)
- **Segredos:** senhas e tokens são criptografados no banco e nunca vão para o repositório nem para os logs. ➔ [backend-architecture](./docs/rules/backend-architecture.md)
- **UI:** todo botão tem ícone `bi bi-*`; combos seguem o padrão global (busca só com mais de 6 itens); campo preenchível tem `id` estável (`{tela}-{campo}`) e `<label for>` igual. ➔ [frontend-ui](./docs/rules/frontend-ui.md)
- **Testes e RPA:** comportamento novo ou corrigido ganha teste em `Ofertio.Tests` no mesmo passo. O que a pessoa vê ou clica entra em `Ofertio.E2E` nesse mesmo passo. O robô só encontra o controle pelo `id` estável ou pelo nome acessível. ➔ [frontend-ui](./docs/rules/frontend-ui.md#10-ids-estáveis-para-testes-pela-tela) e [PLANO_RPA](./docs/PLANO_RPA.md)
  - Campo preenchível (`input`, `textarea`, `select`, checkbox): `id` `{tela}-{campo}` e `<label for>` igual. O teste preenche esse `id`, recusa o valor inválido e só grava o caminho válido com prefixo `E2E-`.
  - Botão que muda o fluxo, inclusive em aviso e modal: `id` próprio quando o texto se repete ou quando o clique libera a tela (`cookie-accept`, `terms-reaccept-confirm`, `terms-api-consent-confirm`). Texto único na tela pode ficar no nome acessível. O teste clica e confere o que aparece.
  - Tela nova no menu do cliente: visita em `MenuSmokeTests`, conferindo o `h1`. Tela nova no menu Master: a mesma visita, quando a sessão permanece em `/master`.
  - Aviso, modal e checkbox que travam o próximo passo entram no robô. Cookies (`#cookie-accept`), reaceite (`#terms-reaccept`) e consentimento de API (`#terms-api-consent`) são clicados. O confirmar fica desabilitado até a caixa ser marcada. O login dispensa o aviso e o reaceite quando eles aparecem, para o restante da suíte seguir.
  - Publicação que vale para todo mundo (termos) grava um parágrafo `E2E-`, confere a mensagem, aceita o reaceite, aceita o consentimento ao clicar em "Conectar nova loja", fecha o formulário da loja e devolve o texto anterior. Sem a permissão de publicar, o robô só confere o título.
  - Ficam de fora do robô: OAuth, sessão real de WhatsApp/Telegram, QR, webhook e cobrança ASAAS.
- **Marketplaces:** só API oficial com Bearer token; sem raspagem nem contorno de anti-bot ou captcha. ➔ [marketplaces-oauth](./docs/rules/marketplaces-oauth.md)
- **Preservação:** não remova validações nem regras existentes ao refatorar. ➔ [backend-architecture](./docs/rules/backend-architecture.md)

---

## 💻 Comandos Principais

Execute na raiz (PowerShell 5.1: encadeie com `;`, não com `&&`).

```powershell
# Normalizar encoding (UTF-8 com BOM) antes de compilar
$WarningPreference='SilentlyContinue'; .\NormalizarEncodingPrecoce.ps1 *> $null

# Build e testes
dotnet build .\Ofertio.sln -c Release
dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj
dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj --filter "FullyQualifiedName~Radar"

# Robô de tela (projeto separado; a senha fica nos User Secrets, nunca no git)
dotnet user-secrets set "E2E:BaseUrl" "https://dev.ofertio.com.br" --project .\Ofertio.E2E\Ofertio.E2E.csproj
dotnet user-secrets set "E2E:Email" "e2e@ofertio.local" --project .\Ofertio.E2E\Ofertio.E2E.csproj
dotnet user-secrets set "E2E:Password" "<senha>" --project .\Ofertio.E2E\Ofertio.E2E.csproj
dotnet build .\Ofertio.E2E\Ofertio.E2E.csproj
powershell -File .\Ofertio.E2E\bin\Debug\net8.0\playwright.ps1 install
dotnet test .\Ofertio.E2E\Ofertio.E2E.csproj

# Carga de homologação (não rode na produção). Preencha o e-mail do dono no script:
# scripts/carga-homolog-rpa.sql

Execute a FASE 2: Publicação e Deploy Limpo no IIS do arquivo ./docs/EXECUCAO_HOMOLOGACAO.md

# Migrations (SQL Server)
dotnet ef migrations add <Nome> --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
```

## 🚀 Homologação (IIS)

O roteiro completo está em [`docs/EXECUCAO_HOMOLOGACAO.md`](./docs/EXECUCAO_HOMOLOGACAO.md), executado em sequência no PowerShell como Administrador. Ele normaliza o encoding, aplica as migrations, publica API e WebUi em `C:\inetpub\ofertio`, reinicia o IIS, roda o smoke test e registra o commit em [`docs/GIT_VERSAO.md`](./docs/GIT_VERSAO.md).

## 🐳 Produção (Docker)

A VPS usa Docker, não o IIS. A configuração está em [`docs/CONFIGURACAO_PRODUCAO_DOCKER.md`](./docs/CONFIGURACAO_PRODUCAO_DOCKER.md). O roteiro de publicação está em [`docs/EXECUCAO_PRODUCAO.md`](./docs/EXECUCAO_PRODUCAO.md).

- Preserve `C:\inetpub\ofertio\api\App_Data` no publish (chaves do Data Protection e sessão do Telegram).
- Não recrie a sub-aplicação `/api` dentro do site `Ofertio.WebUi` (causa HTTP 500.35).
- Logs da API: `C:\inetpub\ofertio\api\logs\stdout_*.log`.

---

## 🗂️ Documentação de Apoio
- ✅ [Checklist Ativo (pendências)](./docs/CHECKLIST_ATIVO.md)
- 🤖 [Plano RPA — testes pela tela](./docs/PLANO_RPA.md)
- 📜 [Histórico de Fases Concluídas](./docs/HISTORICO_FASES_CONCLUIDAS.md): detalhes de cada entrega já feita.
- 📁 [Lista de Mudanças de Arquivos](./docs/LISTA_ARQUIVOS_MUDANCAS.md)
- 🧭 [Diagramas de Arquitetura](./docs/DIAGRAMAS_ARQUITETURA.md)
- 🏷️ [Versões e Commits](./docs/GIT_VERSAO.md)
- 📱 [Build Mobile](./docs/Ofertio_MOBILE_BUILD.md)

### 🔁 Fluxo de documentação
1. Nova tarefa entra como `[ ]` no [Checklist Ativo](./docs/CHECKLIST_ATIVO.md).
2. Ao concluir, o detalhamento vai para o [Histórico](./docs/HISTORICO_FASES_CONCLUIDAS.md) e o item sai do checklist.
3. Arquivos criados, movidos ou excluídos atualizam a [Lista de Mudanças](./docs/LISTA_ARQUIVOS_MUDANCAS.md) e, se mudarem o fluxo, os [Diagramas](./docs/DIAGRAMAS_ARQUITETURA.md) (ver `.cursorrules`).
4. Regra nova ou alterada vai para o arquivo correspondente em `docs/rules/`, não para este README.

---

*Nota para a IA: leia só o arquivo de `docs/rules/` da camada em que estiver trabalhando e o [Checklist Ativo](./docs/CHECKLIST_ATIVO.md). O histórico é arquivo de consulta; abra apenas quando precisar do contexto de uma entrega antiga.*
