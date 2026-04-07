# NuGet Restore Issue Notes

## Symptom

In this workspace, test/build commands that need package restore can fail with:

```text
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json
```

Earlier runs also hit a sandbox-specific user config access problem:

```text
Access to the path 'C:\Users\Ethen\AppData\Roaming\NuGet\NuGet.Config' is denied.
```

## Current Interpretation

Treat this as an environment / restore problem, not a source-code failure, unless a compiler error appears after restore succeeds.

The core projects have been buildable when using a repo-local app data directory and package cache:

- `OSCControl.Compiler`
- `OSCControl.Packaging`
- `OSCControl.AppHost`
- `OSCControl.Packager`
- `OSCControl.DesktopHost`

The xUnit test project can still be blocked because it depends on NuGet packages such as xUnit and Microsoft.NET.Test.Sdk.

## Recommended Workaround

Use a repo-local app data directory before running verification commands:

```powershell
New-Item -ItemType Directory -Force -Path C:\CodexProjects\.appdata\NuGet | Out-Null
$env:APPDATA = 'C:\CodexProjects\.appdata'
```

Then prefer repo-local config/cache flags:

```powershell
dotnet build C:\CodexProjects\src\OSCControl.DesktopHost\OSCControl.DesktopHost.csproj --configfile C:\CodexProjects\NuGet.Config -p:RestorePackagesPath=C:\CodexProjects\.nuget\packages -m:1 -nr:false -v:minimal
```

For tests, only use `--no-restore` when test packages are already restored:

```powershell
dotnet test C:\CodexProjects\tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj --no-restore -m:1 -nr:false -v:minimal
```

If this still reports `NU1301`, restore is still blocked.

## Follow-Up Options

- Ensure network access to `https://api.nuget.org/v3/index.json` in the development environment.
- Pre-restore packages into `C:\CodexProjects\.nuget\packages` before running offline checks.
- Keep using core project builds and packaging smoke tests as fallback verification when xUnit restore is blocked.
