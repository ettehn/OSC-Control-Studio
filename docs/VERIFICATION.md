# OSCControl Verification Notes

This repository is often built in constrained Windows environments where the default user NuGet config may be inaccessible. Use the repository script for normal verification:

```powershell
.\Verify.ps1
```

The repository also includes a solution entry point for IDE loading or manual project-wide builds:

```powershell
dotnet build C:\CodexProjects\OSCControl.sln -m:1 -nr:false -v:minimal
```

Prefer `Verify.ps1` for routine validation because it also runs the in-repo test harness.

For build-only verification:

```powershell
.\Verify.ps1 -SkipTests
```

The script sets:

- `APPDATA=C:\CodexProjects\.appdata`
- `NUGET_PACKAGES=C:\CodexProjects\.nuget\packages`

It then builds the core projects and the compiler test harness with `-m:1 -nr:false -v:minimal`.

Core projects covered:

- `OSCControl.Compiler`
- `OSCControl.Packaging`
- `OSCControl.AppHost`
- `OSCControl.Packager`
- `OSCControl.DesktopHost`

Test verification is run through the in-repo console harness, not the NuGet-based VSTest/xUnit adapter:

```powershell
dotnet build C:\CodexProjects\tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj -m:1 -nr:false -v:minimal
dotnet run --project C:\CodexProjects\tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj --no-restore --no-build
```

In this sandbox, the WebSocket server listener test may be skipped when `HttpListener` cannot start because of an invalid handle. That skip is reported explicitly by the harness.

Minimal packaging smoke check:

```powershell
$env:APPDATA = 'C:\CodexProjects\.appdata'
dotnet run --project C:\CodexProjects\src\OSCControl.Packager\OSCControl.Packager.csproj -- C:\CodexProjects\sample-plan.osccontrol C:\CodexProjects\artifacts\verification VerificationApp C:\CodexProjects\src\OSCControl.AppHost\bin\Debug\net8.0
```

Expected package shape:

```text
VerificationApp/
  app/
    app.manifest.json
    app.osccontrol
    app.plan.json
    assets/
  host/
  data/
  logs/
  run.cmd
```
