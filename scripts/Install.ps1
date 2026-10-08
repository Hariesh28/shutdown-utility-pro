$ErrorActionPreference = 'Stop'
$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$base = Split-Path -Parent $scriptsDir
$build = Join-Path $base 'Build.cmd'
if (-not (Test-Path $build)) { throw 'Build.cmd not found.' }
Write-Host 'Building Shutdown Utility Pro...'
$buildProcess = Start-Process -FilePath $build -ArgumentList '/nopause' -WorkingDirectory $base -Wait -PassThru
if ($buildProcess.ExitCode -ne 0) { throw "Build.cmd failed with exit code $($buildProcess.ExitCode)." }
$exe = Join-Path $base 'artifacts\Shutdown.exe'
if (-not (Test-Path $exe)) { throw 'Build failed: Shutdown.exe was not created.' }
$shortcutScript = Join-Path $scriptsDir 'CreateDesktopShortcut.ps1'
& PowerShell.exe -NoProfile -ExecutionPolicy Bypass -File $shortcutScript
if ($LASTEXITCODE -ne 0) { throw "Desktop shortcut creation failed with exit code $LASTEXITCODE." }
Write-Host ''
Write-Host 'Installed for the current user.' -ForegroundColor Green
Write-Host 'The desktop shortcut has no keyboard shortcut assigned by default.'
