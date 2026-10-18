#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Cria pastas de log IIS e concede escrita para Ofertio.API e Ofertio.WebUi.

.DESCRIPTION
    Prepara C:\inetpub\ofertio\api\logs e C:\inetpub\ofertio\webui\logs para:
    - stdout do ASP.NET Core Module (web.config: stdoutLogFile=".\logs\stdout")
    - arquivos Serilog (logs/app-*.txt) quando configurados no host .NET

    Concede FullControl a IIS_IUSRS, DefaultAppPool e aos app pools dedicados.

.EXAMPLE
    .\Configurar-Logs-IIS.ps1
    .\Configurar-Logs-IIS.ps1 -ApiPublishPath "D:\sites\ofertio\api"
#>
[CmdletBinding()]
param(
    [string] $ApiPublishPath = "C:\inetpub\ofertio\api",
    [string] $WebUiPublishPath = "C:\inetpub\ofertio\webui",
    [string] $ApiAppPoolName = "OfertioApiPool",
    [string] $WebUiAppPoolName = "OfertioWebUiPool"
)

$ErrorActionPreference = "Stop"

function Set-OfertioLogFolderPermissions {
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

Write-Host "Configurando logs IIS Ofertio..." -ForegroundColor Cyan

$sites = @(
    @{ Label = "Ofertio.API"; PublishPath = $ApiPublishPath; AppPool = $ApiAppPoolName },
    @{ Label = "Ofertio.WebUi"; PublishPath = $WebUiPublishPath; AppPool = $WebUiAppPoolName }
)

foreach ($site in $sites) {
    if (-not (Test-Path $site.PublishPath)) {
        Write-Warning "Publicacao nao encontrada ($($site.Label)): $($site.PublishPath) - pasta de logs sera criada mesmo assim."
    }

    $logsPath = Join-Path $site.PublishPath "logs"
    Write-Host "`n[$($site.Label)] $logsPath" -ForegroundColor Yellow
    Set-OfertioLogFolderPermissions -LogsPath $logsPath -AppPoolNames @($site.AppPool)

    if ($site.Label -eq "Ofertio.API") {
        $sessionsPath = Join-Path $site.PublishPath "App_Data\telegram-sessions"
        Write-Host "`n[$($site.Label)] $sessionsPath" -ForegroundColor Yellow
        Set-OfertioLogFolderPermissions -LogsPath $sessionsPath -AppPoolNames @($site.AppPool)

        $durableSessions = "C:\ProgramData\Ofertio\telegram-sessions"
        Write-Host "`n[$($site.Label)] $durableSessions" -ForegroundColor Yellow
        Set-OfertioLogFolderPermissions -LogsPath $durableSessions -AppPoolNames @($site.AppPool)
    }
}

Write-Host "`nLogs IIS configurados com sucesso." -ForegroundColor Cyan
