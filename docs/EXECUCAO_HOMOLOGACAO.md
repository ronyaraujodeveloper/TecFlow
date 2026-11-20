# Roteiro de homologação (Docker)

> Instruções para o agente. Execute as fases em sequência quando o usuário pedir para finalizar uma etapa, publicar a homologação ou rodar este arquivo. *"Rode completo de ponta a ponta sem interrupções."*

A homologação (`https://dev.ofertio.com.br`) roda **inteira em Docker nesta máquina**, com o mesmo compose da produção mais dois overrides. O roteiro antigo, de IIS + SQLEXPRESS, está arquivado em [`arquivo/EXECUCAO_HOMOLOGACAO_IIS.md`](./arquivo/EXECUCAO_HOMOLOGACAO_IIS.md). O IIS do Windows 10 Home aceitava só 3 requisições simultâneas, e cada aba do Blazor segura uma conexão, por isso a terceira aba travava.

Regra obrigatória: [`rules/ambientes-docker.md`](./rules/ambientes-docker.md). Roteiros irmãos: [`EXECUCAO_DESENVOLVIMENTO.md`](./EXECUCAO_DESENVOLVIMENTO.md) e [`EXECUCAO_PRODUCAO.md`](./EXECUCAO_PRODUCAO.md).

Atalho para as fases 1 a 4: `.\Publicar-Homologacao-Docker.ps1` (ou `-SemTestes`).

> **Conceito Docker: imagem x container.** A *imagem* é o pacote gerado pelo `docker compose build` (SO mínimo + .NET + a aplicação publicada). O *container* é a imagem em execução. Publicar = gerar imagens novas e recriar os containers; os volumes (banco, uploads, chaves) continuam.

## Fase 0 — Pré-requisitos (uma vez)

Os mesmos da fase 0 do [`EXECUCAO_DESENVOLVIMENTO.md`](./EXECUCAO_DESENVOLVIMENTO.md): Docker Desktop, `.wslconfig`, `.env`, user-secrets da API e banco restaurado. Além disso:

- Os sites e pools do IIS (`Ofertio.API`, `Ofertio.WebUi`, `Default Web Site`) ficam **parados**, porque o Caddy usa as portas 80 e 7002:

```powershell
Import-Module WebAdministration
Stop-Website -Name "Ofertio.API"; Stop-Website -Name "Ofertio.WebUi"; Stop-Website -Name "Default Web Site"
Stop-WebAppPool -Name "OfertioApiPool"; Stop-WebAppPool -Name "OfertioWebUiPool"
```

- **Acesso público pelo túnel do Cloudflare:** `dev.ofertio.com.br` → Cloudflare → `cloudflared` → `https://127.0.0.1:7002` (config em `%USERPROFILE%\.cloudflared\config.yml`, túnel `9d81a4d1…`, `noTLSVerify`). O Caddy atende a 7002 (`HML_HTTPS_PORT=7002` no `.env`) com certificado interno; o certificado público é do Cloudflare. O túnel precisa estar rodando:

```powershell
& "C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --config "$env:USERPROFILE\.cloudflared\config.yml" run
```

> ⚠️ O serviço `Cloudflared` do Windows está instalado, mas o `config.yml` dele (`C:\Program Files (x86)\cloudflared\config.yml`) só tem `logDirectory`, sem túnel. Enquanto isso não for corrigido, o túnel só funciona com o comando acima aberto, e o domínio responde **530** quando ele não está rodando.

## Fase 1 — Encoding, build e testes

```powershell
$WarningPreference = 'SilentlyContinue'; .\NormalizarEncodingPrecoce.ps1 *> $null
dotnet build -c Release
dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj -c Release --no-build
```

## Fase 2 — Build das imagens e subida

```powershell
$c = '-f','docker-compose.yml','-f','docker-compose.local.yml','-f','docker-compose.hml.yml','--profile','app'
docker compose @c build api webui
docker compose @c up -d
```

O build usa `DOTNET_NUGET_SIGNATURE_VERIFICATION=false` e o `nuget.config` da raiz, igual à VPS (Refit `NU3012`).

## Fase 3 — Migrations incrementais no banco `Ofertio`

```powershell
$sql = (Get-Content .env | Where-Object { $_ -like 'SQL_CONNECTION=*' }) -replace '^SQL_CONNECTION=', ''
$env:ConnectionStrings__DefaultConnection = $sql -replace 'Server=sqlserver,1433', 'Server=127.0.0.1,1433'   # o dotnet ef não lê user-secrets
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
docker compose @c restart api
```

