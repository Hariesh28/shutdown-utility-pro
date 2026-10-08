$ErrorActionPreference = 'Stop'
$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$base = Split-Path -Parent $scriptsDir
$appDir = Join-Path $base 'artifacts'
$exe = Join-Path $appDir 'Shutdown.exe'
if (-not (Test-Path $exe)) { throw "Shutdown.exe not found. Run Build.cmd from the project root first." }
$desktop = [Environment]::GetFolderPath('Desktop')
if (-not (Test-Path -LiteralPath $desktop -PathType Container)) { throw "Desktop folder was not found: $desktop" }
$link = Join-Path $desktop 'Shutdown Utility Pro.lnk'
$temporaryLink = Join-Path $desktop ('.ShutdownUtilityPro-' + [Guid]::NewGuid().ToString('N') + '.lnk')
$shortcut = $null

try {
    $wsh = New-Object -ComObject WScript.Shell
    $shortcut = $wsh.CreateShortcut($temporaryLink)
    $shortcut.TargetPath = $exe
    $shortcut.Arguments = '/shutdown'
    $shortcut.WorkingDirectory = $appDir
    $icon = Join-Path $appDir 'Shutdown.ico'
    if (Test-Path $icon) { $shortcut.IconLocation = "$icon,0" } else { $shortcut.IconLocation = "$exe,0" }
    $shortcut.WindowStyle = 1
    $shortcut.Description = 'Start the configured, cancellable Windows shutdown countdown'
    $shortcut.Save()
    if (-not [System.IO.File]::Exists($temporaryLink)) { throw "Windows did not create the temporary shortcut: $temporaryLink" }

    if ([System.IO.File]::Exists($link)) {
        $backupLink = Join-Path $desktop ('.ShutdownUtilityPro-' + [Guid]::NewGuid().ToString('N') + '.bak')
        try {
            [System.IO.File]::Replace($temporaryLink, $link, $backupLink)
        }
        finally {
            if ([System.IO.File]::Exists($backupLink)) {
                try { [System.IO.File]::Delete($backupLink) }
                catch { Write-Warning "Shortcut was replaced, but its temporary backup could not be removed: $backupLink" }
            }
        }
    }
    else {
        [System.IO.File]::Move($temporaryLink, $link)
    }
}
finally {
    if ([System.IO.File]::Exists($temporaryLink)) { [System.IO.File]::Delete($temporaryLink) }
}

Write-Host "Created or updated: $link"
Write-Host 'Double-click the desktop shortcut to start the shutdown countdown. Windows handles open apps normally and may show save prompts.'
