# OSC-Control-Studio

Windows OSC / WebSocket / VRChat OSC automation toolkit with a custom DSL, Blockly visual editor, runtime host, and packaged app exporter.

OSC-Control-Studio 是一个面向 Windows 的 OSC / WebSocket / VRChat OSC 自动化工具链，包含自定义脚本语言、Blockly 可视化编辑器、运行时宿主和应用打包导出能力。

## Project Status / 项目状态

This project is still under construction and may not run correctly in every environment.

本项目仍在建设中，可能无法在所有环境中正常运行。

## Release / 发布版

The current distribution format is a portable zip. Download or use this file:

当前推荐的分发形式是便携压缩包。下载或使用这个文件：

```text
releases/2026-04-07/archives/OSC-Control-Studio-2026-04-07-win-x64-portable.zip
```

After extracting it, run:

解压后运行：

```powershell
.\OSC-Control-Studio\run.cmd
```

Runtime requirements:

运行环境要求：

- .NET 8 Desktop Runtime
- Microsoft Edge WebView2 Runtime

More details:

更多说明：

- [Portable Release](docs/PORTABLE-RELEASE.md)
- [SHA256 checksums](releases/2026-04-07/SHA256SUMS.txt)

## User Guides / 用户说明

- [English User Guide](docs/USER-GUIDE.md)
- [中文用户说明](docs/USER-GUIDE.zh-CN.md)
- [Desktop launch script](OSCControl-DesktopHost.cmd)

## Developer Documentation / 开发文档

- [Language Spec](OSCControl-language-spec.md)
- [Parser Design](OSCControl-parser-design.md)
- [Blockly Integration](docs/BLOCKLY-INTEGRATION.md)
- [VRChat OSC Sugar](VRChat-OSC-sugar.md)
- [WebSocket Runtime](docs/WEBSOCKET-RUNTIME.md)
- [Verification](docs/VERIFICATION.md)

## Build And Verification / 构建与验证

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Verify.ps1
```

For IDE usage, open:

如果使用 IDE，请打开：

```text
OSCControl.sln
```

## DG-LAB Integration Notice / DG-LAB 集成声明

This project may include experimental DG-LAB Socket or BLE integration for user-controlled automation scenarios. The integration is not an official DG-LAB product, is not affiliated with or endorsed by DG-LAB, and is based on the public DG-LAB open-source protocol materials:

- [DG-LAB-OPENSOURCE](https://github.com/DG-LAB-OPENSOURCE/DG-LAB-OPENSOURCE)
- [SOCKET control protocol v2](https://github.com/DG-LAB-OPENSOURCE/DG-LAB-OPENSOURCE/blob/main/socket/v2/README.md)
- [Coyote V3 Bluetooth protocol](https://github.com/DG-LAB-OPENSOURCE/DG-LAB-OPENSOURCE/blob/main/coyote/v3/README_V3.md)

DG-LAB's public materials state that unauthorized commercial use is not allowed. This project treats DG-LAB support as a non-commercial, open-source, user-operated integration. If you plan to use this integration in a commercial product, paid service, packaged distribution, or hosted platform, obtain authorization from DG-LAB first.

Users must explicitly connect, pair, scan, or select their own devices before using DG-LAB-related automation. Do not use this project for silent remote control, coercive control, or operation without the device user's consent. Scripts should keep strength, pulse, clear, stop, and disconnect behavior visible and controllable by the user.

本项目可能包含实验性的 DG-LAB Socket 或 BLE 集成，用于用户主动控制的自动化场景。该集成不是 DG-LAB 官方产品，也不代表 DG-LAB 官方授权或背书；相关实现参考 DG-LAB 公开的开源协议资料。

DG-LAB 公开资料中说明未经授权不得用于商业用途。本项目将 DG-LAB 支持定位为非商业、开源、用户主动操作的集成能力。如果你计划将该能力用于商业产品、付费服务、打包分发或托管平台，请先取得 DG-LAB 授权。

用户在使用 DG-LAB 相关自动化前，必须主动连接、配对、扫码或选择自己的设备。请勿将本项目用于静默远程控制、强迫控制或未经设备使用者同意的操作。脚本应让强度、波形、清空、停止和断开行为保持可见且可由用户控制。

## Risk and Disclaimer / 风险与免责声明

This project is intended for automation workflows involving OSC, WebSocket, VRChat OSC, and related control scenarios. Users are responsible for verifying the safety of their scripts, network endpoints, devices, and third-party software configurations.

The project maintainer is not responsible for any issues caused by the use, misuse, modification, or distribution of this project, including but not limited to device malfunction, data loss, software configuration damage, account or third-party platform risks, network connection problems, or other indirect losses.

Before running automation rules, make sure you understand the script behavior and how the target device or software will respond. It is recommended to test in a safe environment before using this project with real devices or live environments.

本项目用于 OSC、WebSocket、VRChat OSC 等自动化控制场景。使用者应自行确认脚本、网络端点、设备和第三方软件配置的安全性。

因使用、误用、修改或分发本项目所造成的任何问题，包括但不限于设备异常、数据丢失、软件配置损坏、账号或第三方平台风险、网络连接问题或其他间接损失，项目维护者不承担责任。

请在理解脚本行为和目标设备/软件响应方式后再运行自动化规则。建议先在测试环境中验证，再用于真实设备或线上环境。

## AI-Assisted Development Notice / AI 使用声明

This repository includes code, documentation, and design notes created with assistance from AI coding tools. All changes are reviewed and accepted by the project maintainer before being included in the repository. AI-generated output is provided without additional warranty.

本项目在设计、文档和部分代码实现过程中使用了 AI 编程助手辅助开发。所有提交内容仍由项目维护者审查、运行和接受；AI 生成内容不代表项目具备任何额外保证。
