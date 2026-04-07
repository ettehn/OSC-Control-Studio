# OSCControl Verification Notes

This repository is often built in constrained Windows environments where the default user NuGet config may be inaccessible and external network access to `https://api.nuget.org/v3/index.json` may fail.

Recommended environment setup before verification:

```powershell
New-Item -ItemType Directory -Force -Path C:\CodexProjects\.appdata\NuGet | Out-Null
$env:APPDATA = 'C:\CodexProjects\.appdata'
```

The repo already contains `C:\CodexProjects\NuGet.Config`; use it together with the repo-local package cache.

Core build checks:

```powershell
dotnet build C:\CodexProjects\src\OSCControl.Compiler\OSCControl.Compiler.csproj --configfile C:\CodexProjects\NuGet.Config -p:RestorePackagesPath=C:\CodexProjects\.nuget\packages -m:1 -nr:false -v:minimal
dotnet build C:\CodexProjects\src\OSCControl.Packaging\OSCControl.Packaging.csproj --configfile C:\CodexProjects\NuGet.Config -p:RestorePackagesPath=C:\CodexProjects\.nuget\packages -m:1 -nr:false -v:minimal
dotnet build C:\CodexProjects\src\OSCControl.AppHost\OSCControl.AppHost.csproj --configfile C:\CodexProjects\NuGet.Config -p:RestorePackagesPath=C:\CodexProjects\.nuget\packages -m:1 -nr:false -v:minimal
dotnet build C:\CodexProjects\src\OSCControl.Packager\OSCControl.Packager.csproj --configfile C:\CodexProjects\NuGet.Config -p:RestorePackagesPath=C:\CodexProjects\.nuget\packages -m:1 -nr:false -v:minimal
dotnet build C:\CodexProjects\src\OSCControl.DesktopHost\OSCControl.DesktopHost.csproj --configfile C:\CodexProjects\NuGet.Config -p:RestorePackagesPath=C:\CodexProjects\.nuget\packages -m:1 -nr:false -v:minimal
```

Test checks, when packages are already restored:

```powershell
$env:APPDATA = 'C:\CodexProjects\.appdata'
dotnet test C:\CodexProjects\tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj --no-restore -m:1 -nr:false -v:minimal
```

If the test command fails with `NU1301` for `https://api.nuget.org/v3/index.json`, treat it as a restore/network issue unless a compiler error is also present.

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
