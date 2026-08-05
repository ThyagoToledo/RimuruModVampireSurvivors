@echo off
setlocal EnableExtensions
title Rimuru Mod - Instalador
chcp 65001 >nul 2>&1

set "RIMURU_SETUP_SELF=%~f0"
set "RIMURU_SETUP_TEMP=%TEMP%\RimuruMod-Setup-%RANDOM%%RANDOM%.ps1"

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$lines = Get-Content -LiteralPath $env:RIMURU_SETUP_SELF; $marker = [Array]::IndexOf($lines, '#__RIMURU_POWERSHELL__'); if ($marker -lt 0) { throw 'Payload do instalador nao encontrado.' }; $lines[($marker + 1)..($lines.Length - 1)] | Set-Content -LiteralPath $env:RIMURU_SETUP_TEMP -Encoding UTF8"
if errorlevel 1 goto :bootstrap_error

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%RIMURU_SETUP_TEMP%" %*
set "RIMURU_EXIT=%ERRORLEVEL%"
del /q "%RIMURU_SETUP_TEMP%" >nul 2>&1

echo.
if "%RIMURU_EXIT%"=="0" (
  echo Operacao concluida.
) else (
  echo A operacao terminou com erro. Leia a mensagem acima.
)
pause
exit /b %RIMURU_EXIT%

:bootstrap_error
echo.
echo Nao foi possivel iniciar o instalador.
del /q "%RIMURU_SETUP_TEMP%" >nul 2>&1
pause
exit /b 1

