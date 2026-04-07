# Node-RED Spike Plan

This branch is for evaluating whether OSCControl should interoperate with Node-RED without moving the mainline architecture away from `.osccontrol` as the canonical source.

## Current Decision

Do not merge Node-RED directly into mainline yet.

Recommended approach for the spike:

- keep OSCControl mainline in C# with `.osccontrol -> RuntimePlan -> AppHost` as the primary pipeline.
- do not fork Node-RED source code unless a concrete patch to Node-RED itself becomes necessary.
- evaluate a small adapter/export path first: Node-RED flow JSON -> OSCControl script, or OSCControl package -> Node-RED custom nodes.
- avoid making Node-RED flow JSON the canonical source of this project.

## Why

Node-RED is strong for general event-driven flow editing, but it brings a Node.js runtime/editor stack and a graph-shaped source model. OSCControl currently uses a narrow rule/step model designed for OSC, WebSocket, VRChat OSC, packaged app hosting, and a Windows desktop workflow.

Replacing the current Blocks editor with Node-RED now would add integration cost and create a second truth source before the app packaging path is stable.

## Official Capabilities Checked

- Node-RED can be embedded into an existing Node.js/Express app.
- Node-RED can be extended by adding custom nodes to its palette.
- Node-RED exposes runtime/editor/module APIs.

References:

- https://nodered.org/docs/user-guide/runtime/embedding
- https://nodered.org/docs/creating-nodes/
- https://nodered.org/docs/api/

## Spike Options

### Option A: Export To Node-RED Flow

Generate a Node-RED flow from a subset of `.osccontrol` or Blocks.

Use when:

- the user wants to inspect or extend automations in Node-RED.
- we can tolerate one-way export.

Risk:

- Node-RED flow JSON may not map cleanly back to OSCControl semantics.

### Option B: Node-RED Custom Nodes For OSCControl

Create custom Node-RED nodes such as:

- `osccontrol-vrchat-param`
- `osccontrol-vrchat-input`
- `osccontrol-chatbox`
- `osccontrol-run-plan`

Use when:

- Node-RED is treated as an optional external editor/runtime.

Risk:

- requires npm packaging and a Node.js development/distribution path.

### Option C: Embedded Node-RED Editor In A Separate Host

Build a separate experimental host that embeds Node-RED and bridges to OSCControl runtime over a local API.

Use when:

- we need to test Node-RED editor ergonomics with real OSCControl runtime behavior.

Risk:

- highest complexity; introduces web server, editor auth, storage, runtime bridging, and packaging concerns.

## Recommendation

Start with Option A or B, not C.

The first concrete milestone should be a read-only design spike:

1. define a minimal mapping between `BlockDocument` and Node-RED flow nodes.
2. decide whether `vrchat.param`, `vrchat.input`, and `vrchat.chat` become Node-RED custom nodes or exported generic OSC nodes.
3. write one small sample flow by hand.
4. only then decide whether to implement a generator.

## Stop Conditions

Stop the spike if any of these become true:

- Node-RED flow JSON would need to become canonical source.
- the spike requires replacing AppHost packaging.
- the UI work turns into a generic graph editor project.
- Node.js distribution becomes a larger task than the OSCControl app packaging workflow itself.