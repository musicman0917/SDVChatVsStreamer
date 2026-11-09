# ================================================================
#  Chat vs Streamer — Nexus Upload Packager
#  Run package.ps1 FIRST to build and zip the mod -- this script
#  doesn't build anything itself. It just gathers what you need for
#  a Nexus Mods upload (the zip + the description reference text)
#  into one folder, and sanity-checks the zip actually has the
#  installer bundled in before you upload something incomplete.
# ================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Chat vs Streamer — Nexus Upload Packager" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# ── Find the zip package.ps1 should have already built ─────────────

$manifest = Get-Content (Join-Path $scriptDir "manifest.json") -Raw | ConvertFrom-Json
$version  = $manifest.Version
$zipName  = "ChatVsStreamer-v$version.zip"
$zipPath  = Join-Path $scriptDir "Version zips\$zipName"

if (-not (Test-Path $zipPath)) {
    Write-Host "❌ $zipName not found in 'Version zips\'." -ForegroundColor Red
    Write-Host "   Run package.ps1 first to build and zip the mod, then run this again." -ForegroundColor Gray
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Found: $zipName (version $version)" -ForegroundColor Gray
Write-Host ""

# ── Sanity-check the zip actually has what Nexus users need ────────
# package.ps1 used to produce a mod-only zip with no installer in it at all -- this
# catches that class of mistake again before it reaches an actual upload.

Write-Host "Verifying zip contents..." -ForegroundColor Gray

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    # Compress-Archive is known to write nested entries with backslashes
    # (ChatVsStreamer\ChatVsStreamer.dll) instead of the forward slashes the ZIP
    # spec normally expects -- normalize so the folder check below doesn't miss
    # real entries just because of which separator this run happened to use.
    $entryNames = $zip.Entries | ForEach-Object { $_.FullName -replace '\\', '/' }
} finally {
    $zip.Dispose()
}

$missing = @()
foreach ($f in @("install.ps1", "RunMe.bat")) {
    if ($entryNames -notcontains $f) { $missing += $f }
}
if (-not ($entryNames | Where-Object { $_ -like "ChatVsStreamer/*" })) {
    $missing += "ChatVsStreamer/ (mod folder)"
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "⚠️  $zipName is missing: $($missing -join ', ')" -ForegroundColor Yellow
    Write-Host "   Re-run package.ps1 to rebuild it, then run this script again." -ForegroundColor Gray
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✅ Verified: contains ChatVsStreamer/, Install.ps1, and RunMe.bat" -ForegroundColor Green
Write-Host ""

# ── Stage a dedicated Nexus upload folder ───────────────────────────

$nexusDir = Join-Path $scriptDir "Nexus upload"
if (Test-Path $nexusDir) { Remove-Item $nexusDir -Recurse -Force }
New-Item -ItemType Directory -Path $nexusDir | Out-Null

Copy-Item $zipPath -Destination $nexusDir -Force

$descSource = Join-Path $scriptDir "NEXUS_DESCRIPTION.txt"
if (Test-Path $descSource) {
    Copy-Item $descSource -Destination $nexusDir -Force
} else {
    Write-Host "⚠️  NEXUS_DESCRIPTION.txt not found in the repo root -- skipping." -ForegroundColor Yellow
}

# ── Done ──────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host "  ✅ Nexus upload folder ready!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Folder:  $nexusDir" -ForegroundColor White
Write-Host "  Zip:     $zipName" -ForegroundColor White
Write-Host "  Version: $version" -ForegroundColor White
Write-Host ""
Write-Host "  On the Nexus page:" -ForegroundColor White
Write-Host "    Files tab   -> upload $zipName, category Main, version $version" -ForegroundColor Gray
Write-Host "    Description -> paste sections from NEXUS_DESCRIPTION.txt into the matching" -ForegroundColor Gray
Write-Host "                   fields (Description / Installation instructions / Main" -ForegroundColor Gray
Write-Host "                   features / Requirements / Shout outs)" -ForegroundColor Gray
Write-Host ""

Start-Process explorer.exe $nexusDir
