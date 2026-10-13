# ================================================================
#  Diagnostic 6: v4 proved label1 and label2 both have fully correct
#  Size/Location (560x30 @ 20,20 and 560x15 @ 20,60) in a panel with
#  plenty of room (640x506) -- yet only label2 painted. That's a
#  paint/rendering issue, not a sizing or layout one: specifically,
#  a known WinForms quirk where the FIRST control added to an
#  AutoScroll panel can have correct data but stale paint state until
#  something forces a repaint. This calls .Invalidate($true) after
#  adding each control to test that directly.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic v5 (force repaint after Add)"
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

$longText  = "SCREEN ONE: fixed-height header, force-repaint after each Add. This sentence is deliberately long enough to need two lines at 560 pixels of width."
$shortText = "A second label on screen one, also yellow, also should be visible."

$label1 = New-WrapLabel -Text $longText
$label1.Location = New-Object System.Drawing.Point(20, 20)
$script:ContentPanel.Controls.Add($label1)
$script:ContentPanel.Invalidate($true)
$script:ContentPanel.Update()

$label2 = New-WrapLabel -Text $shortText
$label2.Location = New-Object System.Drawing.Point(20, ($label1.Location.Y + $label1.Height + 10))
$script:ContentPanel.Controls.Add($label2)
$script:ContentPanel.Invalidate($true)
$script:ContentPanel.Update()

[void]$script:Form.ShowDialog()
