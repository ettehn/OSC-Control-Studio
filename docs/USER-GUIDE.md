# OSC-Control-Studio User Guide

OSC-Control-Studio is a Windows desktop tool for building automation rules around OSC, WebSocket, and VRChat OSC. It can run scripts directly during development or export them as packaged app folders with a runtime host.

Current use cases:

- Build automation with the Blockly visual editor.
- Edit `.osccontrol` scripts directly.
- Receive OSC UDP and WebSocket input.
- Send OSC UDP and WebSocket output.
- Use common VRChat OSC actions such as avatar parameters, inputs, chatbox, typing, and avatar-change triggers.
- Package the current script into an app folder with `AppHost`.

The visual editor is not a general node-graph platform. `.osccontrol` remains the canonical automation source.

## Start The Desktop Host

From the repository root, use:

```powershell
.\OSCControl-DesktopHost.cmd
```

Or run the built executable after building the solution:

```powershell
.\src\OSCControl.DesktopHost\bin\Debug\net8.0-windows7.0\OSCControl.DesktopHost.exe
```

Main tabs:

- `Script`: edit `.osccontrol` text directly.
- `Blocks`: use the structured/Blockly visual editor to generate script text.
- `Diagnostics`: view compiler diagnostics.
- `Runtime`: view runtime logs and host errors.

Main buttons:

- `Open`: open a script file.
- `Save`: save the current script.
- `Check`: compile-check the current script.
- `Package App...`: export a packaged app folder.
- `Start`: start the runtime host.
- `Stop`: stop the runtime host.

## Minimal Script

```osccontrol
on startup [
    log info "ready"
]
```

Basic flow:

1. Open the `Script` tab.
2. Paste the script above.
3. Click `Check` and confirm there are no diagnostics.
4. Click `Start`.
5. Check the `Runtime` tab for `ready`.

## Blockly / Blocks Workflow

The visual editor is designed for common automation rules, not every advanced language feature.

Recommended workflow:

1. Add input/output endpoints such as OSC UDP, WebSocket, or VRChat.
2. Add variables if state needs to persist between events.
3. Add event rules such as startup, receive, VRChat avatar change, or VRChat parameter change.
4. Add steps such as log, store, send, if, while, break, continue, and VRChat actions.
5. Preview the generated script.
6. Apply or save the generated script.
7. Run `Check` and `Start` to test.

Advanced expressions and custom functions can still be written directly in the `Script` tab.

## Variables

Use `var` for user-facing persistent variables:

```osccontrol
var count = 0
```

Update a variable with `store`:

```osccontrol
on startup [
    store count = count + 1
    log info count
]
```

Read a variable directly or with `state()`:

```osccontrol
log info count
log info state("count")
```

## Rules And Steps

Receive OSC input:

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

Send OSC output:

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

Use conditional logic:

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

Use loops:

```osccontrol
var count = 0

on startup [
    while count < 3 [
        log info count
        store count = count + 1
    ]
]
```

## Custom Functions

Custom functions are currently script-only and are not exposed as visual blocks yet.

```osccontrol
func greet(name) [
    log info concat("hello ", name)
]

on startup [
    call greet("VRChat")
]
```

Function parameters and local `let` values do not pollute the outer rule scope.

## VRChat OSC

The simplest VRChat setup is:

```osccontrol
vrchat.endpoint

on startup [
    vrchat.param GestureLeft = 3
    vrchat.input Jump = 1
    vrchat.chat "Hello from OSCControl" send=true notify=false
]
```

Common shortcuts:

```osccontrol
vrchat.param GestureLeft = 3
vrchat.input Jump = 1
vrchat.chat "Hello" send=true notify=false
vrchat.typing true
```

Triggers:

```osccontrol
on vrchat.avatar_change [
    log info "avatar changed"
]

on vrchat.param GestureLeft [
    log info arg(0)
]
```

Mapping:

- `vrchat.param X = value` sends to `/avatar/parameters/X`.
- `vrchat.input X = value` sends to `/input/X`.
- `vrchat.chat` sends to `/chatbox/input`.
- `vrchat.typing` sends to `/chatbox/typing`.
- `on vrchat.avatar_change` listens to `/avatar/change`.

## Package An App

Click `Package App...` in the desktop host. The settings dialog includes:

- `App name`: generated app directory and manifest name.
- `Output folder`: parent directory for the generated package.
- `Host source`: optional folder containing built or published `OSCControl.AppHost` files. If left empty, the package contains only the app payload and host files must be copied later.

Typical output:

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

Run the packaged app with:

```powershell
.\run.cmd
```

`AppHost` loads `app.plan.json` first and falls back to compiling `app.osccontrol` only if the plan cannot be loaded.

## CLI Debugging

```powershell
dotnet run --project .\src\oscctlc\oscctlc.csproj -- check C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- tokens C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- ast C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- plan C:\path\to\script.osccontrol
dotnet run --project .\src\oscctlc\oscctlc.csproj -- run C:\path\to\script.osccontrol
```

Commands:

- `check`: diagnostics.
- `tokens`: lexer output.
- `ast`: syntax tree.
- `lowered`: lowered IR.
- `execution`: execution IR.
- `plan`: runtime plan.
- `run`: run a script from the command line.

## Current Limitations

- The visual editor does not cover every `.osccontrol` language feature.
- Custom functions are script-only for now.
- Complex expressions are often easier to write as text.
- WebSocket listener tests may be skipped in constrained sandbox environments where `HttpListener` cannot start.
