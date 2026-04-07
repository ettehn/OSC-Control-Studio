# Portable Release / 便携发布版

OSC-Control-Studio is distributed as a portable zip for Windows.

OSC-Control-Studio 目前以 Windows 便携压缩包的形式分发。

## Recommended Artifact / 推荐文件

Use this portable zip:

使用这个便携压缩包：

```text
releases/2026-04-07/archives/OSC-Control-Studio-2026-04-07-win-x64-portable.zip
```

Extract the zip, then run:

解压后运行：

```powershell
.\OSC-Control-Studio\run.cmd
```

## Directory Shape / 目录结构

The portable app directory contains:

便携应用目录包含：

```text
OSC-Control-Studio/
  app/
  data/
  host/
  logs/
  run.cmd
```

`host/` contains the desktop executable and runtime files. `data/` and `logs/` are reserved for local app state.

`host/` 包含桌面端可执行文件和运行时文件。`data/` 与 `logs/` 用于保留本地应用状态。

## Runtime Requirements / 运行环境要求

The current release is framework-dependent. The target machine needs:

当前发布版依赖目标机器上的运行环境，需要：

- .NET 8 Desktop Runtime
- Microsoft Edge WebView2 Runtime

## Checksums / 校验值

SHA256 checksums are listed here:

SHA256 校验值在这里：

```text
releases/2026-04-07/SHA256SUMS.txt
```

## Notes / 说明

The old PowerShell installer generator remains in `tools/` for internal experiments, but it is not the recommended distribution path. Prefer portable zip releases unless there is a concrete reason to add an installer later.

旧的 PowerShell 安装器生成器仍保留在 `tools/` 中用于内部实验，但不再是推荐分发方式。除非后续有明确理由重新引入安装器，否则优先使用便携压缩包发布。
