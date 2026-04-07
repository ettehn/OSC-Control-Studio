# VRChat OSC Notes and Syntax Sugar

## Sources Checked

These notes are based on VRChat's official documentation checked on 2026-04-06.

- https://docs.vrchat.com/docs/osc-overview
- https://docs.vrchat.com/docs/osc-avatar-parameters
- https://docs.vrchat.com/docs/osc-as-input-controller
- https://docs.vrchat.com/docs/osc-trackers
- https://docs.vrchat.com/docs/osc-eye-tracking
- https://docs.vrchat.com/docs/oscquery
- https://docs.vrchat.com/docs/launch-options

## What VRChat OSC Looks Like

### Default transport

VRChat officially documents these defaults:

- receive on UDP port `9000`
- send on UDP port `9001`
- launch override: `--osc=inPort:outIP:outPort`
- default launch equivalent: `--osc=9000:127.0.0.1:9001`

### Main address families

#### Avatar parameters

Incoming avatar parameter control follows this shape:

- `/avatar/parameters/<ParameterName>`

VRChat can also emit avatar parameter output through OSC config files stored per avatar.

Important related event:

- `/avatar/change` with the avatar id when the local player loads a new avatar

Per-avatar config path on Windows is documented under LocalLow, for example:

- `C:\Users\<User>\AppData\LocalLow\VRChat\VRChat\OSC\<userId>\Avatars\<avatarId>.json`

The config maps parameter names to input/output addresses and OSC types.

#### Input controller

Inputs follow this address family:

- `/input/<Name>`

VRChat distinguishes:

- axes: usually float `-1..1`
- buttons: usually int `0/1`

Examples from the docs include:

- `/input/Vertical`
- `/input/Horizontal`
- `/input/LookHorizontal`
- `/input/Jump`
- `/input/Voice`

#### Chatbox

VRChat documents these chatbox endpoints:

- `/chatbox/input s b n`
- `/chatbox/typing b`

Meaning:

- first arg `s`: text
- second arg `b`: send immediately or only preload
- third arg `n`: whether to play notification SFX, defaulting to true when omitted

#### Trackers

Tracker input uses these address families:

- `/tracking/trackers/<index>/position`
- `/tracking/trackers/<index>/rotation`
- `/tracking/trackers/head/position`
- `/tracking/trackers/head/rotation`

Notes from the docs:

- values are Vector3-shaped as 3 floats
- positions are world-space
- rotations are Euler angles in degrees
- up to 8 numbered trackers are currently supported
- addresses are used for incoming data to VRChat

#### Eye tracking

Documented eye tracking addresses include:

- `/tracking/eye/EyesClosedAmount`
- `/tracking/eye/CenterPitchYaw`
- `/tracking/eye/CenterPitchYawDist`
- `/tracking/eye/CenterVec`
- `/tracking/eye/CenterVecFull`
- `/tracking/eye/LeftRightPitchYaw`
- `/tracking/eye/LeftRightVec`

Notes from the docs:

- addresses are case-sensitive
- timeout is 10 seconds without new data

#### OSCQuery

VRChat also documents OSCQuery support, implemented since release `2023.3.1`.

## Design Implications for OSCControl

VRChat OSC is unusually regular. A lot of its address space follows predictable templates:

- `/avatar/parameters/<name>`
- `/input/<name>`
- `/tracking/trackers/<slot>/<kind>`
- `/tracking/eye/<name>`

That means OSCControl can add VRChat syntax sugar without hiding too much. We can compress the common paths while still lowering to plain OSC addresses.

## Recommended Syntax Sugar

The safest sugar is path sugar, not protocol magic.

That means:

- it should compile to normal OSC send/receive operations
- it should not create a VRChat-only runtime mode
- it should remain easy to inspect in lowered IR

### 1. VRChat endpoint alias

Instead of writing a raw OSC endpoint every time:

```osccontrol
endpoint vrchat: osc.udp {
    mode: duplex
    host: "127.0.0.1"
    port: 9000
}
```

we could allow:

```osccontrol
vrchat vrchat_local {
    host: "127.0.0.1"
    in: 9000
    out: 9001
}
```

Lowering idea:

- compile into one or two normal OSC endpoints
- mark them with `provider = "vrchat"`

### 2. Avatar parameter sugar

Instead of:

```osccontrol
send vrchat_local {
    address: "/avatar/parameters/GestureLeft"
    args: [[3]]
}
```

allow:

```osccontrol
vrchat.param GestureLeft = 3
```

or inside a block:

```osccontrol
send vrchat_local {
    vrchat.param: {
        GestureLeft: 3,
        GestureRight: 1
    }
}
```

Lowering:

- `GestureLeft` -> `/avatar/parameters/GestureLeft`
- scalar value -> single OSC argument

