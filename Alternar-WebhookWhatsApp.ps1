<#
.SYNOPSIS
    Aponta o webhook do WhatsApp (Evolution API) para a homologação ou para o desenvolvimento.

.DESCRIPTION
    A Evolution é uma só (container ofertio-evolution) e manda as mensagens para UM endereço:
      -Ambiente hml : http://api:8080/...                (container ofertio-api)
      -Ambiente dev : http://host.docker.internal:5001/... (API do Visual Studio, perfil https, que também escuta em http://localhost:5001)
    Conceito Docker: "host.docker.internal" é o nome que um container usa para chegar ao Windows.

.EXAMPLE
    .\Alternar-WebhookWhatsApp.ps1 -Ambiente dev
    .\Alternar-WebhookWhatsApp.ps1 -Ambiente hml
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'hml')]
    [string]$Ambiente
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$apiKey = (Get-Content .env | Where-Object { $_ -like 'EVOLUTION_API_KEY=*' }) -replace '^EVOLUTION_API_KEY=', ''
if ([string]::IsNullOrWhiteSpace($apiKey)) { throw "EVOLUTION_API_KEY não encontrada no .env." }

$path = '/api/v1/integrations/whatsapp/webhook'
$url = if ($Ambiente -eq 'hml') { "http://api:8080$path" } else { "http://host.docker.internal:5001$path" }
$headers = @{ apikey = $apiKey }
$evolution = 'http://localhost:8080'

$instances = Invoke-RestMethod -Uri "$evolution/instance/fetchInstances" -Headers $headers
foreach ($instance in $instances) {
    $name = if ($instance.name) { $instance.name } else { $instance.instance.instanceName }
    $body = @{
        webhook = @{
            enabled  = $true
            url      = $url
            byEvents = $true
            base64   = $false
            events   = @('MESSAGES_UPSERT')
        }
    } | ConvertTo-Json -Depth 4
    Invoke-RestMethod -Method Post -Uri "$evolution/webhook/set/$name" -Headers $headers -ContentType 'application/json' -Body $body | Out-Null
    Write-Host "$name -> $url" -ForegroundColor Green
}
