# Contributing

## Development requirements

- Windows with the .NET Framework 4.x C# compiler.
- PowerShell for parsing or using the optional install scripts.
- Git.

The project intentionally has no external package dependencies. `Build.cmd` builds the application; `tests\ShutdownUtility.Tests\RunTests.cmd` compiles and runs the offline regression suite.

## Safe development

- Do not run real shutdown, restart, sleep, hibernate, or lock actions while testing.
- Test mode must remain enabled in all test and CI workflows.
- The regression tests verify that power requests are simulated without calling Windows power APIs.
- Do not add tests that invoke a real Windows power action or create a native shutdown schedule.
- Review any changes to `PowerController` and command-line handling with safety behavior in mind.

## Change expectations

Keep changes focused, document user-visible changes, and run both `Build.cmd /nopause` and `tests\ShutdownUtility.Tests\RunTests.cmd`. Update `CHANGELOG.md` when behavior or user-facing features change. The repository is licensed under the MIT License; see `LICENSE.txt` for the terms.

Automated assistants must not be added as commit co-authors. In particular, do not add a `Co-authored-by: Copilot` trailer; CI rejects that attribution.
