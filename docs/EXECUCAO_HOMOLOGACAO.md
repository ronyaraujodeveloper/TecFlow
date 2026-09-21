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

1.2. Atualizar Migrations do Banco de Dados (SQL Server — `AutomacaoSociais`)
```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project .\TecFlow.Data\TecFlow.Data.csproj --startup-project .\TecFlow.API\TecFlow.API.csproj --context AppDbContext
```
O `AppDbContextFactory` lê `Database:Provider=SqlServer` e `ConnectionStrings:DefaultConnection` da `TecFlow.API` (`Server=localhost\SQLEXPRESS;Database=AutomacaoSociais;Integrated Security=True;TrustServerCertificate=True;`). As migrations SQL Server vivem em `TecFlow.Data` (não usar `Tecso.Infrastructure`, `TecFlow.Infrastructure` nem `-c Homologacao` como contexto).

1.3. Recompilar a Solution (Clean e Build em Release)
```powershell
dotnet build -c Release
```

## 📦 FASE 2: Publicação e Deploy Limpo no IIS

2.1. Parar os Pools do IIS (Libera as DLLs travadas em memória para o Publish)
```powershell
Import-Module WebAdministration
Stop-WebAppPool -Name "TecFlowApiPool"
Stop-WebAppPool -Name "TecFlowWebUiPool"
Start-Sleep -Seconds 2
```

2.2. Limpar Resíduos Físicos das Pastas do IIS
```powershell
Remove-Item -Path "C:\inetpub\tecflow\api\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "C:\inetpub\tecflow\webui\*" -Recurse -Force -ErrorAction SilentlyContinue
```

2.3. Gerar Novo Publish das Aplicações em Release nas pastas do IIS
```powershell
dotnet publish .\TecFlow.API\TecFlow.API.csproj -c Release -o C:\inetpub\tecflow\api
dotnet publish .\TecFlow.WebUi\TecFlow.WebUi.csproj -c Release -o C:\inetpub\tecflow\webui
```

2.4. Iniciar os Pools do IIS e Reciclar o Servidor
```powershell
Import-Module WebAdministration
Start-WebAppPool -Name "TecFlowApiPool"
Start-WebAppPool -Name "TecFlowWebUiPool"
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
# 🤖 INSTRUÇÃO PARA O CURSOR AGENT: Antes de executar a linha abaixo, analise o 'git diff' ou o status dos arquivos modificados neste ciclo, crie um texto breve, técnico e conciso resumindo as alterações usando o padrão Conventional Commits (ex: 'feat(homolog): descrição') e substitua o texto explicativo abaixo pelo seu resumo gerado automaticamente.

```powershell
git commit -m "[CURSOR: INSIRA AQUI O SEU RESUMO TÉCNICO GERADO COM BASE NAS ALTERAÇÕES DETECTADAS]"
```

3.3. Subir para o Repositório Remoto
```powershell
git push origin main
```

## 🔄 FASE 4: Atualização da IDE

4.1. Instrução Manual de Atualização do Cursor

Use o atalho Ctrl + Shift + P

Digite Developer: Reload Window e pressione Enter.