O banco é o mesmo do desenvolvimento. Não rode `dotnet ef database update` em base vazia e não recrie o volume `sqlserver_data` se o comando falhar.

## Fase 4 — Verificação (smoke test)

```powershell
docker compose @c ps --format "table {{.Name}}\t{{.Status}}"
(Invoke-WebRequest http://localhost/ -UseBasicParsing).StatusCode
docker stats --no-stream
```

O esperado:

- Os 7 containers em `Up`, sem reinício em loop: `ofertio-db`, `ofertio-rabbitmq`, `ofertio-api`, `ofertio-webui`, `ofertio-caddy`, `ofertio-evolution` e `ofertio-evolution-db`.
- `http://localhost/` e `https://dev.ofertio.com.br/` respondem `200`.
- **5 ou mais abas** (cliente, master, visitante) navegam ao mesmo tempo.
- **WhatsApp:** `.\Alternar-WebhookWhatsApp.ps1 -Ambiente hml` já aplicado; `/integracoes/conexoes` mostra a instância conectada; uma mensagem com link é convertida e respondida.
- **Telegram:** o UserBot conecta com a sessão do volume, e o webhook do bot fica registrado em `https://dev.ofertio.com.br`.
- `docker logs ofertio-api --tail 50` sem erro de banco, RabbitMQ ou Data Protection.
- `docker stats`: total perto de 2,5 a 3,5 GB.

Se um container reiniciar, leia `docker logs <nome> --tail 80` antes de mudar qualquer coisa.

## Fase 5 — Governança de código e Git

1. Analise o `git status` / `git diff` do ciclo.
2. Faça o commit no padrão Conventional Commits (ex.: `feat(links): adiciona suporte a unshorten do promoby.me`). Não inclua `.env`, `.bak`, `*.pfx` nem `App_Data`.
3. Envie:

```powershell
git push origin main
```

## Diferenças em relação à produção

| Item | Produção | Homologação | Motivo |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Homologacao` | carrega `appsettings.Homologacao.json` (sandbox, URLs de teste) |
| Domínio do Caddy | `ofertio.com.br` | `dev.ofertio.com.br` + `:80` local | outro DNS; `:80` para `http://localhost` |
| Certificado | Let's Encrypt emitido pelo Caddy | `tls internal`; o público é do Cloudflare | o tráfego chega pelo túnel `cloudflared`, e o Let's Encrypt não alcança a máquina |
| Portas do Caddy no host | `80/443` | `80` e `7002` (`HML_HTTP_PORT` / `HML_HTTPS_PORT`) | o túnel aponta para `https://127.0.0.1:7002` |
| Cabeçalhos de proxy | Caddy é a borda | `trusted_proxies private_ranges` no Caddy | preserva o `X-Forwarded-Proto=https` do Cloudflare |
| Portas do banco e RabbitMQ | só rede interna | também em `127.0.0.1` | SSMS e API do Visual Studio |
| Memória do SQL Server | sem teto (Express nos dois) | `MSSQL_MEMORY_LIMIT_MB=1536` | máquina de 16 GB |
| Limites de memória | sem limite | `mem_limit` por serviço + `DOTNET_gcServer=0` | Docker limitado a 5 GB |
| Evolution API (WhatsApp) | não documentada na VPS | `ofertio-evolution` v2.3.7 + Postgres 15 | integração WhatsApp |
| URLs públicas da API | appsettings de produção | `Telegram__WebhookBaseUrl`, `Ofertio__ShortLinks__PublicBaseUrl`, `Email__PublicBaseUrl` = `https://dev.ofertio.com.br` | domínio de homologação |
| Banco | `Ofertio` só da produção | `Ofertio` compartilhado com o desenvolvimento | um SQL Server local só |

## O que não fazer

- Não rodar `docker compose down -v` nem apagar os volumes `ofertio_sqlserver_data`, `ofertio_api_appdata`, `ofertio_uploads` e `ofertio_evolution_pg`.
- Não editar `docker-compose.yml` para resolver algo da homologação. Use `docker-compose.hml.yml` e registre a diferença acima.
- Não religar o IIS com o Docker no ar: os dois disputam as portas.
- Não usar este roteiro para a produção. Produção é [`EXECUCAO_PRODUCAO.md`](./EXECUCAO_PRODUCAO.md), na VPS.
