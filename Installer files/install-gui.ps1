# ================================================================
#  Chat vs Streamer - GUI Installer (WinForms)
#  Version 0.6.0 by NeighborhoodofMusic
#
#  A graphical alternative to install.ps1. Same steps, same logic,
#  same file paths and secrets.json format -- just windows and
#  buttons instead of console prompts. install.ps1 remains the
#  primary/most-tested installer; this is an additional option.
# ================================================================

$ErrorActionPreference = "Stop"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

# -- Shared state -------------------------------------------------

$script:State = @{
    UseTikTok  = $false
    UseYouTube = $false
    GamePath   = $null
    ModsFolder = $null   # parent Mods folder (or Stardrop's "Selected Mods")
    ModFolder  = $null   # ...\ChatVsStreamer
}

$script:StepCounter = 0
$script:StepTotal   = 9
$script:OnNext       = { }

# -- Form shell -----------------------------------------------------

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text            = "Chat vs Streamer Installer"
$script:Form.ClientSize      = New-Object System.Drawing.Size(640, 560)
$script:Form.StartPosition   = "CenterScreen"
$script:Form.FormBorderStyle = "FixedDialog"
$script:Form.MaximizeBox     = $false
$script:Form.MinimizeBox     = $false

$script:HeaderPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:HeaderPanel.Dock          = "Top"
$script:HeaderPanel.FlowDirection = "TopDown"
$script:HeaderPanel.WrapContents  = $false
$script:HeaderPanel.AutoSize      = $true
$script:HeaderPanel.AutoSizeMode  = "GrowAndShrink"
$script:HeaderPanel.Padding       = New-Object System.Windows.Forms.Padding(20, 14, 10, 10)

$script:HeaderTitleLabel = New-Object System.Windows.Forms.Label
$script:HeaderTitleLabel.AutoSize = $true
$script:HeaderTitleLabel.Font     = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$script:HeaderPanel.Controls.Add($script:HeaderTitleLabel)

$script:HeaderStepLabel = New-Object System.Windows.Forms.Label
$script:HeaderStepLabel.AutoSize  = $true
$script:HeaderStepLabel.Font      = New-Object System.Drawing.Font("Segoe UI", 9)
$script:HeaderStepLabel.ForeColor = [System.Drawing.Color]::DimGray
$script:HeaderStepLabel.Margin    = New-Object System.Windows.Forms.Padding(0, 2, 0, 0)
$script:HeaderPanel.Controls.Add($script:HeaderStepLabel)

$script:Form.Controls.Add($script:HeaderPanel)

$script:ContentPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:ContentPanel.Dock          = "Fill"
$script:ContentPanel.FlowDirection = "TopDown"
$script:ContentPanel.WrapContents  = $false
$script:ContentPanel.AutoScroll    = $true
$script:ContentPanel.Padding       = New-Object System.Windows.Forms.Padding(20)
$script:Form.Controls.Add($script:ContentPanel)

$script:FooterPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:FooterPanel.Dock          = "Bottom"
$script:FooterPanel.Height        = 54
$script:FooterPanel.FlowDirection = "RightToLeft"
$script:FooterPanel.Padding       = New-Object System.Windows.Forms.Padding(12)

$script:NextButton = New-Object System.Windows.Forms.Button
$script:NextButton.Text = "Next >"
$script:NextButton.Size = New-Object System.Drawing.Size(110, 32)
$script:NextButton.Add_Click({ & $script:OnNext })
$script:FooterPanel.Controls.Add($script:NextButton)

$script:CancelButton = New-Object System.Windows.Forms.Button
$script:CancelButton.Text = "Cancel"
$script:CancelButton.Size = New-Object System.Drawing.Size(110, 32)
$script:CancelButton.Margin = New-Object System.Windows.Forms.Padding(0, 0, 10, 0)
$script:CancelButton.Add_Click({ $script:Form.Close() })
$script:FooterPanel.Controls.Add($script:CancelButton)

$script:Form.Controls.Add($script:FooterPanel)

# -- Helpers --------------------------------------------------------

function New-WrapLabel {
    param(
        [string]$Text,
        [int]$FontSize = 9,
        [switch]$Bold,
        [System.Drawing.Color]$Color = [System.Drawing.SystemColors]::ControlText
    )
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Text = $Text
    $lbl.AutoSize = $true
    $lbl.MaximumSize = New-Object System.Drawing.Size(560, 0)
    $style = if ($Bold) { [System.Drawing.FontStyle]::Bold } else { [System.Drawing.FontStyle]::Regular }
    $lbl.Font = New-Object System.Drawing.Font("Segoe UI", $FontSize, $style)
    $lbl.ForeColor = $Color
    $lbl.Margin = New-Object System.Windows.Forms.Padding(0, 0, 0, 10)
    return $lbl
}

function Set-Header {
    param([string]$Title, [bool]$ShowStep = $true)
    $script:HeaderTitleLabel.Text = $Title
    if ($ShowStep) {
        $script:StepCounter++
        $script:HeaderStepLabel.Text = "Step $script:StepCounter of $script:StepTotal"
        $script:HeaderStepLabel.Visible = $true
    } else {
        $script:HeaderStepLabel.Visible = $false
    }
    $script:ContentPanel.Controls.Clear()
    $script:NextButton.Text = "Next >"
    $script:NextButton.Enabled = $true
}

