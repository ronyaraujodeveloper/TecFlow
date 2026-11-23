# Roteiro de deploy em produção (Docker)

> Instruções para o Cursor Agent. Execute as fases em sequência quando o usuário pedir para rodar este arquivo, publicar a produção ou implantar a nova versão na VPS.

A configuração do ambiente está em [`CONFIGURACAO_PRODUCAO_DOCKER.md`](./CONFIGURACAO_PRODUCAO_DOCKER.md). Regra dos ambientes: [`rules/ambientes-docker.md`](./rules/ambientes-docker.md). Este arquivo substitui o fluxo antigo de [`BUILD_PRODUCAO.md`](./arquivo/BUILD_PRODUCAO.md), arquivado.

Não grave senha de root, connection string, JWT, chave de criptografia nem segredo OAuth neste repositório, no commit nem na resposta ao usuário. Use a credencial que o usuário informar na conversa, ou uma chave SSH já autorizada.

Diretório: `/var/www/ofertio`. Host: `root@138.199.212.202`. Compose sempre com caminho explícito:

```bash
C="-f /var/www/ofertio/docker-compose.yml --project-directory /var/www/ofertio"
```

> **Primeiro deploy com o compose do git?** Se a VPS ainda não tem `/var/www/ofertio/.env` ou o volume `ofertio_api_appdata`, faça antes a seção [Primeiro deploy com o compose do git (uma vez)](#primeiro-deploy-com-o-compose-do-git-uma-vez). Depois disso, os deploys seguintes usam só as fases 1 a 5.

## Fase 1 — Código local

1. Confira `git status`. Se a árvore estiver limpa, siga para a fase 2.
2. Se houver alterações da entrega, faça o commit no padrão Conventional Commits. Não inclua `.env`, `*.pfx`, `App_Data` nem segredo.
3. Envie a branch que a VPS acompanha (`main`, salvo outra indicação do usuário):

```powershell
git push origin main
```

4. Se houver migrations novas desde a última aplicada na produção, gere o script idempotente e versione em `scripts/producao/`:

```powershell
$sql = (Get-Content .env | Where-Object { $_ -like 'SQL_CONNECTION=*' }) -replace '^SQL_CONNECTION=', ''
$env:ConnectionStrings__DefaultConnection = $sql -replace 'Server=sqlserver,1433', 'Server=127.0.0.1,1433'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations script <UltimaMigrationDaProducao> --idempotent --output .\scripts\producao\migrations-pendentes-<de>-ate-<ate>.sql --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
```

Teste o script no banco local antes (fase 3 do [`EXECUCAO_DESENVOLVIMENTO.md`](./EXECUCAO_DESENVOLVIMENTO.md)): como lá já está tudo aplicado, ele precisa rodar sem erro e sem alterar nada. Bloco de `migrationBuilder.Sql` que cita coluna removida depois precisa estar dentro de `EXEC(N'...')`, senão o script idempotente falha na compilação.

Não rode [`EXECUCAO_HOMOLOGACAO.md`](./EXECUCAO_HOMOLOGACAO.md) nesta publicação. A homologação roda em Docker na máquina local, a produção na VPS. Os dois usam o mesmo `docker-compose.yml`.

## Fase 2 — Atualizar o código na VPS

```bash
cd /var/www/ofertio
df -h / | tail -1                       # precisa de folga (o build usa ~10 GB de cache)
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_ed25519_ofertio
git pull origin main
```

Se o disco estiver acima de 80%, antes do pull: `docker builder prune -af && docker image prune -af` (não toca em containers rodando nem em volumes).

## Fase 3 — Backup, migrations e subida

1. **Backup do banco** antes de qualquer migration:

```bash
docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "BACKUP DATABASE [Ofertio] TO DISK=N'\''/var/opt/mssql/data/Ofertio-pre-deploy.bak'\'' WITH INIT, COPY_ONLY, CHECKSUM"'
```

2. **Migrations** (script idempotente e transacional; se falhar, nada é aplicado). O `-I` liga `QUOTED_IDENTIFIER`: sem ele, índice filtrado falha com `Msg 1934` (aconteceu no deploy de 23/11/2026; o rollback foi automático):

```bash
docker cp scripts/producao/<script>.sql ofertio-db:/tmp/migrations.sql
docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -I -d Ofertio -i /tmp/migrations.sql' && echo "MIGRATIONS OK"
docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -h -1 -W -Q "SELECT TOP 1 MigrationId FROM Ofertio.dbo.__EFMigrationsHistory ORDER BY 1 DESC"'
```

3. **Build e subida:**

```bash
docker compose $C build api webui
docker compose $C up -d
```

O build usa `DOTNET_NUGET_SIGNATURE_VERIFICATION=false` e o `nuget.config` (Refit `NU3012`). Use `--no-cache` só quando o restore ou a imagem base estiver corrompido.

Não apague o volume `sqlserver_data`. Nunca rode `dotnet ef database update` numa base vazia (a migration inicial falha).

## Fase 4 — Verificação

```bash
docker ps --format 'table {{.Names}}\t{{.Status}}'
curl -s -o /dev/null -w 'home:%{http_code}\n' --max-time 20 https://ofertio.com.br/
curl -s -o /dev/null -w 'api:%{http_code}\n' --max-time 20 https://ofertio.com.br/api/sistema/avisos
docker logs ofertio-api --tail 300 2>&1 | grep -E "Hosting environment|Bus started|CryptographicException|SqlException|fail:"
du -h $(docker inspect -f '{{.LogPath}}' ofertio-api)
```

O esperado:

- os 5 containers em `Up`, sem reinício em loop;
- home `200`, API `401` (no ar, sem login);
- `Hosting environment: Production`, `Bus started: rabbitmq://rabbitmq/`, sem `CryptographicException` (as chaves do Data Protection foram preservadas);
- o login com Google funciona; uma imagem enviada aparece (`/uploads/...` responde `200`);
- o log da API fica pequeno (limite 3 × 50 MB; o exportador de console do OpenTelemetry está desligado).

Se um container reiniciar, leia `docker logs <nome> --tail 80` antes de alterar outra coisa. Se o login Google devolver 500, leia o log da API no horário do `POST /api/auth/tiktok/login`.

**Voltar atrás:** `git checkout <commit anterior>` + `docker compose $C build api webui && docker compose $C up -d`. Se a migration tiver causado o problema, restaure `/var/opt/mssql/data/Ofertio-pre-deploy.bak`.

## Fase 5 — O que não fazer

- Não commitar a partir da VPS.
- Não colocar senha no `docker-compose.yml` versionado. Segredo fica no `.env` da VPS.
- Não usar `docker compose down -v` nem remover os volumes `ofertio_sqlserver_data`, `ofertio_api_appdata` e `ofertio_uploads`.
- Não usar `docker compose up --force-recreate` sem antes conferir que as chaves estão no volume `ofertio_api_appdata`.
- Não apontar `OrquestradorApi:BaseUrl` de Development para `http://api:8080`. Esse host só existe na rede Docker.

---

## Primeiro deploy com o compose do git (uma vez)
> **Feito em 23/11/2026.** A VPS já está no compose do git, com `.env`, volume `ofertio_api_appdata` (chave `key-9b6516f4…` migrada) e migrations até `20261119225813_AddMenuItemKindAndComponents`. Esta seção fica como histórico. Diferenças encontradas: além dos arquivos listados no passo C, a VPS tinha `Ofertio.API/appsettings.json` alterado (versão antiga reduzida) e `appsettings.Production.json` (API e WebUi) e `Ofertio.WebUi/wwwroot/appsettings.json` não rastreados, com o segredo do Google. O último era **servido publicamente** em `/appsettings.json`. Todos foram para `/root/ofertio-config-backup` e saíram da pasta. Não recriar arquivos com segredo dentro de `wwwroot`.

Até 20/11/2026 a VPS rodava um compose próprio: segredos escritos no arquivo, sem `.env`, sem volumes na API e na WebUi, chave do Data Protection **dentro do container** em `/usr/share/Ofertio/DataProtectionKeys/` (caminho padrão, porque `DataProtection:KeysPath` não era definido) e log sem limite (19 GB, que derrubou o site em 16/11). Estado conferido em 20/11/2026: nenhuma imagem em `/app/wwwroot/uploads` e nenhuma sessão do Telegram, ou seja, **só a chave precisa ser migrada**.

Faça na ordem. Os passos A a D não mexem nos containers em execução: o site continua no ar até o passo E.

**A. Guardar as cópias atuais da VPS**

```bash
cd /var/www/ofertio
mkdir -p /root/ofertio-config-backup
cp docker-compose.yml Caddyfile nuget.config /root/ofertio-config-backup/
cp Ofertio.API/Dockerfile /root/ofertio-config-backup/Dockerfile.api
cp Ofertio.WebUi/Dockerfile /root/ofertio-config-backup/Dockerfile.webui
```

**B. Criar o `.env` com os valores que a produção usa hoje** (lidos do compose antigo; nada é exibido na tela):

```bash
OLD=/root/ofertio-config-backup/docker-compose.yml
val() { grep -m1 -E "^\s*-\s*$1=" "$OLD" | sed -E "s/^\s*-\s*$1=//"; }
umask 077
cat > /var/www/ofertio/.env <<EOF
MSSQL_SA_PASSWORD=$(val MSSQL_SA_PASSWORD)
SQL_CONNECTION=$(val ConnectionStrings__DefaultConnection)
RABBITMQ_USER=$(val RABBITMQ_DEFAULT_USER)
RABBITMQ_PASSWORD=$(val RABBITMQ_DEFAULT_PASS)
ENCRYPTION_KEY=$(val Encryption__Key)
JWT_KEY=$(val Jwt__Key)
GOOGLE_CLIENT_ID=$(val Authentication__Google__ClientId)
GOOGLE_CLIENT_SECRET=$(val Authentication__Google__ClientSecret)
EOF
grep -E '^[A-Z_]+=$' /var/www/ofertio/.env && echo "ATENÇÃO: há variável vazia acima" || echo ".env completo"
```

`ENCRYPTION_KEY` e `JWT_KEY` **têm que ser os mesmos de hoje**: com outra chave, os segredos criptografados no banco ficam ilegíveis e todos os usuários são deslogados.

**C. Atualizar o código** (as cópias antigas viram rastreadas pelo git, então remova-as antes; o backup está no passo A):

```bash
cd /var/www/ofertio
rm docker-compose.yml Caddyfile nuget.config Ofertio.API/Dockerfile Ofertio.WebUi/Dockerfile
eval "$(ssh-agent -s)"; ssh-add ~/.ssh/id_ed25519_ofertio
git pull origin main
diff /root/ofertio-config-backup/Caddyfile Caddyfile      # esperado: só a remoção do www.ofertio.com.br
docker compose $C config --quiet && echo "compose OK"
```

**D. Copiar a chave do Data Protection para o volume novo** (o container atual continua rodando):

```bash
mkdir -p /root/ofertio-dpkeys
docker cp ofertio-api:/usr/share/Ofertio/DataProtectionKeys/. /root/ofertio-dpkeys/
ls /root/ofertio-dpkeys                                   # esperado: key-*.xml
docker volume create ofertio_api_appdata
docker run --rm -v ofertio_api_appdata:/data -v /root/ofertio-dpkeys:/src:ro alpine \
  sh -c 'mkdir -p /data/OfertioDataProtectionKeys /data/telegram-sessions && cp /src/*.xml /data/OfertioDataProtectionKeys/ && ls -R /data'
```

**E. Backup, migrations, build e subida:** fase 3 completa, com o script `scripts/producao/migrations-pendentes-20261028-ate-20261119.sql` (8 migrations: de `20261108203538_AddTermsVersionsAndAcceptance` até `20261119225813_AddMenuItemKindAndComponents`). O `up -d` recria `ofertio-api`, `ofertio-webui`, `ofertio-rabbitmq` e `ofertio-caddy` com a configuração nova. O `ofertio-db` mantém o volume e os dados. O site fica fora por cerca de 1 minuto.

**F. Verificação:** fase 4. Confira especialmente:
- nenhuma `CryptographicException` no log da API (a chave foi lida do volume);
- `docker exec ofertio-api ls /app/App_Data/OfertioDataProtectionKeys` mostra a mesma `key-*.xml` do passo D.

**G. Depois de validado**, apagar as cópias temporárias: `rm -rf /root/ofertio-dpkeys` (o backup do compose antigo em `/root/ofertio-config-backup` contém senhas; apague quando não precisar mais voltar atrás).
