# Roteiro de desenvolvimento (Docker + Visual Studio)

> Instruções para o agente. Execute as fases em sequência quando o usuário pedir para preparar, subir ou validar o ambiente de desenvolvimento.

No desenvolvimento, a **infraestrutura** roda em Docker, igual à produção: banco, RabbitMQ e WhatsApp. A **API e a WebUi** rodam no Visual Studio (ou `dotnet run`) para ter debug, hot reload e breakpoints.

Regra obrigatória: [`rules/ambientes-docker.md`](./rules/ambientes-docker.md). Antes de mudar qualquer passo deste roteiro, confira o equivalente em [`CONFIGURACAO_PRODUCAO_DOCKER.md`](./CONFIGURACAO_PRODUCAO_DOCKER.md).

Roteiros irmãos: [`EXECUCAO_HOMOLOGACAO.md`](./EXECUCAO_HOMOLOGACAO.md) e [`EXECUCAO_PRODUCAO.md`](./EXECUCAO_PRODUCAO.md).

> **Conceito Docker: Compose com vários arquivos.** `docker-compose.yml` é a produção. `docker-compose.local.yml` é um *override*: o Compose junta os dois e o override soma ou substitui só o que muda localmente. Os comandos abaixo sempre passam os dois com `-f`.

## Fase 0 — Pré-requisitos (uma vez por máquina)

1. **Docker Desktop** instalado, com WSL2. Em Settings, desligue "Start Docker Desktop when you sign in" e ligue o "Resource Saver".
2. **Limite de recursos** em `%USERPROFILE%\.wslconfig` (depois rode `wsl --shutdown` e reabra o Docker):

```ini
[wsl2]
memory=5GB
processors=2
swap=2GB
```

3. **Segredos do Compose:** copie `.env.example` para `.env` e preencha. O `.env` nunca vai para o git.
4. **Segredos da API no Visual Studio (user-secrets):**

```powershell
$sa = (Get-Content .env | Where-Object { $_ -like 'MSSQL_SA_PASSWORD=*' }) -replace '^MSSQL_SA_PASSWORD=', ''
$ru = (Get-Content .env | Where-Object { $_ -like 'RABBITMQ_USER=*' }) -replace '^RABBITMQ_USER=', ''
$rp = (Get-Content .env | Where-Object { $_ -like 'RABBITMQ_PASSWORD=*' }) -replace '^RABBITMQ_PASSWORD=', ''
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,1433;Database=Ofertio;User Id=sa;Password=$sa;TrustServerCertificate=True;" --project .\Ofertio.API\Ofertio.API.csproj
dotnet user-secrets set "RabbitMq:Username" $ru --project .\Ofertio.API\Ofertio.API.csproj
dotnet user-secrets set "RabbitMq:Password" $rp --project .\Ofertio.API\Ofertio.API.csproj
# Chaves que não ficam mais nos appsettings versionados (a API não sobe sem Jwt:Key e Encryption:Key):
foreach ($par in @(@('JWT_KEY','Jwt:Key'), @('ENCRYPTION_KEY','Encryption:Key'), @('WEBHOOK_SECRET','Integrations:Webhook:Secret'), @('EVOLUTION_API_KEY','EvolutionApi:ApiKey'))) {
  $valor = (Get-Content .env | Where-Object { $_ -like "$($par[0])=*" }) -replace "^$($par[0])=", ''
  dotnet user-secrets set $par[1] $valor --project .\Ofertio.API\Ofertio.API.csproj | Out-Null
}
```

5. **Banco inicial:** restaure um `.bak` (fase 0.1). **Nunca** crie o banco do zero com `dotnet ef database update`: a migration inicial altera `Usuarios` e falha em base vazia.

### 0.1 Restaurar o banco `Ofertio` a partir de um `.bak`

> **Conceito Docker: volume.** O banco fica no volume `sqlserver_data`, uma pasta gerenciada pelo Docker que sobrevive quando o container é recriado. Apagar o container não apaga o banco; apagar o volume, sim.

```powershell
docker volume create ofertio_sqlserver_data
docker run --rm -v ofertio_sqlserver_data:/data alpine chown -R 10001:0 /data   # volume novo: dá a pasta ao usuário mssql (ver rules/ambientes-docker.md)
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d sqlserver
docker exec ofertio-db mkdir -p /var/opt/mssql/backup
docker cp C:\OfertioBackup\<data>\Ofertio-<data>.bak ofertio-db:/var/opt/mssql/backup/Ofertio.bak
docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "RESTORE FILELISTONLY FROM DISK=''/var/opt/mssql/backup/Ofertio.bak''"'
# Use os nomes lógicos listados acima (hoje: AutomacaoSociais e AutomacaoSociais_log):
docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "RESTORE DATABASE [Ofertio] FROM DISK=''/var/opt/mssql/backup/Ofertio.bak'' WITH MOVE ''AutomacaoSociais'' TO ''/var/opt/mssql/data/Ofertio.mdf'', MOVE ''AutomacaoSociais_log'' TO ''/var/opt/mssql/data/Ofertio_log.ldf'', REPLACE"'
```

