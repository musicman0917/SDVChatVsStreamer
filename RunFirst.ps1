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
$steamPath = "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
$gogPath   = "C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley"
$smapiExe  = $null

foreach ($path in @($steamPath, $gogPath)) {
    $candidate = Join-Path $path "StardewModdingAPI.exe"
    if (Test-Path $candidate) { $smapiExe = $candidate; break }
}

if ($smapiExe) {
    Write-Host "   ✅ SMAPI found at: $smapiExe" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  SMAPI not found in default locations." -ForegroundColor Yellow
    Write-Host "   Make sure SMAPI is installed before launching the mod." -ForegroundColor Yellow
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