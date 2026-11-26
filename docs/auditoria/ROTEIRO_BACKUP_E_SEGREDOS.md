# Roteiro: backup (bloco G) e troca de segredos (bloco C)

> Parte da auditoria de 25/11/2026 ([RELATORIO.md](./RELATORIO.md), seções 5 e 6). Este roteiro **não contém nenhum segredo**. Os valores novos nascem na VPS ou no painel de cada provedor e vão direto para o `.env`. Nunca cole um segredo em commit, chat ou documento.

**Ordem obrigatória: G antes de C.** A troca da chave de criptografia (C5) regrava dados no banco. Sem um backup testado, um erro ali deixa todo mundo sem conseguir entrar.

Convenções:
- Comandos `bash` rodam na VPS (`ssh root@138.199.212.202`). Comandos `powershell` rodam no seu PC.
- `C="-f /var/www/ofertio/docker-compose.yml --project-directory /var/www/ofertio"`, o mesmo atalho do [EXECUCAO_PRODUCAO.md](../EXECUCAO_PRODUCAO.md). Defina no início de cada sessão SSH.
- **Quem faz:** 🧑 você (painel/VPS), 🤖 eu (código, numa branch com testes), 🧑+🤖 os dois.

---

## Visão geral

| Passo | O que | Quem | Derruba sessões? | Tempo |
|---|---|---|---|---|
| G1 | Backup manual agora (banco + chaves + `.env`) e cópia para o seu PC | 🧑 | não | 15 min |
| G2 | Testar a restauração no seu PC | 🧑 | não | 20 min |
| G3 | Backup diário automático na VPS | 🧑 | não | 15 min |
| G4 | Cópia diária para fora da VPS | 🧑 (decisão) | não | 20 min |
| C1 | Revogar chaves de terceiros (OpenAI, TikTok, Google, Shopee) | 🧑 | não | 20 min |
| C2 | Código: tirar segredos do git e completar o compose | 🤖 | não | – |
| C3 | Trocar `JWT_KEY` | 🧑 | **sim, todos saem** | 10 min |
| C4 | Trocar segredo dos webhooks e reconectar os bots do Telegram | 🧑 | não | 15 min + bots |
| C5 | Trocar `ENCRYPTION_KEY` (com ferramenta de recifragem) | 🤖 + 🧑 | não, mas a API para ~2 min | 30 min |
| C6 | Senhas de infraestrutura (root da VPS, SA do SQL, RabbitMQ) | 🧑 | não | 20 min |
| C7 | Limpar o histórico do git (opcional) | 🧑+🤖 | não | 30 min |

---

## Bloco G: backup

### Conceitos Docker usados aqui

- **`docker exec <container> <comando>`** roda um comando *dentro* de um container que já está no ar. É assim que chamamos o `sqlcmd` dentro do `ofertio-db`.
- **`docker cp <container>:<caminho> <destino>`** copia um arquivo de dentro do container para a VPS.
- **Volume** é uma pasta gerenciada pelo Docker que sobrevive quando o container é recriado. Os nossos: `ofertio_sqlserver_data` (banco), `ofertio_api_appdata` (chaves do Data Protection e sessões do Telegram) e `ofertio_uploads` (imagens).
- **Container descartável `alpine`**: para copiar um volume, subimos um Linux mínimo com o volume montado, rodamos `tar` e ele se apaga sozinho (`--rm`).

### G1. Backup manual agora 🧑

