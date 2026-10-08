# Shutdown Utility Pro

[![Build and test](https://github.com/Hariesh28/shutdown-utility-pro/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Hariesh28/shutdown-utility-pro/actions/workflows/ci.yml)

A self-contained Windows power-control utility with a safe, cancellable shutdown workflow.

## Repository layout

```text
.
|-- .github/
|   `-- workflows/                  CI and tagged-release automation
|-- assets/                     WAV files and optional icons
|-- config/                     Default configuration template
|-- docs/                       Setup, security, release notes, examples
|-- scripts/                    Install, uninstall, shortcut helpers
|-- src/ShutdownUtility/        C# application sources and manifest
|-- tests/ShutdownUtility.Tests/ Offline regression suite
|-- artifacts/                  Generated distributable (ignored by Git)
|-- Build.cmd
`-- README.md
```

The build copies the configuration template and any supported WAV files (`Shutdown.wav`, `Restart.wav`, `Sleep.wav`, `Hibernate.wav`, `Lock.wav`) into the self-contained `artifacts/` application folder. An optional `Shutdown.ico` in `assets/` is embedded and copied when present.

Configuration, logs, and saved window position are stored per-user in:

`%LOCALAPPDATA%\ShutdownUtilityPro`

On first launch, existing `Shutdown.config`, `Shutdown.log`, and `WindowState.txt` files beside the executable are copied into this folder if no user copy exists.

## Main features

### Interactive power actions

- Shut down
- Restart
- Sleep
- Hibernate
- Lock

All interactive actions run through a cancellable countdown window. The default countdown is 3 seconds.

### Sound system

- WAV playback with the built-in `System.Media.SoundPlayer`.
- `WaitForSound=true` plays the WAV fully before the countdown starts.
- Set it to `false` to start the countdown while the WAV is playing.
- If an action-specific WAV exists it is used; otherwise `SoundFile=` is used.
- Missing/broken audio never prevents a power action from being attempted.

### Safety

- No `/f` is used for shutdown or restart.
- ESC cancels an interactive countdown.
- Cancel button cancels an interactive countdown.
- Scheduled shutdowns/restarts use Windows' native timer and can be cancelled.
- The optional global hotkey is disabled by default.
- When enabled, `Ctrl+Alt+Shift+S` is filtered so it only acts if the desktop (`Progman` or `WorkerW`) is foreground.
- Application requests `asInvoker`; it does not elevate to administrator.
- Single-instance mutex prevents multiple copies fighting over the tray/hotkey.

### System tray

The tray menu includes:

- Open dashboard
- Shut down…
- Restart…
- Sleep…
- Hibernate…
- Lock (cancellable countdown)
- Schedule shutdown: 30 seconds / 5 minutes / 10 minutes
- Schedule restart: 10 minutes
- Cancel scheduled power action
- Test sound
- Open folder
- View log
- Exit

### Settings dashboard

- Countdown duration: 1–3600 seconds
- Sound path
- Play sound toggle
- Wait for sound toggle
- Notifications toggle
- Desktop-only hotkey toggle
- Start in tray
- Close-to-tray
- Start with Windows
- Default schedule duration
- Save configuration
- Reset-to-safe-defaults control
- Test mode is enabled by default, shown in the dashboard and tray, and available as a dashboard setting
- Log rotation at approximately 2 MB

Configuration is a simple, human-readable file and is saved atomically. Reset-to-safe-defaults enables Test mode.

Disabling Test mode in the dashboard requires confirming that future actions can affect Windows. The command-line `--test-mode` (or `/test-mode`) switch forces simulation for that run even if the saved setting allows real actions.

### Logging

Important events/errors are written to:

`%LOCALAPPDATA%\ShutdownUtilityPro\Shutdown.log`

The log never controls or blocks the power action.

### Window state

The dashboard can remember its last position and size in `%LOCALAPPDATA%\ShutdownUtilityPro\WindowState.txt`. It checks that the saved rectangle is still on a connected display before restoring it.

## Build

Run `Build.cmd` from the repository root.

The build uses the installed .NET Framework 4.x C# compiler:

`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`

or the 32-bit fallback:

`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe`

The resulting `artifacts\Shutdown.exe` runs on Windows with .NET Framework 4.6 or later.

The executable is built as a GUI app (`winexe`) with optimizations, no debug symbols and an application manifest. If `assets\Shutdown.ico` exists it is embedded into the EXE.

## Source layout

- `src\ShutdownUtility\Program.cs` - application startup and command-line handling
- `src\ShutdownUtility\MainForm.cs` - dashboard and system-tray interface
- `src\ShutdownUtility\CountdownForm.cs` - cancellable action countdown
- `src\ShutdownUtility\WindowsServices.cs` - power controller and Windows integration
- `src\ShutdownUtility\AppConfig.cs` and `Logger.cs` - settings and diagnostics
- `config\Shutdown.config` - safe default configuration template
- `tests\ShutdownUtility.Tests\` - offline regression suite

`Build.cmd` compiles the source into `artifacts\Shutdown.exe`; generated output is intentionally ignored by Git.

## Tests and continuous integration

Run the regression suite on Windows with:

`tests\ShutdownUtility.Tests\RunTests.cmd`

The dependency-free suite checks safe defaults, config parsing and persistence, countdown/schedule bounds, and that every power action and supported schedule is simulated in Test mode. It does not invoke shutdown, restart, sleep, hibernate, lock, or native scheduling.

GitHub Actions builds the application and runs these tests on Windows for pushes and pull requests. Local verification uses:

```text
Build.cmd /nopause
tests\ShutdownUtility.Tests\RunTests.cmd
```

## Create desktop shortcut

After building:

`powershell -ExecutionPolicy Bypass -File ".\scripts\CreateDesktopShortcut.ps1"`

The script points directly at `artifacts\Shutdown.exe` and deliberately leaves the Windows Shortcut key blank.

## Install / uninstall

Install current-user desktop integration:

`powershell -ExecutionPolicy Bypass -File ".\scripts\Install.ps1"`

Remove the desktop shortcut and Start-with-Windows entry (the program folder and per-user settings/logs remain):

`powershell -ExecutionPolicy Bypass -File ".\scripts\Uninstall.ps1"`

## Command line

`artifacts\Shutdown.exe`                 Open the dashboard (does not start a power action)

`artifacts\Shutdown.exe /tray`           Start in system tray

`artifacts\Shutdown.exe /restart 5`      Restart after a 5-second countdown

`artifacts\Shutdown.exe /sleep 5`        Sleep after a 5-second countdown

`artifacts\Shutdown.exe /hibernate 5`    Hibernate after a 5-second countdown

`artifacts\Shutdown.exe /lock 3`          Lock after a 3-second countdown

`artifacts\Shutdown.exe /schedule-shutdown 300`  Schedule shutdown in 300 seconds

`artifacts\Shutdown.exe /schedule-restart 600`  Schedule restart in 600 seconds

`artifacts\Shutdown.exe /cancel`         Cancel Windows scheduled shutdown/restart

`artifacts\Shutdown.exe /test-sound`     Play the configured shutdown sound

`artifacts\Shutdown.exe --test-mode /shutdown 3`  Simulate a 3-second shutdown without affecting Windows

`artifacts\Shutdown.exe /help`           Show command-line help

## Recommended configuration

For your desktop setup, keep:

`CountdownSeconds=3`

`PlaySound=true`

`WaitForSound=true`

`EnableDesktopOnlyHotkey=false`

`CloseToTray=true`

`StartWithWindows=false`

Test mode is enabled on first run. Keep it enabled while checking the UI, sound, countdown, cancellation, and schedule controls. To permit real power actions, turn it off in the dashboard and accept the warning. You can always force simulation for an individual command with `--test-mode`.

## Project guidance

- [Setup instructions](docs/SETUP.md)
- [Release checklist](docs/RELEASE.md)
- [Security notes and reporting](SECURITY.md)
- [Contributing](CONTRIBUTING.md)
- [Support](SUPPORT.md)

## License

Licensed under the [MIT License](LICENSE.txt).

## What it intentionally does not do

This version does not force-close applications, bypass Windows power-policy prompts, change system sleep settings, disable security controls, or request administrator privileges.

## Test mode

Test mode is enabled by default and prevents shutdown, restart, sleep, hibernate, lock, and native schedule requests. The dashboard clearly indicates when it is active. Keep it enabled during development and testing; disable it only when you intentionally want actions to affect Windows.