function Set-SubHeader {
    # Same numbered step as the current screen, just a different sub-screen --
    # does not touch $script:StepCounter.
    param([string]$Title)
    $script:HeaderTitleLabel.Text = $Title
    $script:HeaderStepLabel.Text = "Step $script:StepCounter of $script:StepTotal"
    $script:HeaderStepLabel.Visible = $true
    $script:ContentPanel.Controls.Clear()
    $script:NextButton.Text = "Next >"
    $script:NextButton.Enabled = $true
}

function Show-ErrorBox {
    param([string]$Message)
    [System.Windows.Forms.MessageBox]::Show($Message, "Chat vs Streamer Installer",
        [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
}

function Confirm-YesNo {
    param([string]$Message, [string]$Title = "Chat vs Streamer Installer")
    $r = [System.Windows.Forms.MessageBox]::Show($Message, $Title,
        [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Question)
    return ($r -eq [System.Windows.Forms.DialogResult]::Yes)
}

# Downloads a file with a real progress bar, without freezing the UI thread.
function Invoke-DownloadWithProgress {
    param(
        [Parameter(Mandatory)][string]$Url,
        [Parameter(Mandatory)][string]$DestPath,
        [System.Windows.Forms.ProgressBar]$ProgressBar
    )

    $webClient = New-Object System.Net.WebClient
    $ctx = @{ Done = $false; Error = $null; Bar = $ProgressBar }

    $subProgress = Register-ObjectEvent -InputObject $webClient -EventName DownloadProgressChanged -MessageData $ctx -Action {
        if ($Event.MessageData.Bar) { $Event.MessageData.Bar.Value = [Math]::Min([int]$EventArgs.ProgressPercentage, 100) }
    }
    $subComplete = Register-ObjectEvent -InputObject $webClient -EventName DownloadFileCompleted -MessageData $ctx -Action {
        $Event.MessageData.Done = $true
        if ($EventArgs.Error) { $Event.MessageData.Error = $EventArgs.Error }
    }

    try {
        $webClient.DownloadFileAsync([Uri]$Url, $DestPath)
        while (-not $ctx.Done) {
            [System.Windows.Forms.Application]::DoEvents()
            Start-Sleep -Milliseconds 50
        }
    }
    finally {
        Unregister-Event -SourceIdentifier $subProgress.Name -ErrorAction SilentlyContinue
        Unregister-Event -SourceIdentifier $subComplete.Name -ErrorAction SilentlyContinue
        $webClient.Dispose()
    }

    if ($ctx.Error) { throw $ctx.Error }
}

# Waits for an external process to exit without freezing the UI thread.
function Wait-ProcessResponsive {
    param([System.Diagnostics.Process]$Process)
    while (-not $Process.HasExited) {
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 100
    }
}

# -- Screen: Welcome -------------------------------------------------

function Show-Welcome {
    Set-Header -Title "Welcome to Chat vs Streamer" -ShowStep $false

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "This mod lets your Twitch viewers earn chaos points just by watching - then spend them to sabotage (or bless) your Stardew Valley farm in real time." -FontSize 10))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Optional extras: YouTube Live chat (beta), TikTok Live (via Tikfinity), and DonorDrive/Extra Life donations." -FontSize 10))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Built by NeighborhoodofMusic - twitch.tv/neighborhoodofmusic" -FontSize 9 -Color ([System.Drawing.Color]::Gray)))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "This installer will:" -FontSize 10 -Bold))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "- Check for Stardew Valley and install SMAPI if needed`n- Install the mod to your Mods folder`n- Set up your Twitch app credentials`n- Walk you through the in-game auth flow`n- Optionally set up TikTok and/or YouTube (both beta/at your own risk)" -FontSize 9))

    $script:OnNext = { Show-OptionalPlatforms }
}

# -- Screen: Optional Platforms ---------------------------------------

function Show-OptionalPlatforms {
    Set-Header -Title "Additional Platforms"

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Twitch is always set up - it's the core of the mod. Optionally add either (or both) of these too:" -FontSize 10))

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "TikTok Live (via Tikfinity)" -FontSize 10 -Bold))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Unsupported, provided as-is. TikTok's API policies may change at any time and could break this feature." -FontSize 9 -Color ([System.Drawing.Color]::DimGray)))
    $tikTokCheck = New-Object System.Windows.Forms.CheckBox
    $tikTokCheck.Text = "Set up TikTok integration"
    $tikTokCheck.AutoSize = $true
    $tikTokCheck.Margin = New-Object System.Windows.Forms.Padding(0, 0, 0, 16)
    $tikTokCheck.Checked = $script:State.UseTikTok
    $script:ContentPanel.Controls.Add($tikTokCheck)

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "YouTube Live chat (via Streamer.bot)" -FontSize 10 -Bold))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Beta - experimental and not yet fully tested. Requires Streamer.bot running separately and connected to YouTube." -FontSize 9 -Color ([System.Drawing.Color]::DimGray)))
    $youTubeCheck = New-Object System.Windows.Forms.CheckBox
    $youTubeCheck.Text = "Set up YouTube integration"
    $youTubeCheck.AutoSize = $true
    $youTubeCheck.Checked = $script:State.UseYouTube
    $script:ContentPanel.Controls.Add($youTubeCheck)

    $script:tikTokCheck  = $tikTokCheck
    $script:youTubeCheck = $youTubeCheck

    $script:OnNext = {
        $script:State.UseTikTok  = $script:tikTokCheck.Checked
        $script:State.UseYouTube = $script:youTubeCheck.Checked
        # Base flow: Platforms, License, Find Game, SMAPI, Mods Folder, Install Files,
        # GMCM, Twitch Setup, Complete = 9, plus one more for each optional platform.
        $script:StepTotal = 9 + [int]$script:State.UseTikTok + [int]$script:State.UseYouTube
        Show-License
    }
}

