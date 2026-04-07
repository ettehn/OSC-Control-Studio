# OSC-Control-Studio 用户说明

OSC-Control-Studio 是一个 Windows 桌面工具，用来把 OSC、WebSocket 和 VRChat OSC 的自动化逻辑写成规则，然后直接运行或打包成一个可分发的应用目录。

当前版本适合做这些事：

- 用 Blockly / Blocks 可视化编辑器搭建常见自动化规则。
- 直接编辑 `.osccontrol` 脚本。
- 监听 OSC UDP 或 WebSocket 输入。
- 发送 OSC UDP 或 WebSocket 输出。
- 使用 VRChat 常用 OSC 功能，例如 Avatar 参数、Input、Chatbox、Typing 和 Avatar Change。
- 将当前脚本打包成带 `AppHost` 的应用目录。

可视化编辑器不是通用节点图平台；`.osccontrol` 仍然是自动化逻辑的规范源。

## 启动桌面端

从仓库根目录运行：

```powershell
.\OSCControl-DesktopHost.cmd
```

或者在构建后运行：

```powershell
.\src\OSCControl.DesktopHost\bin\Debug\net8.0-windows7.0\OSCControl.DesktopHost.exe
```

主要页签：

- `Script`：直接编辑 `.osccontrol` 脚本。
- `Blocks`：用结构化/Blockly 可视化编辑器生成脚本。
- `Diagnostics`：查看编译检查结果。
- `Runtime`：查看运行时日志和宿主错误。

常用按钮：

- `Open`：打开脚本文件。
- `Save`：保存当前脚本。
- `Check`：检查脚本能否编译。
- `Package App...`：导出打包应用目录。
- `Start`：启动运行时。
- `Stop`：停止运行时。

## 最小脚本

```osccontrol
on startup [
    log info "ready"
]
```

基本流程：

1. 打开 `Script` 页。
2. 粘贴上面的脚本。
3. 点击 `Check`，确认没有 diagnostics。
4. 点击 `Start`。
5. 在 `Runtime` 页确认输出 `ready`。

## Blockly / Blocks 工作流

可视化编辑器面向常见自动化规则，不覆盖所有高级语言特性。

推荐流程：

1. 添加 OSC UDP、WebSocket 或 VRChat 输入/输出端点。
2. 添加需要跨事件保存的变量。
3. 添加触发规则，例如 startup、receive、VRChat avatar change 或 VRChat parameter change。
4. 添加执行步骤，例如 log、store、send、if、while、break、continue 和 VRChat 动作。
5. 预览生成的脚本。
6. 应用或保存生成的脚本。
7. 使用 `Check` 和 `Start` 测试运行。

复杂表达式和自定义函数仍然可以直接在 `Script` 页里写。

## 变量

使用 `var` 定义面向用户的持久变量：

```osccontrol
var count = 0
```

使用 `store` 修改变量：

```osccontrol
on startup [
    store count = count + 1
    log info count
]
```

读取变量可以直接写变量名，也可以使用 `state()`：

```osccontrol
log info count
log info state("count")
```

## 规则和步骤

接收 OSC 输入：

```osccontrol
endpoint oscIn: osc.udp {
    mode: input
    host: "127.0.0.1"
    port: 9000
    codec: osc
}

on receive oscIn when msg.address == "/note/on" [
    log info arg(0)
]
```

发送 OSC 输出：

```osccontrol
endpoint oscOut: osc.udp {
    mode: output
    host: "127.0.0.1"
    port: 9001
    codec: osc
}

on startup [
    send oscOut {
        address: "/hello"
        args: [[1, 2, 3]]
    }
]
```

条件判断：

```osccontrol
on startup [
    if count > 0 [
        log info "positive"
    ]
    else [
        log info "zero or negative"
    ]
]
```

循环：

```osccontrol
var count = 0

on startup [
    while count < 3 [
        log info count
        store count = count + 1
    ]
]
```

## 自定义函数

自定义函数目前只支持脚本，不做图形化。

```osccontrol
func greet(name) [
    log info concat("hello ", name)
]

on startup [
    call greet("VRChat")
]
```

函数参数和函数内部的 `let` 不会污染外层规则作用域。

## VRChat OSC

最简单的 VRChat 设置：

```osccontrol
vrchat.endpoint

on startup [
    vrchat.param GestureLeft = 3
    vrchat.input Jump = 1
    vrchat.chat "Hello from OSCControl" send=true notify=false
]
```

常用快捷语法：

```osccontrol
vrchat.param GestureLeft = 3
vrchat.input Jump = 1
vrchat.chat "Hello" send=true notify=false
vrchat.typing true
```

触发器：

```osccontrol
on vrchat.avatar_change [
    log info "avatar changed"
]

on vrchat.param GestureLeft [
    log info arg(0)
]
```

映射关系：

- `vrchat.param X = value` 发送到 `/avatar/parameters/X`。
- `vrchat.input X = value` 发送到 `/input/X`。
- `vrchat.chat` 发送到 `/chatbox/input`。
- `vrchat.typing` 发送到 `/chatbox/typing`。
- `on vrchat.avatar_change` 监听 `/avatar/change`。

## 打包应用

在桌面端点击 `Package App...`。打包设置窗口包含：

- `App name`：生成的应用目录名和 manifest 名称。
- `Output folder`：生成应用包的父目录。
- `Host source`：可选，指向已构建或已发布的 `OSCControl.AppHost` 目录。留空时只生成 `app/` payload，之后需要手动补 host 文件。

典型输出结构：

```text
SampleApp/
  app/
    app.manifest.json
    app.osccontrol
    app.plan.json
  host/
    OSCControl.AppHost.exe
    ...
  data/
  logs/
  run.cmd
```

运行打包应用：

```powershell
.\run.cmd
```

`AppHost` 会优先加载 `app.plan.json`。只有 plan 加载失败时，才回退编译 `app.osccontrol`。

## 命令行调试

```powershell
dotnet run --project .\src\oscctlc\oscctlc.csproj -- check C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- tokens C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- ast C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- plan C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- run C:\path\to\script.osccontrol
```

命令说明：

- `check`：查看 diagnostics。
- `tokens`：查看词法 token。
- `ast`：查看语法树。
- `lowered`：查看 lowered IR。
- `execution`：查看 execution IR。
- `plan`：查看 runtime plan。
- `run`：从命令行运行脚本。

## 当前限制

- 可视化编辑器还不覆盖所有 `.osccontrol` 语言特性。
- 自定义函数暂时只支持脚本，不支持图形化。
- 复杂表达式通常仍然更适合直接写文本。
- 在受限 sandbox 环境里，WebSocket listener 测试可能因为 `HttpListener` 无法启动而被跳过。
