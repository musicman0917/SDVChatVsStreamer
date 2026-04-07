# ================================================================
#  Chat vs Streamer — Build & Package Script
#  Run this before creating a GitHub release.
#  Builds the mod and creates a zip ready to upload.
# ================================================================

$ErrorActionPreference = "Stop"

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectFile = Join-Path $scriptDir "ChatVsStreamer.csproj"

# ── Detect mod location ───────────────────────────────────────────
$stardropPath = "$env:APPDATA\Stardrop\Data\Selected Mods\ChatVsStreamer"
$defaultPath  = $null

# Try to find game path for default mods folder
$steamPaths = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\ChatVsStreamer",
    "C:\Program Files\Steam\steamapps\common\Stardew Valley\Mods\ChatVsStreamer"
)
foreach ($p in $steamPaths) {
    if (Test-Path (Split-Path $p)) { $defaultPath = $p; break }
}

$stardropFound = Test-Path "$env:APPDATA\Stardrop\Data\Selected Mods"

Write-Host ""
Write-Host "  Do you use Stardrop Mod Manager?" -ForegroundColor White
Write-Host ""
if ($stardropFound) { Write-Host "  (Stardrop detected on this machine)" -ForegroundColor Green }
Write-Host ""
Write-Host "    1) Yes, use Stardrop folder" -ForegroundColor Cyan
Write-Host "    2) No, use default Mods folder" -ForegroundColor Cyan
Write-Host ""
$choice = Read-Host "  Enter 1 or 2"

$modDir = switch ($choice.Trim()) {
    "1"     { $stardropPath }
    "2"     { $defaultPath ?? $stardropPath }
    default { $stardropPath }
}

Write-Host ""
Write-Host "  Packaging from: $modDir" -ForegroundColor Gray
Write-Host ""


# ── Get version from manifest ─────────────────────────────────────

$manifest = Get-Content (Join-Path $scriptDir "manifest.json") -Raw | ConvertFrom-Json
$version  = $manifest.Version
$zipName  = "ChatVsStreamer-v$version.zip"
$zipPath  = Join-Path $scriptDir "Version zips\$zipName"

# Create the folder if it doesn't exist
if (-not (Test-Path (Join-Path $scriptDir "Version zips"))) {
    New-Item -ItemType Directory -Path (Join-Path $scriptDir "Version zips") | Out-Null
}

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Chat vs Streamer — Build & Package" -ForegroundColor Cyan
Write-Host "  Version: $version" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# ── Build ─────────────────────────────────────────────────────────

Write-Host "Building..." -ForegroundColor Gray
dotnet build $projectFile -c Release --nologo -v quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Build failed. Fix errors before packaging." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✅ Build succeeded." -ForegroundColor Green
Write-Host ""

# ── Verify mod folder exists ──────────────────────────────────────

if (-not (Test-Path $modDir)) {
    Write-Host "❌ Mod output folder not found: $modDir" -ForegroundColor Red
    Write-Host "   Check your csproj ModFolderName setting." -ForegroundColor Gray
    Read-Host "Press Enter to exit"
    exit 1
}

# ── Remove old zip if exists ──────────────────────────────────────

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
    Write-Host "Removed old $zipName" -ForegroundColor DarkGray
}

# ── Create zip ────────────────────────────────────────────────────

Write-Host "Packaging $zipName..." -ForegroundColor Gray

# Create a temp staging folder so the zip contains ChatVsStreamer/
$stagingDir = Join-Path $env:TEMP "ChatVsStreamer-staging"
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
$stagingMod = Join-Path $stagingDir "ChatVsStreamer"
New-Item -ItemType Directory -Path $stagingMod | Out-Null

# Exclude secrets and personal data
$exclude = @("TwitchAuth", "secrets.json", "config.json", "ViewerLedger.db")

Get-ChildItem -Path $modDir | Where-Object { $exclude -notcontains $_.Name -and $_.Extension -ne ".zip" } | ForEach-Object {
    if ($_.PSIsContainer) {
        Copy-Item $_.FullName -Destination $stagingMod -Recurse -Force
    } else {
        Copy-Item $_.FullName -Destination $stagingMod -Force
    }
}

Compress-Archive -Path $stagingMod -DestinationPath $zipPath

Remove-Item $stagingDir -Recurse -Force

# ── Done ──────────────────────────────────────────────────────────

$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)

Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host "  ✅ Package ready!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  File:    $zipName" -ForegroundColor White
Write-Host "  Size:    $size MB" -ForegroundColor White
Write-Host "  Location: $zipPath" -ForegroundColor White
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor White
Write-Host "    1. Go to github.com/musicman0917/SDVChatVsStreamer/releases" -ForegroundColor Gray
Write-Host "    2. Click 'Draft a new release'" -ForegroundColor Gray
Write-Host "    3. Tag: v$version" -ForegroundColor Gray
Write-Host "    4. Upload $zipName as a release asset" -ForegroundColor Gray
Write-Host "    5. Paste patch notes and publish" -ForegroundColor Gray
Write-Host ""

# Open the project folder so they can grab the zip easily
Start-Process explorer.exe (Join-Path $scriptDir "Version zips")