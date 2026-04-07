# Installer

OSC-Control-Studio includes a repository-local PowerShell installer generator for packaged OSCControl apps. It does not require WiX, Inno Setup, or machine-wide install permissions.

## Input Package

Build or export an app package first. The package directory must contain:

```text
AppName/
  app/
  host/
  data/
  logs/
  run.cmd
```

This is the same directory shape produced by the desktop `Package App...` flow and `OSCControl.Packager`.

## Build An Installer

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-OSCControlInstaller.ps1 `
  -PackageRoot C:\Path\To\AppName `
  -OutputPath C:\Path\To\Install-AppName.ps1 `
  -Force
```

The output is a single PowerShell installer script with the packaged app embedded as a compressed payload.

## Runtime Requirement

Packaged apps are framework-dependent by default. During installation, the installer reads `host/*.runtimeconfig.json` from the payload and verifies that the required .NET runtime is installed. For the current `OSCControl.AppHost`, this is `Microsoft.NETCore.App` 8.x.

If the runtime is missing, the installer stops before modifying the target install directory and prints a message that points to the .NET 8 Runtime download page.

## Install

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\Path\To\Install-AppName.ps1
```

By default, the app is installed for the current user under:

```text
%LOCALAPPDATA%\Programs\<AppName>
```

Options:

```powershell
-InstallRoot C:\Custom\Programs
-NoShortcut
-Launch
```

The installer updates `app`, `host`, and `run.cmd` while preserving `data` and `logs`.

## Uninstall

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "%LOCALAPPDATA%\Programs\<AppName>\Uninstall.ps1"
```

Add `-RemoveData` to also delete `data` and `logs`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "%LOCALAPPDATA%\Programs\<AppName>\Uninstall.ps1" -RemoveData
```