# -- Screen: License ---------------------------------------------------

function Show-License {
    Set-Header -Title "License Agreement"

    $licenseBox = New-Object System.Windows.Forms.TextBox
    $licenseBox.Multiline = $true
    $licenseBox.ReadOnly = $true
    $licenseBox.ScrollBars = "Vertical"
    $licenseBox.Size = New-Object System.Drawing.Size(560, 280)
    $licenseBox.Margin = New-Object System.Windows.Forms.Padding(0, 0, 0, 12)
    $licenseBox.Font = New-Object System.Drawing.Font("Consolas", 9)
    $licenseBox.Text = @"
MIT License

Copyright (c) 2026 NeighborhoodofMusic

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, subject to the following:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND. THE
AUTHORS SHALL NOT BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY.
"@
    $script:ContentPanel.Controls.Add($licenseBox)

    $acceptCheck = New-Object System.Windows.Forms.CheckBox
    $acceptCheck.Text = "I accept the license agreement"
    $acceptCheck.AutoSize = $true
    $script:ContentPanel.Controls.Add($acceptCheck)

    $script:NextButton.Enabled = $false
    $acceptCheck.Add_CheckedChanged({
        $script:NextButton.Enabled = $this.Checked
        $script:NextButton.Refresh()
    })

    $script:OnNext = { Show-FindGame }
}

# -- Screen: Find Stardew Valley ---------------------------------------

function Show-FindGame {
    Set-Header -Title "Checking Prerequisites"
    $script:NextButton.Enabled = $false

    $script:statusLabel = New-WrapLabel -Text "Looking for Stardew Valley..." -FontSize 10
    $script:ContentPanel.Controls.Add($script:statusLabel)

    $searchPaths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",
        "C:\Program Files\Steam\steamapps\common\Stardew Valley",
        "C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley",
        "C:\Program Files\GOG Galaxy\Games\Stardew Valley",
        "$env:USERPROFILE\AppData\Local\Programs\Stardew Valley"
    )
    try {
        $steamPath = (Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue).SteamPath
        if ($steamPath) { $searchPaths += "$steamPath\steamapps\common\Stardew Valley" }
    } catch {}

    $gamePath = $null
    foreach ($path in $searchPaths) {
        if (Test-Path (Join-Path $path "Stardew Valley.exe")) { $gamePath = $path; break }
    }

    if ($gamePath) {
        $script:statusLabel.Text = "Found Stardew Valley at:`n$gamePath"
        $script:statusLabel.ForeColor = [System.Drawing.Color]::DarkGreen
        $script:State.GamePath = $gamePath.Trim()
        $script:NextButton.Enabled = $true
        $script:OnNext = { Show-SmapiCheck }
        return
    }

    $script:statusLabel.Text = "Stardew Valley wasn't found in the default locations."
    $script:statusLabel.ForeColor = [System.Drawing.Color]::DarkOrange
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Click Browse and select your Stardew Valley install folder." -FontSize 9))

    $browseButton = New-Object System.Windows.Forms.Button
    $browseButton.Text = "Browse..."
    $browseButton.Size = New-Object System.Drawing.Size(120, 32)
    $browseButton.Add_Click({
        $browser = New-Object System.Windows.Forms.FolderBrowserDialog
        $browser.Description = "Select your Stardew Valley install folder"
        $browser.ShowNewFolderButton = $false
        if ($browser.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
            if (Test-Path (Join-Path $browser.SelectedPath "Stardew Valley.exe")) {
                $script:State.GamePath = $browser.SelectedPath.Trim()
                $script:statusLabel.Text = "Found Stardew Valley at:`n$($script:State.GamePath)"
                $script:statusLabel.ForeColor = [System.Drawing.Color]::DarkGreen
                $script:NextButton.Enabled = $true
                $script:NextButton.Refresh()
            } else {
                Show-ErrorBox "Stardew Valley.exe was not found in that folder."
            }
        }
    })
    $script:ContentPanel.Controls.Add($browseButton)

    $script:OnNext = { Show-SmapiCheck }
}

# -- Screen: SMAPI ------------------------------------------------------

