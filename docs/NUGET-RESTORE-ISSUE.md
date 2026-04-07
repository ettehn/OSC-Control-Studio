# NuGet Restore Issue Notes

## Status

Repository verification no longer depends on downloading test packages from NuGet.

The repo now uses:

- `Directory.Build.props` to pin restore to `NuGet.Config` and `.nuget/packages` inside the repository.
- `Verify.ps1` to set `APPDATA` to `C:\CodexProjects\.appdata` before invoking `dotnet`.
- An in-repo test harness for `tests\OSCControl.Compiler.Tests`, replacing the previous `Microsoft.NET.Test.Sdk` / `xunit` package references.

## Remaining Environment Constraint

In this sandbox, plain `dotnet build` or `dotnet test` can still fail before project restore if NuGet tries to read:

```text
C:\Users\Ethen\AppData\Roaming\NuGet\NuGet.Config
```

Use the repository verification script instead of raw `dotnet` commands:

```powershell
.\Verify.ps1
```

For build-only verification:

```powershell
.\Verify.ps1 -SkipTests
```

## Manual Commands

If you need to run commands manually, set local app data first:

```powershell
New-Item -ItemType Directory -Force -Path C:\CodexProjects\.appdata\NuGet | Out-Null
$env:APPDATA = 'C:\CodexProjects\.appdata'
$env:NUGET_PACKAGES = 'C:\CodexProjects\.nuget\packages'
```

Build with single-node MSBuild in this environment:

```powershell
dotnet build C:\CodexProjects\src\OSCControl.DesktopHost\OSCControl.DesktopHost.csproj -m:1 -nr:false -v:minimal
```

Run compiler tests through the in-repo harness:

```powershell
dotnet build C:\CodexProjects\tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj -m:1 -nr:false -v:minimal
dotnet run --project C:\CodexProjects\tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj --no-restore --no-build
```

`dotnet test` is intentionally not the primary path for this test project because the project no longer uses the NuGet-based VSTest/xUnit adapter.

## If `NU1301` Reappears

Treat `NU1301` as a restore/environment problem unless compiler errors appear after restore succeeds. It usually means a new external `PackageReference` was added or the command did not use the repo-local `APPDATA` setup.
