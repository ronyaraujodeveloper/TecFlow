# 🐳 Ambientes Docker: produção é a referência

> Desenvolvimento e homologação rodam em Docker nesta máquina e seguem a produção (VPS) **o mais igual possível**. Assim, o que funciona localmente funciona na VPS, e cada diferença tem um motivo escrito.

## 1. A produção define os valores

A fonte é [`CONFIGURACAO_PRODUCAO_DOCKER.md`](../CONFIGURACAO_PRODUCAO_DOCKER.md) junto com o [`docker-compose.yml`](../../docker-compose.yml) da raiz, que é o compose da produção. Dev e homologação usam esse mesmo arquivo como base e **não o editam**.

Igual nos três ambientes:

| Item | Valor |
|---|---|
| Serviços (nome no Compose) | `sqlserver`, `rabbitmq`, `api`, `webui`, `caddy` |
| Containers | `ofertio-db`, `ofertio-rabbitmq`, `ofertio-api`, `ofertio-webui`, `ofertio-caddy` |
| Banco | **`Ofertio`** no volume `sqlserver_data` |
| Rede | `ofertio-net` |
| Porta interna da API e da WebUi | `http://+:8080` |
| Variáveis | as mesmas chaves (`OrquestradorApi__BaseUrl`, `RabbitMq__Host`, `UploadStorage__RootPath=/app/uploads`, `DataProtection__KeysPath`...) |
| Build | os mesmos `Ofertio.API/Dockerfile`, `Ofertio.WebUi/Dockerfile` e `nuget.config` |
| Roteamento | Caddy: `/api/*` vai para `api:8080`, o resto para `webui:8080` |

## 2. Diferença só por override e sempre documentada

As diferenças ficam nos arquivos de override, nunca na base:

- [`docker-compose.local.yml`](../../docker-compose.local.yml): comum ao dev e à homologação.
- [`docker-compose.hml.yml`](../../docker-compose.hml.yml): só da homologação.

Cada roteiro (`EXECUCAO_DESENVOLVIMENTO.md` e `EXECUCAO_HOMOLOGACAO.md`) tem uma tabela **"Diferenças em relação à produção"** com o motivo de cada linha. Diferença que não está na tabela é erro.

Diferenças aceitas hoje: `ASPNETCORE_ENVIRONMENT`, domínio e certificado do Caddy (túnel Cloudflare), portas publicadas em `127.0.0.1`, limites de memória e Evolution API (WhatsApp). A edição Express do SQL Server **não** é diferença: a produção também usa `MSSQL_PID=Express`.

O compose do git pode estar **à frente** da VPS em itens que [`CONFIGURACAO_PRODUCAO_DOCKER.md`](../CONFIGURACAO_PRODUCAO_DOCKER.md) exige e a VPS ainda não aplicou. Esses itens ficam na seção "Pendências na VPS" daquele documento, nunca como diferença local.

## 3. Toda instrução é validada contra a produção

- Antes de adicionar ou alterar um passo em `EXECUCAO_DESENVOLVIMENTO.md`, `EXECUCAO_HOMOLOGACAO.md` ou nos composes locais, confira o valor equivalente na produção. Se divergir, ajuste para ficar igual ou registre a diferença com o motivo.
- Se a produção mudar (compose, Caddyfile, variável, Dockerfile), revise os roteiros locais **no mesmo commit**.
- Para conferir: `docker compose -f docker-compose.yml -f docker-compose.local.yml -f docker-compose.hml.yml --profile app config` deve diferir da base só no que está na tabela.

## 4. Segredos

- Senhas, JWT, chave de criptografia e segredos OAuth ficam no `.env` (fora do git), como na VPS. O modelo versionado é o `.env.example`.
- No desenvolvimento pelo Visual Studio, os segredos da API ficam em **user-secrets**, nunca em `appsettings*.json`.

## 5. Dados que não podem se perder

| Volume | Conteúdo | Se perder |
|---|---|---|
| `sqlserver_data` | banco `Ofertio` | perde tudo; restaurar do `.bak` |
| `api_appdata` | chaves do Data Protection e sessões do Telegram | tokens `DP1:` de marketplace ficam ilegíveis; o UserBot pede login de novo |
| `uploads` | imagens enviadas (a API grava; a WebUi lê, somente leitura, e entrega em `/uploads`) | imagens quebradas |
| `evolution_pg` | sessão do WhatsApp (Evolution) | precisa ler o QR de novo |

Nunca rode `docker compose down -v` nem `docker volume rm` nesses volumes sem backup. Nunca rode `dotnet ef database update` em base vazia (a migration inicial falha); restaure o `.bak`.

**Volume novo do SQL Server:** montado em `/var/opt/mssql/data` (igual à produção), o volume nasce com dono `root` e o SQL Server, que roda como `mssql` (uid 10001), entra em loop com `Access is denied` ao copiar o `master.mdf`. Antes do primeiro start, rode:

```powershell
$env:MSYS_NO_PATHCONV = 1   # só no Git Bash
docker run --rm -v ofertio_sqlserver_data:/data alpine chown -R 10001:0 /data
```
