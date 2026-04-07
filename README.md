# OSCControl

OSCControl 是一个面向 Windows 的 OSC / WebSocket / VRChat OSC 自动化工具链，包含脚本语言、编译器、运行时、桌面宿主、Blocks 可视化编辑器和应用打包器。

## 用户入口

- [中文用户说明](docs/USER-GUIDE.zh-CN.md)
- [桌面端启动脚本](OSCControl-DesktopHost.cmd)

## 开发文档

- [语言规范](OSCControl-language-spec.md)
- [解析器设计](OSCControl-parser-design.md)
- [VRChat OSC 语法糖](VRChat-OSC-sugar.md)
- [Blockly 集成方案](docs/BLOCKLY-INTEGRATION.md)
- [验证说明](docs/VERIFICATION.md)
- [NuGet restore 问题记录](docs/NUGET-RESTORE-ISSUE.md)

## 构建入口

- [解决方案](OSCControl.sln)
- [验证脚本](Verify.ps1)

常规验证命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\CodexProjects\Verify.ps1
```
