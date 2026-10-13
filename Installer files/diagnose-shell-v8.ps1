# ================================================================
#  Diagnostic 9: v7 (Content added last) still showed garbled,
#  overlapping-looking text right at the header/content boundary.
#  100% display scaling rules out DPI. This combines v7's corrected
#  add-order with v4's numeric reporting, but now ALSO reports
#  ContentPanel.Location and .Bounds (relative to the Form) -- never
#  captured before. That distinguishes the two remaining
#  possibilities directly: either ContentPanel itself still overlaps
#  the header (Location.Y much less than 70), or ContentPanel is
#  correctly positioned and it's label1/label2 overlapping each
#  other inside it (e.g. label1.Height came back 0 again).
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic v8 (full numeric report)"
$script:Form.ClientSize = New-Object System.Drawing.Size(640, 560)
$script:Form.StartPosition = "CenterScreen"

$script:HeaderPanel = New-Object System.Windows.Forms.Panel
$script:HeaderPanel.Dock = "Top"
$script:HeaderPanel.Height = 70
$script:HeaderPanel.BackColor = [System.Drawing.Color]::LightCyan

$script:HeaderTitleLabel = New-Object System.Windows.Forms.Label
$script:HeaderTitleLabel.AutoSize = $true
$script:HeaderTitleLabel.Location = New-Object System.Drawing.Point(20, 14)
$script:HeaderTitleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$script:HeaderTitleLabel.Text = "Screen One"
$script:HeaderPanel.Controls.Add($script:HeaderTitleLabel)
$script:Form.Controls.Add($script:HeaderPanel)

$script:FooterPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:FooterPanel.Dock = "Bottom"
$script:FooterPanel.Height = 54
$script:FooterPanel.FlowDirection = "RightToLeft"
$script:FooterPanel.BackColor = [System.Drawing.Color]::MistyRose
$script:Form.Controls.Add($script:FooterPanel)

$script:ContentPanel = New-Object System.Windows.Forms.Panel
$script:ContentPanel.Dock = "Fill"
$script:ContentPanel.AutoScroll = $true
$script:Form.Controls.Add($script:ContentPanel)

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

$label1 = New-WrapLabel -Text "LABEL 1: this sentence is long enough to wrap onto a second line at 560 pixels of width, so it should be noticeably taller than label 2."
$label1.Location = New-Object System.Drawing.Point(20, 20)
$script:ContentPanel.Controls.Add($label1)

$label2 = New-WrapLabel -Text "LABEL 2: one line only."
$label2.Location = New-Object System.Drawing.Point(20, ($label1.Location.Y + $label1.Height + 10))
$script:ContentPanel.Controls.Add($label2)

$report = @"
Form.ClientSize: $($script:Form.ClientSize.Width) x $($script:Form.ClientSize.Height)

HeaderPanel.Bounds: X=$($script:HeaderPanel.Bounds.X) Y=$($script:HeaderPanel.Bounds.Y) W=$($script:HeaderPanel.Bounds.Width) H=$($script:HeaderPanel.Bounds.Height)
FooterPanel.Bounds: X=$($script:FooterPanel.Bounds.X) Y=$($script:FooterPanel.Bounds.Y) W=$($script:FooterPanel.Bounds.Width) H=$($script:FooterPanel.Bounds.Height)
ContentPanel.Bounds: X=$($script:ContentPanel.Bounds.X) Y=$($script:ContentPanel.Bounds.Y) W=$($script:ContentPanel.Bounds.Width) H=$($script:ContentPanel.Bounds.Height)
ContentPanel.Location: $($script:ContentPanel.Location.X), $($script:ContentPanel.Location.Y)

label1.Size:     $($label1.Size.Width) x $($label1.Size.Height)
label1.Location: $($label1.Location.X), $($label1.Location.Y)

label2.Size:     $($label2.Size.Width) x $($label2.Size.Height)
label2.Location: $($label2.Location.X), $($label2.Location.Y)
"@

[System.Windows.Forms.MessageBox]::Show($report, "Diagnostic numbers") | Out-Null

[void]$script:Form.ShowDialog()
