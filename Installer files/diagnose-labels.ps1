# ================================================================
#  Minimal diagnostic: does a wrapped Label render in a
#  FlowLayoutPanel at all? Isolated from the rest of the installer.
# ================================================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$form = New-Object System.Windows.Forms.Form
$form.Text = "Label Diagnostic"
$form.ClientSize = New-Object System.Drawing.Size(640, 400)
$form.StartPosition = "CenterScreen"

$panel = New-Object System.Windows.Forms.FlowLayoutPanel
$panel.Dock = "Fill"
$panel.FlowDirection = "TopDown"
$panel.WrapContents = $false
$panel.AutoScroll = $true
$panel.Padding = New-Object System.Windows.Forms.Padding(20)
$panel.BackColor = [System.Drawing.Color]::White
$form.Controls.Add($panel)

# -- Test 1: a plain Label, no wrapping, no AutoSize tricks at all --
$plain = New-Object System.Windows.Forms.Label
$plain.Text = "TEST 1: plain Label, AutoSize true, no MaximumSize"
$plain.AutoSize = $true
$plain.BackColor = [System.Drawing.Color]::Yellow
$panel.Controls.Add($plain)

# -- Test 2: AutoSize + MaximumSize(w,0) -- the original approach --
$wrap1 = New-Object System.Windows.Forms.Label
$wrap1.Text = "TEST 2: AutoSize true + MaximumSize(560,0). This line is deliberately long enough that it should need to wrap onto a second line to fit within five hundred sixty pixels of width, so you should see two lines of text here if wrapping works."
$wrap1.AutoSize = $true
$wrap1.MaximumSize = New-Object System.Drawing.Size(560, 0)
$wrap1.BackColor = [System.Drawing.Color]::LightGreen
$panel.Controls.Add($wrap1)

# -- Test 3: TextRenderer.MeasureText + fixed Size, AutoSize off --
$font3 = New-Object System.Drawing.Font("Segoe UI", 9)
$text3 = "TEST 3: TextRenderer.MeasureText + fixed Size, AutoSize false. This line is also deliberately long enough that it should need to wrap onto a second line to fit within five hundred sixty pixels of width."
$bounds3 = New-Object System.Drawing.Size(560, 0)
$measured3 = [System.Windows.Forms.TextRenderer]::MeasureText($text3, $font3, $bounds3, [System.Windows.Forms.TextFormatFlags]::WordBreak)
$wrap2 = New-Object System.Windows.Forms.Label
$wrap2.Font = $font3
$wrap2.Text = $text3
$wrap2.AutoSize = $false
$wrap2.Size = New-Object System.Drawing.Size(560, $measured3.Height)
$wrap2.BackColor = [System.Drawing.Color]::LightBlue
$panel.Controls.Add($wrap2)

# -- Test 4: same as Test 3 but added to the panel BEFORE sizing --
$font4 = New-Object System.Drawing.Font("Segoe UI", 9)
$text4 = "TEST 4: same as Test 3, but Added to the panel before Size is set, to check whether add-order matters."
$wrap3 = New-Object System.Windows.Forms.Label
$wrap3.Font = $font4
$wrap3.Text = $text4
$wrap3.AutoSize = $false
$wrap3.BackColor = [System.Drawing.Color]::LightPink
$panel.Controls.Add($wrap3)
$bounds4 = New-Object System.Drawing.Size(560, 0)
$measured4 = [System.Windows.Forms.TextRenderer]::MeasureText($text4, $font4, $bounds4, [System.Windows.Forms.TextFormatFlags]::WordBreak)
$wrap3.Size = New-Object System.Drawing.Size(560, $measured4.Height)

[System.Windows.Forms.MessageBox]::Show(
    "Measured sizes (shown before the window, in case the window itself doesn't render text):`n`n" +
    "Test 3: $($measured3.Width) x $($measured3.Height)`n" +
    "Test 4: $($measured4.Width) x $($measured4.Height)",
    "Measured sizes"
) | Out-Null

[void]$form.ShowDialog()