This is the highest-value sugar because avatar params are the most common VRChat OSC workflow.

### 3. Input sugar

Instead of:

```osccontrol
send vrchat_local {
    address: "/input/Jump"
    args: [[1]]
}
```

allow:

```osccontrol
vrchat.input Jump = 1
vrchat.input Vertical = 0.8
```

Optional pulse helper:

```osccontrol
vrchat.tap Jump
vrchat.tap QuickMenuToggleLeft
```

Lowering:

- `vrchat.input X = v` -> `/input/X` with one OSC arg
- `vrchat.tap X` -> send `1`, then `0`

This is especially useful because VRChat buttons often need edge-style behavior.

### 4. Chatbox sugar

Instead of raw argument tuples:

```osccontrol
send vrchat_local {
    address: "/chatbox/input"
    args: [["Hello", true, false]]
}
```

allow:

```osccontrol
vrchat.chat "Hello"
vrchat.chat "Hello" send=true notify=false
vrchat.typing true
```

Lowering:

- `vrchat.chat "Hello"` -> `/chatbox/input`, args `[["Hello", true, true]]`
- `vrchat.typing true` -> `/chatbox/typing`, args `[[true]]`

This sugar is very worthwhile because the chatbox endpoint is positional and not very readable.

### 5. Tracker sugar

Instead of:

```osccontrol
send vrchat_local {
    address: "/tracking/trackers/1/position"
    args: [[x, y, z]]
}
```

allow:

```osccontrol
vrchat.tracker 1.position = [[x, y, z]]
vrchat.tracker 1.rotation = [[rx, ry, rz]]
vrchat.tracker head.position = [[x, y, z]]
vrchat.tracker head.rotation = [[rx, ry, rz]]
```

Lowering:

- slot and field are substituted directly into the official OSC path

This sugar is good because VRChat tracker paths are verbose but regular.

### 6. Eye tracking sugar

Instead of:

```osccontrol
send vrchat_local {
    address: "/tracking/eye/EyesClosedAmount"
    args: [[0.2]]
}
```

allow:

```osccontrol
vrchat.eye EyesClosedAmount = 0.2
vrchat.eye CenterPitchYaw = [[pitch, yaw]]
vrchat.eye LeftRightVec = [[lx, ly, lz, rx, ry, rz]]
```

This one is useful, but should stay thin. The official names are already meaningful enough.

### 7. Avatar-change event sugar

Instead of:

```osccontrol
on receive vrchat_local when msg.address == "/avatar/change" [
    log info arg(0)
]
```

allow:

```osccontrol
on vrchat.avatar_change [
    let avatarId = arg(0)
    log info avatarId
]
```

This is a clean event-level sugar and probably worth adding.

## What I Would Not Sugar Yet

I would avoid these for `v0.1`:

- full JSON/config-file authoring sugar for avatar config files
- automatic parameter type inference from avatar metadata
- OSCQuery-driven dynamic syntax
- aliases for every single VRChat input name

Reason:

- those either require runtime discovery
- or they make the language feel magical and harder to debug

## Best First VRChat Sugar Set

If we only add a small first batch, I recommend:

1. `vrchat` endpoint alias
2. `vrchat.param <Name> = <value>`
3. `vrchat.input <Name> = <value>`
4. `vrchat.tap <Name>`
5. `vrchat.chat <text> [send=bool] [notify=bool]`
6. `on vrchat.avatar_change [ ... ]`

That gives a lot of practical value with very little compiler complexity.

## Suggested Lowering Examples

### Example A

Source sugar:

```osccontrol
vrchat.param GestureLeft = 3
```

Lowered form:

```osccontrol
send vrchat_local {
    address: "/avatar/parameters/GestureLeft"
    args: [[3]]
}
```

### Example B

Source sugar:

```osccontrol
vrchat.tap Jump
```

Lowered form:

```osccontrol
send vrchat_local {
    address: "/input/Jump"
    args: [[1]]
}

wait 50

send vrchat_local {
    address: "/input/Jump"
    args: [[0]]
}
```

### Example C

Source sugar:

```osccontrol
vrchat.chat "Now playing"
```

Lowered form:

```osccontrol
send vrchat_local {
    address: "/chatbox/input"
    args: [["Now playing", true, true]]
}
```

## Compiler Impact

These sugars fit best as a desugaring pass between parse and validation/lowering.

Recommended stage order:

1. tokenize
2. parse
3. desugar VRChat constructs into core OSCControl syntax
4. validate
5. lower

That keeps the runtime provider-neutral while still making authoring much nicer.

## Recommendation

Yes, VRChat is a very good target for syntax sugar.

The strongest candidates are:

- avatar parameters
- input buttons and axes
- chatbox
- avatar-change event

Those are stable, common, and structurally regular in the official docs.
