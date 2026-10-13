# ================================================================
#  Diagnostic 13: the Welcome screen's last bullet is clipped at the
#  bottom and doesn't scroll into view, even with AutoScroll=true on
#  the content FlowLayoutPanel. This reproduces it with enough
#  stacked labels to overflow a small window, then tests whether
#  explicitly recalculating AutoScrollMinSize after adding content
#  fixes it.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Scroll Diagnostic"
$script:Form.ClientSize = New-Object System.Drawing.Size(640, 300)   # deliberately short, so content overflows
$script:Form.StartPosition = "CenterScreen"

$script:ContentPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$script:ContentPanel.Dock          = "Fill"
$script:ContentPanel.FlowDirection = "TopDown"
$script:ContentPanel.WrapContents  = $false
$script:ContentPanel.AutoScroll    = $true
$script:ContentPanel.Padding       = New-Object System.Windows.Forms.Padding(20)
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
    $lbl.Margin = New-Object System.Windows.Forms.Padding(0, 0, 0, 10)
    return $lbl
}

for ($i = 1; $i -le 30; $i++) {
    $script:ContentPanel.Controls.Add((New-WrapLabel -Text "Line $i of 30 -- this content panel is only 300px tall, this definitely needs scrolling to see all of it."))
}

# The fix being tested: explicitly tell the panel how tall its content actually is,
# instead of trusting AutoScroll to have picked it up automatically.
$preferred = $script:ContentPanel.GetPreferredSize((New-Object System.Drawing.Size($script:ContentPanel.ClientSize.Width, 0)))
$script:ContentPanel.AutoScrollMinSize = New-Object System.Drawing.Size(0, $preferred.Height)

[System.Windows.Forms.MessageBox]::Show("AutoScrollMinSize set to: 0 x $($preferred.Height)`nContentPanel.ClientSize: $($script:ContentPanel.ClientSize.Width) x $($script:ContentPanel.ClientSize.Height)", "Diagnostic numbers") | Out-Null

[void]$script:Form.ShowDialog()
