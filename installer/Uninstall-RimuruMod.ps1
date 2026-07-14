[CmdletBinding()]
param(
    [string]$GamePath,
    [switch]$RestoreBackup,
    [switch]$NonInteractive,
    [string]$CustomRootOverride,
    [string]$StateRootOverride
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$StateRoot = if ($StateRootOverride) { [IO.Path]::GetFullPath($StateRootOverride) } else { Join-Path $env:LOCALAPPDATA 'RimuruModVampireSurvivors' }
$StateFile = Join-Path $StateRoot 'install.json'

function Get-FullPath([string]$Path) { return [IO.Path]::GetFullPath($Path) }
function Assert-ExactLeaf([string]$Path, [string]$Leaf, [string]$Parent) {
    $fullPath = Get-FullPath $Path
    $fullParent = Get-FullPath $Parent
    if ([IO.Path]::GetFileName($fullPath) -ne $Leaf -or (Get-FullPath (Split-Path $fullPath)) -ne $fullParent) {
        throw "Caminho recusado para seguranca: $Path"
    }
}
function Copy-DirectoryContent([string]$Source, [string]$Destination) {
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
}

try {
    if (Get-Process -Name VampireSurvivors -ErrorAction SilentlyContinue) { throw 'Feche Vampire Survivors antes de remover o mod.' }
    if (-not (Test-Path $StateFile)) { throw 'Nao existe instalacao registrada do Rimuru Mod neste usuario.' }
    $manifest = Get-Content $StateFile -Raw | ConvertFrom-Json
    if (-not $GamePath) { $GamePath = $manifest.gamePath }
    $gameRoot = Get-FullPath $GamePath
    $customParent = if ($CustomRootOverride) { Get-FullPath $CustomRootOverride } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\poncle\Vampire Survivors\CustomCharacters' }
    $customPath = Join-Path $customParent 'RIMURU'
    $pluginParent = Join-Path $gameRoot 'BepInEx\plugins'
    $pluginPath = Join-Path $pluginParent 'RimuruSurvivor'
    Assert-ExactLeaf $customPath 'RIMURU' $customParent
    Assert-ExactLeaf $pluginPath 'RimuruSurvivor' $pluginParent

    if (-not $NonInteractive) {
        $answer = Read-Host 'Remover o Rimuru Mod desta copia? (s/N)'
        if ($answer -notmatch '^(s|sim|y|yes)$') { Write-Host 'Operacao cancelada.'; exit 0 }
    }

    if (Test-Path $customPath) { Remove-Item -LiteralPath $customPath -Recurse -Force }
    if (Test-Path $pluginPath) { Remove-Item -LiteralPath $pluginPath -Recurse -Force }

    if ($RestoreBackup -and $manifest.backupPath -and (Test-Path $manifest.backupPath)) {
        $backupRoot = Get-FullPath $manifest.backupPath
        $stateFull = Get-FullPath $StateRoot
        if (-not $backupRoot.StartsWith($stateFull, [StringComparison]::OrdinalIgnoreCase)) { throw 'Backup fora da pasta de estado; restauracao recusada.' }
        $customBackup = Join-Path $backupRoot 'CustomCharacters\RIMURU'
        $pluginBackup = Join-Path $backupRoot 'BepInEx\plugins\RimuruSurvivor'
        if (Test-Path $customBackup) { Copy-DirectoryContent $customBackup $customPath }
        if (Test-Path $pluginBackup) { Copy-DirectoryContent $pluginBackup $pluginPath }
        Write-Host 'Backup restaurado.' -ForegroundColor Green
    }

    Remove-Item -LiteralPath $StateFile -Force -ErrorAction SilentlyContinue
    Write-Host 'Rimuru Mod removido. O BepInEx e os saves foram preservados.' -ForegroundColor Green
} catch {
    Write-Error $_
    exit 1
}
