#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Cria pastas de log IIS e concede escrita para TecFlow.API e TecFlow.WebUi.

.DESCRIPTION
    Prepara C:\inetpub\tecflow\api\logs e C:\inetpub\tecflow\webui\logs para:
    - stdout do ASP.NET Core Module (web.config: stdoutLogFile=".\logs\stdout")
    - arquivos Serilog (logs/app-*.txt) quando configurados no host .NET

    Concede FullControl a IIS_IUSRS, DefaultAppPool e aos app pools dedicados.

.EXAMPLE
    .\Configurar-Logs-IIS.ps1
    .\Configurar-Logs-IIS.ps1 -ApiPublishPath "D:\sites\tecflow\api"
#>
[CmdletBinding()]
param(
    [string] $ApiPublishPath = "C:\inetpub\tecflow\api",
    [string] $WebUiPublishPath = "C:\inetpub\tecflow\webui",
    [string] $ApiAppPoolName = "TecFlowApiPool",
    [string] $WebUiAppPoolName = "TecFlowWebUiPool"
)

$ErrorActionPreference = "Stop"

function Set-TecFlowLogFolderPermissions {
    param(
        [Parameter(Mandatory = $true)]
        [string] $LogsPath,

        [Parameter(Mandatory = $true)]
        [string[]] $AppPoolNames
    )

    if (-not (Test-Path $LogsPath)) {
        New-Item -Path $LogsPath -ItemType Directory -Force | Out-Null
        Write-Host "Pasta criada: $LogsPath" -ForegroundColor Green
    }
    else {
        Write-Host "Pasta já existente: $LogsPath" -ForegroundColor DarkGray
    }

    $identities = @(
        "IIS_IUSRS",
        "IIS AppPool\DefaultAppPool"
    )

    foreach ($pool in $AppPoolNames) {
        if (-not [string]::IsNullOrWhiteSpace($pool)) {
            $identities += "IIS AppPool\$pool"
        }
    }

    $acl = Get-Acl $LogsPath

    foreach ($identity in ($identities | Select-Object -Unique)) {
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $identity,
            "FullControl",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow")
        $acl.SetAccessRule($accessRule)
        Write-Host "  FullControl -> $identity"
    }

    Set-Acl -Path $LogsPath -AclObject $acl
}

Write-Host "Configurando logs IIS TecFlow..." -ForegroundColor Cyan

$sites = @(
    @{ Label = "TecFlow.API"; PublishPath = $ApiPublishPath; AppPool = $ApiAppPoolName },
    @{ Label = "TecFlow.WebUi"; PublishPath = $WebUiPublishPath; AppPool = $WebUiAppPoolName }
)

foreach ($site in $sites) {
    if (-not (Test-Path $site.PublishPath)) {
        Write-Warning "Publicacao nao encontrada ($($site.Label)): $($site.PublishPath) - pasta de logs sera criada mesmo assim."
    }

    $logsPath = Join-Path $site.PublishPath "logs"
    Write-Host "`n[$($site.Label)] $logsPath" -ForegroundColor Yellow
    Set-TecFlowLogFolderPermissions -LogsPath $logsPath -AppPoolNames @($site.AppPool)
}

Write-Host "`nLogs IIS configurados com sucesso." -ForegroundColor Cyan
