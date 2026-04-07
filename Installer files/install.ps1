# ================================================================
#  Chat vs Streamer - Installer
#  Version 0.3.1 by NeighborhoodofMusic
# ================================================================

$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Chat vs Streamer Installer"

# -- Helpers ------------------------------------------------------

function Clear-Screen { Clear-Host }

function Write-Header {
    param([string]$Title, [string]$Step = "", [string]$Total = "")
    Clear-Screen
    Write-Host ""
    Write-Host "  +======================================================+" -ForegroundColor Cyan
    Write-Host "  *          CHAT VS STREAMER - INSTALLER                *" -ForegroundColor Cyan
    Write-Host "  *                    v0.3.1                            *" -ForegroundColor Cyan
    Write-Host "  +======================================================+" -ForegroundColor Cyan
    Write-Host ""
    if ($Step -and $Total) {
        Write-Host "  Step $Step of $Total - $Title" -ForegroundColor White
    } else {
        Write-Host "  $Title" -ForegroundColor White
    }
    Write-Host "  ------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""
}

function Write-Success { param([string]$msg) Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-Warn    { param([string]$msg) Write-Host "  [!!] $msg" -ForegroundColor Yellow }
function Write-Err     { param([string]$msg) Write-Host "  [XX] $msg" -ForegroundColor Red }
function Write-Info    { param([string]$msg) Write-Host "  [..] $msg" -ForegroundColor Gray }
function Write-Step    { param([string]$msg) Write-Host "  -->  $msg" -ForegroundColor Cyan }

function Prompt-Continue {
    Write-Host ""
    Write-Host "  Press ENTER to continue or CTRL+C to exit..." -ForegroundColor DarkGray
    Read-Host | Out-Null
}

function Prompt-YesNo {
    param([string]$Question)
    Write-Host ""
    $response = Read-Host "  $Question (y/n)"
    return $response.ToLower() -eq "y"
}

function Write-ProgressBar {
    param([int]$Percent, [string]$Label = "")
    $width   = 50
    $filled  = [math]::Floor($width * $Percent / 100)
    $empty   = $width - $filled
    $bar     = ("#" * $filled) + ("." * $empty)
    Write-Host "`r  [$bar] $Percent% $Label" -NoNewline -ForegroundColor Cyan
}

# -- Screen 1: Welcome ---------------------------------------------

function Show-Welcome {
    Write-Header "Welcome"
    Write-Host "  Welcome to the Chat vs Streamer installer!" -ForegroundColor White
    Write-Host ""
    Write-Host "  This mod lets your Twitch and TikTok viewers earn chaos" -ForegroundColor Gray
    Write-Host "  points just by watching - then spend them to sabotage" -ForegroundColor Gray
    Write-Host "  (or bless) your Stardew Valley farm in real time." -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Built by NeighborhoodofMusic" -ForegroundColor DarkGray
    Write-Host "  twitch.tv/neighborhoodofmusic" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  This installer will:" -ForegroundColor White
    Write-Host "    - Check for Stardew Valley and install SMAPI if needed" -ForegroundColor Gray
    Write-Host "    - Install the mod to your Mods folder" -ForegroundColor Gray
    Write-Host "    - Set up your Twitch credentials" -ForegroundColor Gray
    Write-Host "    - Optionally configure TikTok integration" -ForegroundColor Gray
    Write-Host "    - Show you how to set up OBS browser sources" -ForegroundColor Gray
    Write-Host ""
    Prompt-Continue
}

function Get-StreamingPlatform {
    Write-Header "Streaming Platform" "2" "11"

    Write-Host "  Where do you plan on streaming?" -ForegroundColor White
    Write-Host ""
    Write-Host "    1. Twitch only" -ForegroundColor Cyan
    Write-Host "    2. TikTok only" -ForegroundColor Cyan
    Write-Host "    3. Both Twitch and TikTok" -ForegroundColor Cyan
    Write-Host ""

    $choice = Read-Host "  Enter 1, 2, or 3"
    switch ($choice.Trim()) {
        "1" { return "twitch" }
        "2" { return "tiktok" }
        "3" { return "both"   }
        default {
            Write-Warn "Invalid choice - defaulting to Both."
            return "both"
        }
    }
}

# -- Screen 3: License ---------------------------------------------

function Show-License {
    Write-Header "License Agreement" "3" "11"
    Write-Host "  MIT License" -ForegroundColor White
    Write-Host ""
    Write-Host "  Copyright (c) 2026 NeighborhoodofMusic" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Permission is hereby granted, free of charge, to any" -ForegroundColor Gray
    Write-Host "  person obtaining a copy of this software to use, copy," -ForegroundColor Gray
    Write-Host "  modify, merge, publish, distribute, sublicense, and/or" -ForegroundColor Gray
    Write-Host "  sell copies of the Software, subject to the following:" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  The above copyright notice and this permission notice" -ForegroundColor Gray
    Write-Host "  shall be included in all copies or substantial portions." -ForegroundColor Gray
    Write-Host ""
    Write-Host "  THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF" -ForegroundColor Gray
    Write-Host "  ANY KIND. THE AUTHORS SHALL NOT BE LIABLE FOR ANY CLAIM," -ForegroundColor Gray
    Write-Host "  DAMAGES OR OTHER LIABILITY." -ForegroundColor Gray
    Write-Host ""

    $accept = Prompt-YesNo "Do you accept the license agreement?"
    if (-not $accept) {
        Write-Host ""
        Write-Err "License not accepted. Installation cancelled."
        Write-Host ""
        exit 0
    }
}

# -- Screen 3: Find Stardew Valley --------------------------------

function Find-StardewValley {
    Write-Header "Checking Prerequisites" "4" "11"
    Write-Step "Looking for Stardew Valley..."
    Write-Host ""

    $searchPaths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",
        "C:\Program Files\Steam\steamapps\common\Stardew Valley",
        "C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley",
        "C:\Program Files\GOG Galaxy\Games\Stardew Valley",
        "$env:USERPROFILE\AppData\Local\Programs\Stardew Valley"
    )

    # Check Steam registry for custom install path
    try {
        $steamPath = (Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue).SteamPath
        if ($steamPath) {
            $searchPaths += "$steamPath\steamapps\common\Stardew Valley"
        }
    } catch {}

    $gamePath = $null
    foreach ($path in $searchPaths) {
        if (Test-Path (Join-Path $path "Stardew Valley.exe")) {
            $gamePath = $path
            break
        }
    }

    if (-not $gamePath) {
        Write-Warn "Stardew Valley not found in default locations."
        Write-Host ""
        Write-Host "  A folder browser will open - navigate to your Stardew Valley" -ForegroundColor White
        Write-Host "  install folder and click OK." -ForegroundColor White
        Write-Host ""
        Read-Host "  Press ENTER to open the folder browser"

        # Open Windows folder browser dialog
        Add-Type -AssemblyName System.Windows.Forms
        $browser = New-Object System.Windows.Forms.FolderBrowserDialog
        $browser.Description   = "Select your Stardew Valley install folder"
        $browser.RootFolder    = [System.Environment+SpecialFolder]::MyComputer
        $browser.ShowNewFolderButton = $false

        $result = $browser.ShowDialog()

        if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
            $selected = $browser.SelectedPath
            if (Test-Path (Join-Path $selected "Stardew Valley.exe")) {
                $gamePath = $selected
            } else {
                Write-Err "Stardew Valley.exe not found in that folder."
                Write-Info "Please make sure you selected the Stardew Valley game folder."
                Read-Host "Press Enter to exit"
                exit 1
            }
        } else {
            Write-Err "No folder selected. Please install Stardew Valley first."
            Read-Host "Press Enter to exit"
            exit 1
        }
    }

    Write-Success "Found Stardew Valley at: $gamePath"
    return $gamePath.Trim()
}

