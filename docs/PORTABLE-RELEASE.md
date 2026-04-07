# Portable Release

OSC-Control-Studio is distributed as a portable zip for Windows.

## Build Output Shape

The portable app directory contains:

```text
OSC-Control-Studio/
  app/
  data/
  host/
  logs/
  run.cmd
```

Run `run.cmd` to start the desktop host.

## Current Release Artifact

The local release artifact is:

```text
releases/2026-04-07/archives/OSC-Control-Studio-2026-04-07-win-x64-portable.zip
```

Extract the zip and run:

```powershell
.\OSC-Control-Studio\run.cmd
```

## Runtime Requirements

The current release is framework-dependent. The target machine needs:

- .NET 8 Desktop Runtime
- Microsoft Edge WebView2 Runtime

## Notes

The old PowerShell installer generator remains in `tools/` for internal experiments, but it is not the recommended distribution path. Prefer portable zip releases unless there is a concrete reason to add an installer later.
