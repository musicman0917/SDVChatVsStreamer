# ================================================================
#  Chat vs Streamer -- Update Script
#  Downloads and installs the latest version from GitHub.
#  Your credentials and save data are preserved.
# ================================================================

$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Chat vs Streamer Updater"

function Clear-Screen { Clear-Host }

function Write-Header {
    param([string]$Title)
    Clear-Screen
    Write-Host ""
    Write-Host "  +======================================================+" -ForegroundColor Cyan
    Write-Host "  *          CHAT VS STREAMER -- UPDATER                 *" -ForegroundColor Cyan
    Write-Host "  +======================================================+" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  $Title" -ForegroundColor White
    Write-Host "  ------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""
}

function Write-Success { param([string]$msg) Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-Warn    { param([string]$msg) Write-Host "  [!!] $msg" -ForegroundColor Yellow }
function Write-Err     { param([string]$msg) Write-Host "  [XX] $msg" -ForegroundColor Red }
function Write-Info    { param([string]$msg) Write-Host "  [..] $msg" -ForegroundColor Gray }
function Write-Step    { param([string]$msg) Write-Host "  -->  $msg" -ForegroundColor Cyan }

# ----------------------------------------------------------------

Write-Header "Finding your mod installation..."

# Check Stardrop first, then default Steam/GOG paths
$candidates = @(
    "$env:APPDATA\Stardrop\Data\Selected Mods\ChatVsStreamer",
    "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\ChatVsStreamer",
    "C:\Program Files\Steam\steamapps\common\Stardew Valley\Mods\ChatVsStreamer",
    "C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley\Mods\ChatVsStreamer",
    "C:\Program Files\GOG Galaxy\Games\Stardew Valley\Mods\ChatVsStreamer"
)

$modFolder = $null
foreach ($path in $candidates) {
    if (Test-Path (Join-Path $path "manifest.json")) {
        $modFolder = $path
        break
    }
}

if (-not $modFolder) {
    Write-Host ""
    Write-Warn "Chat vs Streamer not found in default locations."
    Write-Host ""
    Write-Host "  A folder browser will open -- navigate to your" -ForegroundColor White
    Write-Host "  ChatVsStreamer mod folder and click OK." -ForegroundColor White
    Write-Host ""
    Read-Host "  Press ENTER to open the folder browser"

    Add-Type -AssemblyName System.Windows.Forms
    $browser = New-Object System.Windows.Forms.FolderBrowserDialog
    $browser.Description = "Select your ChatVsStreamer mod folder"
    $browser.RootFolder  = [System.Environment+SpecialFolder]::MyComputer
    $browser.ShowNewFolderButton = $false

    $result = $browser.ShowDialog()
    if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
        $modFolder = $browser.SelectedPath.Trim()
    } else {
        Write-Err "No folder selected. Exiting."
        Read-Host "Press Enter to exit"
        exit 1
    }
}

$modFolder = $modFolder.Trim()

# Read current version from manifest
$currentVersion = "unknown"
try {
    $manifest = Get-Content (Join-Path $modFolder "manifest.json") -Raw | ConvertFrom-Json
    $currentVersion = $manifest.Version
} catch {}

Write-Success "Found mod at: $modFolder"
Write-Info "Current version: $currentVersion"

# ----------------------------------------------------------------

Write-Host ""
Write-Step "Checking GitHub for latest version..."

try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/musicman0917/SDVChatVsStreamer/releases/latest" -UseBasicParsing
    $latestVersion = $release.tag_name -replace "^v", ""
    $asset = $release.assets | Where-Object { $_.name -like "ChatVsStreamer*.zip" } | Select-Object -First 1

    if (-not $asset) {
        throw "No mod zip found in latest release."
    }

    Write-Success "Latest version: v$latestVersion"

    if ($currentVersion -eq $latestVersion) {
        Write-Host ""
        Write-Host "  You are already on the latest version!" -ForegroundColor Green
        Write-Host ""
        Read-Host "  Press ENTER to exit"
        exit 0
    }

    Write-Host ""
    Write-Host "  Update available: v$currentVersion --> v$latestVersion" -ForegroundColor Yellow
    Write-Host ""
    $confirm = Read-Host "  Install update now? (y/n)"
    if ($confirm.ToLower() -ne "y") {
        Write-Info "Update cancelled."
        Read-Host "Press Enter to exit"
        exit 0
    }

    # ----------------------------------------------------------------

    Write-Host ""
    Write-Step "Downloading $($asset.name)..."

    $zipPath     = Join-Path $env:TEMP "ChatVsStreamer-update.zip"
    $extractPath = Join-Path $env:TEMP "ChatVsStreamer-update"

    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -UseBasicParsing
    Write-Success "Download complete."

    Write-Step "Extracting..."
    if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
    Expand-Archive -Path $zipPath -DestinationPath $extractPath

    $modSource = Get-ChildItem -Path $extractPath -Filter "ChatVsStreamer" -Directory -Recurse | Select-Object -First 1
    if (-not $modSource) { $modSource = Get-Item $extractPath }

    # ----------------------------------------------------------------

    Write-Step "Installing update..."

    # Files and folders to never overwrite
    $preserve = @("TwitchAuth", "secrets.json", "config.json", "ViewerLedger.db")

    Get-ChildItem -Path $modSource.FullName | Where-Object { $preserve -notcontains $_.Name } | ForEach-Object {
        if ($_.PSIsContainer) {
            Copy-Item $_.FullName -Destination $modFolder -Recurse -Force
        } else {
            Copy-Item $_.FullName -Destination $modFolder -Force
        }
    }

    # Cleanup
    Remove-Item $zipPath     -Force -ErrorAction SilentlyContinue
    Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue

    # ----------------------------------------------------------------

    Write-Host ""
    Write-Host "  +======================================================+" -ForegroundColor Green
    Write-Host "  *   Chat vs Streamer updated to v$latestVersion!           *" -ForegroundColor Green
    Write-Host "  +======================================================+" -ForegroundColor Green
    Write-Host ""
    Write-Info "Your credentials and save data were preserved."
    Write-Info "Launch Stardew Valley through SMAPI to play."
    Write-Host ""
    Read-Host "  Press ENTER to exit"
}
catch {
    Write-Host ""
    Write-Err "Update failed: $_"
    Write-Info "Please update manually from:"
    Write-Host "    https://github.com/musicman0917/SDVChatVsStreamer/releases" -ForegroundColor Cyan
    Write-Host ""
    Read-Host "  Press ENTER to exit"
    exit 1
}