```bash
C="-f /var/www/ofertio/docker-compose.yml --project-directory /var/www/ofertio"
STAMP=$(date +%Y%m%d-%H%M)
mkdir -p /root/backups && chmod 700 /root/backups
df -h / | tail -1          # precisa de espaço para o tamanho do banco + folga

# 1) Banco: o SQL Server grava o .bak dentro do próprio volume dele.
#    A senha vem da variável de ambiente do container ($MSSQL_SA_PASSWORD), então não aparece aqui.
docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "BACKUP DATABASE [Ofertio] TO DISK=N'\''/var/opt/mssql/data/backup-manual.bak'\'' WITH INIT, COPY_ONLY, CHECKSUM"'

# 2) Tira o .bak de dentro do container e compacta.
docker cp ofertio-db:/var/opt/mssql/data/backup-manual.bak /root/backups/Ofertio-$STAMP.bak
gzip /root/backups/Ofertio-$STAMP.bak

# 3) Chaves do Data Protection + sessões do Telegram (sem elas, os tokens de marketplace "DP1:" ficam ilegíveis).
docker run --rm -v ofertio_api_appdata:/data:ro -v /root/backups:/out alpine \
  tar czf /out/api_appdata-$STAMP.tgz -C /data .

# 4) O .env (contém as chaves atuais; sem ele o backup do banco não abre os campos "ENC1:").
cp /var/www/ofertio/.env /root/backups/env-$STAMP
chmod 600 /root/backups/*

# 5) (Opcional) imagens enviadas. Veja o tamanho antes: docker system df -v | grep uploads
docker run --rm -v ofertio_uploads:/data:ro -v /root/backups:/out alpine \
  tar czf /out/uploads-$STAMP.tgz -C /data .

ls -lh /root/backups
```

`COPY_ONLY` faz uma cópia avulsa que não interfere em nenhuma outra rotina de backup. `CHECKSUM` faz o SQL Server conferir cada página enquanto grava; se algo estiver corrompido, o comando falha em vez de gerar um arquivo inútil. Não usamos `COMPRESSION` porque o SQL Server Express não suporta; por isso o `gzip`.

**Trazer para o seu PC.** Um backup que fica só na VPS não protege contra perder a VPS:

```powershell
$dest = "$env:USERPROFILE\Documents\Ofertio-backups"
New-Item -ItemType Directory -Force $dest | Out-Null
scp "root@138.199.212.202:/root/backups/*-AAAAMMDD-HHMM*" $dest   # troque pelo STAMP que apareceu
Get-ChildItem $dest
```

> ⚠️ **Esses arquivos são tão sensíveis quanto o banco.** O `.env` traz todas as chaves, e enquanto a `ENCRYPTION_KEY` antiga estiver no git (até o C5), qualquer um que tenha o `.bak` abre os campos cifrados. Guarde numa pasta que não sincronize com serviço público e apague as cópias antigas depois do C5.

### G2. Testar a restauração no seu PC 🧑

Um backup só vale se restaurar. Faça num SQL Server **temporário**, separado do banco de desenvolvimento:

```powershell
$bak = Get-ChildItem "$env:USERPROFILE\Documents\Ofertio-backups\Ofertio-*.bak.gz" | Sort-Object Name | Select-Object -Last 1
# Descompactar: o tar do Windows 10 não abre .gz sozinho; use o 7-Zip ou rode no Git Bash: gunzip -k "<arquivo>"
$senha = "Teste#Restore2026"   # só para este container temporário

docker run -d --name ofertio-restore-test -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=$senha" mcr.microsoft.com/mssql/server:2022-latest
Start-Sleep -Seconds 20       # tempo para o SQL Server iniciar
docker cp "<caminho do .bak descompactado>" ofertio-restore-test:/tmp/teste.bak
docker exec -u 0 ofertio-restore-test chown mssql /tmp/teste.bak

# 1) Descobrir os nomes lógicos dos arquivos (colunas LogicalName):
docker exec ofertio-restore-test /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $senha -C -Q "RESTORE FILELISTONLY FROM DISK='/tmp/teste.bak'"

# 2) Restaurar (troque <DADOS> e <LOG> pelos LogicalName do passo 1):
docker exec ofertio-restore-test /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $senha -C -Q "RESTORE DATABASE [OfertioRestore] FROM DISK='/tmp/teste.bak' WITH MOVE '<DADOS>' TO '/var/opt/mssql/data/OfertioRestore.mdf', MOVE '<LOG>' TO '/var/opt/mssql/data/OfertioRestore.ldf'"

# 3) Conferir que os dados estão lá:
docker exec ofertio-restore-test /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $senha -C -Q "SELECT COUNT(*) AS Usuarios FROM OfertioRestore.dbo.Usuarios; SELECT COUNT(*) AS Tenants FROM OfertioRestore.dbo.Tenants; SELECT TOP 1 MigrationId FROM OfertioRestore.dbo.__EFMigrationsHistory ORDER BY 1 DESC"

# 4) Apagar o container de teste:
docker rm -f ofertio-restore-test
```

