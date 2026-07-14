[CmdletBinding()]
param(
    [string]$GamePath,
    [switch]$SkipBepInExDownload,
    [switch]$NonInteractive,
    [string]$CustomRootOverride,
    [string]$StateRootOverride
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$PackageRoot = Split-Path -Parent $PSScriptRoot
$Version = '0.3.0'
$BepInExUrl = 'https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785%2B6abdba4.zip'
$BepInExSha256 = '2A7CBF74D26ABE4765C3E662DB1721B923BAC39849EBFEF2CA5DC7DE7E2D9B7F'
$StateRoot = if ($StateRootOverride) { [IO.Path]::GetFullPath($StateRootOverride) } else { Join-Path $env:LOCALAPPDATA 'RimuruModVampireSurvivors' }

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Get-FullPath([string]$Path) {
    return [IO.Path]::GetFullPath($Path)
}

function Test-GameRoot([string]$Path) {
    return (Test-Path (Join-Path $Path 'VampireSurvivors.exe'))
}

function Get-SteamLibraryRoots {
    $roots = [Collections.Generic.List[string]]::new()
    $steamRoots = [Collections.Generic.List[string]]::new()
    $registryPaths = @(
        'HKCU:\Software\Valve\Steam',
        'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
        'HKLM:\SOFTWARE\Valve\Steam'
    )

    foreach ($registryPath in $registryPaths) {
        try {
            $installPath = (Get-ItemProperty -Path $registryPath -Name SteamPath -ErrorAction Stop).SteamPath
            if ($installPath) { $steamRoots.Add($installPath) }
        } catch { }
    }

    $steamRoots.Add('C:\Program Files (x86)\Steam')
    $steamRoots.Add('C:\Program Files\Steam')
    $steamRoots.Add('E:\SteamLibrary')

    foreach ($steamRoot in ($steamRoots | Select-Object -Unique)) {
        if (-not (Test-Path $steamRoot)) { continue }
        $roots.Add((Join-Path $steamRoot 'steamapps\common'))
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (Test-Path $libraryFile) {
            $content = Get-Content $libraryFile -Raw
            foreach ($match in [regex]::Matches($content, '"path"\s+"(?<path>[^"]+)"')) {
                $libraryPath = $match.Groups['path'].Value -replace '\\\\', '\'
                $roots.Add((Join-Path $libraryPath 'steamapps\common'))
            }
        }
    }

    return @($roots | Select-Object -Unique)
}

function Resolve-GameRoot {
    $candidates = [Collections.Generic.List[string]]::new()
    if ($GamePath) { $candidates.Add($GamePath) }
    foreach ($root in (Get-SteamLibraryRoots)) {
        $candidates.Add((Join-Path $root 'Vampire Survivors'))
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        try {
            $full = Get-FullPath $candidate
            if (Test-GameRoot $full) { return $full }
        } catch { }
    }

    if ($NonInteractive) {
        throw 'Nao foi possivel localizar Vampire Survivors. Informe -GamePath com a pasta que contem VampireSurvivors.exe.'
    }

    $manual = Read-Host 'Informe a pasta que contem VampireSurvivors.exe'
    if (-not $manual -or -not (Test-GameRoot $manual)) {
        throw 'A pasta informada nao contem VampireSurvivors.exe.'
    }
    return (Get-FullPath $manual)
}

function Assert-GameClosed {
    $processes = @(Get-Process -Name VampireSurvivors -ErrorAction SilentlyContinue)
    if ($processes.Count -gt 0) {
        throw 'Feche Vampire Survivors antes de instalar, verificar ou remover o mod.'
    }
}

function Copy-DirectoryContent([string]$Source, [string]$Destination) {
    if (-not (Test-Path $Source)) { throw "Origem ausente: $Source" }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
}

function Ensure-BepInEx([string]$Root) {
    $loaderDll = Join-Path $Root 'BepInEx\core\BepInEx.Unity.IL2CPP.dll'
    $doorstop = Join-Path $Root 'winhttp.dll'
    if ((Test-Path $loaderDll) -and (Test-Path $doorstop)) {
        Write-Host 'BepInEx IL2CPP ja esta presente.' -ForegroundColor DarkGray
        return $false
    }
    if ($SkipBepInExDownload) {
        throw 'BepInEx IL2CPP nao foi encontrado e -SkipBepInExDownload foi informado.'
    }

    Write-Step 'Baixando BepInEx IL2CPP x64 da fonte oficial'
    $download = Join-Path $env:TEMP 'BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785.zip'
    $extract = Join-Path $env:TEMP ('Rimuru-BepInEx-' + [guid]::NewGuid().ToString('N'))
    try {
        Invoke-WebRequest -Uri $BepInExUrl -OutFile $download
        $actualHash = (Get-FileHash $download -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actualHash -ne $BepInExSha256) {
            throw "SHA-256 inesperado para BepInEx: $actualHash"
        }
        Expand-Archive -LiteralPath $download -DestinationPath $extract -Force
        Get-ChildItem -LiteralPath $extract -Force | Copy-Item -Destination $Root -Recurse -Force
    } finally {
        Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (-not (Test-Path $loaderDll)) { throw 'O pacote BepInEx foi extraido, mas o loader esperado nao apareceu.' }
    return $true
}

try {
    Assert-GameClosed
    $gameRoot = Resolve-GameRoot
    $customRoot = if ($CustomRootOverride) { Get-FullPath $CustomRootOverride } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\poncle\Vampire Survivors\CustomCharacters' }
    $customPath = Join-Path $customRoot 'RIMURU'
    $pluginPath = Join-Path $gameRoot 'BepInEx\plugins\RimuruSurvivor'
    $customSource = Join-Path $PackageRoot 'dist\custom-character'
    $pluginSource = Join-Path $PackageRoot 'dist\plugin'
    if (-not (Test-Path (Join-Path $pluginSource 'RimuruSurvivor.dll'))) { throw 'O pacote dist/plugin esta incompleto.' }

    Write-Step "Instalando em $gameRoot"
    $downloadedBepInEx = Ensure-BepInEx $gameRoot
    $backupPath = Join-Path $StateRoot ('backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Force -Path $backupPath | Out-Null

    if (Test-Path $customPath) {
        Copy-DirectoryContent $customPath (Join-Path $backupPath 'CustomCharacters\RIMURU')
    }
    if (Test-Path $pluginPath) {
        Copy-DirectoryContent $pluginPath (Join-Path $backupPath 'BepInEx\plugins\RimuruSurvivor')
    }

    Write-Step 'Copiando personagem CUSTOM, plugin e assets'
    Copy-DirectoryContent $customSource $customPath
    Copy-DirectoryContent $pluginSource $pluginPath

    $manifest = [ordered]@{
        version = $Version
        installedAt = (Get-Date).ToUniversalTime().ToString('o')
        gamePath = $gameRoot
        customPath = $customPath
        pluginPath = $pluginPath
        backupPath = $backupPath
        downloadedBepInEx = $downloadedBepInEx
    }
    New-Item -ItemType Directory -Force -Path $StateRoot | Out-Null
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $StateRoot 'install.json') -Encoding UTF8

    Write-Host "`nInstalacao concluida: Rimuru $Version" -ForegroundColor Green
    Write-Host 'Abra o jogo pelo Steam. Na primeira execucao o BepInEx pode levar mais tempo para preparar as assemblies IL2CPP.'
    Write-Host "Backup: $backupPath"
} catch {
    Write-Error $_
    exit 1
}
