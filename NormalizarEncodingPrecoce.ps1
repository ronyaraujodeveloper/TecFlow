#Requires -Version 5.1
<#
.SYNOPSIS
    Normaliza arquivos de texto da solution TecFlow para UTF-8 com BOM (Windows/IIS).
#>
[CmdletBinding()]
param(
    [string] $SolutionRoot = $PSScriptRoot,
    [string[]] $Extensions = @("*.cs", "*.razor", "*.json", "*.css", "*.html")
)

$utf8WithBom = New-Object System.Text.UTF8Encoding $true
$excludeDirs = @("bin", "obj", ".git", ".vs", "node_modules", "packages")
$converted = 0
$skipped = 0

$files = Get-ChildItem -Path $SolutionRoot -Recurse -File -Include $Extensions -ErrorAction SilentlyContinue |
    Where-Object {
        $relative = $_.FullName.Substring($SolutionRoot.Length).TrimStart('\', '/')
        $parts = $relative -split '[\\/]'
        -not ($parts | Where-Object { $excludeDirs -contains $_ })
    }

foreach ($file in $files) {
    try {
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF

        $content = if ($hasBom) {
            [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        } else {
            [System.IO.File]::ReadAllText($file.FullName)
        }

        if ($hasBom) {
            $skipped++
            continue
        }

        [System.IO.File]::WriteAllText($file.FullName, $content, $utf8WithBom)
        $converted++
        Write-Host "UTF-8 BOM: $($file.FullName)"
    }
    catch {
        Write-Warning "Falha em $($file.FullName): $_"
    }
}

Write-Host ""
Write-Host "Concluído. Convertidos: $converted | Já UTF-8 BOM: $skipped | Total analisado: $($files.Count)"
