$ErrorActionPreference = 'Stop'
$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$base = Split-Path -Parent $scriptsDir
$appDir = Join-Path $base 'artifacts'
$exe = Join-Path $appDir 'Shutdown.exe'
if (-not (Test-Path $exe))
{
    throw "Shutdown.exe not found. Run Build.cmd from the project root first."
}

$desktop = [Environment]::GetFolderPath('Desktop')
if (-not (Test-Path -LiteralPath $desktop -PathType Container))
{
    throw "Desktop folder was not found: $desktop"
}

function Publish-ShortcutFile
{
    param(
        [string]$TemporaryPath,
        [string]$DestinationPath
    )

    if ( [System.IO.File]::Exists($DestinationPath))
    {
        $backupPath = Join-Path (Split-Path -Parent $DestinationPath) ('.ShutdownUtilityPro-' + [Guid]::NewGuid().ToString('N') + '.bak')
        try
        {
            [System.IO.File]::Replace($TemporaryPath, $DestinationPath, $backupPath)
        }
        finally
        {
            if ( [System.IO.File]::Exists($backupPath))
            {
                try
                {
                    [System.IO.File]::Delete($backupPath)
                }
                catch
                {
                    Write-Warning "Shortcut was replaced, but the temporary backup could not be removed: $backupPath"
                }
            }
        }
    }
    else
    {
        [System.IO.File]::Move($TemporaryPath, $DestinationPath)
    }
}

$invisibleName = [string][char]0x3164
$invisibleLink = Join-Path $desktop ($invisibleName + '.lnk')
$visibleLink = Join-Path $desktop 'Shutdown Utility Pro.lnk'
$temporaryLink = Join-Path $desktop ('.ShutdownUtilityPro-' + [Guid]::NewGuid().ToString('N') + '.lnk')
$shortcut = $null

if (-not ('ShutdownUtilityShellNotifications' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class ShutdownUtilityShellNotifications
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern void SHChangeNotify(uint eventId, uint flags, string path, IntPtr item);
}
'@
}

try
{
    $wsh = New-Object -ComObject WScript.Shell
    $shortcut = $wsh.CreateShortcut($temporaryLink)
    $shortcut.TargetPath = $exe
    $shortcut.WorkingDirectory = $appDir
    $icon = Join-Path $appDir 'Shutdown.ico'
    if (Test-Path $icon)
    {
        $shortcut.IconLocation = "$icon,0"
    }
    else
    {
        $shortcut.IconLocation = "$exe,0"
    }
    $shortcut.WindowStyle = 1
    $shortcut.Hotkey = ''
    $shortcut.Description = 'Shutdown Utility Pro'
    $shortcut.Save()
    if (-not [System.IO.File]::Exists($temporaryLink))
    {
        throw "Windows did not create the temporary shortcut: $temporaryLink"
    }

    try
    {
        # Save through WSH to an ASCII path, then rename with .NET's Unicode-aware file API.
        Publish-ShortcutFile -TemporaryPath $temporaryLink -DestinationPath $invisibleLink
        [ShutdownUtilityShellNotifications]::SHChangeNotify(0x00000002, 0x00000005, $invisibleLink, [IntPtr]::Zero)
        [ShutdownUtilityShellNotifications]::SHChangeNotify(0x00002000, 0x00000005, $invisibleLink, [IntPtr]::Zero)
        [ShutdownUtilityShellNotifications]::SHChangeNotify(0x00001000, 0x00000005, $desktop, [IntPtr]::Zero)
        Write-Host 'Created shortcut. Its icon remains visible; only the filename label is blank.'
        Write-Host "Desktop location: $desktop"
    }
    catch
    {
        $invisibleError = $_.Exception.Message
        Publish-ShortcutFile -TemporaryPath $temporaryLink -DestinationPath $visibleLink
        [ShutdownUtilityShellNotifications]::SHChangeNotify(0x00000002, 0x00000005, $visibleLink, [IntPtr]::Zero)
        [ShutdownUtilityShellNotifications]::SHChangeNotify(0x00002000, 0x00000005, $visibleLink, [IntPtr]::Zero)
        [ShutdownUtilityShellNotifications]::SHChangeNotify(0x00001000, 0x00000005, $desktop, [IntPtr]::Zero)
        Write-Warning "Windows could not publish the invisible-name shortcut ($invisibleError). Created a standard shortcut instead: $visibleLink"
    }
}
finally
{
    if ( [System.IO.File]::Exists($temporaryLink))
    {
        [System.IO.File]::Delete($temporaryLink)
    }
}

Write-Host "Note: shortcut arrows are controlled by Windows globally; this script does not alter the registry."
