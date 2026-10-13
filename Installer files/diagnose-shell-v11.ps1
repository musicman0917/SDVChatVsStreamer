# ================================================================
#  Diagnostic 12: confirms the real installer's exact configuration
#  works -- Content added FIRST (the confirmed fix from v10) combined
#  with the real installer's actual AutoSize FlowLayoutPanel header
#  (two stacked labels), not v10's simplified fixed-height Panel
#  header. Checking this combination specifically before pushing the
#  fix to install-gui.ps1 and asking for another full re-test.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic v11 (Content first + AutoSize header)"
$script:Form.ClientSize = New-Object System.Drawing.Size(640, 560)
$script:Form.StartPosition = "CenterScreen"

# -- ContentPanel added FIRST --
$script:ContentPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:ContentPanel.Dock          = "Fill"
$script:ContentPanel.FlowDirection = "TopDown"
$script:ContentPanel.WrapContents  = $false
$script:ContentPanel.AutoScroll    = $true
$script:ContentPanel.Padding       = New-Object System.Windows.Forms.Padding(20)
$script:Form.Controls.Add($script:ContentPanel)

# -- Header: AutoSize FlowLayoutPanel, same as the real installer --
$script:HeaderPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:HeaderPanel.Dock          = "Top"
$script:HeaderPanel.FlowDirection = "TopDown"
$script:HeaderPanel.WrapContents  = $false
$script:HeaderPanel.AutoSize      = $true
$script:HeaderPanel.AutoSizeMode  = "GrowAndShrink"
$script:HeaderPanel.Padding       = New-Object System.Windows.Forms.Padding(20, 14, 10, 10)
$script:HeaderPanel.BackColor     = [System.Drawing.Color]::LightCyan

$script:HeaderTitleLabel = New-Object System.Windows.Forms.Label
$script:HeaderTitleLabel.AutoSize = $true
$script:HeaderTitleLabel.Font     = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$script:HeaderTitleLabel.Text     = "Screen One"
$script:HeaderPanel.Controls.Add($script:HeaderTitleLabel)

$script:HeaderStepLabel = New-Object System.Windows.Forms.Label
$script:HeaderStepLabel.AutoSize  = $true
$script:HeaderStepLabel.Font      = New-Object System.Drawing.Font("Segoe UI", 9)
$script:HeaderStepLabel.ForeColor = [System.Drawing.Color]::DimGray
$script:HeaderStepLabel.Margin    = New-Object System.Windows.Forms.Padding(0, 2, 0, 0)
$script:HeaderStepLabel.Text      = "Step 1 of 3"
$script:HeaderPanel.Controls.Add($script:HeaderStepLabel)

$script:Form.Controls.Add($script:HeaderPanel)

$script:FooterPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:FooterPanel.Dock          = "Bottom"
$script:FooterPanel.Height        = 54
$script:FooterPanel.FlowDirection = "RightToLeft"
$script:FooterPanel.BackColor     = [System.Drawing.Color]::MistyRose
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

$script:ContentPanel.Controls.Add((New-WrapLabel -Text "LABEL 1: this sentence is long enough to wrap onto a second line at 560 pixels of width, so it should be noticeably taller than label 2."))
$script:ContentPanel.Controls.Add((New-WrapLabel -Text "LABEL 2: one line only."))

$report = @"
HeaderPanel.Bounds: X=$($script:HeaderPanel.Bounds.X) Y=$($script:HeaderPanel.Bounds.Y) W=$($script:HeaderPanel.Bounds.Width) H=$($script:HeaderPanel.Bounds.Height)
FooterPanel.Bounds: X=$($script:FooterPanel.Bounds.X) Y=$($script:FooterPanel.Bounds.Y) W=$($script:FooterPanel.Bounds.Width) H=$($script:FooterPanel.Bounds.Height)
ContentPanel.Bounds: X=$($script:ContentPanel.Bounds.X) Y=$($script:ContentPanel.Bounds.Y) W=$($script:ContentPanel.Bounds.Width) H=$($script:ContentPanel.Bounds.Height)
"@
[System.Windows.Forms.MessageBox]::Show($report, "Diagnostic numbers") | Out-Null

[void]$script:Form.ShowDialog()
