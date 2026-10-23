# Configuração de ambiente em produção (Docker)

Referência do que a VPS precisa para a API e a WebUi conversarem sem alterar o ambiente local. O roteiro de publicação está em [`EXECUCAO_PRODUCAO.md`](./EXECUCAO_PRODUCAO.md).

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
| `Ofertio.API/Dockerfile`, `Ofertio.WebUi/Dockerfile`, `Caddyfile`, `nuget.config`, `docker-compose.yml` | No git. Hoje existem só na VPS e estão fora do controle de versão. |
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
```

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

## O que desta implantação entra no código local

| Alteração feita na VPS | No código local |
|---|---|
| `appsettings.json` da WebUi restaurado (a cópia da VPS tinha `AllowedHosts` duplicado) | Já está no git. A cópia quebrada era só sujeira no servidor. |
| `OrquestradorApi__BaseUrl=http://api:8080/` | Não muda a classe C#. Precisa estar no `docker-compose.yml` versionado. |
| Esquema SQL criado no volume | Não é arquivo. Fica no volume da VPS. |
| `Dockerfile` da API, `Dockerfile` da WebUi, `Caddyfile`, `nuget.config`, `docker-compose.yml` | Ainda não estão no git. Precisam ser commitados, com segredos só no `.env` da VPS. |