**Esperado:** números parecidos com os da produção e a última migration igual à da VPS.

### G3. Backup diário automático na VPS 🧑

Crie o script, que não contém nenhuma senha:

```bash
cat > /root/backup-ofertio.sh <<'SCRIPT'
#!/usr/bin/env bash
# Backup diário do Ofertio: banco + chaves (api_appdata) + .env. Mantém 7 dias em /root/backups.
set -euo pipefail
DEST=/root/backups
STAMP=$(date +%Y%m%d-%H%M)
mkdir -p "$DEST" && chmod 700 "$DEST"

docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "BACKUP DATABASE [Ofertio] TO DISK=N'\''/var/opt/mssql/data/backup-diario.bak'\'' WITH INIT, COPY_ONLY, CHECKSUM"'
docker cp ofertio-db:/var/opt/mssql/data/backup-diario.bak "$DEST/Ofertio-$STAMP.bak"
gzip "$DEST/Ofertio-$STAMP.bak"

docker run --rm -v ofertio_api_appdata:/data:ro -v "$DEST":/out alpine tar czf "/out/api_appdata-$STAMP.tgz" -C /data .
cp /var/www/ofertio/.env "$DEST/env-$STAMP"
chmod 600 "$DEST"/*

find "$DEST" -type f -mtime +7 -delete
echo "$(date -Is) backup OK $STAMP ($(du -sh "$DEST" | cut -f1) no total)"
SCRIPT
chmod 700 /root/backup-ofertio.sh
/root/backup-ofertio.sh        # primeira execução manual: tem que terminar com "backup OK"
```

Agende no **cron**, o agendador de tarefas do Linux:

```bash
crontab -e
# acrescente a linha abaixo. A VPS usa UTC, então 03:30 UTC = 00:30 em Brasília:
30 3 * * * /root/backup-ofertio.sh >> /var/log/ofertio-backup.log 2>&1
```

No dia seguinte, confira com `tail -3 /var/log/ofertio-backup.log` e `ls -lh /root/backups`.

### G4. Cópia diária para fora da VPS 🧑 (decisão sua)

O G3 protege contra erro humano e migration ruim, mas não contra perder a VPS (conta suspensa, disco, invasão). Escolha um destino:

| Opção | Custo | Como |
|---|---|---|
| **Seu PC, puxando por agendamento** | grátis | Tarefa agendada do Windows rodando o `scp` do G1 todo dia. Depende do PC ligado. |
| **Storage Box da Hetzner** (mesmo provedor da VPS) | baixo | `rsync`/`scp` por SSH no fim do script do G3. |
| **Backblaze B2 / Google Drive via `rclone`** | baixo/grátis | Instala o `rclone` na VPS e acrescenta `rclone copy` no script. |

Me diga qual prefere e eu escrevo os comandos exatos. Se o destino for um serviço de nuvem, recomendo cifrar o arquivo antes (`gpg -c`), porque ele contém o `.env`.

**Pronto quando:** existe backup de ontem na VPS **e** fora dela, e a restauração do G2 funcionou. Aí marque o item "Backup de produção" do [CHECKLIST_ATIVO.md](../CHECKLIST_ATIVO.md).

