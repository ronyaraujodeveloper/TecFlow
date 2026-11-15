# Gera o grafo Mermaid de dependencias entre projetos a partir dos <ProjectReference> dos .csproj.
# Uso: powershell -File scripts\gerar-diagrama-dependencias.ps1   (imprime o bloco ```mermaid)
# Cole a saida na secao 1 de docs\DIAGRAMAS_ARQUITETURA.md quando as referencias mudarem.
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$projetos = Get-ChildItem -Path $raiz -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules)\\' } |
    Sort-Object Name

$linhas = New-Object System.Collections.Generic.List[string]
$linhas.Add('```mermaid')
$linhas.Add('graph LR')
foreach ($p in $projetos) {
    [xml]$xml = Get-Content -LiteralPath $p.FullName -Raw
    $origem = $p.BaseName -replace '^Ofertio\.', ''
    $refs = @($xml.SelectNodes('//ProjectReference')) | ForEach-Object { $_.Include } | Where-Object { $_ }
    foreach ($r in $refs) {
        $destino = [System.IO.Path]::GetFileNameWithoutExtension(($r -replace '\\', '/')) -replace '^Ofertio\.', ''
        $linhas.Add(('    {0} --> {1}' -f ($origem -replace '\.', ''), ($destino -replace '\.', '')))
    }
}
$linhas.Add('```')
$linhas -join "`n"
