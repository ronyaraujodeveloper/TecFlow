#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Publica TecFlow.API e/ou TecFlow.WebUi em homologação (IIS) sem erro de arquivo bloqueado.

.DESCRIPTION
    Para os app pools do IIS antes do dotnet publish e reinicia após a cópia.
    Evita MSB3027/MSB3021 quando o Worker Process mantém as DLLs em C:\inetpub\tecflow\*.

.PARAMETER Projeto
    Api, WebUi ou All (padrão).

.EXAMPLE
    .\Publicar-Homologacao.ps1
    .\Publicar-Homologacao.ps1 -Projeto Api
#>
[CmdletBinding()]
param(
    [ValidateSet("Api", "WebUi", "All")]
    [string] $Projeto = "All",

    [string[]] $AppPools = @("TecFlowApiPool", "TecFlowWebUiPool"),

    [string] $Configuration = "Homologacao",

    [string] $ApiOutput = "C:\inetpub\tecflow\api",
    [string] $WebUiOutput = "C:\inetpub\tecflow\webui"
)

$ErrorActionPreference = "Stop"

$SolutionRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }

function Stop-TecFlowAppPools {
    param([string[]] $Names)
    Import-Module WebAdministration -ErrorAction Stop
    foreach ($name in $Names) {
        if (-not (Test-Path "IIS:\AppPools\$name")) {
            Write-Warning "App pool '$name' não encontrado; ignorando."
            continue
        }
        if ((Get-WebAppPoolState -Name $name).Value -eq "Started") {
            Write-Host "Parando app pool: $name"
            Stop-WebAppPool -Name $name
            $deadline = (Get-Date).AddSeconds(30)
            while ((Get-WebAppPoolState -Name $name).Value -ne "Stopped") {
                if ((Get-Date) -gt $deadline) {
                    throw "Timeout ao parar o app pool '$name'."
                }
                Start-Sleep -Milliseconds 250
            }
        }
    }
}

function Start-TecFlowAppPools {
    param([string[]] $Names)
    Import-Module WebAdministration -ErrorAction Stop
    foreach ($name in $Names) {
        if (-not (Test-Path "IIS:\AppPools\$name")) { continue }
        if ((Get-WebAppPoolState -Name $name).Value -ne "Started") {
            Write-Host "Iniciando app pool: $name"
            Start-WebAppPool -Name $name
        }
    }
}

function Publish-TecFlowProject {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProjectPath,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $OutputPath
    )
    if (-not (Test-Path $ProjectPath)) {
        throw "Projeto não encontrado: $ProjectPath"
    }
    Write-Host "Publicando $ProjectPath -> $OutputPath"
    & dotnet publish $ProjectPath -c $Configuration -o $OutputPath --no-self-contained
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish falhou para $ProjectPath (exit $LASTEXITCODE)."
    }
}

$poolsToStop = switch ($Projeto) {
    "Api"    { @("TecFlowApiPool") }
    "WebUi"  { @("TecFlowWebUiPool") }
    default  { $AppPools }
}

$stopped = $false
try {
    Stop-TecFlowAppPools -Names $poolsToStop
    $stopped = $true

    $root = (Resolve-Path $SolutionRoot).Path

    if ($Projeto -in @("Api", "All")) {
        Publish-TecFlowProject `
            -ProjectPath (Join-Path $root "TecFlow.API\TecFlow.API.csproj") `
            -OutputPath $ApiOutput
    }

    if ($Projeto -in @("WebUi", "All")) {
        Publish-TecFlowProject `
            -ProjectPath (Join-Path $root "TecFlow.WebUi\TecFlow.WebUi.csproj") `
            -OutputPath $WebUiOutput
    }

    Write-Host "Publicação concluída com sucesso."
}
finally {
    if ($stopped) {
        Start-TecFlowAppPools -Names $poolsToStop
    }
}