# -- Screen 4: SMAPI -----------------------------------------------

function Install-SMAPI {
    param([string]$GamePath)
    Write-Header "SMAPI Check" "5" "11"

    $smapiExe = Join-Path $GamePath "StardewModdingAPI.exe"

    if (Test-Path $smapiExe) {
        Write-Success "SMAPI is already installed."
        Start-Sleep -Seconds 1
        return
    }

    Write-Warn "SMAPI is not installed."
    Write-Host ""
    Write-Info "SMAPI is required to run this mod."
    Write-Info "It is safe, widely used, and does not affect your saves."
    Write-Host ""

    $install = Prompt-YesNo "Download and install SMAPI now?"
    if (-not $install) {
        Write-Warn "SMAPI is required. Please install it from https://smapi.io then re-run this installer."
        Read-Host "Press Enter to exit"
        exit 1
    }

    Write-Host ""
    Write-Step "Fetching latest SMAPI release from GitHub..."

    try {
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/Pathoschild/SMAPI/releases/latest" -UseBasicParsing
        $asset   = $release.assets | Where-Object { $_.name -like "SMAPI-*-installer.zip" } | Select-Object -First 1

        if (-not $asset) { throw "Installer not found in release assets." }

        $zipPath     = Join-Path $env:TEMP "SMAPI-installer.zip"
        $extractPath = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "SMAPI-installer"

        Write-Step "Downloading $($asset.name)..."
        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -UseBasicParsing

        Write-Step "Extracting..."
        if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
        Expand-Archive -Path $zipPath -DestinationPath $extractPath
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

        $installer = Get-ChildItem -Path $extractPath -Filter "install on Windows.bat" -Recurse | Select-Object -First 1
        if (-not $installer) { throw "SMAPI installer not found after extraction." }

        Write-Host ""
        Write-Host "  +-------------------------------------------------+" -ForegroundColor Yellow
        Write-Host "  *  The SMAPI installer will open in a new window.  *" -ForegroundColor Yellow
        Write-Host "  *                                                   *" -ForegroundColor Yellow
        Write-Host "  *  Follow the prompts in that window, then come     *" -ForegroundColor Yellow
        Write-Host "  *  back here and press ENTER to continue.           *" -ForegroundColor Yellow
        Write-Host "  +-------------------------------------------------+" -ForegroundColor Yellow
        Write-Host ""
        Read-Host "  Press ENTER to launch the SMAPI installer"

        Start-Process -FilePath "cmd.exe" -ArgumentList "/c `"$($installer.FullName)`"" -WorkingDirectory $installer.DirectoryName

        Write-Host ""
        Read-Host "  Press ENTER once SMAPI is installed to continue"

        Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue

        if (Test-Path $smapiExe) {
            Write-Success "SMAPI installed successfully!"
        } else {
            Write-Warn "Could not verify SMAPI installation. Please check before launching."
        }
    }
    catch {
        Write-Err "Failed to download SMAPI: $_"
        Write-Info "Please install manually from https://smapi.io"
        Read-Host "Press Enter to exit"
        exit 1
    }
}

# -- Screen 5: Install Location -----------------------------------

function Get-ModsFolder {
    param([string]$GamePath)
    Write-Header "Mod Manager" "6" "11"

    $GamePath = $GamePath.Trim()

    # Check if Stardrop is installed
    $stardropPath = "$env:APPDATA\Stardrop\Data\Selected Mods"
    $stardropExe  = "$env:LOCALAPPDATA\Programs\Stardrop\Stardrop.exe"
    $stardropFound = (Test-Path $stardropExe) -or (Test-Path $stardropPath)

    Write-Host "  Do you use Stardrop Mod Manager?" -ForegroundColor White
    Write-Host ""

    if ($stardropFound) {
        Write-Success "Stardrop detected on this machine."
        Write-Host ""
    }

    Write-Host "    1. Yes, I use Stardrop" -ForegroundColor Cyan
    Write-Host "    2. No, I use the default Mods folder" -ForegroundColor Cyan
    Write-Host "    3. I use a custom folder" -ForegroundColor Cyan
    Write-Host ""

    $choice = Read-Host "  Enter 1, 2, or 3"

    switch ($choice.Trim()) {
        "1" {
            if (-not (Test-Path $stardropPath)) {
                New-Item -ItemType Directory -Path $stardropPath -Force | Out-Null
            }
            Write-Host ""
            Write-Success "Using Stardrop Mods folder: $stardropPath"
            return $stardropPath.Trim()
        }
        "2" {
            $defaultMods = Join-Path $GamePath "Mods"
            Write-Host ""
            Write-Success "Using default Mods folder: $defaultMods"
            return $defaultMods.Trim()
        }
        "3" {
            Write-Host ""
            Write-Host "  A folder browser will open -- select your Mods folder." -ForegroundColor White
            Write-Host ""
            Read-Host "  Press ENTER to open the folder browser"

            Add-Type -AssemblyName System.Windows.Forms
            $browser = New-Object System.Windows.Forms.FolderBrowserDialog
            $browser.Description = "Select your Stardew Valley Mods folder"
            $browser.RootFolder  = [System.Environment+SpecialFolder]::MyComputer
            $browser.ShowNewFolderButton = $true

            $result = $browser.ShowDialog()
            if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
                $custom = $browser.SelectedPath
                Write-Host ""
                Write-Success "Using custom folder: $custom"
                return $custom.Trim()
            } else {
                Write-Warn "No folder selected -- using default Mods folder."
                return (Join-Path $GamePath "Mods")
            }
        }
        default {
            Write-Warn "Invalid choice - using default Mods folder."
            return (Join-Path $GamePath "Mods")
        }
    }
}

# -- Screen 6: Install Files ---------------------------------------

function Install-ModFiles {
    param([string]$ModsFolder)
    Write-Header "Downloading & Installing" "7" "11"

    $ModsFolder  = $ModsFolder.Trim()
    $destFolder  = Join-Path $ModsFolder "ChatVsStreamer"

    if (Test-Path $destFolder) {
        Write-Warn "Existing installation found."
        $overwrite = Prompt-YesNo "Overwrite existing files?"
        if (-not $overwrite) {
            Write-Info "Installation cancelled - existing files kept."
            return $destFolder
        }
    }

    # -- Fetch latest release from GitHub -------------------------
    Write-Host ""
    Write-Step "Checking for latest version on GitHub..."

    try {
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/musicman0917/SDVChatVsStreamer/releases/latest" -UseBasicParsing
        $version = $release.tag_name
        $asset   = $release.assets | Where-Object { $_.name -like "ChatVsStreamer*.zip" } | Select-Object -First 1

        if (-not $asset) {
            throw "No mod zip found in latest release. Please download manually from github.com/musicman0917/SDVChatVsStreamer/releases"
        }

        Write-Success "Latest version: $version"
        Write-Host ""
        Write-Step "Downloading $($asset.name)..."

        $zipPath     = Join-Path $env:TEMP "ChatVsStreamer.zip"
        $extractPath = Join-Path $env:TEMP "ChatVsStreamer-install"

        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -UseBasicParsing
        Write-Host ""

        # -- Extract -----------------------------------------------
        Write-Step "Extracting..."
        if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
        Expand-Archive -Path $zipPath -DestinationPath $extractPath

        # Find the ChatVsStreamer folder inside the zip
        $modSource = Get-ChildItem -Path $extractPath -Filter "ChatVsStreamer" -Directory -Recurse | Select-Object -First 1
        if (-not $modSource) {
            # Zip might extract directly as the folder
            $modSource = Get-Item $extractPath
        }

        # -- Copy to Mods folder -----------------------------------
        Write-Step "Installing to Mods folder..."
        New-Item -ItemType Directory -Path $destFolder -Force | Out-Null

        $items = Get-ChildItem -Path $modSource.FullName
        $total = $items.Count
        $done  = 0

        foreach ($item in $items) {
            if ($item.PSIsContainer) {
                Copy-Item $item.FullName -Destination $destFolder -Recurse -Force
            } else {
                Copy-Item $item.FullName -Destination $destFolder -Force
            }
            $done++
            $pct = [math]::Floor($done / $total * 100)
            Write-ProgressBar $pct "Installing..."
        }

        Write-ProgressBar 100 "Done!"
        Write-Host ""
        Write-Host ""

        # -- Cleanup -----------------------------------------------
        Remove-Item $zipPath     -Force -ErrorAction SilentlyContinue
        Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue

        Write-Success "Chat vs Streamer $version installed!"
    }
    catch {
        Write-Err "Download failed: $_"
        Write-Host ""
        Write-Info "Please download manually from:"
        Write-Host "    https://github.com/musicman0917/SDVChatVsStreamer/releases" -ForegroundColor Cyan
        Read-Host "Press Enter to exit"
        exit 1
    }

    return $destFolder
}

# -- Screen 8: GMCM -----------------------------------------------

function Install-GMCM {
    param([string]$ModsFolder)
    Write-Header "Generic Mod Config Menu" "8" "11"

    $ModsFolder = $ModsFolder.Trim()
    $gmcmFolder = Join-Path $ModsFolder "GenericModConfigMenu"

    Write-Host "  Generic Mod Config Menu (GMCM) lets you configure" -ForegroundColor White
    Write-Host "  Chat vs Streamer settings from inside the game." -ForegroundColor White
    Write-Host ""
    Write-Host "  It's optional but highly recommended." -ForegroundColor Gray
    Write-Host ""

    if (Test-Path $gmcmFolder) {
        Write-Success "GMCM is already installed."
        Start-Sleep -Seconds 1
        return
    }

    $install = Prompt-YesNo "Download and install GMCM now?"
    if (-not $install) {
        Write-Info "Skipping GMCM - you can install it later from Nexus Mods."
        Write-Info "https://www.nexusmods.com/stardewvalley/mods/5098"
        Start-Sleep -Seconds 2
        return
    }

    Write-Host ""
    Write-Host "  GMCM is hosted on Nexus Mods." -ForegroundColor Gray
    Write-Host "  We'll open the download page for you." -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Instructions:" -ForegroundColor White
    Write-Host "    1. Click 'Mod Manager Download' or 'Manual Download'" -ForegroundColor Gray
    Write-Host "    2. Unzip the downloaded file" -ForegroundColor Gray
    Write-Host "    3. Copy the GenericModConfigMenu folder into your Mods folder" -ForegroundColor Gray
    Write-Host ""
    Read-Host "  Press ENTER to open the Nexus page"

    Start-Process "https://www.nexusmods.com/stardewvalley/mods/5098"

    Write-Host ""
    Read-Host "  Press ENTER once you've installed GMCM to continue"
}

# -- Screen 9: Twitch Setup ----------------------------------------

function Setup-Twitch {
    param([string]$ModFolder)
    Write-Header "Twitch Setup" "9" "11"

    $authDir    = Join-Path $ModFolder "TwitchAuth"
    $configFile = Join-Path $authDir "secrets.json"

    New-Item -ItemType Directory -Path $authDir -Force | Out-Null

    if (Test-Path $configFile) {
        Write-Info "Twitch credentials already configured."
        $reconfigure = Prompt-YesNo "Reconfigure Twitch credentials?"
        if (-not $reconfigure) { return }
    }

    Write-Host "  We need two things from Twitch to connect the mod:" -ForegroundColor White
    Write-Host "  a Client ID and an OAuth Token." -ForegroundColor White
    Write-Host ""
    Write-Host "  Don't worry - we'll walk you through both." -ForegroundColor Gray
    Write-Host ""
    Prompt-Continue

    # -- Step 1: Client ID -----------------------------------------
    Clear-Screen
    Write-Header "Twitch Setup - Client ID" "9" "11"

    Write-Host "  STEP 1 OF 2 - Get your Twitch Client ID" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  1. We'll open the Twitch Developer Console." -ForegroundColor White
    Write-Host "     Sign in with your Twitch account if prompted." -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Click 'Register Your Application'" -ForegroundColor White
    Write-Host ""
    Write-Host "  3. Fill in the form:" -ForegroundColor White
    Write-Host "       Name:          anything (e.g. ChatVsStreamer)" -ForegroundColor Gray
    Write-Host "       OAuth Redirect: http://localhost" -ForegroundColor Gray
    Write-Host "       Category:      Chat Bot" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. Click 'Create'" -ForegroundColor White
    Write-Host ""
    Write-Host "  5. Copy the Client ID shown on the next page." -ForegroundColor White
    Write-Host ""
    Read-Host "  Press ENTER to open the Twitch Developer Console"
    Start-Process "https://dev.twitch.tv/console/apps/create"

    Write-Host ""
    $clientId = Read-Host "  Paste your Client ID here"

    # -- Step 2: OAuth Token ---------------------------------------
    Clear-Screen
    Write-Header "Twitch Setup - OAuth Token" "9" "11"

    Write-Host "  STEP 2 OF 2 - Get your OAuth Token" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  BOT ACCOUNT NOTE:" -ForegroundColor Yellow
    Write-Host "  If you want a separate bot account to post messages in chat" -ForegroundColor Gray
    Write-Host "  (e.g. bardbouncerbot), sign into Twitch on the token generator" -ForegroundColor Gray
    Write-Host "  AS THAT BOT ACCOUNT before generating the token." -ForegroundColor Gray
    Write-Host "  Otherwise, sign in as your main channel account." -ForegroundColor Gray
    Write-Host ""
    Write-Host "  1. We'll open the Twitch Token Generator." -ForegroundColor White
    Write-Host "     Sign in with whichever account you want posting in chat." -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. A dialog will ask what type of token you want:" -ForegroundColor White
    Write-Host ""
    Write-Host "     BOT CHAT TOKEN" -ForegroundColor Yellow
    Write-Host "       Simpler setup. Covers chat only." -ForegroundColor Gray
    Write-Host "       Channel points and bits will NOT work." -ForegroundColor Gray
    Write-Host ""
    Write-Host "     CUSTOM SCOPE TOKEN  (recommended)" -ForegroundColor Green
    Write-Host "       Required for full functionality." -ForegroundColor Gray
    Write-Host "       Enable these four scopes:" -ForegroundColor Gray
    Write-Host "         [x] chat:read" -ForegroundColor Gray
    Write-Host "         [x] chat:edit" -ForegroundColor Gray
    Write-Host "         [x] channel:read:redemptions" -ForegroundColor Gray
    Write-Host "         [x] bits:read" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Click Generate Token and authorize the app." -ForegroundColor White
    Write-Host ""
    Write-Host "  4. Copy the ACCESS TOKEN (not the refresh token)." -ForegroundColor White
    Write-Host ""
    Read-Host "  Press ENTER to open the Token Generator"
    Start-Process "https://twitchtokengenerator.com"

    Write-Host ""
    $accessToken = Read-Host "  Paste your Access Token here"
    $accessToken  = $accessToken -replace "^oauth:", ""

    # -- Bot username ----------------------------------------------
    Clear-Screen
    Write-Header "Twitch Setup - Username" "9" "11"

    Write-Host "  LAST STEP - Enter the username of the account you just" -ForegroundColor Cyan
    Write-Host "  generated the token for." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  If you used your MAIN CHANNEL account on the token generator," -ForegroundColor Gray
    Write-Host "  enter your channel name here." -ForegroundColor Gray
    Write-Host ""
    Write-Host "  If you used a SEPARATE BOT ACCOUNT, enter that bot's" -ForegroundColor Gray
    Write-Host "  username here (e.g. bardbouncerbot)." -ForegroundColor Gray
    Write-Host ""

    $botUsername = Read-Host "  Twitch username (lowercase)"

    # -- Save ------------------------------------------------------
    $config = @{
        client_id    = $clientId.Trim()
        access_token = $accessToken.Trim()
        bot_username = $botUsername.Trim().ToLower()
    } | ConvertTo-Json -Depth 2

    Set-Content -Path $configFile -Value $config -Encoding UTF8
    Write-Host ""
    Write-Success "Twitch credentials saved!"
    Write-Host ""
    Write-Info "When you launch the game check the SMAPI console for:"
    Write-Host "    [TwitchManager] IRC connected" -ForegroundColor DarkGreen
    Write-Host "    [TwitchManager] PubSub connected" -ForegroundColor DarkGreen
    Start-Sleep -Seconds 2
}

# -- Screen 8: TikTok Setup ----------------------------------------

function Setup-TikTok {
    Write-Header "TikTok Integration" "10" "11"

    $tikfinityPaths = @(
        "$env:LOCALAPPDATA\Programs\Tikfinity\Tikfinity.exe",
        "$env:PROGRAMFILES\Tikfinity\Tikfinity.exe",
        "$env:LOCALAPPDATA\Programs\TikFinity\TikFinity.exe"
    )

    $tikfinityFound = $false
    foreach ($path in $tikfinityPaths) {
        if (Test-Path $path) { $tikfinityFound = $true; break }
    }

    if ($tikfinityFound) {
        Write-Success "Tikfinity is already installed!"
        Write-Host ""
        Write-Info "To enable TikTok integration:"
        Write-Info "  1. Launch Stardew Valley through SMAPI"
        Write-Info "  2. Open the in-game menu > Mod Config (GMCM)"
        Write-Info "  3. Find Chat vs Streamer > TikTok tab"
        Write-Info "  4. Enable TikTok Integration"
        Write-Info "  5. Launch Tikfinity and connect to TikTok LIVE before streaming"
        Prompt-Continue
        return
    }

    Write-Warn "Tikfinity is not installed."
    Write-Host ""
    Write-Host "  Tikfinity is required for TikTok integration." -ForegroundColor White
    Write-Host "  It connects your TikTok LIVE events to the mod." -ForegroundColor Gray
    Write-Host ""

    $install = Prompt-YesNo "Download and install Tikfinity now?"

    if (-not $install) {
        Write-Host ""
        Write-Info "You can install Tikfinity later from:"
        Write-Host "    https://tikfinity.zerody.one/app/" -ForegroundColor Cyan
        Write-Info "Then enable TikTok in the mod's GMCM settings."
        Prompt-Continue
        return
    }

    Write-Host ""
    Write-Step "Downloading Tikfinity installer..."

    try {
        $installerPath = Join-Path $env:TEMP "TikFinity_installer.exe"
        Invoke-WebRequest -Uri "https://tikfinity.zerody.one/download/win" -OutFile $installerPath -UseBasicParsing

        Write-Host ""
        Write-Host "  +-------------------------------------------------+" -ForegroundColor Yellow
        Write-Host "  *  The Tikfinity installer will open now.          *" -ForegroundColor Yellow
        Write-Host "  *  If Windows SmartScreen appears, click           *" -ForegroundColor Yellow
        Write-Host "  *  'More info' then 'Run anyway'.                  *" -ForegroundColor Yellow
        Write-Host "  *  Complete the install then return here.          *" -ForegroundColor Yellow
        Write-Host "  +-------------------------------------------------+" -ForegroundColor Yellow
        Write-Host ""
        Read-Host "  Press ENTER to launch the Tikfinity installer"

        Start-Process -FilePath $installerPath -Wait
        Remove-Item $installerPath -Force -ErrorAction SilentlyContinue

        # Check if it installed successfully
        $installedNow = $false
        foreach ($path in $tikfinityPaths) {
            if (Test-Path $path) { $installedNow = $true; break }
        }

        if ($installedNow) {
            Write-Success "Tikfinity installed successfully!"
        } else {
            Write-Warn "Tikfinity installer ran but could not verify installation."
            Write-Info "Please check that Tikfinity is installed before streaming."
        }
    }
    catch {
        Write-Warn "Could not download Tikfinity: $_"
        Write-Info "Please install manually from:"
        Write-Host "    https://tikfinity.zerody.one/app/" -ForegroundColor Cyan
    }

    Write-Host ""
    Write-Info "To enable TikTok integration after install:"
    Write-Info "  1. Launch Stardew Valley through SMAPI"
    Write-Info "  2. Open Mod Config (GMCM) > Chat vs Streamer > TikTok tab"
    Write-Info "  3. Enable TikTok Integration"
    Write-Info "  4. Launch Tikfinity and connect to TikTok LIVE before streaming"
    Prompt-Continue
}

# -- Screen 9: Complete --------------------------------------------

function Show-Complete {
    param([string]$Platform = "both")
    Write-Header "Installation Complete!" "11" "11"

    Write-Host "  +======================================================+" -ForegroundColor Green
    Write-Host "  *           Chat vs Streamer is installed!             *" -ForegroundColor Green
    Write-Host "  +======================================================+" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Next steps:" -ForegroundColor White
    Write-Host ""
    Write-Host "    1. Launch Stardew Valley through SMAPI" -ForegroundColor Gray
    Write-Host "    2. Load a save file" -ForegroundColor Gray

    if ($Platform -eq "twitch" -or $Platform -eq "both") {
        Write-Host "    3. Check the SMAPI console for:" -ForegroundColor Gray
        Write-Host "         [TwitchManager] IRC connected" -ForegroundColor DarkGreen
        Write-Host "         [TwitchManager] PubSub connected" -ForegroundColor DarkGreen
        Write-Host "    4. Type !shop in your Twitch chat to test it" -ForegroundColor Gray
    }

    if ($Platform -eq "tiktok" -or $Platform -eq "both") {
        Write-Host "    5. Launch Tikfinity and connect to TikTok LIVE before streaming" -ForegroundColor Gray
        Write-Host "       Check SMAPI console for:" -ForegroundColor Gray
        Write-Host "         [TikTokManager] Connected to Tikfinity" -ForegroundColor DarkGreen
    }

    Write-Host ""
    Write-Host "  OBS Browser Sources:" -ForegroundColor White
    Write-Host ""
    Write-Host "    Chat overlay  -->  http://localhost:7373/chat" -ForegroundColor Cyan
    Write-Host "    Shop sidebar  -->  http://localhost:7373/" -ForegroundColor Cyan
    Write-Host "    Mobile shop   -->  http://localhost:7373/mobile" -ForegroundColor Cyan

    if ($Platform -eq "tiktok" -or $Platform -eq "both") {
        Write-Host "    TikTok guide  -->  http://localhost:7373/tiktok" -ForegroundColor Cyan
    }

    Write-Host ""
    Write-Host "  GitHub:  github.com/musicman0917/SDVChatVsStreamer" -ForegroundColor DarkGray
    Write-Host "  Twitch:  twitch.tv/neighborhoodofmusic" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  Good luck on stream! ~" -ForegroundColor Cyan
    Write-Host ""
    Read-Host "  Press ENTER to exit"
}

# -- Main Flow -----------------------------------------------------

try {
    Show-Welcome
    $platform   = Get-StreamingPlatform
    Show-License
    $gamePath   = Find-StardewValley
    Install-SMAPI -GamePath $gamePath
    $modsFolder = Get-ModsFolder -GamePath $gamePath
    $modsFolder = $modsFolder.Trim()
    $modFolder  = Install-ModFiles -ModsFolder $modsFolder
    Install-GMCM -ModsFolder $modsFolder

    if ($platform -eq "twitch" -or $platform -eq "both") {
        Setup-Twitch -ModFolder $modFolder
    }

    if ($platform -eq "tiktok" -or $platform -eq "both") {
        Setup-TikTok
    }

    Show-Complete -Platform $platform
}
catch {
    Write-Host ""
    Write-Host "  [XX] An unexpected error occurred:" -ForegroundColor Red
    Write-Host "     $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Please report this at github.com/musicman0917/SDVChatVsStreamer/issues" -ForegroundColor Gray
    Write-Host ""
    Read-Host "  Press ENTER to exit"
    exit 1
}