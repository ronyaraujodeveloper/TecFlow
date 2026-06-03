# Setup PostgreSQL local (Windows)
# Requer: PostgreSQL instalado OU Docker Desktop

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

Write-Host "=== TecFlow PostgreSQL Setup ===" -ForegroundColor Cyan

if (Get-Command docker -ErrorAction SilentlyContinue) {
    Write-Host "A iniciar PostgreSQL via Docker..." -ForegroundColor Yellow
    docker compose -f "$Root\TecFlow.Orquestrador\docker-compose.yml" up postgres -d
    Start-Sleep -Seconds 8
}
elseif (Get-Command psql -ErrorAction SilentlyContinue) {
    Write-Host "A criar role/base via psql..." -ForegroundColor Yellow
    psql -U postgres -f "$Root\scripts\setup-postgresql.sql"
}
else {
    Write-Host "Instale PostgreSQL (https://www.postgresql.org/download/) ou Docker Desktop." -ForegroundColor Red
    Write-Host "Depois execute novamente este script." -ForegroundColor Red
    exit 1
}

Write-Host "A aplicar migrations..." -ForegroundColor Yellow
dotnet ef database update `
    --project "$Root\TecFlow.Infrastructure\TecFlow.Infrastructure.csproj" `
    --startup-project "$Root\TecFlow.Orquestrador\TecFlow.Orquestrador.csproj"

if (Get-Command psql -ErrorAction SilentlyContinue) {
    Write-Host "A executar seed SQL..." -ForegroundColor Yellow
    $env:PGPASSWORD = "tecban321@"
    psql -U Us_Automacao -d automacaosociais -f "$Root\scripts\seed-dashboard-demo.postgresql.sql"
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
else {
    Write-Host "Seed SQL ignorado (psql não encontrado). O Orquestrador insere dados demo em Development no primeiro arranque." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Concluído! Login: demo@TecFlow.local / Test@123" -ForegroundColor Green
Write-Host "Connection: Host=localhost;Port=5432;Database=automacaosociais;Username=Us_Automacao;Password=tecban321@" -ForegroundColor Green
