<#
.SYNOPSIS
    Reset limpo das ofertas capturadas (Telegram/WhatsApp) e das fotos em uploads\products.

.DESCRIPTION
    Apaga as linhas de GroupCapturedMessages e o conteúdo de <UploadsRoot>\products para reconectar os grupos
    do zero com as regras atuais de resolução de links (bypass Meli Social + matt_tool oficial).
    Conexões, grupos monitorados, lojas e usuários são preservados.

    -UserId limita a limpeza a um usuário (pasta uploads\products\<UserId>).
    -IncluirHistoricoPrecos também zera ProductPriceHistory (global; ignorado quando -UserId é informado).
    -PararIis para os pools Ofertio antes da limpeza (evita o UserBot gravar no meio) e os inicia no final.
    -WhatIf mostra o que seria apagado sem alterar nada.

.EXAMPLE
    .\ResetarOfertasLegadas.ps1 -PararIis
.EXAMPLE
    .\ResetarOfertasLegadas.ps1 -UserId 1 -Force
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Server = 'localhost\SQLEXPRESS',
    [string]$Database = 'Ofertio',
    [string]$UploadsRoot = 'C:\inetpub\ofertio\uploads',
    [int]$UserId = 0,
    [switch]$IncluirHistoricoPrecos,
    [switch]$PararIis,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$pools = @('OfertioApiPool', 'OfertioWebUiPool')

function Invoke-Sql([string]$query) {
    $output = sqlcmd -S $Server -d $Database -E -b -W -h -1 -f 65001 -Q "SET NOCOUNT ON; $query"
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd falhou ($LASTEXITCODE): $output"
    }
    return ($output | Where-Object { $_ -and $_.Trim() }) -join ' '
}

$filter = if ($UserId -gt 0) { "WHERE UserId = $UserId" } else { '' }
$productsFolder = Join-Path $UploadsRoot 'products'
$targetFolder = if ($UserId -gt 0) { Join-Path $productsFolder $UserId } else { $productsFolder }
$clearHistory = $IncluirHistoricoPrecos -and $UserId -le 0

$offers = Invoke-Sql "SELECT COUNT(*) FROM GroupCapturedMessages $filter"
$files = if (Test-Path $targetFolder) { (Get-ChildItem $targetFolder -Recurse -File | Measure-Object).Count } else { 0 }
$history = if ($clearHistory) { Invoke-Sql 'SELECT COUNT(*) FROM ProductPriceHistory' } else { '-' }

Write-Host "Banco: $Server / $Database"
Write-Host "Escopo: $(if ($UserId -gt 0) { "UserId $UserId" } else { 'todos os usuários' })"
Write-Host "Ofertas (GroupCapturedMessages): $offers"
Write-Host "Arquivos em ${targetFolder}: $files"
Write-Host "Histórico de preços (ProductPriceHistory): $history"

if ($WhatIfPreference) {
    Write-Host 'WhatIf: nada foi alterado.'
    return
}

if (-not $Force) {
    $answer = Read-Host 'Digite RESETAR para confirmar a limpeza'
    if ($answer -cne 'RESETAR') {
        Write-Host 'Cancelado.'
        return
    }
}

if ($PararIis) {
    Import-Module WebAdministration
    foreach ($pool in $pools) {
        if ((Get-WebAppPoolState -Name $pool).Value -ne 'Stopped') {
            Stop-WebAppPool -Name $pool
        }
    }
    Start-Sleep -Seconds 8
}

try {
    $deleted = Invoke-Sql "DELETE FROM GroupCapturedMessages $filter; SELECT @@ROWCOUNT"
    Write-Host "Ofertas removidas: $deleted"

    if ($clearHistory) {
        $deletedHistory = Invoke-Sql 'DELETE FROM ProductPriceHistory; SELECT @@ROWCOUNT'
        Write-Host "Histórico de preços removido: $deletedHistory"
    }

    if (Test-Path $targetFolder) {
        Get-ChildItem $targetFolder -Force | Remove-Item -Recurse -Force
    }
    else {
        New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
    }
    Write-Host "Pasta limpa: $targetFolder"
}
finally {
    if ($PararIis) {
        foreach ($pool in $pools) {
            Start-WebAppPool -Name $pool
        }
        Write-Host 'Pools Ofertio iniciados.'
    }
}

Write-Host 'Reset concluído. Use "Sincronizar grupos" em Grupos Monitorados para recapturar o histórico com as regras atuais.'
