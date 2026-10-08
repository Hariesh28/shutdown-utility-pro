$ErrorActionPreference = 'Stop'
$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$base = Split-Path -Parent $scriptsDir
$appDir = Join-Path $base 'artifacts'
$exe = Join-Path $appDir 'Shutdown.exe'
if (-not (Test-Path $exe)) { throw "Shutdown.exe not found. Run Build.cmd from the project root first." }

# U+3164 HANGUL FILLER is visually blank in normal Explorer views.
# Windows hides the .lnk extension by default when "Hide extensions for known file types" is enabled.
$invisibleName = [char]0x3164
$desktop = [Environment]::GetFolderPath('Desktop')
$link = Join-Path $desktop ($invisibleName + '.lnk')

$wsh = New-Object -ComObject WScript.Shell
$sc = $wsh.CreateShortcut($link)
$sc.TargetPath = $exe
$sc.WorkingDirectory = $appDir
$icon = Join-Path $appDir 'Shutdown.ico'
if (Test-Path $icon) { $sc.IconLocation = "$icon,0" } else { $sc.IconLocation = "$exe,0" }
$sc.WindowStyle = 1
$sc.Hotkey = ''
$sc.Description = 'Shutdown Utility Pro'
$sc.Save()
Write-Host "Created invisible-name shortcut on Desktop."
Write-Host "Note: shortcut arrows are controlled by Windows globally; this script does not alter the registry."