`-P "$MSSQL_SA_PASSWORD"` fica entre aspas simples para ser lido **dentro** do container, onde a variável já existe. A senha não aparece no comando.

## Fase 1 — Subir a infraestrutura

```powershell
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d
docker compose -f docker-compose.yml -f docker-compose.local.yml ps
```

> **Conceito Docker: perfil.** `api`, `webui` e `caddy` têm o perfil `app` no override local e só sobem quando o perfil é pedido (`--profile app`, na homologação). Sem ele, sobem só `sqlserver`, `rabbitmq`, `evolution-db` e `evolution`.

O esperado: `ofertio-db`, `ofertio-rabbitmq`, `ofertio-evolution-db` e `ofertio-evolution` em `Up`.

Se a homologação estiver no ar, a infraestrutura já está de pé e é a mesma. Dev e homologação usam **o mesmo banco `Ofertio`**, como na produção, que tem um banco só.

## Fase 2 — Banco: migrations incrementais

```powershell
$sql = (Get-Content .env | Where-Object { $_ -like 'SQL_CONNECTION=*' }) -replace '^SQL_CONNECTION=', ''
$env:ConnectionStrings__DefaultConnection = $sql -replace 'Server=sqlserver,1433', 'Server=127.0.0.1,1433'
$env:Encryption__Key = (Get-Content .env | Where-Object { $_ -like 'ENCRYPTION_KEY=*' }) -replace '^ENCRYPTION_KEY=', ''
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
```

O `dotnet ef` usa o `AppDbContextFactory`, que lê os appsettings e as **variáveis de ambiente**, mas não os user-secrets. Por isso a connection string e a `Encryption:Key` (que não fica mais no `appsettings.json`) vão por variável, montada a partir do `SQL_CONNECTION` do `.env` (o mesmo valor da produção, trocando o host `sqlserver`, que só existe na rede Docker, por `127.0.0.1`). Use `127.0.0.1`, não `localhost`: o Docker publica a porta só em IPv4, e o `localhost` tenta primeiro o IPv6 (`::1`) e recebe "conexão recusada".

O esperado: `Done.` (ou `No migrations were applied. The database is already up to date.`).

Migrations SQL Server: `Ofertio.Data` + `AppDbContext`. Não use `Ofertio.Infrastructure` (PostgreSQL legado).

⚠️ O banco é compartilhado: uma migration aplicada aqui já vale na homologação.

## Fase 3 — Encoding, build e testes

```powershell
$WarningPreference = 'SilentlyContinue'; .\NormalizarEncodingPrecoce.ps1 *> $null
dotnet build
dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj
```

## Fase 4 — Rodar a API e a WebUi

### 4.1 Pausar a API da homologação (obrigatório)

```powershell
docker stop ofertio-api
```

Dev e homologação usam o **mesmo banco** e a **mesma sessão do Telegram** (`C:\ProgramData\Ofertio\telegram-sessions` foi copiada para o volume da homologação). Com as duas APIs ligadas: as 15 rotinas em segundo plano (disparos, Radar, limpezas) rodam em dobro sobre os mesmos dados, e duas conexões simultâneas com a mesma sessão podem fazer o Telegram derrubá-la. O resto da homologação (WebUi, Caddy, banco) continua no ar.

### 4.2 Subir a API e a WebUi

No Visual Studio, inicie `Ofertio.API` e `Ofertio.WebUi` (perfil `https`, vários projetos de inicialização), ou pelo terminal, em dois terminais:

```powershell
dotnet run --project .\Ofertio.API\Ofertio.API.csproj --launch-profile https
# API em https://localhost:7001 e http://localhost:5001 (este para o webhook do WhatsApp)

$env:Kestrel__Endpoints__Https__Url = 'https://localhost:7012'
$env:Kestrel__Endpoints__Https__Certificate__Path = ''
dotnet run --project .\Ofertio.WebUi\Ofertio.WebUi.csproj --launch-profile https
# WebUi em https://localhost:7012
```

Por que a WebUi vai para a 7012:

- O Caddy da homologação ocupa a **7002**, porque o túnel do Cloudflare aponta para ela. Usando a 7012, não é preciso derrubar o dev.ofertio.com.br.
- `Certificate__Path` vazio faz o Kestrel usar o certificado de desenvolvimento do .NET (`localhost`). A senha do `C:\Programacao\certificado\dev.ofertio.com.br.pfx` gravada nos user-secrets da WebUi **não abre o arquivo** (verificado em 19/11/2026), e sem essa troca a WebUi não sobe.
- Se precisar da 7002 (por exemplo, para o login com Google, cujo Console só conhece `https://localhost:7002`), pare o Caddy antes (`docker stop ofertio-caddy`, o que derruba o dev.ofertio.com.br) e religue depois.

