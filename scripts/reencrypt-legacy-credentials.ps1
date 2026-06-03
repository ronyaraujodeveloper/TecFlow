# Re-criptografa senhas/tokens legados (plaintext) para ENC1: no PostgreSQL.
# Uso:
#   .\scripts\reencrypt-legacy-credentials.ps1              # aplica alterações
#   .\scripts\reencrypt-legacy-credentials.ps1 -DryRun      # apenas simula

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

$argsList = @("--reencrypt-credentials")
if ($DryRun) {
    $argsList += "--dry-run"
}

Push-Location $repoRoot
try {
    dotnet run --project "TecFlow.Orquestrador\TecFlow.Orquestrador.csproj" --no-launch-profile -- @argsList
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
