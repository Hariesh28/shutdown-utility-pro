$ErrorActionPreference = 'Stop'
$appDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $appDir 'Shutdown.exe'
$config = Join-Path $appDir 'Shutdown.config'
$icon = Join-Path $appDir 'Shutdown.ico'

if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Shutdown.exe was not found beside this script: $exe" }
if (-not (Test-Path -LiteralPath $config -PathType Leaf)) { throw "Shutdown.config was not found beside this script: $config" }
if (-not (Test-Path -LiteralPath $icon -PathType Leaf)) { throw "Shutdown.ico was not found beside this script: $icon" }

Write-Host 'Running the installed application self-test...'
Write-Host 'This check never requests a Windows power action or native shutdown schedule.'
$process = Start-Process -FilePath $exe -ArgumentList '/self-test' -WorkingDirectory $appDir -PassThru
try {
    if (-not $process.WaitForExit(30000)) {
        Stop-Process -Id $process.Id
        throw 'The application self-test timed out after 30 seconds.'
    }
    if ($process.ExitCode -ne 0) {
        $log = Join-Path $env:LOCALAPPDATA 'ShutdownUtilityPro\Shutdown.log'
        throw "The application self-test failed with exit code $($process.ExitCode). Check the log: $log"
    }
}
finally {
    $process.Dispose()
}

Write-Host 'PASS: the installed executable started, verified its packaged configuration, wrote per-user diagnostic data, and simulated all supported actions and schedules.'
Write-Host 'PASS: no real power action or native schedule was requested.'