Para o **WhatsApp** entregar mensagens à API do Visual Studio, aponte o webhook para o dev (e devolva depois):

```powershell
.\Alternar-WebhookWhatsApp.ps1 -Ambiente dev   # Evolution -> http://host.docker.internal:5001
.\Alternar-WebhookWhatsApp.ps1 -Ambiente hml   # ao terminar
```

> **Conceito Docker: `host.docker.internal`.** É o nome que um container usa para chegar ao Windows. A Evolution (container) chama a API do Visual Studio por `http://host.docker.internal:5001`. É HTTP porque o Node.js da Evolution não aceita o certificado de desenvolvimento.

## Fase 5 — Verificação

```powershell
(Invoke-WebRequest https://localhost:7001/metrics -UseBasicParsing).StatusCode
(Invoke-WebRequest http://localhost:5001/metrics -UseBasicParsing).StatusCode
docker exec ofertio-evolution sh -c 'wget -q -O /dev/null -T 8 http://host.docker.internal:5001/metrics && echo alcancou'
docker stats --no-stream
```

O esperado:

- `/metrics` responde `200` nas duas portas, e a WebUi abre em `https://localhost:7012` (ou na 7002).
- O log da API mostra `Bus started: rabbitmq://localhost/` e `UserBot MTProto autenticado`.
- O container da Evolution alcança a API do Visual Studio (`alcancou`).
- **SSMS:** servidor `127.0.0.1,1433`, autenticação SQL Server, usuário `sa`, senha `MSSQL_SA_PASSWORD` do `.env`. Mostra o banco `Ofertio`.
- Aviso conhecido: `Falha ao apagar imagem antiga ... Access denied` em `C:\inetpub\ofertio_dev\uploads`. A pasta foi criada pelo IIS e o seu usuário não pode apagar os arquivos. Não afeta a homologação. Para corrigir: `icacls C:\inetpub\ofertio_dev\uploads /grant "$env:USERNAME:(OI)(CI)M" /T`, como administrador.
- **RabbitMQ:** painel em `http://localhost:15672` (usuário e senha do `.env`).
- **WhatsApp:** `/integracoes/conexoes` mostra a instância conectada.
- `docker stats`: infraestrutura perto de 1 GB, dentro dos limites.

## Fase 6 — Encerrar

1. Pare a API e a WebUi do Visual Studio (Shift+F5 ou Ctrl+C nos terminais).
2. Devolva o webhook do WhatsApp, se ele foi trocado: `.\Alternar-WebhookWhatsApp.ps1 -Ambiente hml`.
3. Religue a API da homologação: `docker start ofertio-api`.
4. Só se a homologação também puder ficar fora do ar (para liberar memória):

```powershell
docker compose -f docker-compose.yml -f docker-compose.local.yml -f docker-compose.hml.yml --profile app stop
```

> **Conceito Docker: `stop` x `down` x `down -v`.** `stop` só desliga os containers. `down` apaga os containers, mas mantém os volumes. **`down -v` apaga também os volumes, ou seja, o banco.** Não use `-v`.

## Diferenças em relação à produção

| Item | Produção | Desenvolvimento | Motivo |
|---|---|---|---|
| API e WebUi | containers `ofertio-api` / `ofertio-webui` | Visual Studio (7001/7002) | debug e hot reload |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Development` | appsettings e logs de desenvolvimento |
| Segredos da API | `.env` da VPS | user-secrets | o Visual Studio não lê o `.env` |
| Portas do banco, RabbitMQ e Evolution | só na rede interna | `127.0.0.1:1433/5672/15672/8080` | SSMS e API fora do Docker |
| API e WebUi ao mesmo tempo que a homologação | — | `ofertio-api` pausado enquanto a API do VS roda | banco e sessão do Telegram compartilhados |
| Memória do SQL Server | sem teto (Express nos dois) | `MSSQL_MEMORY_LIMIT_MB=1536` | máquina de 16 GB |
| Limites de memória | sem limite | `mem_limit` por serviço | Docker limitado a 5 GB |
| Evolution API (WhatsApp) | não documentada na VPS | `ofertio-evolution` v2.3.7 + Postgres 15 | integração WhatsApp local |
| Data Protection | Linux, chaves sem DPAPI | Windows: a API do Visual Studio usa as próprias chaves com DPAPI | tokens `DP1:` gravados pelo dev não abrem na homologação, e vice-versa |

## O que não fazer

- Não rodar `docker compose down -v` nem `docker volume rm` nos volumes `ofertio_*`.
- Não editar o `docker-compose.yml` (base de produção) para resolver algo local. Use o override e registre a diferença acima.
- Não versionar `.env`, `.bak` nem senhas em `appsettings*.json`.
- Não usar `api:8080` ou `sqlserver` no Development. Esses nomes só existem dentro da rede Docker; no Windows é `localhost`.
- Não rodar `dotnet ef database update` em base vazia.
