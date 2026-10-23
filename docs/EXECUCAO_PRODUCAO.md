# Roteiro de deploy em produção (Docker)

> Instruções para o Cursor Agent. Execute as fases em sequência quando o usuário pedir para rodar este arquivo, publicar a produção ou implantar a nova versão na VPS.

A configuração do ambiente está em [`CONFIGURACAO_PRODUCAO_DOCKER.md`](./CONFIGURACAO_PRODUCAO_DOCKER.md). Este arquivo substitui o fluxo antigo de [`BUILD_PRODUCAO.md`](./BUILD_PRODUCAO.md).

Não grave senha de root, connection string, JWT, chave de criptografia nem segredo OAuth neste repositório, no commit nem na resposta ao usuário. Use a credencial que o usuário informar na conversa, ou uma chave SSH já autorizada.

## Fase 1 — Código local

1. Confira `git status`. Se a árvore estiver limpa, siga para a fase 2.
2. Se houver alterações da entrega, registre em `docs/GIT_VERSAO.md` e faça o commit no padrão Conventional Commits. Não inclua `.env`, `*.pfx`, `App_Data` nem segredo.
3. Envie a branch que a VPS acompanha (`main`, salvo outra indicação do usuário):

```powershell
git push origin main
```

Não rode o roteiro de IIS de [`EXECUCAO_HOMOLOGACAO.md`](./EXECUCAO_HOMOLOGACAO.md) nesta publicação. Homologação e produção são ambientes separados.

## Fase 2 — Atualizar o código na VPS

Diretório: `/var/www/ofertio`. Host: `root@138.199.212.202`.

1. Conecte por SSH.
2. Entre em `/var/www/ofertio`.
3. Antes do `git pull`, preserve o que ainda não está no git: `docker-compose.yml`, `Caddyfile`, `nuget.config`, `Ofertio.API/Dockerfile`, `Ofertio.WebUi/Dockerfile` e qualquer `.env`. Se o pull for recusar porque um desses arquivos passaria a ser rastreado, faça cópia para `/root/ofertio-config-backup` e só então atualize.
4. Puxe o código com a chave de deploy do repositório:

```bash
cd /var/www/ofertio
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_ed25519_ofertio
git pull origin main
```

5. Confirme que o serviço `webui` do Compose define `OrquestradorApi__BaseUrl=http://api:8080/`. Se o arquivo da VPS ainda tiver `ApiBaseUrl`, troque por essa chave antes de subir. Ela é a que o Blazor lê.
6. Confirme que a API define `RabbitMq__Host=rabbitmq`. `MassTransit__Host` não altera o host usado pelo código.

## Fase 3 — Build e subida dos containers

Use sempre o arquivo e o diretório explícitos:

```bash
cd /var/www/ofertio
docker compose -f /var/www/ofertio/docker-compose.yml --project-directory /var/www/ofertio build api webui
docker compose -f /var/www/ofertio/docker-compose.yml --project-directory /var/www/ofertio up -d
```

O build precisa de `DOTNET_NUGET_SIGNATURE_VERIFICATION=false` nos Dockerfiles (já usado nas imagens da VPS) por causa do `NU3012` do Refit. Use `--no-cache` só quando o restore ou a imagem base estiver corrompido.

Não apague o volume `sqlserver_data`. Não rode `dotnet ef database update` numa base vazia: a migration inicial altera `Usuarios` e falha se essa tabela não existir. O banco atual já tem `__EFMigrationsHistory`. Uma migration nova, criada depois desse histórico, pode ser aplicada em cima dele. Se o comando falhar em `Usuarios`, pare e não recrie o volume.

## Fase 4 — Verificação

```bash
docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
curl -s -o /dev/null -w 'home:%{http_code}\n' --max-time 20 https://ofertio.com.br/
```

O esperado:

- `ofertio-webui` e `ofertio-api` em `Up`, sem reinício em loop.
- `https://ofertio.com.br/` responde `200`.
- O log do WebUi não mostra `duplicate key 'AllowedHosts'` nem `Falha OAuth para Google` com `Erro na API (500)` por `AspNetUserLogins`.
- `docker logs ofertio-webui --tail 30` mostra `Now listening on: http://[::]:8080` e `Hosting environment: Production`.

Se o WebUi reiniciar, leia `docker logs ofertio-webui --tail 80` antes de alterar outra coisa. Se o login Google devolver 500, leia o log da API no horário do `POST /api/auth/tiktok/login` (o portal sem plataforma usa a rota TikTok; o provedor no corpo continua Google).

## Fase 5 — O que não fazer neste roteiro

- Não commitar a partir da VPS.
- Não colocar senha no `docker-compose.yml` versionado. Segredo fica no `.env` da VPS.
- Não apontar `OrquestradorApi:BaseUrl` de Development para `http://api:8080`. Esse host só existe na rede Docker.
- Não incluir `www.ofertio.com.br` no Caddyfile enquanto o DNS desse nome não existir.
