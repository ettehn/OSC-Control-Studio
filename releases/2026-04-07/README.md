# Local Build 2026-04-07

This folder contains a locally built Windows framework-dependent release of OSC Control Studio.

## Contents

- `build/OSC-Control-Studio/`: desktop host package with Blockly/WebView2 enabled.
- `build/OSCControl.AppHost/`: runtime app host publish output.
- `build/OSCControl.Packager/`: packaged-app builder CLI publish output.
- `build/oscctlc/`: compiler/debug CLI publish output.
- `installers/Install-OSC-Control-Studio.ps1`: PowerShell installer for the desktop host package.
- `SHA256SUMS.txt`: SHA256 checksums for release files.

## Install

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\installers\Install-OSC-Control-Studio.ps1
```

The installer requires the .NET 8 Desktop Runtime and WebView2 Runtime on the target machine.