function Show-SmapiCheck {
    Set-Header -Title "SMAPI Check"

    $script:smapiExe = Join-Path $script:State.GamePath "StardewModdingAPI.exe"

    if (Test-Path $script:smapiExe) {
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "SMAPI is already installed." -FontSize 10 -Color ([System.Drawing.Color]::DarkGreen)))
        $script:OnNext = { Show-ModsFolder }
        return
    }

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "SMAPI is not installed." -FontSize 10 -Color ([System.Drawing.Color]::DarkOrange)))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "SMAPI is required to run this mod. It is safe, widely used, and does not affect your saves." -FontSize 9))

    $script:smapiInstallButton = New-Object System.Windows.Forms.Button
    $script:smapiInstallButton.Text = "Download && Install SMAPI"
    $script:smapiInstallButton.Size = New-Object System.Drawing.Size(220, 32)
    $script:ContentPanel.Controls.Add($script:smapiInstallButton)

    $script:smapiProgressBar = New-Object System.Windows.Forms.ProgressBar
    $script:smapiProgressBar.Size = New-Object System.Drawing.Size(560, 24)
    $script:smapiProgressBar.Visible = $false
    $script:ContentPanel.Controls.Add($script:smapiProgressBar)

    $script:smapiStatusLabel = New-WrapLabel -Text "" -FontSize 9
    $script:ContentPanel.Controls.Add($script:smapiStatusLabel)

    $script:smapiContinueButton = New-Object System.Windows.Forms.Button
    $script:smapiContinueButton.Text = "I've finished the SMAPI installer"
    $script:smapiContinueButton.Size = New-Object System.Drawing.Size(240, 32)
    $script:smapiContinueButton.Visible = $false
    $script:ContentPanel.Controls.Add($script:smapiContinueButton)

    $script:NextButton.Enabled = $false

    $script:smapiInstallButton.Add_Click({
        $script:smapiInstallButton.Enabled = $false
        $script:smapiProgressBar.Visible = $true
        $script:smapiStatusLabel.Text = "Fetching latest SMAPI release..."
        try {
            $release = Invoke-RestMethod -Uri "https://api.github.com/repos/Pathoschild/SMAPI/releases/latest" -UseBasicParsing
            $asset   = $release.assets | Where-Object { $_.name -like "SMAPI-*-installer.zip" } | Select-Object -First 1
            if (-not $asset) { throw "Installer not found in release assets." }

            $zipPath     = Join-Path $env:TEMP "SMAPI-installer.zip"
            $extractPath = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "SMAPI-installer"

            $script:smapiStatusLabel.Text = "Downloading $($asset.name)..."
            Invoke-DownloadWithProgress -Url $asset.browser_download_url -DestPath $zipPath -ProgressBar $script:smapiProgressBar

            $script:smapiStatusLabel.Text = "Extracting..."
            if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
            Expand-Archive -Path $zipPath -DestinationPath $extractPath
            Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

            $installer = Get-ChildItem -Path $extractPath -Filter "install on Windows.bat" -Recurse | Select-Object -First 1
            if (-not $installer) { throw "SMAPI installer not found after extraction." }

            $script:smapiStatusLabel.Text = "The SMAPI installer will open in a new window. Follow its prompts, then come back here and click 'I've finished the SMAPI installer'."
            Start-Process -FilePath "cmd.exe" -ArgumentList "/c `"$($installer.FullName)`"" -WorkingDirectory $installer.DirectoryName

            $script:smapiContinueButton.Visible = $true
        }
        catch {
            Show-ErrorBox "Failed to download SMAPI: $_`n`nPlease install manually from https://smapi.io"
            $script:smapiInstallButton.Enabled = $true
        }
    })

    $script:smapiContinueButton.Add_Click({
        Remove-Item (Join-Path ([Environment]::GetFolderPath("MyDocuments")) "SMAPI-installer") -Recurse -Force -ErrorAction SilentlyContinue
        if (Test-Path $script:smapiExe) {
            $script:smapiStatusLabel.Text = "SMAPI installed successfully!"
            $script:smapiStatusLabel.ForeColor = [System.Drawing.Color]::DarkGreen
        } else {
            $script:smapiStatusLabel.Text = "Could not verify SMAPI installation. Please check before launching the game."
            $script:smapiStatusLabel.ForeColor = [System.Drawing.Color]::DarkOrange
        }
        $script:NextButton.Enabled = $true
        $script:NextButton.Refresh()
    })

    $script:OnNext = { Show-ModsFolder }
}

# -- Screen: Mods Folder ------------------------------------------------

function Show-ModsFolder {
    Set-Header -Title "Mod Manager"

    $script:stardropPath = "$env:APPDATA\Stardrop\Data\Selected Mods"
    $stardropExe         = "$env:LOCALAPPDATA\Programs\Stardrop\Stardrop.exe"
    $stardropFound       = (Test-Path $stardropExe) -or (Test-Path $script:stardropPath)

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Do you use Stardrop Mod Manager?" -FontSize 10))
    if ($stardropFound) {
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Stardrop detected on this machine." -FontSize 9 -Color ([System.Drawing.Color]::DarkGreen)))
    }

    $script:radioStardrop = New-Object System.Windows.Forms.RadioButton
    $script:radioStardrop.Text = "Yes, I use Stardrop"
    $script:radioStardrop.AutoSize = $true
    $script:radioStardrop.Checked = $stardropFound
    $script:ContentPanel.Controls.Add($script:radioStardrop)

    $radioDefault = New-Object System.Windows.Forms.RadioButton
    $radioDefault.Text = "No, I use the default Mods folder"
    $radioDefault.AutoSize = $true
    $radioDefault.Checked = -not $stardropFound
    $script:ContentPanel.Controls.Add($radioDefault)

    $script:radioCustom = New-Object System.Windows.Forms.RadioButton
    $script:radioCustom.Text = "I use a custom folder"
    $script:radioCustom.AutoSize = $true
    $script:ContentPanel.Controls.Add($script:radioCustom)

    $customPanel = New-Object System.Windows.Forms.FlowLayoutPanel
    $customPanel.AutoSize = $true
    $customPanel.Margin = New-Object System.Windows.Forms.Padding(20, 4, 0, 0)

    $script:customPathLabel = New-WrapLabel -Text "(no folder selected)" -FontSize 9
    $script:modsFolderBrowseButton = New-Object System.Windows.Forms.Button
    $script:modsFolderBrowseButton.Text = "Browse..."
    $script:modsFolderBrowseButton.Size = New-Object System.Drawing.Size(120, 30)
    $script:modsFolderBrowseButton.Enabled = $false
    $script:customFolder = $null

    $script:modsFolderBrowseButton.Add_Click({
        $browser = New-Object System.Windows.Forms.FolderBrowserDialog
        $browser.Description = "Select your Stardew Valley Mods folder"
        $browser.ShowNewFolderButton = $true
        if ($browser.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
            $script:customFolder = $browser.SelectedPath.Trim()
            $script:customPathLabel.Text = $script:customFolder
        }
    })

    $customPanel.Controls.Add($script:modsFolderBrowseButton)
    $customPanel.Controls.Add($script:customPathLabel)
    $script:ContentPanel.Controls.Add($customPanel)

    $script:radioCustom.Add_CheckedChanged({ $script:modsFolderBrowseButton.Enabled = $this.Checked })

    $script:OnNext = {
        if ($script:radioStardrop.Checked) {
            if (-not (Test-Path $script:stardropPath)) { New-Item -ItemType Directory -Path $script:stardropPath -Force | Out-Null }
            $script:State.ModsFolder = $script:stardropPath.Trim()
        }
        elseif ($script:radioCustom.Checked) {
            if (-not $script:customFolder) { Show-ErrorBox "Please select a custom folder first."; return }
            $script:State.ModsFolder = $script:customFolder
        }
        else {
            $script:State.ModsFolder = Join-Path $script:State.GamePath "Mods"
        }
        Show-InstallFiles
    }
}

# -- Screen: Install Files -----------------------------------------------

function Show-InstallFiles {
    Set-Header -Title "Downloading & Installing"
    $script:NextButton.Enabled = $false

    $destFolder = Join-Path $script:State.ModsFolder "ChatVsStreamer"

    if (Test-Path $destFolder) {
        if (-not (Confirm-YesNo "An existing installation was found at:`n$destFolder`n`nOverwrite existing files?")) {
            $script:State.ModFolder = $destFolder
            $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Keeping existing installation at:`n$destFolder" -FontSize 10))
            $script:NextButton.Enabled = $true
            $script:OnNext = { Show-Gmcm }
            return
        }
    }

    $statusLabel = New-WrapLabel -Text "Checking for latest version on GitHub..." -FontSize 10
    $script:ContentPanel.Controls.Add($statusLabel)

    $progressBar = New-Object System.Windows.Forms.ProgressBar
    $progressBar.Size = New-Object System.Drawing.Size(560, 24)
    $script:ContentPanel.Controls.Add($progressBar)

    try {
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/musicman0917/SDVChatVsStreamer/releases/latest" -UseBasicParsing
        $version = $release.tag_name
        $asset   = $release.assets | Where-Object { $_.name -like "ChatVsStreamer*.zip" } | Select-Object -First 1
        if (-not $asset) { throw "No mod zip found in latest release. Please download manually from github.com/musicman0917/SDVChatVsStreamer/releases" }

        $statusLabel.Text = "Latest version: $version`nDownloading $($asset.name)..."

        $zipPath     = Join-Path $env:TEMP "ChatVsStreamer.zip"
        $extractPath = Join-Path $env:TEMP "ChatVsStreamer-install"

        Invoke-DownloadWithProgress -Url $asset.browser_download_url -DestPath $zipPath -ProgressBar $progressBar

        $statusLabel.Text = "Extracting..."
        if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
        Expand-Archive -Path $zipPath -DestinationPath $extractPath

        $modSource = Get-ChildItem -Path $extractPath -Filter "ChatVsStreamer" -Directory -Recurse | Select-Object -First 1
        if (-not $modSource) { $modSource = Get-Item $extractPath }

        $statusLabel.Text = "Installing to Mods folder..."
        New-Item -ItemType Directory -Path $destFolder -Force | Out-Null

        $items = Get-ChildItem -Path $modSource.FullName
        $total = $items.Count
        $done  = 0
        $progressBar.Value = 0

        foreach ($item in $items) {
            if ($item.PSIsContainer) {
                Copy-Item $item.FullName -Destination $destFolder -Recurse -Force
            } else {
                Copy-Item $item.FullName -Destination $destFolder -Force
            }
            $done++
            if ($total -gt 0) { $progressBar.Value = [Math]::Min([math]::Floor($done / $total * 100), 100) }
            [System.Windows.Forms.Application]::DoEvents()
        }

        Remove-Item $zipPath     -Force -ErrorAction SilentlyContinue
        Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue

        $statusLabel.Text = "Chat vs Streamer $version installed!"
        $statusLabel.ForeColor = [System.Drawing.Color]::DarkGreen
        $script:State.ModFolder = $destFolder
        $script:NextButton.Enabled = $true
    }
    catch {
        $statusLabel.Text = "Download failed: $_"
        $statusLabel.ForeColor = [System.Drawing.Color]::Red
        $retryButton = New-Object System.Windows.Forms.Button
        $retryButton.Text = "Retry"
        $retryButton.Size = New-Object System.Drawing.Size(110, 32)
        $retryButton.Add_Click({ Show-InstallFiles })
        $script:ContentPanel.Controls.Add($retryButton)
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "You can also download manually from:`nhttps://github.com/musicman0917/SDVChatVsStreamer/releases" -FontSize 9))
    }

    $script:OnNext = { Show-Gmcm }
}

