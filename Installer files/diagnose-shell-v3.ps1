# ================================================================
#  Diagnostic 4: same as v2 (plain Panel content area, manual
#  positioning), but the Header panel now has a FIXED height instead
#  of AutoSize -- to test whether AutoSize on the Dock=Top sibling is
#  specifically what's corrupting the Dock=Fill content area's layout.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic v3 (fixed-height header)"
$script:Form.ClientSize = New-Object System.Drawing.Size(640, 560)
$script:Form.StartPosition = "CenterScreen"

# -- Header is now FIXED HEIGHT, no AutoSize at all --
$script:HeaderPanel = New-Object System.Windows.Forms.Panel
$script:HeaderPanel.Dock = "Top"
$script:HeaderPanel.Height = 70
$script:HeaderPanel.Padding = New-Object System.Windows.Forms.Padding(20, 14, 10, 10)

$script:HeaderTitleLabel = New-Object System.Windows.Forms.Label
$script:HeaderTitleLabel.AutoSize = $true
$script:HeaderTitleLabel.Location = New-Object System.Drawing.Point(20, 14)
$script:HeaderTitleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$script:HeaderPanel.Controls.Add($script:HeaderTitleLabel)

$script:HeaderStepLabel = New-Object System.Windows.Forms.Label
$script:HeaderStepLabel.AutoSize = $true
$script:HeaderStepLabel.Location = New-Object System.Drawing.Point(20, 44)
$script:HeaderStepLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$script:HeaderStepLabel.ForeColor = [System.Drawing.Color]::DimGray
$script:HeaderPanel.Controls.Add($script:HeaderStepLabel)

$script:Form.Controls.Add($script:HeaderPanel)

$script:ContentPanel = New-Object System.Windows.Forms.Panel
$script:ContentPanel.Dock = "Fill"
$script:ContentPanel.AutoScroll = $true
$script:Form.Controls.Add($script:ContentPanel)

$script:FooterPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:FooterPanel.Dock = "Bottom"
$script:FooterPanel.Height = 54
$script:FooterPanel.FlowDirection = "RightToLeft"
$script:FooterPanel.Padding = New-Object System.Windows.Forms.Padding(12)

$script:NextButton = New-Object System.Windows.Forms.Button
$script:NextButton.Text = "Next >"
$script:NextButton.Size = New-Object System.Drawing.Size(110, 32)
$script:FooterPanel.Controls.Add($script:NextButton)

$script:Form.Controls.Add($script:FooterPanel)

$script:ContentY = 20

function Add-ContentControl {
    param([System.Windows.Forms.Control]$Control)
    $Control.Location = New-Object System.Drawing.Point(20, $script:ContentY)
    $script:ContentPanel.Controls.Add($Control)
    $script:ContentY += $Control.Height + 10
}

function New-WrapLabel {
    param([string]$Text, [int]$FontSize = 9)
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Font = New-Object System.Drawing.Font("Segoe UI", $FontSize)
    $lbl.Text = $Text
    $lbl.AutoSize = $false
    $bounds = New-Object System.Drawing.Size(560, 0)
    $measured = [System.Windows.Forms.TextRenderer]::MeasureText($Text, $lbl.Font, $bounds, [System.Windows.Forms.TextFormatFlags]::WordBreak)
    $lbl.Size = New-Object System.Drawing.Size(560, $measured.Height)
    $lbl.BackColor = [System.Drawing.Color]::Yellow
    return $lbl
}

function Set-Header {
    param([string]$Title, [int]$Step)
    $script:HeaderTitleLabel.Text = $Title
    $script:HeaderStepLabel.Text  = "Step $Step of 3"
    $script:ContentPanel.Controls.Clear()
    $script:ContentY = 20
}

function Show-ScreenOne {
    Set-Header -Title "Screen One" -Step 1
    Add-ContentControl (New-WrapLabel -Text "SCREEN ONE: fixed-height header this time, no AutoSize anywhere. This sentence is deliberately long enough to need two lines at 560 pixels of width.")
    Add-ContentControl (New-WrapLabel -Text "A second label on screen one, also yellow, also should be visible.")
    $script:NextButton.Text = "Next >"
    $script:OnNext = { Show-ScreenTwo }
}

function Show-ScreenTwo {
    Set-Header -Title "Screen Two" -Step 2
    Add-ContentControl (New-WrapLabel -Text "SCREEN TWO: if THIS label is visible where v1/v2 were blank, AutoSize on the Dock=Top header is the confirmed cause, and the real installer's header needs a fixed or manually-managed height instead.")
    $script:NextButton.Text = "Finish"
    $script:OnNext = { $script:Form.Close() }
}

$script:OnNext = { }
$script:NextButton.Add_Click({ & $script:OnNext })

Show-ScreenOne
[void]$script:Form.ShowDialog()
