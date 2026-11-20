<#
.SYNOPSIS
    Publica a homologação (dev.ofertio.com.br) em Docker, espelhando a produção.

.DESCRIPTION
    Roteiro completo em docs/EXECUCAO_HOMOLOGACAO.md. Regra: docs/rules/ambientes-docker.md.
    1. Normaliza o encoding.  2. Build e testes (opcional: -SemTestes).
    3. Build das imagens api/webui.  4. Sobe todos os serviços (perfil "app").
    5. Aplica migrations incrementais no banco Ofertio.  6. Smoke test.

.EXAMPLE
    .\Publicar-Homologacao-Docker.ps1
    .\Publicar-Homologacao-Docker.ps1 -SemTestes
#>
param(
    [switch]$SemTestes
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$compose = @('-f', 'docker-compose.yml', '-f', 'docker-compose.local.yml', '-f', 'docker-compose.hml.yml', '--profile', 'app')

if (-not (Test-Path '.env')) {
    throw "Arquivo .env não encontrado. Copie .env.example para .env e preencha (docs/EXECUCAO_HOMOLOGACAO.md, fase 0)."
}

Write-Host "1/6 Normalizando encoding..." -ForegroundColor Cyan
$WarningPreference = 'SilentlyContinue'
.\NormalizarEncodingPrecoce.ps1 *> $null

if (-not $SemTestes) {
    Write-Host "2/6 Build e testes..." -ForegroundColor Cyan
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) { throw "Build falhou." }
    dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw "Testes falharam." }
}

Write-Host "3/6 Build das imagens api e webui..." -ForegroundColor Cyan
docker compose @compose build api webui
if ($LASTEXITCODE -ne 0) { throw "docker compose build falhou." }

Write-Host "4/6 Subindo os containers..." -ForegroundColor Cyan
docker compose @compose up -d
if ($LASTEXITCODE -ne 0) { throw "docker compose up falhou." }

Write-Host "5/6 Migrations incrementais no banco Ofertio (127.0.0.1,1433)..." -ForegroundColor Cyan
$sql = (Get-Content .env | Where-Object { $_ -like 'SQL_CONNECTION=*' }) -replace '^SQL_CONNECTION=', ''
$env:ConnectionStrings__DefaultConnection = $sql -replace 'Server=sqlserver,1433', 'Server=127.0.0.1,1433'   # o dotnet ef não lê user-secrets
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project .\Ofertio.Data\Ofertio.Data.csproj --startup-project .\Ofertio.API\Ofertio.API.csproj --context AppDbContext
if ($LASTEXITCODE -ne 0) { throw "Migration falhou. Não recrie o volume sqlserver_data; leia o erro." }
docker compose @compose restart api | Out-Null

Write-Host "6/6 Smoke test..." -ForegroundColor Cyan
Start-Sleep -Seconds 10
docker compose @compose ps --format "table {{.Name}}\t{{.Status}}"
# WebUi deve responder 200; a API (via Caddy em /api/*) responde 401 sem login, o que já prova que está no ar.
foreach ($url in 'http://localhost/', 'http://localhost/api/sistema/avisos') {
    try {
        $code = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30).StatusCode
    }
    catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { $_.Exception.Message }
    }
    $ok = $code -in 200, 401
    Write-Host "$url -> $code" -ForegroundColor $(if ($ok) { 'Green' } else { 'Yellow' })
}
Write-Host "Homologação publicada. Abra https://dev.ofertio.com.br" -ForegroundColor Cyan