# -- Screen: GMCM -----------------------------------------------------

function Show-Gmcm {
    Set-Header -Title "Generic Mod Config Menu"

    $gmcmFolder = Join-Path $script:State.ModsFolder "GenericModConfigMenu"

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Generic Mod Config Menu (GMCM) lets you configure Chat vs Streamer settings from inside the game -- points, sabotage costs, TikTok/YouTube/DonorDrive, and more. It's optional but highly recommended." -FontSize 10))

    if (Test-Path $gmcmFolder) {
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "GMCM is already installed." -FontSize 10 -Color ([System.Drawing.Color]::DarkGreen)))
        $script:OnNext = { Show-TwitchSetup }
        return
    }

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "GMCM is hosted on Nexus Mods. Click the button below to open the download page, grab it, unzip it, and copy the GenericModConfigMenu folder into your Mods folder. Then click Next." -FontSize 9))

    $openButton = New-Object System.Windows.Forms.Button
    $openButton.Text = "Open Nexus Mods Page"
    $openButton.Size = New-Object System.Drawing.Size(200, 32)
    $openButton.Add_Click({ Start-Process "https://www.nexusmods.com/stardewvalley/mods/5098" })
    $script:ContentPanel.Controls.Add($openButton)

    $script:OnNext = { Show-TwitchSetup }
}

