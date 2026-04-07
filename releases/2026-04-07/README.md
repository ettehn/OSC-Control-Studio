# Local Build 2026-04-07

This folder contains a locally built Windows framework-dependent release of OSC Control Studio.

## Recommended Download

Use the portable zip:

```text
archives/OSC-Control-Studio-2026-04-07-win-x64-portable.zip
```

Extract it, then run:

```text
OSC-Control-Studio/run.cmd
```

## Contents

- `archives/OSC-Control-Studio-2026-04-07-win-x64-portable.zip`: portable desktop package for direct distribution.
- `build/OSC-Control-Studio/`: uncompressed desktop host package with Blockly/WebView2 enabled.
- `build/OSCControl.AppHost/`: runtime app host publish output.
- `build/OSCControl.Packager/`: packaged-app builder CLI publish output.
- `build/oscctlc/`: compiler/debug CLI publish output.
- `SHA256SUMS.txt`: SHA256 checksums for release files.

## Runtime Requirements

This build is framework-dependent. The target machine needs:

- .NET 8 Desktop Runtime
- Microsoft Edge WebView2 Runtime
