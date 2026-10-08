$ErrorActionPreference = 'Stop'
$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$base = Split-Path -Parent $scriptsDir
$appDir = Join-Path $base 'artifacts'
$exe = Join-Path $appDir 'Shutdown.exe'
if (-not (Test-Path $exe)) { throw "Shutdown.exe not found. Run Build.cmd from the project root first." }
$desktop = [Environment]::GetFolderPath('Desktop')
$link = Join-Path $desktop 'Shutdown Utility Pro.lnk'
$wsh = New-Object -ComObject WScript.Shell
$sc = $wsh.CreateShortcut($link)
$sc.TargetPath = $exe
$sc.WorkingDirectory = $appDir
$icon = Join-Path $appDir 'Shutdown.ico'
if (Test-Path $icon) { $sc.IconLocation = "$icon,0" } else { $sc.IconLocation = "$exe,0" }
$sc.WindowStyle = 1
$sc.Description = 'Safe shutdown utility with cancellable countdown'
$sc.Save()
Write-Host "Created: $link"
