$ErrorActionPreference = 'Stop'
$base = Split-Path -Parent $MyInvocation.MyCommand.Path
$desktop = [Environment]::GetFolderPath('Desktop')
$link = Join-Path $desktop 'Shutdown Utility Pro.lnk'
if (Test-Path $link) { Remove-Item $link -Force }
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Remove-ItemProperty -Path $runKey -Name 'ShutdownUtilityPro' -ErrorAction SilentlyContinue
Write-Host 'Desktop shortcut and Start with Windows entry removed.' -ForegroundColor Green
Write-Host 'The program folder was not deleted.'