---

## Bloco C: troca de segredos

### O que está exposto e o que cada um protege

| Segredo | Onde está exposto | O que um atacante faz com ele | Onde vive em produção |
|---|---|---|---|
| `JWT_KEY` (`4F7A****`) | `appsettings*.json` versionados, igual ao `.env` local | **Forja login de qualquer usuário, inclusive Master** | `.env` da VPS |
| `ENCRYPTION_KEY` (`z4wT****`) | `appsettings*.json` versionados, igual ao `.env` local | Com uma cópia do banco ou de um backup, abre senhas (hash), tokens e o cofre | `.env` da VPS |
| Segredo dos webhooks (`tecf****`) | `Ofertio.API/appsettings.json:149` | Injeta mensagens falsas de Telegram/WhatsApp e no webhook social | **appsettings versionado** (o compose não sobrescreve) |
| OpenAI (`sk-p****`) | `comandos.txt:155` | Gasta na sua conta OpenAI | painel da OpenAI |
| Client secret TikTok (`nvPN****`) | `comandos.txt:160` | Age como o seu app no TikTok | painel TikTok for Developers |
| Google OAuth secret | conversa de 23/11 ([CHECKLIST_ATIVO.md:73](../CHECKLIST_ATIVO.md)) | Faz se passar pelo login Google do Ofertio | `.env` da VPS |
| Evolution ApiKey (`4233****`) | `appsettings*.json` | Controla a Evolution (WhatsApp) local/homologação | só local (a produção não roda Evolution) |
| Senha Postgres (`tecb****`) | appsettings do Orquestrador/Worker | Acessa esse Postgres, se estiver exposto | só local |

> **Correção do relatório:** o `LegacyCredentialReEncryptService` só cifra valores que estão em texto puro; ele **não troca de chave**. A troca da `ENCRYPTION_KEY` precisa de uma ferramenta nova (C5).

### C1. Revogar chaves de terceiros 🧑 (pode fazer já, sem deploy)

1. **OpenAI.** Em platform.openai.com → *API keys*, revogue a chave chamada `auto****`. Confira em *Usage* se houve consumo que você não reconhece. Se o Ofertio usa OpenAI em produção, gere uma chave nova e cadastre no **Cofre** do Master (não em arquivo).
2. **TikTok for Developers.** No app cujo client key começa com `awmv`, use *Regenerate* no client secret. Se a integração TikTok estiver ativa, atualize o segredo novo onde ele estiver configurado (Cofre / Integrações globais do Master).
3. **Google OAuth** (pendência do checklist). Em console.cloud.google.com → *APIs e serviços* → *Credenciais* → cliente OAuth do Ofertio, adicione um segredo novo. Depois:
   ```bash
   cd /var/www/ofertio && cp .env .env.antes-google
   read -rsp "Novo GOOGLE_CLIENT_SECRET: " NOVO; echo     # digita sem aparecer na tela
   sed -i "s|^GOOGLE_CLIENT_SECRET=.*|GOOGLE_CLIENT_SECRET=$NOVO|" .env; unset NOVO
   docker compose $C up -d api webui    # o compose percebe a mudança no .env e recria só esses dois
   ```
   Teste o login com Google e então **desative o segredo antigo** no console do Google. Atualize também o `.env` do seu PC se você usa login Google em dev/homologação.
4. **Shopee (PartnerKey `tecf****`).** Se for uma chave real (não de sandbox), gere outra no Shopee Open Platform.

### C2. Código: tirar os segredos do git 🤖

