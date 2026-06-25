#requires -Version 5.1
<#
.SYNOPSIS
    Package the Extra Terrain Tools build into a release zip.

.DESCRIPTION
    Assumes the build is finished and its dependency DLLs are already in place (your
    editor copy tool handles that step before this runs). This script only:
      1. Reads the mod version from the source manifest.json ("Version") to name the zip.
      2. Zips the (symlinked) build CONTENTS into Release\ExtraTerrainTools_<version>.zip,
         contents at the zip root, dereferencing the symlink so real files are stored.
    It refuses to overwrite an existing zip, since published versions on GitHub must stay
    valid. It only reads the build folder (in Documents) and writes the zip under Release,
    so Controlled Folder Access is a non-issue.

    Nothing is committed. Review the zip, then commit manually.

.PARAMETER RepoRoot
    The repo folder containing Source\ and Release\. The VSCode task passes
    ${workspaceFolder}\Repo. The default is a fallback for running by hand.

.PARAMETER DryRun
    Reports what would be written without creating the zip.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = 'E:\Dev\Timberborn\Mods\Extra Terrain Tools\Repo',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Write-Step   { param([string]$m) Write-Host "`n=== $m ===" -ForegroundColor Cyan }
function Write-Info   { param([string]$m) Write-Host "    $m" }
function Write-Action { param([string]$m) Write-Host "    -> $m" -ForegroundColor Green }

# Resolve a symlink/junction to its real target (PowerShell 5.1 and 7 compatible).
function Resolve-LinkTarget {
    param([Parameter(Mandatory)][string]$Path)
    $item = Get-Item -LiteralPath $Path -Force
    if ($item.PSObject.Methods.Name -contains 'ResolveLinkTarget') {
        try {
            $t = $item.ResolveLinkTarget($true)
            if ($t) { return $t.FullName }
        } catch { }
    }
    if ($item.Target) {
        $target = @($item.Target)[0]
        if (-not [System.IO.Path]::IsPathRooted($target)) {
            $target = Join-Path (Split-Path -Parent $Path) $target
        }
        return (Resolve-Path -LiteralPath $target).Path
    }
    return $item.FullName
}

Write-Host "Package release zip" -ForegroundColor White
if ($DryRun) { Write-Host "(DRY RUN - no zip will be written)" -ForegroundColor Yellow }
Write-Info "Repo root: $RepoRoot"

# --- Read mod version (zip name) --------------------------------------------
Write-Step "1/2  Reading mod version"
$manifestPath = Join-Path $RepoRoot 'Source\ExtraTerrainTools\manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "manifest.json not found at: $manifestPath"
}
$modVersion = (Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json).Version
if ([string]::IsNullOrWhiteSpace($modVersion)) {
    throw "No 'Version' property found in $manifestPath"
}
Write-Action "Mod version = $modVersion"

$zipName = "ExtraTerrainTools_$modVersion.zip"
$zipPath = Join-Path (Join-Path $RepoRoot 'Release') $zipName
if (Test-Path -LiteralPath $zipPath) {
    if ($DryRun) {
        Write-Warning "$zipName already exists - a real run would STOP here (no overwrite)."
    } else {
        throw "$zipName already exists. Published versions must stay valid - bump 'Version' in manifest.json (or remove the old zip yourself) before releasing."
    }
}

# --- Zip the build contents -------------------------------------------------
Write-Step "2/2  Building release zip"
$buildLink = Join-Path $RepoRoot 'Release\ExtraTerrainTools'
if (-not (Test-Path -LiteralPath $buildLink)) {
    throw "Build folder/symlink not found at: $buildLink"
}
$realBuildDir = Resolve-LinkTarget $buildLink
Write-Info "Build symlink: $buildLink"
Write-Info "Real contents: $realBuildDir"
Write-Info "Zip target:    $zipPath"

$files   = @(Get-ChildItem -LiteralPath $realBuildDir -Recurse -File)
$rootLen = $realBuildDir.TrimEnd('\', '/').Length + 1
Write-Info "Files to add:  $($files.Count)"

if ($DryRun) {
    Write-Action "Would write $zipName ($($files.Count) files, contents at zip root)"
    Write-Host "`nDry run complete." -ForegroundColor Yellow
    return
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($f in $files) {
        $entryName = $f.FullName.Substring($rootLen) -replace '\\', '/'
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $f.FullName, $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {
    $zip.Dispose()
}
Write-Action "Wrote $zipName"

Write-Host "`nDone. Review the zip, then commit manually." -ForegroundColor White
Write-Host "  - Zip created: $zipPath"
