# Quick setup

## 1. Get the project

Build and run the project from any local folder on Windows.

## 2. Build

Run:

`Build.cmd`

A successful build creates a runnable folder at `artifacts\`. Copy that complete folder when relocating the application.

## 3. First run

Test mode is enabled by default. Run `artifacts\Shutdown.exe` to open the dashboard; launching without arguments does not start a power action.

After extracting a release, run `.\VerifyInstall.ps1` from the extracted folder to check the executable, packaged safe-default configuration, and writable per-user data folder. Its power-action checks are always simulated; it does not send a real power request or create a Windows schedule.

Use the visible Test mode indicator and checkbox to check the countdown, sound, and cancellation behavior without affecting Windows. Schedules are also simulated. To enable real actions later, uncheck Test mode, save, and confirm the warning. You can force a one-run simulation regardless of saved settings with:

`artifacts\Shutdown.exe --test-mode /shutdown 3`

Configuration and logs are stored in `%LOCALAPPDATA%\ShutdownUtilityPro`.

## 4. Sound and icon assets

Place optional action sounds (`Shutdown.wav`, `Restart.wav`, `Sleep.wav`, `Hibernate.wav`, or `Lock.wav`) and `Shutdown.ico` in `assets\`, then rebuild to include them in the output.

## 5. Desktop shortcut and install

Create a desktop shortcut:

`powershell -ExecutionPolicy Bypass -File ".\scripts\CreateDesktopShortcut.ps1"`

Install current-user desktop integration:

`powershell -ExecutionPolicy Bypass -File ".\scripts\Install.ps1"`

Uninstall the shortcut and Start-with-Windows entry:

`powershell -ExecutionPolicy Bypass -File ".\scripts\Uninstall.ps1"`

An optional invisible-name shortcut helper is available at `scripts\CreateInvisibleDesktopShortcut.ps1`; it does not edit the registry. The helper creates the shortcut under a temporary ASCII filename, applies the invisible Unicode filename with a Unicode-aware file operation, and notifies Explorer to refresh the Desktop. The shortcut icon remains visible; only the filename label is blank. If it does not appear immediately, press **F5** while the Desktop is focused. If the filename is not supported by Windows or the desktop provider, the helper creates a standard visible shortcut instead.

## 6. Optional Start with Windows

Enable `Start with Windows (tray mode)` in the dashboard and save settings. This uses the current user's Run key and does not request administrator privileges.

## 7. Desktop-only hotkey

Leave it disabled unless needed. If enabled, the default is:

`Ctrl + Alt + Shift + S`

The app ignores the hotkey when a normal application is foreground.

## 8. Emergency cancellation

For a Windows native scheduled shutdown/restart:

`artifacts\Shutdown.exe /cancel`

Interactive countdowns can be cancelled with the Cancel button, ESC, or the countdown window's close button.