Faço numa branch `fix/seguranca-c-segredos`, com testes:
- Esvaziar `Jwt:Key/Secret`, `Encryption:Key`, `Integrations:Webhook:Secret`, `EvolutionApi:ApiKey` e senhas de connection string em **todos** os `appsettings*.json` (API, WebUi, Orquestrador, Worker). A WebUi nem usa a chave JWT, então ela sai de lá de vez.
- `docker-compose.yml`: acrescentar `Integrations__Webhook__Secret: ${WEBHOOK_SECRET}` na API.
- `docker-compose.local.yml`: acrescentar `EvolutionApi__ApiKey: ${EVOLUTION_API_KEY}` na API, para a API local usar a mesma chave da Evolution do `.env`.
- `.env.example`: documentar `WEBHOOK_SECRET`.
- Apagar `comandos.txt`, os `test_*.txt` e o `temp_replace_orquestrador.ps1` do repositório. **Antes, copie para fora do repo o que quiser guardar de `comandos.txt`.**
- Completar o `.gitignore` (`.env.*`, `secrets.json`, `appsettings.*.local.json`, `*.pem`, `*.key`).
- Criar a ferramenta do C5.

> ⚠️ Esvaziar o `appsettings.json` exige que o seu `.env` **local** tenha `JWT_KEY` e `ENCRYPTION_KEY` (já tem) e que quem roda sem Docker use User Secrets. Vou deixar isso descrito no [EXECUCAO_DESENVOLVIMENTO.md](../EXECUCAO_DESENVOLVIMENTO.md).

Este passo é deploy normal ([EXECUCAO_PRODUCAO.md](../EXECUCAO_PRODUCAO.md)), **depois** de acrescentar `WEBHOOK_SECRET` ao `.env` da VPS (C4, passo 1). Sem ele, os webhooks passam a recusar tudo.

### C3. Trocar a `JWT_KEY` 🧑

É a troca mais urgente: com a chave atual, qualquer pessoa com acesso ao repositório forja um login Master. É também simples, porque nada é gravado com ela; só as sessões caem.

```bash
cd /var/www/ofertio && cp .env .env.antes-jwt
NOVA=$(openssl rand -base64 64 | tr -d '\n')
sed -i "s|^JWT_KEY=.*|JWT_KEY=$NOVA|" .env; unset NOVA
grep -c '^JWT_KEY=.\{80,\}' .env      # esperado: 1 (chave longa presente, sem mostrar o valor)

# A API passa a assinar com a chave nova. A WebUi é recriada para descartar os cookies de sessão
# (ela não guarda as chaves do cookie em volume), assim ninguém fica preso com um token antigo.
docker compose $C up -d api
docker compose $C up -d --force-recreate webui
```

**Conferir:**
- Entrar com e-mail/senha e com Google funciona.
- Uma aba aberta antes da troca vai para a tela de login (ou mostra erro até sair e entrar de novo).
- `docker logs ofertio-api --tail 100 | grep -iE "IDX10|SecurityToken"` pode mostrar recusas de tokens antigos logo após a troca. Isso é esperado.

**No seu PC:** gere uma `JWT_KEY` **diferente** para o `.env` local. Desenvolvimento e homologação nunca devem usar a chave da produção.

**Voltar atrás:** `cp .env.antes-jwt .env && docker compose $C up -d api && docker compose $C up -d --force-recreate webui`. Depois de validar, apague `.env.antes-*`.

### C4. Trocar o segredo dos webhooks 🧑

O segredo é o mesmo para Telegram, WhatsApp e o webhook social. O Telegram guarda o segredo **no momento em que o bot é conectado** (`secret_token`). Por isso, depois da troca, os bots conectados param de entregar mensagens até serem conectados de novo.

1. **Quantos bots existem:**
   ```bash
   docker exec ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -h -1 -W -Q "SELECT COUNT(*) FROM Ofertio.dbo.TelegramIntegrations WHERE IsActive = 1 AND Token IS NOT NULL"'
   ```
2. **Gravar o segredo novo no `.env`** (antes do deploy do C2):
   ```bash
   cd /var/www/ofertio && cp .env .env.antes-webhook
   echo "WEBHOOK_SECRET=$(openssl rand -hex 32)" >> .env
   ```
