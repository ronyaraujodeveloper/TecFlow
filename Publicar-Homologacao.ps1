#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Publica Ofertio.API e/ou Ofertio.WebUi em homologação (IIS) sem erro de arquivo bloqueado.

.DESCRIPTION
    Para os app pools do IIS antes do dotnet publish e reinicia após a cópia.
    Evita MSB3027/MSB3021 quando o Worker Process mantém as DLLs em C:\inetpub\ofertio\*.

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

    [string[]] $AppPools = @("OfertioApiPool", "OfertioWebUiPool"),

    [string] $Configuration = "Homologacao",

    [string] $ApiOutput = "C:\inetpub\ofertio\api",
    [string] $WebUiOutput = "C:\inetpub\ofertio\webui"
)

$ErrorActionPreference = "Stop"

$SolutionRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }

function Stop-OfertioAppPools {
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

function Start-OfertioAppPools {
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

function Publish-OfertioProject {
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
    "Api"    { @("OfertioApiPool") }
    "WebUi"  { @("OfertioWebUiPool") }
    default  { $AppPools }
}

$stopped = $false
try {
    Stop-OfertioAppPools -Names $poolsToStop
    $stopped = $true

    $root = (Resolve-Path $SolutionRoot).Path

    Write-Host "Aplicando migrations SQL Server (Development / Ofertio.Data)..."
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    & dotnet ef database update `
        --project (Join-Path $root "Ofertio.Data\Ofertio.Data.csproj") `
        --startup-project (Join-Path $root "Ofertio.API\Ofertio.API.csproj") `
        --context AppDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef database update (SQL Server/Development) falhou (exit $LASTEXITCODE)."
    }

    Write-Host "Aplicando migrations SQL Server (Homologacao IIS / Ofertio)..."
    $env:ASPNETCORE_ENVIRONMENT = "Homologacao"
    & dotnet ef database update `
        --project (Join-Path $root "Ofertio.Data\Ofertio.Data.csproj") `
        --startup-project (Join-Path $root "Ofertio.API\Ofertio.API.csproj") `
        --context AppDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef database update (SQL Server/Homologacao) falhou (exit $LASTEXITCODE)."
    }

    if ($Projeto -in @("Api", "All")) {
        Publish-OfertioProject `
            -ProjectPath (Join-Path $root "Ofertio.API\Ofertio.API.csproj") `
            -OutputPath $ApiOutput
    }

    if ($Projeto -in @("WebUi", "All")) {
        Publish-OfertioProject `
            -ProjectPath (Join-Path $root "Ofertio.WebUi\Ofertio.WebUi.csproj") `
            -OutputPath $WebUiOutput
    }

    Write-Host "Publicação concluída com sucesso."
}
finally {
    if ($stopped) {
        Start-OfertioAppPools -Names $poolsToStop
    }
}
