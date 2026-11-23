# Configuração de ambiente em produção (Docker)

Referência do que a VPS precisa para a API e a WebUi conversarem sem alterar o ambiente local. O roteiro de publicação está em [`EXECUCAO_PRODUCAO.md`](./EXECUCAO_PRODUCAO.md).

Este documento é a **referência dos ambientes locais**. Desenvolvimento e homologação usam o mesmo `docker-compose.yml` com overrides e seguem a regra [`rules/ambientes-docker.md`](./rules/ambientes-docker.md). Ao mudar algo aqui, revise [`EXECUCAO_DESENVOLVIMENTO.md`](./EXECUCAO_DESENVOLVIMENTO.md) e [`EXECUCAO_HOMOLOGACAO.md`](./EXECUCAO_HOMOLOGACAO.md) no mesmo commit.

## Onde roda

| Item | Valor |
|---|---|
| Servidor | `138.199.212.202` (Hetzner) |
| Diretório | `/var/www/ofertio` |
| Domínio público | `https://ofertio.com.br` |
| Rede Docker | `ofertio-net` (nome no host: `ofertio_ofertio-net`) |
| Compose | `docker compose -f /var/www/ofertio/docker-compose.yml --project-directory /var/www/ofertio` |

O comando precisa do `-f` e do `--project-directory`. A partir de `/root`, o Compose não enxerga o arquivo da aplicação.

Só o Caddy publica porta no host (`80` e `443`). Banco, RabbitMQ, API e WebUi ficam na rede interna.

| Container | Serviço no Compose | Escuta |
|---|---|---|
| `ofertio-db` | `sqlserver` | SQL Server `1433`, só na rede |
| `ofertio-rabbitmq` | `rabbitmq` | `5672`, só na rede |
| `ofertio-api` | `api` | `http://+:8080` |
| `ofertio-webui` | `webui` | `http://+:8080` |
| `ofertio-caddy` | `caddy` | `80` e `443` |

O Caddy encaminha `/api/*` para `api:8080` e o restante para `webui:8080`. Entre containers o tráfego é HTTP. O TLS termina no Caddy.

## O que fica no git e o que fica só na VPS

A máquina de desenvolvimento continua com `ASPNETCORE_ENVIRONMENT=Development` e `Ofertio.WebUi/appsettings.Development.json` apontando para `https://localhost:7001/`. Variável de ambiente do container tem precedência sobre o JSON e não altera esse arquivo.

