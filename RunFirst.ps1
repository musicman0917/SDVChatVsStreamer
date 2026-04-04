# ============================================================
#  Chat vs Streamer — First Run Setup
#  Run this once after installing the mod to configure auth.
# ============================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   Chat vs Streamer — First Run Setup" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# ─── Locate mod folder ───────────────────────────────────────────────────────

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$authDir    = Join-Path $scriptDir "TwitchAuth"
$configFile = Join-Path $authDir "config.json"
$template   = Join-Path $authDir "config.template.json"

if (-not (Test-Path $authDir)) {
    New-Item -ItemType Directory -Path $authDir | Out-Null
}

# ─── Check if already configured ─────────────────────────────────────────────

if (Test-Path $configFile) {
    Write-Host "⚠️  TwitchAuth\config.json already exists." -ForegroundColor Yellow
    $overwrite = Read-Host "   Overwrite it? (y/n)"
    if ($overwrite -ne "y") {
        Write-Host ""
        Write-Host "✅ Setup skipped — existing config kept." -ForegroundColor Green
        exit 0
    }
}

# ─── Check SMAPI ─────────────────────────────────────────────────────────────

Write-Host "Checking for SMAPI..." -ForegroundColor Gray

$gamePaths = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",
    "C:\Program Files\Steam\steamapps\common\Stardew Valley",
    "C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley",
    "C:\Program Files\GOG Galaxy\Games\Stardew Valley"
)

$gamePath  = $null
$smapiExe  = $null

foreach ($path in $gamePaths) {
    if (Test-Path (Join-Path $path "Stardew Valley.exe")) {
        $gamePath = $path
        $candidate = Join-Path $path "StardewModdingAPI.exe"
        if (Test-Path $candidate) { $smapiExe = $candidate }
        break
    }
}

if ($smapiExe) {
    Write-Host "   ✅ SMAPI found." -ForegroundColor Green
}
elseif ($gamePath) {
    Write-Host "   ⚠️  Stardew Valley found but SMAPI is not installed." -ForegroundColor Yellow
    $installSmapi = Read-Host "   Install SMAPI now? (y/n)"

    if ($installSmapi -eq "y") {
        Write-Host "   Fetching latest SMAPI release..." -ForegroundColor Gray

        try {
            $release  = Invoke-RestMethod -Uri "https://api.github.com/repos/Pathoschild/SMAPI/releases/latest" -UseBasicParsing
            $asset    = $release.assets | Where-Object { $_.name -like "SMAPI-*-installer.zip" } | Select-Object -First 1

            if (-not $asset) {
                Write-Host "   ❌ Could not find SMAPI installer in release assets." -ForegroundColor Red
            } else {
                $zipPath = Join-Path $env:TEMP "SMAPI-installer.zip"
                $extractPath = Join-Path $env:TEMP "SMAPI-installer"

                Write-Host "   Downloading $($asset.name)..." -ForegroundColor Gray
                Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -UseBasicParsing

                Write-Host "   Extracting..." -ForegroundColor Gray
                if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
                Expand-Archive -Path $zipPath -DestinationPath $extractPath

                $installer = Get-ChildItem -Path $extractPath -Filter "install on Windows.bat" -Recurse | Select-Object -First 1
                if ($installer) {
                    Write-Host "   Launching SMAPI installer — follow the prompts in the new window." -ForegroundColor Cyan
                    Start-Process -FilePath $installer.FullName -Wait
                    Write-Host "   ✅ SMAPI installation complete." -ForegroundColor Green
                } else {
                    Write-Host "   ❌ Installer batch file not found. Please install SMAPI manually from https://smapi.io" -ForegroundColor Red
                }

                # Cleanup
                Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
                Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
            Write-Host "   ❌ Failed to download SMAPI: $_" -ForegroundColor Red
            Write-Host "   Please install manually from https://smapi.io" -ForegroundColor Yellow
        }
    }
}
else {
    Write-Host "   ❌ Stardew Valley not found in default locations." -ForegroundColor Red
    Write-Host "   Please install Stardew Valley first, then re-run this script." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""

# ─── Twitch credentials ───────────────────────────────────────────────────────

Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host " Twitch Credentials" -ForegroundColor White
Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host ""
Write-Host " You'll need:" -ForegroundColor Gray
Write-Host "   • A Twitch Client ID  →  dev.twitch.tv/console" -ForegroundColor Gray
Write-Host "   • An OAuth token      →  twitchtokengenerator.com" -ForegroundColor Gray
Write-Host "     (Scopes: chat:read  chat:edit  channel:read:redemptions  bits:read)" -ForegroundColor Gray
Write-Host "   • Your bot or channel username (lowercase)" -ForegroundColor Gray
Write-Host ""

$clientId    = Read-Host " Client ID"
$accessToken = Read-Host " OAuth Token (without 'oauth:' prefix)"
$botUsername = Read-Host " Bot/Channel Username"

# Strip oauth: prefix if user included it
$accessToken = $accessToken -replace "^oauth:", ""

# ─── Write config ─────────────────────────────────────────────────────────────

$config = @{
    ClientId    = $clientId.Trim()
    AccessToken = $accessToken.Trim()
    BotUsername = $botUsername.Trim().ToLower()
} | ConvertTo-Json -Depth 2

Set-Content -Path $configFile -Value $config -Encoding UTF8

Write-Host ""
Write-Host "   ✅ Config written to TwitchAuth\config.json" -ForegroundColor Green

# ─── Tikfinity check ─────────────────────────────────────────────────────────

Write-Host ""
Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host " TikTok Integration (Optional)" -ForegroundColor White
Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host ""

$tikfinityPath = Join-Path $env:LOCALAPPDATA "Programs\Tikfinity\Tikfinity.exe"
if (Test-Path $tikfinityPath) {
    Write-Host "   ✅ Tikfinity found!" -ForegroundColor Green
    Write-Host "   Make sure it's running and connected to TikTok LIVE before streaming." -ForegroundColor Gray
} else {
    Write-Host "   ℹ️  Tikfinity not found." -ForegroundColor Gray
    Write-Host "   To enable TikTok integration, download Tikfinity from:" -ForegroundColor Gray
    Write-Host "   https://tikfinity.zerinity.com/" -ForegroundColor Cyan
    Write-Host "   Then enable TikTok in the mod's GMCM settings." -ForegroundColor Gray
}

# ─── Done ─────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   Setup Complete!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host " Next steps:" -ForegroundColor White
Write-Host "   1. Launch Stardew Valley through SMAPI" -ForegroundColor Gray
Write-Host "   2. Load a save file" -ForegroundColor Gray
Write-Host "   3. Check SMAPI console for:" -ForegroundColor Gray
Write-Host "      [TwitchManager] IRC connected" -ForegroundColor DarkGreen
Write-Host "      [TwitchManager] PubSub connected" -ForegroundColor DarkGreen
Write-Host "   4. Viewers can type !shop in your Twitch chat to get started" -ForegroundColor Gray
Write-Host ""
Write-Host " OBS Browser Sources (add these to your scenes):" -ForegroundColor White
Write-Host "   Chat overlay  →  http://localhost:7373/chat" -ForegroundColor Gray
Write-Host "   Shop sidebar  →  http://localhost:7373/" -ForegroundColor Gray
Write-Host "   Mobile shop   →  http://localhost:7373/mobile" -ForegroundColor Gray
Write-Host ""
Write-Host " Good luck on stream! 🌾" -ForegroundColor Cyan
Write-Host ""