# OSC-Control-Studio

Windows OSC / WebSocket / VRChat OSC automation toolkit with a custom DSL, Blockly visual editor, runtime host, and packaged app exporter.

OSC-Control-Studio 是一个面向 Windows 的 OSC / WebSocket / VRChat OSC 自动化工具链，包含自定义脚本语言、编译器、运行时、桌面宿主、Blockly 可视化编辑器和应用打包器。

## this project is still under construction
## 项目还在建设中，可能无法正常运行

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
- [Portable Release](docs/PORTABLE-RELEASE.md)
- [Verification](docs/VERIFICATION.md)

## Build And Verification

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Verify.ps1
```

For IDE usage, open:

```text
OSCControl.sln
```

## Risk and Disclaimer

This project is intended for automation workflows involving OSC, WebSocket, VRChat OSC, and related control scenarios. Users are responsible for verifying the safety of their scripts, network endpoints, devices, and third-party software configurations.

The project maintainer is not responsible for any issues caused by the use, misuse, modification, or distribution of this project, including but not limited to device malfunction, data loss, software configuration damage, account or third-party platform risks, network connection problems, or other indirect losses.

Before running automation rules, make sure you understand the script behavior and how the target device or software will respond. It is recommended to test in a safe environment before using this project with real devices or live environments.

## 风险与免责声明

本项目用于 OSC、WebSocket、VRChat OSC 等自动化控制场景。使用者应自行确认脚本、网络端点、设备和第三方软件配置的安全性。

因使用、误用、修改或分发本项目所造成的任何问题，包括但不限于设备异常、数据丢失、软件配置损坏、账号或第三方平台风险、网络连接问题或其他间接损失，项目维护者不承担责任。

请在理解脚本行为和目标设备/软件响应方式后再运行自动化规则。建议先在测试环境中验证，再用于真实设备或线上环境。

## AI-Assisted Development Notice / AI 使用声明

This repository includes code, documentation, and design notes created with assistance from AI coding tools. All changes are reviewed and accepted by the project maintainer before being included in the repository. AI-generated output is provided without additional warranty.

本项目在设计、文档和部分代码实现过程中使用了 AI 编程助手辅助开发。所有提交内容仍由项目维护者审查、运行和接受；AI 生成内容不代表项目具备任何额外保证。