| Artefato | Onde deve viver |
|---|---|
| `Ofertio.API/Dockerfile`, `Ofertio.WebUi/Dockerfile`, `Caddyfile`, `nuget.config`, `docker-compose.yml` | No git, na raiz do repositório (desde 19/11/2026). Conferidos com a VPS em 19/11/2026: ver [Pendências na VPS](#pendências-na-vps-comparação-de-19112026). |
| `.env.example` | No git. Modelo das variáveis do `.env`. |
| Senhas, JWT, chave de criptografia, segredo do Google | Arquivo `.env` na VPS, fora do git. Não copiar para `appsettings*.json` nem para o Compose versionado. |
| Volume `sqlserver_data` | Só na VPS. O esquema já aplicado não é um arquivo de código. |

O fallback `https://localhost:7001/` em `OrquestradorApiOptions` permanece. Ele só entra quando nenhuma configuração define `OrquestradorApi:BaseUrl`. Em Development o JSON local cobre isso. Em produção a variável abaixo cobre isso.

## Variáveis que o código realmente lê

O nome no Compose usa `__` no lugar de `:`.

### WebUi

```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
OrquestradorApi__BaseUrl: http://api:8080/
OrquestradorApi__TimeoutSeconds: "180"
UploadStorage__RootPath: /app/uploads          # volume uploads:/app/uploads:ro, o mesmo da API
Authentication__Google__Enabled: "true"
Authentication__Google__ClientId: ${GOOGLE_CLIENT_ID}
Authentication__Google__ClientSecret: ${GOOGLE_CLIENT_SECRET}
```

`http://api:8080/` é o nome do serviço e a porta interna. `ApiBaseUrl` e `ApiSettings:BaseUrl` são ignorados pelo Blazor. `https://localhost:7001` dentro do container aponta para o próprio WebUi.

### API

```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
Database__Provider: SqlServer
ConnectionStrings__DefaultConnection: ${SQL_CONNECTION}
Encryption__Key: ${ENCRYPTION_KEY}
Jwt__Key: ${JWT_KEY}
Jwt__Secret: ${JWT_KEY}
Jwt__Issuer: ofertio.com.br
Jwt__Audience: ofertio.com.br
RabbitMq__Host: rabbitmq
RabbitMq__Port: "5672"
RabbitMq__Username: ${RABBITMQ_USER}
RabbitMq__Password: ${RABBITMQ_PASSWORD}
Authentication__Google__Enabled: "true"
Authentication__Google__ClientId: ${GOOGLE_CLIENT_ID}
Authentication__Google__ClientSecret: ${GOOGLE_CLIENT_SECRET}
UploadStorage__RootPath: /app/uploads
Email__Smtp__Host: ${SMTP_HOST:-}
Email__Smtp__Port: "587"
Email__Smtp__User: ${SMTP_USER:-}
Email__Smtp__Password: ${SMTP_PASSWORD:-}
```

**E-mail transacional:** a API envia pelo SMTP do Gmail (`smtp.gmail.com`, porta 587) com a conta `ofertiotecso@gmail.com`, configurada no Painel Master → Cofre (`/master/configuracoes`) → seção E-mail: servidor, usuário e senha de app de 16 letras. Vale sem reiniciar, e o botão "Enviar e-mail de teste" confirma. **Remetente:** com conta @gmail.com, o código usa sempre a própria conta (nome "Ofertio"), com Responder para `contato@ofertio.com.br`. O motivo é o DMARC `p=reject` do domínio: o Gmail assina como gmail.com, e um remetente @ofertio.com.br seria recusado. Para usar `contato@` como remetente no futuro, troque por um provedor com DKIM do domínio (ex.: Brevo); só mudam as chaves do Cofre. O `.env` (`SMTP_*`) é só reserva: o valor do Cofre vence. Sem servidor em nenhum dos dois, a API só registra o e-mail no log.
**`DOTNET_SYSTEM_NET_DISABLEIPV6: "1"` na API:** a rede Docker não tem IPv6. O `smtp.gmail.com` resolve primeiro para IPv6 e o `SmtpClient` do .NET não cai para o IPv4, então o envio falhava com `Network is unreachable` (diagnosticado em 20/11/2026). Com a variável, o .NET usa só IPv4 em todas as chamadas de saída.

**Email Address Obfuscation (Cloudflare) desligado:** ligado, ele troca os `mailto:` por `/cdn-cgi/l/email-protection#...` e a navegação do Blazor não roda o script que desfaz a troca, então o "Fale conosco" abre a página de bloqueio do Cloudflare.

`SQL_CONNECTION` usa o host `sqlserver`, porta `1433`, banco `Ofertio`, `TrustServerCertificate=True`. O host `localhost` dentro da API é o container da API, não o banco.

A classe `RabbitMqOptions` lê `RabbitMq:Host`. O valor padrão no `appsettings.json` é `localhost`. A chave `MassTransit__Host` não muda esse host. Na VPS, em 23/10/2026, o Compose ainda tinha `MassTransit__Host=rabbitmq` e o log mostrava `rabbitmq://localhost/`.

`UploadStorage:RootPath` precisa terminar na pasta `uploads`. O `appsettings.json` traz `C:\inetpub\ofertio\uploads`. No Linux isso vira um caminho inválido dentro de `/app`. O valor de produção é `/app/uploads`, com um volume montado nesse caminho. Na data acima essa variável ainda não estava no Compose da VPS.

### Caddyfile

Publicar só o domínio que tem DNS. `www.ofertio.com.br` não resolve (`NXDOMAIN`) e o Caddy fica repetindo a emissão desse certificado. `ofertio.com.br` já tem certificado Let's Encrypt.

```caddyfile
ofertio.com.br {
    handle /api/* {
        reverse_proxy api:8080
    }

    handle {
        reverse_proxy webui:8080
    }
}
```

### Build das imagens

Os Dockerfiles da API e da WebUi usam `dotnet/sdk:8.0` e `dotnet/aspnet:8.0`, publicam em Release e sobem com `dotnet Ofertio.API.dll` ou `dotnet Ofertio.WebUi.dll`. O restore precisa de:

```text
DOTNET_NUGET_SIGNATURE_VERIFICATION=false
```

e de `nuget.config` com `signatureValidationMode=accept`. Sem isso o pacote Refit 10.1.6 falha com `NU3012` (certificado revogado) e a imagem não gera.

## Banco de dados

O volume `sqlserver_data` guarda o banco `Ofertio`. Em 23/10/2026 o esquema atual (60 tabelas, inclusive `Usuarios` e `AspNetUserLogins`) foi criado direto no volume e as 36 migrations de `Ofertio.Data` foram registradas em `__EFMigrationsHistory`.

`dotnet ef database update` em um banco vazio falha: `20260920224523_InitialSqlServerMigration` altera `Usuarios` em vez de criar o banco do zero. Não rode esse comando numa base nova. Com o histórico já gravado, uma migration nova e incremental pode ser aplicada em cima dessa base.

## Pendências na VPS (comparação de 19/11/2026)

O `docker-compose.yml`, o `Caddyfile`, o `nuget.config` e os Dockerfiles da VPS foram comparados com os do git. **Dockerfiles e `nuget.config` são idênticos.** O compose do git já traz o que este documento exige; a VPS ainda roda a versão antiga, com estas diferenças:

| # | Na VPS hoje | No git (alvo) | Risco |
|---|---|---|---|
| 1 | **Disco estava em 38 GB de 38 GB (100%).** Em 19/11/2026 o cache de build e as imagens sem uso foram limpos (13,2 GB): ficou em 72%. Causa: **log da API com 19 GB**, porque o exportador de console do OpenTelemetry (`Telemetry:EnableConsoleExporter=true` no appsettings) imprime cada trace e métrica (~1,3 GB/dia) e o Docker da VPS não limita logs (sem `/etc/docker/daemon.json`) | `Telemetry__EnableConsoleExporter=false` na API e `logging` com `max-size: 50m` / `max-file: 3` em todos os serviços | Alto: sem o deploy, o disco enche de novo em cerca de 8 dias. Paliativo: `truncate -s 0 $(docker inspect -f '{{.LogPath}}' ofertio-api)` |
| 2 | API e WebUi **sem volume**. A chave do Data Protection está dentro do container em `/usr/share/Ofertio/DataProtectionKeys/` (caminho padrão, sem `DataProtection:KeysPath`). Em 20/11/2026 não havia imagens em `/app/wwwroot/uploads` nem sessões do Telegram | API: `uploads:/app/uploads` e `api_appdata:/app/App_Data` com `DataProtection__KeysPath`; WebUi: `uploads:/app/uploads:ro` com `UploadStorage__RootPath=/app/uploads` (o Caddy manda `/uploads` para a WebUi) | Alto: recriar a API sem copiar a chave deixa os tokens `DP1:` ilegíveis. Sem o volume na WebUi, as imagens quebram (404) |
| 3 | Senhas, JWT, chave de criptografia e segredo do Google escritos no próprio compose; não existe `.env` | `${VAR}` lidos do `.env` | Médio: o compose não pode ir para o git como está |
| 4 | Sem `UploadStorage__RootPath` | `/app/uploads` | Médio: cai no caminho Windows do appsettings |
| 5 | `MassTransit__Host=rabbitmq`; API sem `RabbitMq__Username/Password` (usa `guest` do appsettings) | `RabbitMq__Host/Port/Username/Password` | Baixo |
| 6 | Caddyfile com `ofertio.com.br, www.ofertio.com.br` | só `ofertio.com.br` | Baixo: o Caddy repete a emissão do certificado do `www` sem DNS |
| 7 | Banco com 62 tabelas e **39 migrations** (última `20261028005516_AddGlobalIntegrationExternalAccount`) | **8 migrations pendentes**, no script idempotente `scripts/producao/migrations-pendentes-20261028-ate-20261119.sql` (testado no banco local) | É o deploy pendente. O banco local tem 49 porque guarda 2 registros antigos (`20260524_InitialMigration`, `20260525_AddTikTokTokensToUsuario`) que não existem no código |
| 8 | Volume do banco montado em `/var/opt/mssql/data` | igual | — (o git foi alinhado à VPS) |

O passo a passo do primeiro deploy, com os comandos prontos, está em [`EXECUCAO_PRODUCAO.md`](./EXECUCAO_PRODUCAO.md#primeiro-deploy-com-o-compose-do-git-uma-vez).

## O que desta implantação entra no código local

| Alteração feita na VPS | No código local |
|---|---|
| `appsettings.json` da WebUi restaurado (a cópia da VPS tinha `AllowedHosts` duplicado) | Já está no git. A cópia quebrada era só sujeira no servidor. |
| `OrquestradorApi__BaseUrl=http://api:8080/` | Não muda a classe C#. Precisa estar no `docker-compose.yml` versionado. |
| Esquema SQL criado no volume | Não é arquivo. Fica no volume da VPS. |
| `Dockerfile` da API, `Dockerfile` da WebUi, `Caddyfile`, `nuget.config`, `docker-compose.yml` | Ainda não estão no git. Precisam ser commitados, com segredos só no `.env` da VPS. |
