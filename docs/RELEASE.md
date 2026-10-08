# Release checklist

Releases are created by pushing a version tag such as `v3.0.0`. The GitHub release workflow builds the application, runs the offline regression suite, packages the distributable as a ZIP, computes a SHA-256 checksum, and publishes both files with generated release notes. The regular CI workflow also runs for the tag.

Before publishing a release:

1. Build on a clean supported Windows machine.
2. Run `tests\ShutdownUtility.Tests\RunTests.cmd` and retain Test mode for all automated verification.
3. Test shutdown, restart, sleep, hibernate, lock and scheduling with Test mode enabled; do not disable it on a primary workstation just to test.
4. Test with and without a WAV file.
5. Test cancel via ESC and the Cancel button.
6. Verify native schedule/cancel suppression in Test mode; exercise real Windows scheduling only in an isolated test VM.
7. Test tray mode and Start with Windows.
8. Test the desktop-only hotkey with the desktop, Chrome, VS Code and a fullscreen app.
9. Review `%LOCALAPPDATA%\ShutdownUtilityPro\Shutdown.log` for unexpected errors.
10. Ship with Test mode enabled by default. Document that turning it off permits real system actions.
11. Digitally sign the final EXE if it will be distributed to other machines.

The repository uses the MIT License. Review the license and generated release notes before announcing a release.