# -- Screen: Twitch Setup ----------------------------------------------

function Show-AfterTwitchBranch {
    if ($script:State.UseTikTok) { Show-TikTokSetup }
    elseif ($script:State.UseYouTube) { Show-YouTubeSetup }
    else { Show-Complete }
}

function Show-TwitchSetup {
    Set-Header -Title "Twitch Setup"

    $authDir            = Join-Path $script:State.ModFolder "TwitchAuth"
    $script:configFile  = Join-Path $authDir "secrets.json"
    New-Item -ItemType Directory -Path $authDir -Force | Out-Null

    if (Test-Path $script:configFile) {
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Twitch credentials are already configured." -FontSize 10 -Color ([System.Drawing.Color]::DarkGreen)))
        $script:reconfigureCheck = New-Object System.Windows.Forms.CheckBox
        $script:reconfigureCheck.Text = "Reconfigure Twitch credentials"
        $script:reconfigureCheck.AutoSize = $true
        $script:ContentPanel.Controls.Add($script:reconfigureCheck)

        $script:OnNext = {
            if ($script:reconfigureCheck.Checked) { Show-TwitchCredentials }
            else { Show-AfterTwitchBranch }
        }
        return
    }

    Show-TwitchCredentials
}

function Show-TwitchCredentials {
    Set-SubHeader -Title "Twitch Setup - Create App"

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "STEP 1 - Create a Twitch Application" -FontSize 11 -Bold))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "1. Click below to open the Twitch Developer Console and sign in.`n2. Click 'Register Your Application'.`n3. Fill in the form:" -FontSize 9))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "   Name:           SDV - ChatVsStreamer (or anything you like)`n   OAuth Redirect: http://localhost:7379/`n   Category:       Game Integration`n   Client Type:    Confidential" -FontSize 9 -Color ([System.Drawing.Color]::DarkSlateGray)))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "4. Click 'Create', then 'Manage' on your new app.`n5. Copy the Client ID shown on the page.`n6. Click 'New Secret', copy the Client Secret (it's only shown once!)." -FontSize 9))

    $openButton = New-Object System.Windows.Forms.Button
    $openButton.Text = "Open Twitch Developer Console"
    $openButton.Size = New-Object System.Drawing.Size(240, 32)
    $openButton.Add_Click({ Start-Process "https://dev.twitch.tv/console/apps/create" })
    $script:ContentPanel.Controls.Add($openButton)

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Client ID:" -FontSize 9 -Bold))
    $script:clientIdBox = New-Object System.Windows.Forms.TextBox
    $script:clientIdBox.Size = New-Object System.Drawing.Size(400, 24)
    $script:clientIdBox.Margin = New-Object System.Windows.Forms.Padding(0, 0, 0, 10)
    $script:ContentPanel.Controls.Add($script:clientIdBox)

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Client Secret:" -FontSize 9 -Bold))
    $script:clientSecretBox = New-Object System.Windows.Forms.TextBox
    $script:clientSecretBox.Size = New-Object System.Drawing.Size(400, 24)
    $script:clientSecretBox.UseSystemPasswordChar = $true
    $script:ContentPanel.Controls.Add($script:clientSecretBox)

    $script:NextButton.Text = "Save && Continue"

    $script:OnNext = {
        if (-not $script:clientIdBox.Text.Trim() -or -not $script:clientSecretBox.Text.Trim()) {
            Show-ErrorBox "Please enter both the Client ID and Client Secret."
            return
        }
        $secrets = @{
            client_id     = $script:clientIdBox.Text.Trim()
            ClientId      = $script:clientIdBox.Text.Trim()
            client_secret = $script:clientSecretBox.Text.Trim()
        } | ConvertTo-Json -Depth 2
        Set-Content -Path $script:configFile -Value $secrets -Encoding UTF8

        Show-TwitchAuthorizationInfo
    }
}

