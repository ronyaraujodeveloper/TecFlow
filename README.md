# 📝 Ofertio - Painel do Projeto

> Ponto de entrada do projeto (antigo `TODO.md`). As regras detalhadas ficam em [`docs/rules/`](./docs/rules/): abra só o arquivo da camada em que estiver trabalhando. Pendências em [`docs/CHECKLIST_ATIVO.md`](./docs/CHECKLIST_ATIVO.md).

## 🎯 Objetivo do Projeto
Plataforma de **automação e inteligência para afiliados de alta escala**: orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com marketplaces (Mercado Livre, Shopee, Amazon, TikTok Shop e outros). O backend em C# (`Ofertio.API`, `Ofertio.Worker`, `Ofertio.Orquestrador`) é operado pelo frontend **`Ofertio.WebUi`** para controle, auditoria e monitoramento em produção.

## 🛠️ Stack Tecnológica
- **Backend:** .NET 8.0 Web API (C#), Worker e Orquestrador.
- **Frontend:** Blazor WebApp `Ofertio.WebUi` com componentes compartilhados em `Ofertio.SharedUi` (Bootstrap 5 + Bootstrap Icons).
- **Banco de Dados:** SQL Server 2022 em container (`ofertio-db`, banco `Ofertio`) via `Database:Provider=SqlServer` nos três ambientes. Local: `127.0.0.1,1433` (SSMS com login SQL).
- **ORM:** Entity Framework Core 8 (`UseSqlServer`, migrations em `Ofertio.Data`); `Ofertio.Infrastructure` guarda só as migrations legadas de PostgreSQL.
- **Hospedagem:** Docker (Caddy + Kestrel + SQL Server + RabbitMQ) na produção (VPS) e na homologação local, com o mesmo `docker-compose.yml`. ➔ [ambientes-docker](./docs/rules/ambientes-docker.md)

---

## 📚 Regras por Camada (consulte conforme a tarefa)

| Arquivo | Quando abrir |
| --- | --- |
| 🏗️ [Arquitetura Backend & Banco](./docs/rules/backend-architecture.md) | C#/.NET 8, camadas, Padrão 3 Objetos, SQL Server, Data Protection, LGPD e testes. |
| 🎨 [Interface & UI/UX](./docs/rules/frontend-ui.md) | Telas Blazor, ícones `bi bi-*`, combos, modais, Mobile First e badges. |
| 🔑 [Marketplaces & OAuth](./docs/rules/marketplaces-oauth.md) | OAuth do Mercado Livre e do TikTok Shop (App de Criador), token Master, busca oficial, sanitização de links e novas plataformas. |
| 🔤 [Encoding & Nomenclatura](./docs/rules/encoding-standards.md) | UTF-8 com BOM, cultura pt-BR, idioma, nomes e identificadores legados. |
| 🐳 [Ambientes Docker](./docs/rules/ambientes-docker.md) | Compose, Dockerfile, `.env`, roteiros `EXECUCAO_*`: dev e homologação espelham a produção. |

### ⚡ Regras de ouro (resumo)
- **Encoding:** todo arquivo criado ou alterado vai em UTF-8 com BOM. ➔ [encoding-standards](./docs/rules/encoding-standards.md)
- **Idioma:** código em inglês; comentários e documentação em português. ➔ [encoding-standards](./docs/rules/encoding-standards.md)
- **Dados:** cada entidade tem só `Filter`, `Dto` e `ResponseDto`; listagens paginadas em no máximo 30. ➔ [backend-architecture](./docs/rules/backend-architecture.md)
- **Segredos:** senhas e tokens são criptografados no banco e nunca vão para o repositório nem para os logs. ➔ [backend-architecture](./docs/rules/backend-architecture.md)
- **UI:** todo botão tem ícone `bi bi-*`; combos seguem o padrão global (busca só com mais de 6 itens); campo preenchível tem `id` estável (`{tela}-{campo}`) e `<label for>` igual. ➔ [frontend-ui](./docs/rules/frontend-ui.md)
- **Testes e RPA:** comportamento novo ou corrigido ganha teste em `Ofertio.Tests` e, se a pessoa vê ou clica, em `Ofertio.E2E`, no mesmo passo. O robô só encontra o controle pelo `id` estável `{tela}-{campo}` ou pelo nome acessível. ➔ [frontend-ui §10](./docs/rules/frontend-ui.md#10-ids-estáveis-para-testes-pela-tela)
- **Marketplaces:** só API oficial com Bearer token; sem raspagem nem contorno de anti-bot ou captcha. ➔ [marketplaces-oauth](./docs/rules/marketplaces-oauth.md)
- **Preservação:** não remova validações nem regras existentes ao refatorar. ➔ [backend-architecture](./docs/rules/backend-architecture.md)
- **Ambientes:** dev e homologação seguem a produção; diferença só em override e documentada. ➔ [ambientes-docker](./docs/rules/ambientes-docker.md)

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

# Migrations (SQL Server)
dotnet ef migrations add <Nome> --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
```

## 🐳 Ambientes (Docker)

Os três ambientes usam o mesmo [`docker-compose.yml`](./docker-compose.yml) (o da produção). Dev e homologação acrescentam overrides com as diferenças documentadas ([regra](./docs/rules/ambientes-docker.md)).

| Ambiente | Roteiro | Onde roda | Compose |
|---|---|---|---|
| Desenvolvimento | [`EXECUCAO_DESENVOLVIMENTO.md`](./docs/EXECUCAO_DESENVOLVIMENTO.md) | infra em Docker + API/WebUi no Visual Studio | `-f docker-compose.yml -f docker-compose.local.yml` |
| Homologação | [`EXECUCAO_HOMOLOGACAO.md`](./docs/EXECUCAO_HOMOLOGACAO.md) | tudo em Docker nesta máquina (`dev.ofertio.com.br`) | `+ -f docker-compose.hml.yml --profile app` |
| Produção | [`EXECUCAO_PRODUCAO.md`](./docs/EXECUCAO_PRODUCAO.md) ([configuração](./docs/CONFIGURACAO_PRODUCAO_DOCKER.md)) | VPS | `-f docker-compose.yml` |

Portas locais: SQL Server `127.0.0.1,1433` · RabbitMQ `5672` / painel `15672` · Evolution (WhatsApp) `8080` · API (VS) `7001` · WebUi (VS) `7002` · homologação via Caddy `80` e `7002` (túnel Cloudflare).

- Segredos no `.env` (modelo `.env.example`) e, no Visual Studio, em user-secrets.
- Nunca rode `docker compose down -v`: apaga banco, chaves, uploads e sessão do WhatsApp.
- Logs: `docker logs ofertio-api --tail 80` (idem `ofertio-webui`, `ofertio-evolution`).
- O roteiro antigo de IIS está em [`docs/arquivo/EXECUCAO_HOMOLOGACAO_IIS.md`](./docs/arquivo/EXECUCAO_HOMOLOGACAO_IIS.md).

---

## 🗂️ Documentação de Apoio
- ✅ [Checklist Ativo (pendências)](./docs/CHECKLIST_ATIVO.md)
- 🤖 [Plano RPA — testes pela tela](./docs/PLANO_RPA.md)
- 📜 [Histórico de Fases Concluídas](./docs/HISTORICO_FASES_CONCLUIDAS.md): detalhes de cada entrega já feita.
- 🧭 [Diagramas de Arquitetura](./docs/DIAGRAMAS_ARQUITETURA.md)
- 📦 [Arquivo histórico](./docs/arquivo/): exploração de mai/2026, lista de mudanças e registro de versões (congelados; não são regra).
- 📱 [Build Mobile](./docs/Ofertio_MOBILE_BUILD.md)

### 🔁 Fluxo de documentação
1. Nova tarefa entra como `[ ]` no [Checklist Ativo](./docs/CHECKLIST_ATIVO.md).
2. Ao concluir, o detalhamento vai para o [Histórico](./docs/HISTORICO_FASES_CONCLUIDAS.md) e o item sai do checklist.
3. Arquivos criados, movidos ou excluídos ficam registrados no próprio commit (Conventional Commits). Se mudarem o fluxo, atualize os [Diagramas](./docs/DIAGRAMAS_ARQUITETURA.md) (ver `.cursorrules`).
4. Regra nova ou alterada vai para o arquivo correspondente em `docs/rules/`, não para este README.

---

*Nota para a IA: leia só o arquivo de `docs/rules/` da camada em que estiver trabalhando e o [Checklist Ativo](./docs/CHECKLIST_ATIVO.md). O histórico é arquivo de consulta; abra apenas quando precisar do contexto de uma entrega antiga. Não leia `docs/arquivo/`.*