3. **Deploy do C2** (que passa a ler `${WEBHOOK_SECRET}`).
4. **Reconectar os bots:** cada cliente com bot ativo abre *Integrações → Telegram* e clica em **Conectar** de novo, o que registra o webhook com o segredo novo. Se forem poucos, avise-os. Se forem muitos, posso fazer o reregistro automático na subida da API (me peça).
5. **Conferir:** mande uma mensagem para um bot conectado e veja `docker logs ofertio-api --tail 50 | grep -i telegram`. Um `401` nesse log significa um bot que ainda não foi reconectado.

**Local/homologação:** no `.env` do PC, troque também o `EVOLUTION_API_KEY` (a Evolution local passa a exigir o valor novo, e a API lê o mesmo valor depois do C2) e defina um `WEBHOOK_SECRET` próprio.

### C5. Trocar a `ENCRYPTION_KEY` 🤖 + 🧑

**Por que é delicado:** cerca de 25 colunas em 12 tabelas são gravadas cifradas com essa chave (prefixo `ENC1:`): `PasswordHash` dos usuários, tokens TikTok, CPF/CNPJ (`UserProfile.Document`), tokens de marketplace e das lojas, integrações de Telegram e WhatsApp, **o Cofre (`SystemSettings`)**, contas de desenvolvedor e tokens globais. Se a API subir com a chave nova sem os dados recifrados, ninguém consegue entrar com senha e o cofre fica ilegível. Os tokens `DP1:` (Data Protection) não dependem dessa chave.

**Por que não é tão urgente quanto o C3:** só serve para quem já tem uma cópia do banco. Enquanto isso, proteja os backups (aviso do G1).

**O que eu vou construir (C2):** um comando da própria API, `--rotate-encryption-key`, no mesmo estilo do `--reset-user-password` que já existe:
- Recebe a chave antiga e a nova por variável de ambiente, nunca por argumento, para não ficar no histórico do shell.
- Tem modo `--dry-run`, que só conta quantos valores de cada coluna seriam recifrados e confere que **todos** abrem com a chave antiga.
- Recifra tudo numa única transação: se um único valor falhar, nada é gravado.
- Vem com testes: recifragem de cada coluna, falha no meio sem gravar nada, e valor que já está com a chave nova.

**Execução na VPS (depois que a ferramenta estiver em produção):**
```bash
cd /var/www/ofertio
/root/backup-ofertio.sh                                  # 1) backup imediatamente antes
cp .env .env.antes-encryption
NOVA=$(openssl rand -base64 32)                          # 32 bytes = AES-256, formato exigido

docker compose $C stop api                               # 2) ninguém grava durante a troca (~2 min fora)
docker compose $C run --rm \
  -e ROTATE_OLD_KEY="$(grep '^ENCRYPTION_KEY=' .env | cut -d= -f2-)" \
  -e ROTATE_NEW_KEY="$NOVA" \
  api --rotate-encryption-key --dry-run                  # 3) tem que dizer "todos os valores abrem"
# 4) mesmo comando sem --dry-run. Só siga se terminar com sucesso.

sed -i "s|^ENCRYPTION_KEY=.*|ENCRYPTION_KEY=$NOVA|" .env; unset NOVA   # 5) só depois do passo 4
docker compose $C up -d api
```

`docker compose run --rm api ...` sobe um container **temporário** da API, com a mesma configuração e a mesma rede, só para rodar o comando, e o apaga no fim. O container normal da API continua parado.

**Conferir:** entrar com e-mail/senha; abrir o Cofre no Master e ver os valores; enviar um teste pelo Telegram; `docker logs ofertio-api --tail 200 | grep -iE "CryptographicException|Não foi possível abrir"` sem nenhum resultado.

**Voltar atrás:** `cp .env.antes-encryption .env` e restaurar o backup do passo 1 (o banco já foi regravado com a chave nova).

