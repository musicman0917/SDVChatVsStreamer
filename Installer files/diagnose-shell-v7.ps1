# ================================================================
#  Diagnostic 8: the user spotted it directly -- content was
#  "staying behind the header," a Z-order overlap, not a sizing or
#  paint issue. The fix: a Dock=Fill control must be added to its
#  parent's Controls collection LAST, after every Dock=Top/Bottom
#  sibling, or its fill calculation won't correctly exclude the
#  space those siblings already claimed. Every version so far added
#  ContentPanel (Fill) BEFORE FooterPanel (Bottom). This reorders to
#  Header, Footer, THEN Content -- Content added last.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Shell Diagnostic v7 (Content added LAST)"
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

# -- ContentPanel added LAST, after both Header and Footer --
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

$label1 = New-WrapLabel -Text "LABEL 1: this label is positioned right at the top of the content area (Y=20, closest to the header). This sentence is long enough to wrap onto a second line."
$label1.Location = New-Object System.Drawing.Point(20, 20)
$script:ContentPanel.Controls.Add($label1)

$label2 = New-WrapLabel -Text "LABEL 2: positioned below label 1."
$label2.Location = New-Object System.Drawing.Point(20, ($label1.Location.Y + $label1.Height + 10))
$script:ContentPanel.Controls.Add($label2)

[void]$script:Form.ShowDialog()
