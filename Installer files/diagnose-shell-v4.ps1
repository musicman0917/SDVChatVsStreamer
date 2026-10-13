# ================================================================
#  Diagnostic 5: same as v3, but reports exact numbers via a
#  MessageBox instead of relying on reading pixels -- v3 showed only
#  the second (short) label, suggesting the first (long, wrapping)
#  label may be getting Height=0. This confirms or refutes that
#  directly, plus reports the content panel's own effective size.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic v4 (numbers, not pixels)"
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

$longText  = "SCREEN ONE: fixed-height header this time, no AutoSize anywhere. This sentence is deliberately long enough to need two lines at 560 pixels of width."
$shortText = "A second label on screen one, also yellow, also should be visible."

$label1 = New-WrapLabel -Text $longText
$label1.Location = New-Object System.Drawing.Point(20, 20)
$script:ContentPanel.Controls.Add($label1)

$label2 = New-WrapLabel -Text $shortText
$label2.Location = New-Object System.Drawing.Point(20, ($label1.Location.Y + $label1.Height + 10))
$script:ContentPanel.Controls.Add($label2)

$report = @"
label1.Size:     $($label1.Size.Width) x $($label1.Size.Height)
label1.Location: $($label1.Location.X), $($label1.Location.Y)

label2.Size:     $($label2.Size.Width) x $($label2.Size.Height)
label2.Location: $($label2.Location.X), $($label2.Location.Y)

ContentPanel.ClientSize:       $($script:ContentPanel.ClientSize.Width) x $($script:ContentPanel.ClientSize.Height)
ContentPanel.DisplayRectangle: X=$($script:ContentPanel.DisplayRectangle.X) Y=$($script:ContentPanel.DisplayRectangle.Y) W=$($script:ContentPanel.DisplayRectangle.Width) H=$($script:ContentPanel.DisplayRectangle.Height)
ContentPanel.Controls.Count:   $($script:ContentPanel.Controls.Count)
"@

[System.Windows.Forms.MessageBox]::Show($report, "Diagnostic numbers") | Out-Null

[void]$script:Form.ShowDialog()
