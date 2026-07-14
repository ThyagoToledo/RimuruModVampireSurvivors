[CmdletBinding()]
param(
    [string]$GamePath,
    [switch]$NonInteractive,
    [string]$CustomRootOverride,
    [string]$StateRootOverride
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PackageRoot = Split-Path -Parent $PSScriptRoot
$StateRoot = if ($StateRootOverride) { [IO.Path]::GetFullPath($StateRootOverride) } else { Join-Path $env:LOCALAPPDATA 'RimuruModVampireSurvivors' }
$StateFile = Join-Path $StateRoot 'install.json'

function Get-FullPath([string]$Path) { return [IO.Path]::GetFullPath($Path) }
function Test-GameRoot([string]$Path) { return (Test-Path (Join-Path $Path 'VampireSurvivors.exe')) }

try {
    if (Get-Process -Name VampireSurvivors -ErrorAction SilentlyContinue) { throw 'Feche Vampire Survivors antes de verificar o mod.' }
    if (-not $GamePath -and (Test-Path $StateFile)) { $GamePath = (Get-Content $StateFile -Raw | ConvertFrom-Json).gamePath }
    if (-not $GamePath -or -not (Test-GameRoot $GamePath)) {
        if ($NonInteractive) { throw 'Informe -GamePath ou instale o mod antes de verificar.' }
        $GamePath = Read-Host 'Informe a pasta que contem VampireSurvivors.exe'
    }
    $gameRoot = Get-FullPath $GamePath
    if (-not (Test-GameRoot $gameRoot)) { throw 'A pasta informada nao contem VampireSurvivors.exe.' }

    $customRoot = if ($CustomRootOverride) { Get-FullPath $CustomRootOverride } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\poncle\Vampire Survivors\CustomCharacters' }
    $customPath = Join-Path $customRoot 'RIMURU'
    $pluginPath = Join-Path $gameRoot 'BepInEx\plugins\RimuruSurvivor'
    $expectedDll = Join-Path $PackageRoot 'dist\plugin\RimuruSurvivor.dll'
    $installedDll = Join-Path $pluginPath 'RimuruSurvivor.dll'
    $required = @(
        (Join-Path $customPath 'character.json'),
        (Join-Path $customPath 'charsel.png'),
        (Join-Path $customPath 'sprites\rimuru_01.png'),
        (Join-Path $customPath 'skins\slime\sprites\rimuru_01.png'),
        (Join-Path $customPath 'skins\humanoid\sprites\rimuru_01.png'),
        (Join-Path $customPath 'skins\demon_lord\sprites\rimuru_01.png'),
        $installedDll,
        (Join-Path $pluginPath 'assets\weapons\rimuru-katana-v2.png'),
        (Join-Path $pluginPath 'assets\summons\ranga\ranga_01.png')
    )
    $missing = @($required | Where-Object { -not (Test-Path $_) })
    if ($missing.Count -gt 0) { throw ('Arquivos ausentes:`n' + ($missing -join "`n")) }
    $expectedHash = (Get-FileHash $expectedDll -Algorithm SHA256).Hash.ToUpperInvariant()
    $actualHash = (Get-FileHash $installedDll -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($expectedHash -ne $actualHash) { throw "Hash do plugin diferente do pacote. Esperado $expectedHash, encontrado $actualHash." }

    Write-Host 'Rimuru Mod: OK' -ForegroundColor Green
    Write-Host "CUSTOM: $customPath"
    Write-Host "Plugin: $pluginPath"
    Write-Host "DLL SHA-256: $actualHash"
} catch {
    Write-Error $_
    exit 1
}
