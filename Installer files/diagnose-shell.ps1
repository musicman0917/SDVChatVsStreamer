# ================================================================
#  Diagnostic 2: clones the installer's actual Header/Content/Footer
#  shell (same Dock=Top/Fill/Bottom structure, same Set-Header-style
#  clear-and-rebuild pattern) to see whether THAT breaks label
#  rendering, since a bare Form+Panel (diagnose-labels.ps1) did not.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic"
$script:Form.ClientSize = New-Object System.Drawing.Size(640, 560)
$script:Form.StartPosition = "CenterScreen"

$script:HeaderPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:HeaderPanel.Dock = "Top"
$script:HeaderPanel.FlowDirection = "TopDown"
$script:HeaderPanel.WrapContents = $false
$script:HeaderPanel.AutoSize = $true
$script:HeaderPanel.AutoSizeMode = "GrowAndShrink"
$script:HeaderPanel.Padding = New-Object System.Windows.Forms.Padding(20, 14, 10, 10)

$script:HeaderTitleLabel = New-Object System.Windows.Forms.Label
$script:HeaderTitleLabel.AutoSize = $true
$script:HeaderTitleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$script:HeaderPanel.Controls.Add($script:HeaderTitleLabel)

$script:HeaderStepLabel = New-Object System.Windows.Forms.Label
$script:HeaderStepLabel.AutoSize = $true
$script:HeaderStepLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$script:HeaderStepLabel.ForeColor = [System.Drawing.Color]::DimGray
$script:HeaderStepLabel.Margin = New-Object System.Windows.Forms.Padding(0, 2, 0, 0)
$script:HeaderPanel.Controls.Add($script:HeaderStepLabel)

$script:Form.Controls.Add($script:HeaderPanel)

$script:ContentPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:ContentPanel.Dock = "Fill"
$script:ContentPanel.FlowDirection = "TopDown"
$script:ContentPanel.WrapContents = $false
$script:ContentPanel.AutoScroll = $true
$script:ContentPanel.Padding = New-Object System.Windows.Forms.Padding(20)
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
    $lbl.Margin = New-Object System.Windows.Forms.Padding(0, 0, 0, 10)
    return $lbl
}

function Set-Header {
    param([string]$Title, [int]$Step)
    $script:HeaderTitleLabel.Text = $Title
    $script:HeaderStepLabel.Text  = "Step $Step of 3"
    $script:ContentPanel.Controls.Clear()
}

function Show-ScreenOne {
    Set-Header -Title "Screen One" -Step 1
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "SCREEN ONE: this label should be visible with a yellow background. This sentence is deliberately long enough to need two lines at 560 pixels of width."))
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "A second label on screen one, also yellow, also should be visible."))
    $script:NextButton.Text = "Next >"
    $script:OnNext = { Show-ScreenTwo }
}

function Show-ScreenTwo {
    Set-Header -Title "Screen Two" -Step 2
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "SCREEN TWO: if this label is visible, the bug is NOT in the Header/Content/Footer shell or the clear-and-rebuild pattern -- something else in the real installer is different."))
    $script:NextButton.Text = "Finish"
    $script:OnNext = { $script:Form.Close() }
}

$script:OnNext = { }
$script:NextButton.Add_Click({ & $script:OnNext })

Show-ScreenOne
[void]$script:Form.ShowDialog()