function Show-TwitchAuthorizationInfo {
    Set-SubHeader -Title "Twitch Setup - Authorization"

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "STEP 2 - Authorize the mod with your bot account" -FontSize 11 -Bold))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "When you first launch the game, the mod will automatically open Chrome in incognito mode and ask you to authorize." -FontSize 9))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Sign in as your BOT ACCOUNT (e.g. bardbouncerbot) when prompted -- NOT your main channel account." -FontSize 9 -Color ([System.Drawing.Color]::DarkOrange) -Bold))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "After you click Authorize, the browser will show 'Authorized! You can close this tab.' Your token will refresh in the background automatically from then on." -FontSize 9))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Check the SMAPI console for:`n  [TwitchManager] IRC connected as <botname>`n  [TwitchManager] Joined channel: <yourchannel>`n  [TwitchManager] PubSub connected" -FontSize 9 -Color ([System.Drawing.Color]::DarkGreen)))

    $script:OnNext = { Show-AfterTwitchBranch }
}

# -- Screen: TikTok Setup -----------------------------------------------

function Show-AfterTikTokBranch {
    if ($script:State.UseYouTube) { Show-YouTubeSetup } else { Show-Complete }
}

function Show-TikTokSetup {
    Set-Header -Title "TikTok Integration (USE AT YOUR OWN RISK)"

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "WARNING: TikTok integration is unsupported and provided as-is. TikTok's API policies may break this feature at any time without notice." -FontSize 9 -Color ([System.Drawing.Color]::DarkOrange) -Bold))

    $tikfinityPaths = @(
        "$env:LOCALAPPDATA\Programs\Tikfinity\Tikfinity.exe",
        "$env:PROGRAMFILES\Tikfinity\Tikfinity.exe",
        "$env:LOCALAPPDATA\Programs\TikFinity\TikFinity.exe"
    )
    $tikfinityFound = $false
    foreach ($path in $tikfinityPaths) { if (Test-Path $path) { $tikfinityFound = $true; break } }

    if ($tikfinityFound) {
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Tikfinity is already installed!" -FontSize 10 -Color ([System.Drawing.Color]::DarkGreen)))
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "To enable TikTok integration:`n1. Launch Stardew Valley through SMAPI`n2. Open GMCM > Chat vs Streamer > TikTok (Tikfinity)`n3. Enable TikTok Integration`n4. Launch Tikfinity and connect to TikTok LIVE before streaming" -FontSize 9))
        $script:OnNext = { Show-AfterTikTokBranch }
        return
    }

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Tikfinity is not installed." -FontSize 10 -Color ([System.Drawing.Color]::DarkOrange)))

    $script:tikTokInstallButton = New-Object System.Windows.Forms.Button
    $script:tikTokInstallButton.Text = "Download && Install Tikfinity"
    $script:tikTokInstallButton.Size = New-Object System.Drawing.Size(220, 32)
    $script:ContentPanel.Controls.Add($script:tikTokInstallButton)

    $script:tikTokProgressBar = New-Object System.Windows.Forms.ProgressBar
    $script:tikTokProgressBar.Size = New-Object System.Drawing.Size(560, 24)
    $script:tikTokProgressBar.Visible = $false
    $script:ContentPanel.Controls.Add($script:tikTokProgressBar)

    $script:tikTokStatusLabel = New-WrapLabel -Text "" -FontSize 9
    $script:ContentPanel.Controls.Add($script:tikTokStatusLabel)

    $script:tikfinityPaths = $tikfinityPaths

    $script:tikTokInstallButton.Add_Click({
        $script:tikTokInstallButton.Enabled = $false
        $script:tikTokProgressBar.Visible = $true
        try {
            $installerPath = Join-Path $env:TEMP "TikFinity_installer.exe"
            $script:tikTokStatusLabel.Text = "Downloading Tikfinity installer..."
            Invoke-DownloadWithProgress -Url "https://tikfinity.zerody.one/download/win" -DestPath $installerPath -ProgressBar $script:tikTokProgressBar

            $script:tikTokStatusLabel.Text = "Launching Tikfinity installer... (if SmartScreen appears, click 'More info' then 'Run anyway')"
            $proc = Start-Process -FilePath $installerPath -PassThru
            Wait-ProcessResponsive -Process $proc
            Remove-Item $installerPath -Force -ErrorAction SilentlyContinue

            $installedNow = $false
            foreach ($path in $script:tikfinityPaths) { if (Test-Path $path) { $installedNow = $true; break } }

            if ($installedNow) {
                $script:tikTokStatusLabel.Text = "Tikfinity installed successfully!"
                $script:tikTokStatusLabel.ForeColor = [System.Drawing.Color]::DarkGreen
            } else {
                $script:tikTokStatusLabel.Text = "Tikfinity installer ran but could not verify installation."
                $script:tikTokStatusLabel.ForeColor = [System.Drawing.Color]::DarkOrange
            }
        }
        catch {
            $script:tikTokStatusLabel.Text = "Could not download Tikfinity: $_`nPlease install manually from https://tikfinity.zerody.one/app/"
            $script:tikTokStatusLabel.ForeColor = [System.Drawing.Color]::Red
            $script:tikTokInstallButton.Enabled = $true
        }
    })

    $script:OnNext = { Show-AfterTikTokBranch }
}

