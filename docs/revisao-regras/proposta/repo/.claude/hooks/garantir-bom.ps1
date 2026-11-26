#Requires -Version 5.1
<#
.SYNOPSIS
    Hook PostToolUse do Claude Code: garante UTF-8 com BOM no script .ps1 que acabou de ser criado ou editado.
.DESCRIPTION
    Recebe no stdin o JSON do evento (campo tool_input.file_path, conforme https://code.claude.com/docs/en/hooks).
    Só age em .ps1 (regra docs/projeto/encoding-e-legado.md §1) e ignora .claude/, .github/ e bin/obj.
    Nunca bloqueia: sai sempre com código 0. Custo: milissegundos por edição.
#>
$ErrorActionPreference = 'Stop'
try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $path = $payload.tool_input.file_path
    if (-not $path -or -not (Test-Path -LiteralPath $path -PathType Leaf)) { exit 0 }

    $extensoes = @('.ps1')
    if ($extensoes -notcontains ([System.IO.Path]::GetExtension($path).ToLowerInvariant())) { exit 0 }
    if ($path -match '[\\/](\.claude|\.github|bin|obj|node_modules)[\\/]') { exit 0 }

    $bytes = [System.IO.File]::ReadAllBytes($path)
    $temBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    if ($temBom) { exit 0 }

    $texto = [System.Text.Encoding]::UTF8.GetString($bytes)
    [System.IO.File]::WriteAllText($path, $texto, (New-Object System.Text.UTF8Encoding $true))
    exit 0
}
catch {
    # Falha do hook não pode atrapalhar a edição; o CI e o NormalizarEncodingPrecoce.ps1 pegam o que escapar.
    exit 0
}
