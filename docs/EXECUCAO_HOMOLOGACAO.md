Markdown
# 🚀 Script de Automação de Deploy e Versionamento (Homologação)

> ⚠️ **Instruções para o Cursor Agent:** Execute as fases abaixo de forma sequencial no terminal integrado do PowerShell como Administrador sempre que o usuário solicitar a finalização e publicação de uma etapa.

---

## 🛠️ FASE 1: Limpeza, Normalização e Banco de Dados

### 1.1. Normalizar Encoding (Garantir UTF-8 com BOM em arquivos editados)
```powershell
.\NormalizarEncodingPrecoce.ps1

1.2. Atualizar Migrations do Banco de Dados (PostgreSQL)
```powershell

dotnet ef database update --project .\Tecso.Infrastructure\Tecso.Infrastructure.csproj --startup-project .\TecFlow.API\TecFlow.API.csproj -c Homologacao

1.3. Recompilar a Solution (Clean e Build)
```powershell
dotnet clean
dotnet build

📦 FASE 2: Publicação e Deploy Limpo no IIS
2.1. Limpar Resíduos Físicos das Pastas do IIS (Evitar travamento de arquivos velhos)
```powershell
Remove-Item -Path "C:\inetpub\tecflow\api\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "C:\inetpub\tecflow\webui\*" -Recurse -Force -ErrorAction SilentlyContinue

2.2. Gerar Novo Publish das Aplicações em Modo Homologação
PowerShell
dotnet publish .\TecFlow.API\TecFlow.API.csproj -c Homologacao -o C:\inetpub\tecflow\api
dotnet publish .\TecFlow.WebUi\TecFlow.WebUi.csproj -c Homologacao -o C:\inetpub\tecflow\webui

2.3. Reiniciar o Servidor Web IIS
```powershell
iisreset

2.4. Teste de Fumaça (Smoke Test) - Validar se as portas 5000 e 5001 estão online
```powershell
Write-Host "Verificando endpoints pós-deploy..." -ForegroundColor Cyan
(Invoke-WebRequest -Uri "http://localhost:5001/metrics" -UseBasicParsing).StatusCode
(Invoke-WebRequest -Uri "http://localhost:5000/TecFlow.WebUi" -UseBasicParsing).StatusCode

💾 FASE 3: Governança de Código e Versionamento Git
3.1. Adicionar todos os arquivos modificados ao Stage
```powershell
git add .

3.2. Realizar Commit Automatizado
# 🤖 INSTRUÇÃO PARA O CURSOR AGENT: Antes de executar a linha abaixo, analise o 'git diff' ou o status dos arquivos modificados neste ciclo, crie um texto breve, técnico e conciso resumindo as alterações usando o padrão Conventional Commits (ex: 'feat(homolog): descrição') e substitua o texto explicativo abaixo pelo seu resumo gerado automaticamente.

```powershell
git commit -m "[CURSOR: INSIRA AQUI O SEU RESUMO TÉCNICO GERADO COM BASE NAS ALTERAÇÕES DETECTADAS]"

3.3. Subir para o Repositório Remoto
PowerShell
git push origin main
🔄 FASE 4: Atualização da IDE

4.1. Instrução Manual de Atualização do Cursor
Use o atalho Ctrl + Shift + P

Digite Developer: Reload Window e pressione Enter.