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
- **UI:** todo botão tem ícone `bi bi-*`; combos seguem o padrão global (busca só com mais de 6 itens). ➔ [frontend-ui](./docs/rules/frontend-ui.md)
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
