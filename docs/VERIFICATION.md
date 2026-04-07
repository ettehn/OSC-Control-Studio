# Verification

Use the repository verification script for normal local checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Verify.ps1
```

For build-only verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Verify.ps1 -SkipTests
```

The script configures repository-local .NET/NuGet state so constrained Windows environments are less likely to depend on user-level NuGet configuration.

It builds these projects:

- `OSCControl.Compiler`
- `OSCControl.Packaging`
- `OSCControl.AppHost`
- `OSCControl.Packager`
- `OSCControl.DesktopHost`
- `OSCControl.Compiler.Tests`

Test verification runs through the in-repo console harness instead of the NuGet-based VSTest/xUnit adapter.

The repository also includes a solution entry point for IDE loading or manual project-wide builds:

```powershell
dotnet build .\OSCControl.sln -m:1 -nr:false -v:minimal
```

Prefer `Verify.ps1` for routine validation because it also runs the in-repo test harness.

## Known Environment Limitation

In constrained sandbox environments, WebSocket listener tests may be skipped when `HttpListener` cannot start. The test harness reports that skip explicitly. A skip for that case does not automatically mean the source failed to build.

## Minimal Packaging Smoke Check

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Verify.ps1 -SkipTests

dotnet run --project .\src\OSCControl.Packager\OSCControl.Packager.csproj -- `
  .\sample-plan.osccontrol `
  .\artifacts\verification `
  VerificationApp `
  .\src\OSCControl.AppHost\bin\Debug\net8.0
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
