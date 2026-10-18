#Requires -Version 5.1
<#
.SYNOPSIS
    Cria a pasta de stdout logs do Ofertio.WebUi no IIS e concede escrita ao pool.

.DESCRIPTION
    Habilita captura imediata de falhas Blazor/server-side (ex.: login) via aspNetCore stdoutLogEnabled.
    Execute como Administrador após publicar em C:\inetpub\ofertio\webui.
#>
[CmdletBinding()]
param(
    [string] $LogsPath = "C:\inetpub\ofertio\webui\logs",
    [string] $AppPoolName = "OfertioWebUiPool"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $LogsPath)) {
    New-Item -Path $LogsPath -ItemType Directory -Force | Out-Null
    Write-Host "Pasta criada: $LogsPath"
} else {
    Write-Host "Pasta já existente: $LogsPath"
}

$acl = Get-Acl $LogsPath

$rules = @(
    @{ Identity = "IIS_IUSRS"; Rights = "FullControl" },
    @{ Identity = "IIS AppPool\$AppPoolName"; Rights = "FullControl" },
    @{ Identity = "IIS AppPool\DefaultAppPool"; Rights = "FullControl" }
)

foreach ($rule in $rules) {
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $rule.Identity,
        $rule.Rights,
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow")
    $acl.SetAccessRule($accessRule)
    Write-Host "Permissão $($rule.Rights) concedida para $($rule.Identity)"
}

Set-Acl -Path $LogsPath -AclObject $acl
Write-Host "Logs do WebUi liberados em $LogsPath"