#__RIMURU_POWERSHELL__
[CmdletBinding()]
param(
    [ValidateSet('Menu', 'Install', 'Repair', 'Verify', 'Uninstall')]
    [string]$Mode = 'Menu',
    [string]$GamePath,
    [switch]$NonInteractive,
    [switch]$SkipBepInExDownload,
    [string]$CustomRootOverride,
    [string]$StateRootOverride
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PackageRoot = Split-Path -Parent $env:RIMURU_SETUP_SELF
$StateRoot = if ($StateRootOverride) {
    [IO.Path]::GetFullPath($StateRootOverride)
} else {
    Join-Path $env:LOCALAPPDATA 'RimuruModVampireSurvivors'
}
$StateFile = Join-Path $StateRoot 'install.json'
$BepInExUrl = 'https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785%2B6abdba4.zip'
$BepInExSha256 = '2A7CBF74D26ABE4765C3E662DB1721B923BAC39849EBFEF2CA5DC7DE7E2D9B7F'

function Write-Title([string]$Text) {
    Clear-Host
    Write-Host '============================================================' -ForegroundColor Cyan
    Write-Host '          RIMURU MOD - VAMPIRE SURVIVORS' -ForegroundColor Cyan
    Write-Host '============================================================' -ForegroundColor Cyan
    if ($Text) { Write-Host "`n$Text" -ForegroundColor White }
}

function Write-Step([string]$Text) {
    Write-Host "`n==> $Text" -ForegroundColor Cyan
}

function Get-NormalizedPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
    $clean = [Environment]::ExpandEnvironmentVariables($Path.Trim().Trim('"'))
    try {
        $full = [IO.Path]::GetFullPath($clean)
        if ([IO.Path]::GetFileName($full) -ieq 'VampireSurvivors.exe') {
            return Split-Path -Parent $full
        }
        return $full.TrimEnd('\')
    } catch {
        return $null
    }
}

function Test-GameRoot([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    return Test-Path -LiteralPath (Join-Path $Path 'VampireSurvivors.exe') -PathType Leaf
}

function Assert-Package {
    $required = @(
        (Join-Path $PackageRoot 'dist\manifest.json'),
        (Join-Path $PackageRoot 'dist\custom-character\character.json'),
        (Join-Path $PackageRoot 'dist\custom-character\charsel.png'),
        (Join-Path $PackageRoot 'dist\plugin\RimuruSurvivor.dll'),
        (Join-Path $PackageRoot 'dist\plugin\assets\weapons\rimuru-katana-v2.png'),
        (Join-Path $PackageRoot 'dist\plugin\assets\summons\ranga\ranga_01.png')
    )
    $missing = @($required | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })
    if ($missing.Count -gt 0) {
        throw "O pacote foi extraido incompleto. Mantenha Install-RimuruMod.bat e a pasta dist juntos.`n$($missing -join "`n")"
    }

    $manifest = Get-Content -LiteralPath (Join-Path $PackageRoot 'dist\manifest.json') -Raw | ConvertFrom-Json
    $plugin = Join-Path $PackageRoot 'dist\plugin\RimuruSurvivor.dll'
    $actual = (Get-FileHash -LiteralPath $plugin -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($manifest.pluginSha256 -and $actual -ne $manifest.pluginSha256.ToUpperInvariant()) {
        throw "O plugin do pacote falhou na verificacao de integridade. Esperado $($manifest.pluginSha256), encontrado $actual."
    }
    return $manifest
}

function Get-SteamCommonRoots {
    $steamRoots = [Collections.Generic.List[string]]::new()
    foreach ($registryPath in @(
        'HKCU:\Software\Valve\Steam',
        'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
        'HKLM:\SOFTWARE\Valve\Steam'
    )) {
        foreach ($property in @('SteamPath', 'InstallPath')) {
            try {
                $value = (Get-ItemProperty -Path $registryPath -Name $property -ErrorAction Stop).$property
                if ($value) { $steamRoots.Add($value) }
            } catch { }
        }
    }

    $steamRoots.Add('C:\Program Files (x86)\Steam')
    $steamRoots.Add('C:\Program Files\Steam')
    foreach ($drive in (Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue)) {
        $steamRoots.Add((Join-Path $drive.Root 'SteamLibrary'))
        $steamRoots.Add((Join-Path $drive.Root 'Steam'))
    }

    $commonRoots = [Collections.Generic.List[string]]::new()
    foreach ($steamRoot in ($steamRoots | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $steamRoot -PathType Container)) { continue }
        $commonRoots.Add((Join-Path $steamRoot 'steamapps\common'))
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $libraryFile -PathType Leaf) {
            $content = Get-Content -LiteralPath $libraryFile -Raw
            foreach ($match in [regex]::Matches($content, '"path"\s+"(?<path>[^"]+)"')) {
                $libraryPath = $match.Groups['path'].Value -replace '\\\\', '\'
                $commonRoots.Add((Join-Path $libraryPath 'steamapps\common'))
            }
        }
    }
    return @($commonRoots | Select-Object -Unique)
}

function Get-GameCandidates([string]$RequestedPath) {
    $candidates = [Collections.Generic.List[string]]::new()
    if ($RequestedPath) { $candidates.Add($RequestedPath) }
    $candidates.Add($PackageRoot)
    $candidates.Add((Split-Path -Parent $PackageRoot))
    $candidates.Add((Get-Location).Path)
    if (Test-Path -LiteralPath $StateFile -PathType Leaf) {
        try { $candidates.Add((Get-Content -LiteralPath $StateFile -Raw | ConvertFrom-Json).gamePath) } catch { }
    }
    foreach ($common in (Get-SteamCommonRoots)) {
        $candidates.Add((Join-Path $common 'Vampire Survivors'))
    }

    $result = [Collections.Generic.List[string]]::new()
    foreach ($candidate in $candidates) {
        $normalized = Get-NormalizedPath $candidate
        if ($normalized -and -not $result.Contains($normalized)) { $result.Add($normalized) }
    }
    return $result
}

function Resolve-GameRoot([string]$RequestedPath) {
    foreach ($candidate in (Get-GameCandidates $RequestedPath)) {
        if (Test-GameRoot $candidate) {
            Write-Host "Jogo encontrado: $candidate" -ForegroundColor Green
            return $candidate
        }
    }
    if ($NonInteractive) {
        throw 'Vampire Survivors nao foi localizado. Use -GamePath com a pasta que contem VampireSurvivors.exe.'
    }

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Write-Host "`nNao consegui localizar o jogo automaticamente." -ForegroundColor Yellow
        $manual = Read-Host 'Cole a pasta do jogo ou arraste VampireSurvivors.exe para esta janela'
        $normalized = Get-NormalizedPath $manual
        if (Test-GameRoot $normalized) { return $normalized }
        Write-Host 'Esse local nao contem VampireSurvivors.exe.' -ForegroundColor Yellow
    }
    throw 'Local do jogo invalido apos tres tentativas.'
}

function Assert-GameClosed([string]$GameRoot) {
    $targetExe = [IO.Path]::GetFullPath((Join-Path $GameRoot 'VampireSurvivors.exe'))
    for ($attempt = 0; $attempt -lt 2; $attempt++) {
        $running = @(Get-CimInstance Win32_Process -Filter "Name = 'VampireSurvivors.exe'" -ErrorAction SilentlyContinue | Where-Object {
            $_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath) -ieq $targetExe
        })
        if ($running.Count -eq 0) { return }
        if ($NonInteractive) { throw 'Feche Vampire Survivors antes de continuar.' }
        Write-Host "`nO jogo esta aberto. Feche-o para evitar arquivos corrompidos." -ForegroundColor Yellow
        Read-Host 'Depois de fechar, pressione Enter'
    }
    throw 'Vampire Survivors continua aberto.'
}

