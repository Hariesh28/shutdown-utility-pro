# Changelog

## Unreleased

## 3.1.0 - 2026-10-08

- Reject invalid, out-of-range, and extra command-line arguments instead of silently coercing delays or opening the dashboard.
- Allow command-line actions and cancellation to run while the dashboard is already open; retain single-instance behavior for dashboard launches.
- Add regression coverage for command-line delay bounds and numeric format.
- Make the installer stop when desktop shortcut creation fails.
- Notify Explorer after publishing the blank-label shortcut and clarify that its icon remains visible.
- Add a safe post-extraction installation verifier, including simulated action checks, and run it against built and packaged release outputs.
- Publish a newly built executable only after its configuration and optional assets have been staged successfully.
- Fix dashboard control clipping, header overlap, and inconsistent dark button styling.
- Make invisible-name desktop shortcut creation robust to Unicode path handling and fall back clearly when unsupported.
- Expand setup and usage documentation with release verification, settings, command examples, and troubleshooting.
- Open the dashboard on a no-argument launch instead of starting a shutdown countdown.
- Enable Test mode by default and expose its state and controls in the dashboard and tray.
- Add a command-line switch that forces safe simulation for a single run.
- Store mutable settings, logs, and window state in the current user's Local AppData folder, migrating existing files on first launch.
- Preserve the previous executable if a rebuild fails, and make the installer check the build exit code.
- Require at least a one-second cancellable countdown and reject malformed command-line delays.
- Split the monolithic C# source into files grouped by application, UI, system integration, configuration, and logging responsibilities.
- Route power-action and scheduling requests through a centrally test-mode-aware controller, with offline regression coverage.
- Add a Windows CI workflow, repository ignore rules, editor settings, and contribution guidance.
- Organize application code, tests, scripts, assets, configuration templates, and supporting documentation into dedicated directories.
- Add GitHub issue/PR templates, support and conduct guidance, plus Dependabot updates for workflow actions.
- Adopt the MIT License and automate tested, checksummed Windows release assets for version tags.

## 3.0.0

- Rebuilt as a production-oriented WinForms utility.
- Cancellable interactive countdowns for shutdown, restart, sleep, hibernate and lock.
- Custom WAV playback with action-specific file selection and fallback sound.
- Configurable wait-for-sound behavior.
- System tray dashboard and quick actions.
- Native Windows scheduling for shutdown and restart.
- Cancel scheduled native shutdown/restart.
- Optional current-user Start with Windows integration.
- Optional desktop-only global hotkey, disabled by default.
- Single-instance guard.
- Atomic configuration writes.
- Window position persistence with multi-monitor validation.
- Error logging with millisecond timestamps.
- Test mode for safe development/testing.
- Embedded icon support.
- Current-user install/uninstall scripts.
- Invisible-name desktop shortcut helper without registry changes.
- Explicit absolute path to Windows shutdown.exe.
- No /f in normal shutdown/restart actions.
