# NuGet Restore Agent Handoff

This note is for future agents working in `C:\CodexProjects`.

## Current State

The old verification blocker was not a source-code failure. It was caused by NuGet restore trying to read the sandbox-inaccessible user config:

```text
C:\Users\Ethen\AppData\Roaming\NuGet\NuGet.Config
```

The test project also used to depend on NuGet packages:

- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`

Those package references have been removed from `tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj`. The tests now run through an in-repo console harness in `tests\OSCControl.Compiler.Tests\TestFramework.cs`.

## Preferred Verification

Use the repo script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\CodexProjects\Verify.ps1
```

For build-only checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\CodexProjects\Verify.ps1 -SkipTests
```

The script sets repo-local state before running `dotnet`:

- `APPDATA=C:\CodexProjects\.appdata`
- `NUGET_PACKAGES=C:\CodexProjects\.nuget\packages`

It builds the core projects and then runs the compiler test harness with:

```powershell
dotnet run --project C:\CodexProjects\tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj --no-restore --no-build
```

## Do Not Use As Primary Path

Do not treat raw `dotnet test` as the main validation command for this repository. The test project is intentionally no longer a NuGet/VSTest/xUnit-adapter project.

Plain `dotnet build` or `dotnet run` may also fail in this sandbox if they trigger restore without the repo-local `APPDATA` setup. Use `Verify.ps1` unless you are explicitly debugging restore behavior.

## Expected Result

In the current sandbox, full verification is expected to pass with one explicit skip:

```text
21 passed, 0 failed, 1 skipped, 22 total
```

The skipped test is:

```text
RuntimeHostTests.WebSocketServerInput_TriggersRuntimeRule
```

Reason: `HttpListener` cannot start in this sandbox and reports an invalid handle. This is not a NuGet issue.

## Files Involved

- `Directory.Build.props`: pins restore config/cache to the repository.
- `Verify.ps1`: standard verification entry point for agents.
- `docs\NUGET-RESTORE-ISSUE.md`: user-facing NuGet issue record.
- `docs\VERIFICATION.md`: verification instructions.
- `tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj`: now an executable test harness, not a VSTest package project.
- `tests\OSCControl.Compiler.Tests\TestFramework.cs`: minimal `[Fact]`, `Assert`, skip, and test runner implementation.
- `tests\OSCControl.Compiler.Tests\RuntimeHostTests.cs`: skips the WebSocket listener test when sandbox `HttpListener` startup fails.

## Caution

At the time this note was written, `docs\agent-advice\osccontrol-internal-advice.md` already had unrelated uncommitted edits. Do not revert or rewrite that file unless the user explicitly asks.
