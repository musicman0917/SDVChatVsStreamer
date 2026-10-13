# ================================================================
#  Diagnostic 7: v5 (forced repaint) didn't fix it either -- same
#  result, only the second-added label shows. This swaps the ORDER:
#  the SHORT (one-line) label is now added FIRST, and the LONG
#  (two-line, wrapping) label SECOND. If the short one is now the
#  one that's invisible, this is purely an "add order" bug
#  (whichever is added first never paints). If the long one is STILL
#  the invisible one regardless of order, it's specific to that
#  control's content/wrapping, not add order.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic v6 (swapped add order)"
$script:Form.ClientSize = New-Object System.Drawing.Size(640, 560)
$script:Form.StartPosition = "CenterScreen"

$script:HeaderPanel = New-Object System.Windows.Forms.Panel
$script:HeaderPanel.Dock = "Top"
$script:HeaderPanel.Height = 70

$script:HeaderTitleLabel = New-Object System.Windows.Forms.Label
$script:HeaderTitleLabel.AutoSize = $true
$script:HeaderTitleLabel.Location = New-Object System.Drawing.Point(20, 14)
$script:HeaderTitleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$script:HeaderTitleLabel.Text = "Screen One"
$script:HeaderPanel.Controls.Add($script:HeaderTitleLabel)
$script:Form.Controls.Add($script:HeaderPanel)

$script:ContentPanel = New-Object System.Windows.Forms.Panel
$script:ContentPanel.Dock = "Fill"
$script:ContentPanel.AutoScroll = $true
$script:Form.Controls.Add($script:ContentPanel)

$script:FooterPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:FooterPanel.Dock = "Bottom"
$script:FooterPanel.Height = 54
$script:FooterPanel.FlowDirection = "RightToLeft"
$script:Form.Controls.Add($script:FooterPanel)

function New-WrapLabel {
    param([string]$Text, [int]$FontSize = 9, [System.Drawing.Color]$Color)
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Font = New-Object System.Drawing.Font("Segoe UI", $FontSize)
    $lbl.Text = $Text
    $lbl.AutoSize = $false
    $bounds = New-Object System.Drawing.Size(560, 0)
    $measured = [System.Windows.Forms.TextRenderer]::MeasureText($Text, $lbl.Font, $bounds, [System.Windows.Forms.TextFormatFlags]::WordBreak)
    $lbl.Size = New-Object System.Drawing.Size(560, $measured.Height)
    $lbl.BackColor = $Color
    return $lbl
}

$longText  = "LONG (wraps to 2 lines): this sentence is deliberately long enough to need two lines at 560 pixels of width, so you can tell it apart from the short one below."
$shortText = "SHORT (one line): added first this time."

# SHORT added first now (was second in v4/v5), colored orange to tell apart
$labelShort = New-WrapLabel -Text $shortText -Color ([System.Drawing.Color]::Orange)
$labelShort.Location = New-Object System.Drawing.Point(20, 20)
$script:ContentPanel.Controls.Add($labelShort)

# LONG added second now (was first in v4/v5), colored yellow
$labelLong = New-WrapLabel -Text $longText -Color ([System.Drawing.Color]::Yellow)
$labelLong.Location = New-Object System.Drawing.Point(20, ($labelShort.Location.Y + $labelShort.Height + 10))
$script:ContentPanel.Controls.Add($labelLong)

[void]$script:Form.ShowDialog()