**Depois:** os backups anteriores ao C5 continuam abríveis com a chave antiga, que é pública no git. Apague-os quando o primeiro backup pós-troca estiver validado (G2).

### C6. Senhas de infraestrutura 🧑 (pendências do [CHECKLIST_ATIVO.md:73](../CHECKLIST_ATIVO.md))

1. **Root da VPS:** `passwd` na VPS. Melhor ainda: entrar só com chave SSH e desligar o login por senha (`PasswordAuthentication no` em `/etc/ssh/sshd_config`, depois `systemctl reload ssh`). **Teste o login por chave numa segunda janela antes de fechar a primeira.**
2. **SA do SQL Server.** A variável `MSSQL_SA_PASSWORD` só vale na **primeira** criação do banco; mudar o `.env` não muda a senha. A troca é feita no SQL e depois no `.env`:
   ```bash
   cd /var/www/ofertio && cp .env .env.antes-sa
   # Símbolos permitidos: ! @ # % * - _ + = . (aspas, ; & | \ e $ quebram os comandos abaixo)
   read -rsp "Nova senha do SA (8+ com maiúscula, minúscula, número e símbolo): " NOVA; echo
   docker exec -e NOVA="$NOVA" ofertio-db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "ALTER LOGIN sa WITH PASSWORD = N'\''$NOVA'\''"'
   sed -i "s|^MSSQL_SA_PASSWORD=.*|MSSQL_SA_PASSWORD=$NOVA|" .env
   sed -i -E "s|(^SQL_CONNECTION=.*Password=)[^;]*|\1$NOVA|" .env; unset NOVA
   docker compose $C up -d sqlserver api
   ```
   Confira que a API sobe sem `SqlException` (fase 4 do EXECUCAO_PRODUCAO).
3. **RabbitMQ:** o container do RabbitMQ não tem volume, então ele relê usuário e senha do `.env` sempre que é recriado:
   ```bash
   cd /var/www/ofertio && cp .env .env.antes-rabbit
   sed -i "s|^RABBITMQ_USER=.*|RABBITMQ_USER=ofertio|" .env
   sed -i "s|^RABBITMQ_PASSWORD=.*|RABBITMQ_PASSWORD=$(openssl rand -hex 24)|" .env
   docker compose $C up -d rabbitmq api
   docker logs ofertio-api --tail 100 | grep "Bus started"     # esperado: Bus started: rabbitmq://rabbitmq/
   ```
4. Apague `/root/ofertio-config-backup`, que contém senhas antigas (checklist), e os `.env.antes-*` depois de tudo validado.

### C7. Limpar o histórico do git 🧑+🤖 (opcional, por último)

Depois do C1 ao C6, os valores antigos não servem mais para nada, então limpar o histórico é higiene, não urgência. Exige a ferramenta `git filter-repo` (precisa instalar; eu peço antes), um **push forçado** e que todo clone, inclusive o da VPS, seja refeito. Se o repositório já foi público ou tem forks, os valores antigos continuam nesses lugares. Por isso o que protege de verdade é a troca, não a limpeza.

---

## Checklist final

- [ ] G1 backup manual feito e copiado para o PC
- [ ] G2 restauração testada
- [ ] G3 backup diário no cron, com log de ontem
- [ ] G4 cópia fora da VPS
- [ ] C1 OpenAI, TikTok e Google revogados/trocados (Shopee se for real)
- [ ] C2 branch de código mergeada e publicada
- [ ] C3 `JWT_KEY` nova em produção (e outra no `.env` local)
- [ ] C4 `WEBHOOK_SECRET` em produção e bots do Telegram reconectados
- [ ] C5 `ENCRYPTION_KEY` trocada com a ferramenta, backups antigos apagados
- [ ] C6 root, SA e RabbitMQ trocados; `/root/ofertio-config-backup` e `.env.antes-*` apagados
- [ ] C7 (opcional) histórico do git limpo