function Assert-SafeChild([string]$Child, [string]$Parent, [string]$ExpectedLeaf) {
    $fullChild = [IO.Path]::GetFullPath($Child).TrimEnd('\')
    $fullParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\')
    if ([IO.Path]::GetFileName($fullChild) -cne $ExpectedLeaf) {
        throw "Operacao recusada: pasta inesperada $fullChild"
    }
    $prefix = $fullParent + [IO.Path]::DirectorySeparatorChar
    if (-not $fullChild.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Operacao recusada: $fullChild esta fora de $fullParent"
    }
}

function Copy-DirectoryContent([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source -PathType Container)) { throw "Origem ausente: $Source" }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
}

function Backup-Mod([string]$CustomPath, [string]$PluginPath) {
    $backupPath = Join-Path $StateRoot ('backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
    if (Test-Path -LiteralPath $CustomPath -PathType Container) {
        Copy-DirectoryContent $CustomPath (Join-Path $backupPath 'CustomCharacters\RIMURU')
    }
    if (Test-Path -LiteralPath $PluginPath -PathType Container) {
        Copy-DirectoryContent $PluginPath (Join-Path $backupPath 'BepInEx\plugins\RimuruSurvivor')
    }
    return $backupPath
}

function Ensure-BepInEx([string]$GameRoot) {
    $loaderDll = Join-Path $GameRoot 'BepInEx\core\BepInEx.Unity.IL2CPP.dll'
    $doorstop = Join-Path $GameRoot 'winhttp.dll'
    if ((Test-Path -LiteralPath $loaderDll -PathType Leaf) -and (Test-Path -LiteralPath $doorstop -PathType Leaf)) {
        Write-Host 'BepInEx IL2CPP: OK' -ForegroundColor DarkGreen
        return $false
    }
    if ($SkipBepInExDownload) {
        throw 'BepInEx IL2CPP esta ausente e o download foi desativado.'
    }

    Write-Step 'Baixando BepInEx IL2CPP x64 oficial'
    $download = Join-Path $env:TEMP 'BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785.zip'
    $extract = Join-Path $env:TEMP ('Rimuru-BepInEx-' + [guid]::NewGuid().ToString('N'))
    try {
        Invoke-WebRequest -Uri $BepInExUrl -OutFile $download -UseBasicParsing
        $actualHash = (Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actualHash -ne $BepInExSha256) {
            throw "Download do BepInEx recusado: SHA-256 inesperado $actualHash."
        }
        Expand-Archive -LiteralPath $download -DestinationPath $extract -Force
        Get-ChildItem -LiteralPath $extract -Force | Copy-Item -Destination $GameRoot -Recurse -Force
    } finally {
        if (Test-Path -LiteralPath $extract) { Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction SilentlyContinue }
        if (Test-Path -LiteralPath $download) { Remove-Item -LiteralPath $download -Force -ErrorAction SilentlyContinue }
    }
    if (-not (Test-Path -LiteralPath $loaderDll -PathType Leaf)) {
        throw 'O BepInEx foi extraido, mas o loader IL2CPP esperado nao apareceu.'
    }
    return $true
}

function Get-InstallPaths([string]$GameRoot) {
    $customParent = if ($CustomRootOverride) {
        [IO.Path]::GetFullPath($CustomRootOverride)
    } else {
        Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\poncle\Vampire Survivors\CustomCharacters'
    }
    $pluginParent = Join-Path $GameRoot 'BepInEx\plugins'
    return [pscustomobject]@{
        CustomParent = $customParent
        CustomPath = Join-Path $customParent 'RIMURU'
        PluginParent = $pluginParent
        PluginPath = Join-Path $pluginParent 'RimuruSurvivor'
    }
}

function Compare-PackageTree([string]$Source, [string]$Destination) {
    $issues = [Collections.Generic.List[string]]::new()
    foreach ($sourceFile in (Get-ChildItem -LiteralPath $Source -Recurse -File)) {
        $relative = $sourceFile.FullName.Substring([IO.Path]::GetFullPath($Source).TrimEnd('\').Length).TrimStart('\')
        $destinationFile = Join-Path $Destination $relative
        if (-not (Test-Path -LiteralPath $destinationFile -PathType Leaf)) {
            $issues.Add("Ausente: $destinationFile")
            continue
        }
        $sourceHash = (Get-FileHash -LiteralPath $sourceFile.FullName -Algorithm SHA256).Hash
        $destinationHash = (Get-FileHash -LiteralPath $destinationFile -Algorithm SHA256).Hash
        if ($sourceHash -ne $destinationHash) { $issues.Add("Diferente: $destinationFile") }
    }
    return $issues
}

function Invoke-Verify([string]$GameRoot, [switch]$Quiet) {
    $paths = Get-InstallPaths $GameRoot
    $issues = [Collections.Generic.List[string]]::new()
    foreach ($issue in (Compare-PackageTree (Join-Path $PackageRoot 'dist\custom-character') $paths.CustomPath)) { $issues.Add($issue) }
    foreach ($issue in (Compare-PackageTree (Join-Path $PackageRoot 'dist\plugin') $paths.PluginPath)) { $issues.Add($issue) }

    $loaderDll = Join-Path $GameRoot 'BepInEx\core\BepInEx.Unity.IL2CPP.dll'
    if (-not (Test-Path -LiteralPath $loaderDll -PathType Leaf)) { $issues.Add("BepInEx ausente: $loaderDll") }
    if (-not (Test-Path -LiteralPath (Join-Path $GameRoot 'winhttp.dll') -PathType Leaf)) { $issues.Add('BepInEx ausente: winhttp.dll') }

    if ($issues.Count -gt 0) {
        if (-not $Quiet) {
            Write-Host "`nVerificacao encontrou $($issues.Count) problema(s):" -ForegroundColor Yellow
            $issues | ForEach-Object { Write-Host " - $_" }
        }
        return $false
    }
    if (-not $Quiet) {
        $hash = (Get-FileHash -LiteralPath (Join-Path $paths.PluginPath 'RimuruSurvivor.dll') -Algorithm SHA256).Hash
        Write-Host "`nRimuru Mod: OK" -ForegroundColor Green
        Write-Host "Jogo:   $GameRoot"
        Write-Host "CUSTOM: $($paths.CustomPath)"
        Write-Host "Plugin: $($paths.PluginPath)"
        Write-Host "SHA-256: $hash"
    }
    return $true
}

function Invoke-Install([string]$GameRoot, [object]$Manifest) {
    Assert-GameClosed $GameRoot
    $paths = Get-InstallPaths $GameRoot
    Assert-SafeChild $paths.CustomPath $paths.CustomParent 'RIMURU'
    Assert-SafeChild $paths.PluginPath $paths.PluginParent 'RimuruSurvivor'

    Write-Step "Preparando o jogo em $GameRoot"
    $downloadedBepInEx = Ensure-BepInEx $GameRoot
    $backupPath = Backup-Mod $paths.CustomPath $paths.PluginPath

    Write-Step 'Instalando ou reparando personagem, plugin e assets'
    if (Test-Path -LiteralPath $paths.CustomPath) { Remove-Item -LiteralPath $paths.CustomPath -Recurse -Force }
    if (Test-Path -LiteralPath $paths.PluginPath) { Remove-Item -LiteralPath $paths.PluginPath -Recurse -Force }
    Copy-DirectoryContent (Join-Path $PackageRoot 'dist\custom-character') $paths.CustomPath
    Copy-DirectoryContent (Join-Path $PackageRoot 'dist\plugin') $paths.PluginPath

    New-Item -ItemType Directory -Force -Path $StateRoot | Out-Null
    [ordered]@{
        version = $Manifest.version
        installedAt = (Get-Date).ToUniversalTime().ToString('o')
        gamePath = $GameRoot
        customPath = $paths.CustomPath
        pluginPath = $paths.PluginPath
        backupPath = $backupPath
        downloadedBepInEx = $downloadedBepInEx
    } | ConvertTo-Json | Set-Content -LiteralPath $StateFile -Encoding UTF8

    Write-Step 'Validando a instalacao'
    if (-not (Invoke-Verify $GameRoot)) { throw 'A copia terminou, mas a verificacao final falhou.' }
    Write-Host "`nRimuru Mod $($Manifest.version) instalado com sucesso." -ForegroundColor Green
    Write-Host "Backup: $backupPath"
    Write-Host 'Abra Vampire Survivors pela Steam. A primeira inicializacao do BepInEx pode ser mais lenta.'
}

function Invoke-Uninstall([string]$GameRoot) {
    Assert-GameClosed $GameRoot
    $paths = Get-InstallPaths $GameRoot
    Assert-SafeChild $paths.CustomPath $paths.CustomParent 'RIMURU'
    Assert-SafeChild $paths.PluginPath $paths.PluginParent 'RimuruSurvivor'

    $restore = $false
    $manifest = $null
    if (Test-Path -LiteralPath $StateFile -PathType Leaf) {
        $manifest = Get-Content -LiteralPath $StateFile -Raw | ConvertFrom-Json
        if (-not $NonInteractive -and $manifest.backupPath -and (Test-Path -LiteralPath $manifest.backupPath)) {
            $answer = Read-Host 'Restaurar o backup anterior depois de remover? (s/N)'
            $restore = $answer -match '^(s|sim|y|yes)$'
        }
    }

    if (Test-Path -LiteralPath $paths.CustomPath) { Remove-Item -LiteralPath $paths.CustomPath -Recurse -Force }
    if (Test-Path -LiteralPath $paths.PluginPath) { Remove-Item -LiteralPath $paths.PluginPath -Recurse -Force }

    if ($restore -and $manifest) {
        $backupRoot = [IO.Path]::GetFullPath($manifest.backupPath)
        $stateFull = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\') + '\'
        if (-not $backupRoot.StartsWith($stateFull, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Restauracao recusada: backup fora da pasta de estado.'
        }
        $customBackup = Join-Path $backupRoot 'CustomCharacters\RIMURU'
        $pluginBackup = Join-Path $backupRoot 'BepInEx\plugins\RimuruSurvivor'
        if (Test-Path -LiteralPath $customBackup) { Copy-DirectoryContent $customBackup $paths.CustomPath }
        if (Test-Path -LiteralPath $pluginBackup) { Copy-DirectoryContent $pluginBackup $paths.PluginPath }
    }

    if (Test-Path -LiteralPath $StateFile) { Remove-Item -LiteralPath $StateFile -Force }
    Write-Host "`nRimuru Mod removido. BepInEx e saves foram preservados." -ForegroundColor Green
}

try {
    Write-Title 'Instalador, reparador e verificador em um unico arquivo.'
    $manifest = Assert-Package

    if ($Mode -eq 'Menu') {
        if ($NonInteractive) { $Mode = 'Install' }
        else {
            Write-Host "`n1. Instalar ou reparar"
            Write-Host '2. Verificar instalacao'
            Write-Host '3. Remover mod'
            Write-Host '4. Sair'
            $choice = Read-Host '`nEscolha uma opcao'
            $Mode = switch ($choice) {
                '1' { 'Install' }
                '2' { 'Verify' }
                '3' { 'Uninstall' }
                default { 'Exit' }
            }
        }
    }
    if ($Mode -eq 'Exit') { exit 0 }

    $gameRoot = Resolve-GameRoot $GamePath
    switch ($Mode) {
        'Install' { Invoke-Install $gameRoot $manifest }
        'Repair' { Invoke-Install $gameRoot $manifest }
        'Verify' {
            Assert-GameClosed $gameRoot
            if (-not (Invoke-Verify $gameRoot)) {
                if ($NonInteractive) { throw 'A instalacao precisa de reparo.' }
                $answer = Read-Host '`nReparar automaticamente agora? (S/n)'
                if ($answer -notmatch '^(n|nao|no)$') { Invoke-Install $gameRoot $manifest }
                else { throw 'A instalacao precisa de reparo.' }
            }
        }
        'Uninstall' { Invoke-Uninstall $gameRoot }
    }
} catch {
    Write-Host "`nERRO: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nDica: execute novamente e informe a pasta que contem VampireSurvivors.exe." -ForegroundColor Yellow
    exit 1
}
