# Ofertio

Plataforma SaaS para afiliados de marketplaces: converte links com o código de afiliado do cliente, busca ofertas pelas APIs oficiais (Mercado Livre, Shopee, TikTok Shop, Amazon e outros), agenda envios para grupos de WhatsApp e Telegram, publica páginas públicas de ofertas e mede os cliques. Cada cliente (inquilino) assina um plano, e o Painel Master controla planos, permissões por tela e integrações globais.

## Stack

| Parte | Tecnologia |
|---|---|
| Backend | .NET 8: `Ofertio.API` (Web API + workers), `Ofertio.Worker` |
| Frontend | Blazor Web App (Interactive Server) `Ofertio.WebUi` + componentes `Ofertio.SharedUi` (Bootstrap 5, Bootstrap Icons) |
| Mobile | .NET MAUI Blazor Hybrid `Ofertio.Mobile` |
| Banco | SQL Server 2022 (banco `Ofertio`), EF Core 8, migrations em `Ofertio.Data` |
| Mensageria | RabbitMQ (MassTransit) |
| Hospedagem | Docker Compose: Caddy + API + WebUi + SQL Server + RabbitMQ, numa VPS |

Visão geral em diagramas: [`docs/DIAGRAMAS_ARQUITETURA.md`](./docs/DIAGRAMAS_ARQUITETURA.md).

## Como rodar

Pré-requisitos: Windows com Docker Desktop, .NET 8 SDK, Visual Studio 2022 (opcional) e um backup `.bak` do banco.

| Quero | Roteiro |
|---|---|
| Desenvolver (infra em Docker, API e WebUi no Visual Studio) | [`docs/EXECUCAO_DESENVOLVIMENTO.md`](./docs/EXECUCAO_DESENVOLVIMENTO.md) |
| Subir a homologação completa em Docker (`dev.ofertio.com.br`) | [`docs/EXECUCAO_HOMOLOGACAO.md`](./docs/EXECUCAO_HOMOLOGACAO.md) |
| Publicar em produção | [`docs/EXECUCAO_PRODUCAO.md`](./docs/EXECUCAO_PRODUCAO.md) e [`docs/CONFIGURACAO_PRODUCAO_DOCKER.md`](./docs/CONFIGURACAO_PRODUCAO_DOCKER.md) |
| Gerar o app mobile | [`docs/Ofertio_MOBILE_BUILD.md`](./docs/Ofertio_MOBILE_BUILD.md) |

Resumo do desenvolvimento:

```powershell
copy .env.example .env          # preencha os valores (nunca versione o .env)
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d
dotnet build .\Ofertio.sln
dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj
```

> Nunca rode `docker compose down -v`: apaga o banco, as chaves de criptografia, os uploads e a sessão do WhatsApp.

Portas locais: SQL Server `127.0.0.1,1433` · RabbitMQ `5672` (painel `15672`) · Evolution `8080` · API `7001` · WebUi `7002` (ou `7012` com a homologação no ar; ver roteiro).

## Como contribuir

1. Crie uma branch a partir da `main`: `feat/...`, `fix/...`, `docs/...`.
2. Leia as regras da área em que vai mexer: [`docs/padroes/`](./docs/padroes/) (gerais) e [`docs/projeto/`](./docs/projeto/) (deste sistema).
3. Escreva o teste junto com o código. Tela nova também ganha teste em `Ofertio.E2E`.
4. Commits em [Conventional Commits](https://www.conventionalcommits.org/pt-br/), em português: `fix(busca): ...`.
5. Antes de levar para a `main`, revise o diff (`/revisar-pr` no Claude Code) e faça o merge local; o CI roda em todo push e precisa ficar verde ([fluxo Git](./docs/padroes/git.md)).
6. A tarefa só está pronta quando cumpre a [Definição de Pronto](./docs/padroes/definicao-de-pronto.md).

## Documentação

| Documento | Para quê |
|---|---|
| [`docs/CHECKLIST_ATIVO.md`](./docs/CHECKLIST_ATIVO.md) | pendências |
| [`docs/HISTORICO_FASES_CONCLUIDAS.md`](./docs/HISTORICO_FASES_CONCLUIDAS.md) | detalhe das entregas já feitas |
| [`docs/padroes/`](./docs/padroes/) | padrões gerais (código, segurança, testes, Git...) |
| [`docs/projeto/`](./docs/projeto/) | regras do Ofertio (arquitetura, UI, integrações, ambientes) |
| [`docs/adr/`](./docs/adr/) | decisões de arquitetura |
| [`docs/PLANO_RPA.md`](./docs/PLANO_RPA.md) | testes pela tela (Playwright) |
| [`docs/arquivo/`](./docs/arquivo/) | histórico congelado; não é regra |

Assistentes de IA: as instruções ficam no [`CLAUDE.md`](./CLAUDE.md).
