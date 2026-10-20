# 🚀 Script de Automação de Deploy e Versionamento (Homologação)

> ⚠️ **Instruções para o Cursor Agent:** Execute as fases abaixo de forma sequencial no terminal integrado do PowerShell como Administrador sempre que o usuário solicitar a finalização e publicação de uma etapa.

*"Rode completo de ponta a ponta sem interrupções."*

---

## 🛠️ FASE 1: Limpeza, Normalização e Banco de Dados

1.1. Normalizar Encoding (Garantir UTF-8 com BOM em arquivos editados)
```powershell
$WarningPreference = 'SilentlyContinue';
.\NormalizarEncodingPrecoce.ps1;
```

1.2. Atualizar Migrations do Banco de Dados (SQL Server — `Ofertio`)

O IIS (`web.config` → `ASPNETCORE_ENVIRONMENT=Homologacao`) e o `appsettings.Homologacao.json` usam **SQL Server** `localhost\SQLEXPRESS` / `Ofertio` (`Database:Provider=SqlServer`). Cadastro de lojas e conversão de links gravam nesse banco. O runtime garante um registro em `Tenants` (`Tenant Principal`) antes de inserir `MarketplaceAccounts` (FK `FK_MarketplaceAccounts_Tenants_TenantId`).

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
$env:ASPNETCORE_ENVIRONMENT = 'Homologacao'
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
```
Migrations SQL Server: `Ofertio.Data` + `AppDbContext`. Não usar `Ofertio.Infrastructure` (PostgreSQL) para o fluxo de homologação IIS.

1.3. Recompilar a Solution (Clean e Build em Release)
```powershell
dotnet build -c Release
```

## 📦 FASE 2: Publicação e Deploy Limpo no IIS

2.1. Parar os Pools do IIS (Libera as DLLs travadas em memória para o Publish)
```powershell
Import-Module WebAdministration
Stop-WebAppPool -Name "OfertioApiPool"
Stop-WebAppPool -Name "OfertioWebUiPool"
Start-Sleep -Seconds 2
```

2.2. Limpar Resíduos Físicos das Pastas do IIS (preservando `api\App_Data`, onde ficam as chaves do Data Protection)
```powershell
Get-ChildItem -Path "C:\inetpub\ofertio\api" -Force | Where-Object { $_.Name -ne 'App_Data' } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "C:\inetpub\ofertio\webui\*" -Recurse -Force -ErrorAction SilentlyContinue
```
> ⚠️ Nunca apague `C:\inetpub\ofertio\api\App_Data\OfertioDataProtectionKeys`: sem essas chaves os tokens do Mercado Livre gravados com `DP1:` ficam ilegíveis.

2.3. Gerar Novo Publish das Aplicações em Release nas pastas do IIS
```powershell
dotnet publish .\Ofertio.API\Ofertio.API.csproj -c Release -o C:\inetpub\ofertio\api
dotnet publish .\Ofertio.WebUi\Ofertio.WebUi.csproj -c Release -o C:\inetpub\ofertio\webui
```

2.3.1. Reaplicar migrations no SQL Server do IIS após o publish
```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Homologacao'
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
```
Garante que `MarketplaceAccounts`, `ShortAffiliateLinks` e `ShortAffiliateLinkAccounts` existam em `Ofertio` (SQLEXPRESS) usado pelo pool `OfertioApiPool`.

2.4. Iniciar os Pools do IIS e Reciclar o Servidor

A WebUi repassa `/api/mercadolivre/callback` e `/api/mercadolivre/notifications` para a API (YARP). Não pode existir subaplicação `/api` no site `Ofertio.WebUi`: no `OfertioApiPool` ela derruba a API com HTTP 500.35.
```powershell
Import-Module WebAdministration
if (Get-WebApplication -Site "Ofertio.WebUi" -Name "api") { Remove-WebApplication -Site "Ofertio.WebUi" -Name "api" }
Start-WebAppPool -Name "OfertioApiPool"
Start-WebAppPool -Name "OfertioWebUiPool"
iisreset
```

2.5. Teste de Fumaça (Smoke Test) - Validar se as portas 5000 e 5001 estão online
```powershell
Write-Host "Verificando endpoints pós-deploy..." -ForegroundColor Cyan
(Invoke-WebRequest -Uri "http://localhost:5001/metrics" -UseBasicParsing).StatusCode
(Invoke-WebRequest -Uri "http://localhost:5000/" -UseBasicParsing).StatusCode
```

2.6. se houver 404 em assets estáticos.
```powershell
.\LiberarPermissoesWebUi.ps1
.\AjustarPermissoesIIS.ps1
```

## 💾 FASE 3: Governança de Código e Versionamento Git

3.1. Adicionar todos os arquivos modificados ao Stage
```powershell
git add .
```

3.2. Realizar Commit Automatizado
# 🤖 INSTRUÇÃO PARA O CURSOR AGENT:
1. Analise o 'git diff' ou 'git status' das alterações deste ciclo.
2. Crie um texto breve, técnico e conciso no padrão Conventional Commits (ex: 'feat(links): adiciona suporte a unshorten do promoby.me').
3. Antes do commit, registre essa versão no arquivo '\docs\GIT_VERSAO.md' executando:
Add-Content -Path ".\docs\GIT_VERSAO.md" -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [COMMIT_MENSAGEM]"
4. Substitua '[CURSOR: INSIRA AQUI O SEU RESUMO TÉCNICO]' pela mensagem gerada e execute o commit.

```powershell
$CommitMessage = "[CURSOR: INSIRA AQUI O SEU RESUMO TÉCNICO GERADO COM BASE NAS ALTERAÇÕES DETECTADAS]"
if (-not (Test-Path ".\docs")) { New-Item -ItemType Directory -Path ".\docs" }
Add-Content -Path ".\docs\GIT_VERSAO.md" -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')]$CommitMessage"
git add .\docs\GIT_VERSAO.md
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
