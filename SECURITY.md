# Security / design notes

- The utility is a user-mode Win32/WinForms executable.
- The application manifest uses `asInvoker` and does not request elevation.
- Shutdown and restart are delegated to `%WINDIR%\System32\shutdown.exe` via an absolute path.
- `/f` is intentionally omitted from normal shutdown/restart.
- Interactive countdowns are application-local and are cancellable until execution starts.
- Native scheduled shutdown/restart uses Windows' own shutdown timer.
- The optional hotkey is registered with RegisterHotKey but is guarded by a foreground-window check.
- The configuration is plain text in `%LOCALAPPDATA%\ShutdownUtilityPro` and should be considered user-editable, not a security boundary.
- Test mode is enabled by default, is visible in the dashboard and tray, and can be forced for a single launch with `--test-mode`.
- Disabling test mode from the dashboard requires explicit confirmation; this is an accidental-use guard, not a security boundary.
- The log file is append-only from the application's perspective but is also user-writable.
- The utility does not make remote/network calls.

## Reporting a vulnerability

Do not post exploitable vulnerability details in a public issue. Use GitHub's private vulnerability reporting for this repository when enabled; otherwise contact the repository owner through a private channel and include reproduction steps and impact.
