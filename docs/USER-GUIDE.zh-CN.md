# OSCControl 用户说明

OSCControl 是一个 Windows 桌面工具，用来把 OSC、WebSocket 和 VRChat OSC 的自动化逻辑写成规则，然后运行或打包成一个可分发的小应用。

当前版本适合做这些事：

- 用 Blocks 页面搭建简单规则，再生成 `.osccontrol` 脚本。
- 直接编辑 `.osccontrol` 脚本。
- 监听 OSC UDP 或 WebSocket Server 输入。
- 发送 OSC UDP 或 WebSocket Client 输出。
- 使用 VRChat 的常用 OSC 功能，例如 Avatar 参数、Input、Chatbox 和 Avatar Change。
- 将当前脚本打包成带 `AppHost` 的应用目录。

当前版本还不是完整 Scratch 风格编辑器。Blocks 是结构化编辑器，不是自由节点画布；复杂表达式和自定义函数仍然建议在脚本里写。

## 启动桌面端

推荐从源码构建输出启动：

```powershell
C:\CodexProjects\src\OSCControl.DesktopHost\bin\Debug\net8.0-windows7.0\OSCControl.DesktopHost.exe
```

也可以使用项目根目录的启动脚本：

```powershell
C:\CodexProjects\OSCControl-DesktopHost.cmd
```

打开后会看到这些主要页签：

- `Script`：直接编辑 `.osccontrol` 脚本。
- `Blocks`：用表格和块列表生成脚本。
- `Diagnostics`：显示编译检查结果。
- `Runtime`：显示运行时日志和错误。

顶部常用按钮：

- `Open`：打开脚本文件。
- `Save`：保存当前脚本。
- `Check`：检查脚本是否能编译。
- `Package App...`：打包当前脚本。
- `Start`：启动运行时。
- `Stop`：停止运行时。

## 最小脚本

```osccontrol
on startup [
    log info "ready"
]
```

使用方式：

1. 打开 `Script` 页。
2. 粘贴上面的脚本。
3. 点击 `Check`，确认没有 diagnostics。
4. 点击 `Start`。
5. 在 `Runtime` 页确认输出 `ready`。

## 使用 Blocks

`Blocks` 页用于生成脚本。它包含几个区域：

- `Endpoints`：配置 OSC / WebSocket / VRChat 端点。
- `Variables`：定义持久变量。
- `Rules`：事件规则列表。
- `Steps`：当前规则里的执行步骤。
- `Generated Script Preview`：生成出来的脚本预览。

推荐流程：

1. 在 `Endpoints` 增加输入或输出端点。
2. 在 `Variables` 增加需要跨事件保存的变量。
3. 在 `Rules` 增加触发规则，例如 `Startup` 或 `Receive`。
4. 在 `Steps` 增加动作，例如 `Log`、`Store`、`Send`、`If`、`While`。
5. 点击 `Preview Script` 查看生成脚本。
6. 点击 `Apply To Script` 把生成脚本写入 `Script` 页。
7. 点击 `Check` 和 `Start` 测试运行。

注意：`Script -> Blocks` 支持部分导入，但不是所有脚本语法都能完整还原成 Blocks。如果导入时遇到不支持的语句，会在 `Runtime` 页显示导入说明。

## 变量

Blocks 里的 `Variables` 会生成脚本级变量声明：

```osccontrol
var count = 0
```

`var` 是面向用户的写法，运行时语义等同于持久状态。也可以写旧形式：

```osccontrol
state count = 0
```

修改变量使用 `store`：

```osccontrol
on startup [
    store count = count + 1
    log info count
]
```

读取变量可以直接写变量名，也可以用函数：

```osccontrol
log info count
log info state("count")
```

## 规则和步骤

启动时执行：

```osccontrol
on startup [
    log info "app started"
]
```

接收 OSC 地址时执行：

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

发送 OSC：

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

函数适合复用一段步骤逻辑。函数参数和函数内部的 `let` 是局部变量，不会污染外层规则。

## VRChat OSC

如果使用 VRChat，最简单方式是写：

```osccontrol
vrchat.endpoint

on startup [
    vrchat.param GestureLeft = 3
    vrchat.input Jump = 1
    vrchat.chat "Hello from OSCControl" send=true notify=false
]
```

常用语法：

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

说明：

- `vrchat.param X = value` 会发送到 `/avatar/parameters/X`。
- `vrchat.input X = value` 会发送到 `/input/X`。
- `vrchat.chat` 会发送到 `/chatbox/input`。
- `vrchat.typing` 会发送到 `/chatbox/typing`。
- `on vrchat.avatar_change` 对应 `/avatar/change`。

## 打包应用

桌面端点击 `Package App...` 会先打开打包设置窗口。常用字段：

- `App name`：生成的应用目录名和 manifest 名称。
- `Output folder`：应用包输出到哪个父目录。
- `Host source`：可选，指向已构建或已发布的 `OSCControl.AppHost` 目录。留空时会只生成 `app/` payload，稍后需要手动补 host 文件。

确认后会生成一个应用目录，典型结构如下：

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

运行方式：

```powershell
run.cmd
```

`AppHost` 会优先加载 `app.plan.json`。如果加载失败，才回退编译 `app.osccontrol`。

## 命令行调试

如果需要看脚本的编译结果，可以使用 `oscctlc`：

```powershell
dotnet run --project C:\CodexProjects\src\oscctlc\oscctlc.csproj -- check C:\path\to\script.osccontrol
dotnet run --project C:\CodexProjects\src\oscctlc\oscctlc.csproj -- tokens C:\path\to\script.osccontrol
dotnet run --project C:\CodexProjects\src\oscctlc\oscctlc.csproj -- ast C:\path\to\script.osccontrol
dotnet run --project C:\CodexProjects\src\oscctlc\oscctlc.csproj -- plan C:\path\to\script.osccontrol
dotnet run --project C:\CodexProjects\src\oscctlc\oscctlc.csproj -- run C:\path\to\script.osccontrol
```

常用命令：

- `check`：检查 diagnostics。
- `tokens`：查看词法 token。
- `ast`：查看语法树。
- `lowered`：查看 lowered IR。
- `execution`：查看 execution IR。
- `plan`：查看 runtime plan。
- `run`：直接运行脚本。

## 常见问题

如果桌面端启动后自己关闭：

- 优先使用 `OSCControl-DesktopHost.cmd` 启动。
- 查看日志文件：

```text
C:\CodexProjects\src\OSCControl.DesktopHost\bin\Debug\net8.0-windows7.0\desktop-host.log
```

如果 `Check` 显示错误：

- 先看 `Diagnostics` 页的行号和列号。
- 确认执行块使用单层 `[` 和 `]`。
- 确认列表使用双层 `[[` 和 `]]`。
- 确认对象使用 `{` 和 `}`。

如果 VRChat 没有响应：

- 确认 VRChat 已启用 OSC。
- 确认端口方向正确。默认 VRChat 通常是输入 `9000`，输出到本机 `9001`。
- 如果脚本里没有显式端点，优先使用 `vrchat.endpoint`。

如果 xUnit 测试跑不起来：

- 当前环境可能无法访问 `https://api.nuget.org/v3/index.json`。
- 这通常是网络或 NuGet 配置问题，不一定代表项目代码构建失败。

## 当前限制

- Blocks 不是完整 Scratch 风格拖拽系统，当前是结构化块编辑器。
- 自定义函数暂时只支持脚本，不支持 Blocks 图形化。
- `Script -> Blocks` 只能导入已支持的语法。
- `ws.server` 出站广播不是第一优先级功能。
- 复杂表达式建议先在文本字段里写。