# -- Screen: YouTube Setup ------------------------------------------------

function Show-YouTubeSetup {
    Set-Header -Title "YouTube Integration (BETA)"

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "BETA: YouTube Live chat integration is experimental and not yet fully tested." -FontSize 9 -Color ([System.Drawing.Color]::DarkOrange) -Bold))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Unlike Twitch, this mod doesn't talk to YouTube directly -- it connects through Streamer.bot, a separate free app that reads YouTube Live chat and forwards it over a local WebSocket." -FontSize 9))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Setup:`n1. Install Streamer.bot from streamer.bot`n2. Connect Streamer.bot to your YouTube channel`n3. Leave Streamer.bot's WebSocket server on its default port (8080)`n4. Launch Stardew Valley through SMAPI`n5. Open GMCM > Chat vs Streamer > YouTube (Streamer.bot) [BETA]`n6. Enable it, and set the port if you changed it in Streamer.bot" -FontSize 9))

    $openButton = New-Object System.Windows.Forms.Button
    $openButton.Text = "Open Streamer.bot Website"
    $openButton.Size = New-Object System.Drawing.Size(220, 32)
    $openButton.Add_Click({ Start-Process "https://streamer.bot" })
    $script:ContentPanel.Controls.Add($openButton)

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Check the SMAPI console for '[YouTubeManager] Connected to Streamer.bot.' once you've enabled it in GMCM and Streamer.bot is running." -FontSize 9 -Color ([System.Drawing.Color]::DimGray)))

    $script:OnNext = { Show-Complete }
}

# -- Screen: Complete -------------------------------------------------

function Show-Complete {
    Set-Header -Title "Installation Complete!"

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Chat vs Streamer is installed!" -FontSize 12 -Bold -Color ([System.Drawing.Color]::DarkGreen)))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Next steps:`n1. Launch Stardew Valley through SMAPI`n2. Load a save file`n3. An incognito Chrome window will open automatically - sign in as your BOT ACCOUNT and click Authorize`n4. Check the SMAPI console for the IRC/PubSub connected messages`n5. Type !shop in your Twitch chat to test it" -FontSize 9))

    if ($script:State.UseTikTok) {
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "TikTok: launch Tikfinity and connect to TikTok LIVE, then enable TikTok in GMCM > Chat vs Streamer > TikTok (USE AT YOUR OWN RISK)." -FontSize 9 -Color ([System.Drawing.Color]::DarkSlateGray)))
    }
    if ($script:State.UseYouTube) {
        $script:ContentPanel.Controls.Add((New-WrapLabel -Text "YouTube (beta): make sure Streamer.bot is running and connected to YouTube, then enable it in GMCM > Chat vs Streamer > YouTube (Streamer.bot)." -FontSize 9 -Color ([System.Drawing.Color]::DarkSlateGray)))
    }

    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Want donations (Extra Life / DonorDrive) to trigger chaos too? Open GMCM > Chat vs Streamer > DonorDrive, enable it, and enter your participant ID." -FontSize 9))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "OBS Browser Sources:`n  Shop sidebar     -->  http://localhost:7373/`n  Shop board       -->  http://localhost:7373/mobile`n  Chat overlay     -->  http://localhost:7373/chat`n  Alert popup      -->  http://localhost:7373/alert`n  Alert (vertical) -->  http://localhost:7373/alert-mobile`n  Debug panel      -->  http://localhost:7373/debug" -FontSize 9 -Color ([System.Drawing.Color]::DarkSlateGray)))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "GitHub:  github.com/musicman0917/SDVChatVsStreamer`nTwitch:  twitch.tv/neighborhoodofmusic`n`nGood luck on stream! ~" -FontSize 9))

    $script:NextButton.Text = "Finish"
    $script:OnNext = { $script:Form.Close() }
}

# -- Entry point -----------------------------------------------------

try {
    Show-Welcome
    [void]$script:Form.ShowDialog()
}
catch {
    Show-ErrorBox "An unexpected error occurred:`n$_`n`nPlease report this at github.com/musicman0917/SDVChatVsStreamer/issues"
}
