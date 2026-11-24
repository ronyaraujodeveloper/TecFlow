# 🚀 Script de Automação de Deploy e Versionamento (Homologação)

> ⛔ **ARQUIVADO — não executar.** O roteiro vigente da homologação é [`../EXECUCAO_HOMOLOGACAO.md`](../EXECUCAO_HOMOLOGACAO.md) (Docker, com testes e o atalho `Publicar-Homologacao-Docker.ps1`). Se pedirem para rodar este arquivo, use o vigente e avise.

> ⚠️ **Instruções para o Cursor Agent:** Execute as fases abaixo de forma sequencial no terminal integrado do PowerShell sempre que o usuário solicitar a finalização e publicação de uma etapa.

*"Rode completo de ponta a ponta sem interrupções."*

> A homologação (`https://dev.ofertio.com.br`) roda em **Docker** nesta máquina, com o mesmo `docker-compose.yml` da produção mais os overrides locais (regra em [`../rules/ambientes-docker.md`](../rules/ambientes-docker.md)). O IIS fica parado: ele disputa as portas 80 e 7002 com o Docker.

---

## 🛠️ FASE 1: Limpeza, Normalização e Banco de Dados

1.1. Normalizar Encoding (Garantir UTF-8 com BOM em arquivos editados)
```powershell
$WarningPreference = 'SilentlyContinue';
.\NormalizarEncodingPrecoce.ps1;
```

1.2. Atualizar Migrations do Banco de Dados (SQL Server — `Ofertio`)

O banco é o `Ofertio` no container `ofertio-db`, publicado em `127.0.0.1,1433`. Desenvolvimento e homologação usam o mesmo banco. O `dotnet ef` não lê user-secrets, então a connection string vem do `SQL_CONNECTION` do `.env`, trocando o host `sqlserver` (que só existe na rede Docker) por `127.0.0.1`.

```powershell
$sql = (Get-Content .env | Where-Object { $_ -like 'SQL_CONNECTION=*' }) -replace '^SQL_CONNECTION=', ''
$env:ConnectionStrings__DefaultConnection = $sql -replace 'Server=sqlserver,1433', 'Server=127.0.0.1,1433'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
```
Migrations SQL Server: `Ofertio.Data` + `AppDbContext`. Não usar `Ofertio.Infrastructure` (PostgreSQL). Nunca rodar em base vazia: a migration inicial falha; restaure um `.bak`.

1.3. Recompilar a Solution (Clean e Build em Release)
```powershell
dotnet build -c Release
```

## 📦 FASE 2: Publicação no Docker

2.1. Definir os arquivos do Compose (base de produção + overrides locais)
```powershell
$c = '-f','docker-compose.yml','-f','docker-compose.local.yml','-f','docker-compose.hml.yml','--profile','app'
```

2.2. Gerar as imagens novas da API e da WebUi (equivale ao antigo `dotnet publish` para `C:\inetpub`)
```powershell
docker compose @c build api webui
```

2.3. Subir os containers com as imagens novas (equivale a parar os pools, limpar as pastas e iniciar de novo)
```powershell
docker compose @c up -d
```
> ⚠️ O `up -d` recria só os containers que mudaram. Os **volumes** continuam: banco (`sqlserver_data`), chaves do Data Protection e sessões do Telegram (`api_appdata`), uploads (`uploads`) e WhatsApp (`evolution_pg`). Nunca use `docker compose down -v`: sem as chaves, os tokens do Mercado Livre gravados com `DP1:` ficam ilegíveis.

2.3.1. Reaplicar migrations após o publish e reiniciar a API
```powershell
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
docker compose @c restart api
```

2.4. Túnel do Cloudflare (acesso público pelo `dev.ofertio.com.br`)

O túnel leva `dev.ofertio.com.br` até `https://127.0.0.1:7002` (Caddy). Se o domínio responder **530**, o túnel não está rodando:
```powershell
& "C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --config "$env:USERPROFILE\.cloudflared\config.yml" run
```
**O túnel é um serviço do Windows** (configurado em 23/11/2026) e sobe sozinho com o PC. Se o 530 voltar, confira `Get-Service cloudflared`. Para reconfigurar, use um PowerShell **como administrador**. O `--%` é obrigatório: sem ele, o PowerShell quebra as aspas no `(x86)`.
```powershell
sc.exe --% config cloudflared binPath= "\"C:\Program Files (x86)\cloudflared\cloudflared.exe\" tunnel --config \"C:\Users\rony_\.cloudflared\config.yml\" --protocol http2 run"
Restart-Service cloudflared
```
Se o serviço travar em "Aguardando que o serviço seja parado": Ctrl + C e `Stop-Process -Id (Get-CimInstance Win32_Service -Filter "Name='cloudflared'").ProcessId -Force`.

2.5. Teste de Fumaça (Smoke Test): validar containers, WebUi e API
```powershell
Write-Host "Verificando endpoints pós-deploy..." -ForegroundColor Cyan
docker compose @c ps --format "table {{.Name}}\t{{.Status}}"
(Invoke-WebRequest -Uri "http://localhost/" -UseBasicParsing).StatusCode
try { (Invoke-WebRequest -Uri "http://localhost/api/sistema/avisos" -UseBasicParsing).StatusCode } catch { [int]$_.Exception.Response.StatusCode }
(Invoke-WebRequest -Uri "https://dev.ofertio.com.br/" -UseBasicParsing).StatusCode
```
O esperado: 7 containers `Up` (`ofertio-db`, `ofertio-rabbitmq`, `ofertio-api`, `ofertio-webui`, `ofertio-caddy`, `ofertio-evolution`, `ofertio-evolution-db`); a WebUi responde `200`; a API responde `401` sem login (está no ar); o domínio responde `200`.

2.6. Se algum container reiniciar ou o smoke test falhar, ler o log antes de mudar qualquer coisa
```powershell
docker logs ofertio-api --tail 80
docker logs ofertio-webui --tail 80
```

## 💾 FASE 3: Governança de Código e Versionamento Git

3.1. Adicionar todos os arquivos modificados ao Stage
```powershell
git add .
```
(O `.gitignore` já exclui `.env`, `*.bak`, `*.pfx` e `App_Data`.)

3.2. Realizar Commit Automatizado
# 🤖 INSTRUÇÃO PARA O CURSOR AGENT:
1. Analise o 'git diff' ou 'git status' das alterações deste ciclo.
2. Crie um texto breve, técnico e conciso no padrão Conventional Commits (ex: 'feat(links): adiciona suporte a unshorten do promoby.me').
3. Substitua '[CURSOR: INSIRA AQUI O SEU RESUMO TÉCNICO]' pela mensagem gerada e execute o commit.

```powershell
$CommitMessage = "[CURSOR: INSIRA AQUI O SEU RESUMO TÉCNICO GERADO COM BASE NAS ALTERAÇÕES DETECTADAS]"
git commit -m "$CommitMessage"

```

3.3. Subir para o Repositório Remoto
```powershell
git push origin main
```

## 🔄 FASE 4: Atualização da IDE

4.1. Instrução Manual de Atualização do Cursor

Use o atalho Ctrl + Shift + P

Digite Developer: Reload Window e pressione Enter.
