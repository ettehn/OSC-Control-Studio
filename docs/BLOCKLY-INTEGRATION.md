# Blockly Integration Plan

This document records the current decision to use Blockly as the primary Scratch-like visual editor path for OSCControl.

## Current Decision

Use Blockly for the next-generation Blocks editor.

Keep `.osccontrol` as the canonical source format for now. Blockly should generate OSCControl script text first, then reuse the existing compiler, diagnostics, runtime, and app packaging pipeline.

Do not replace the C# runtime or `AppHost` pipeline with a JavaScript runtime.

## Why Blockly

Blockly is the best fit for the current project shape because:

- it is designed for custom block-based programming editors.
- it supports custom blocks and code generation.
- it can generate OSCControl DSL text instead of forcing a new runtime model.
- it can run inside the existing WinForms desktop host through WebView2.
- it is closer to Scratch-style blocks than a node graph editor such as Rete.js.

## Alternatives Considered

### Scratch Blocks

Scratch Blocks is not the preferred integration point. It is useful when the target environment is aligned with Scratch VM semantics, but OSCControl needs custom DSL generation and integration with the existing C# compiler/runtime.

### Rete.js

Rete.js is a good fit for a professional node/flow editor, especially for OSC/WebSocket routing graphs. It is not the best first choice for the requested Scratch-like editor because the UI model is nodes and connections rather than stacked blocks.

Rete.js can remain a future option if OSCControl adds a graph editor mode.

### Node-RED

Node-RED should remain an interoperability or export spike, not the embedded main editor. It brings a separate Node.js runtime/editor stack and a graph-shaped source model.

## Proposed Architecture

The first implementation should be a WebView2-hosted Blockly editor inside the existing `Blocks` tab.

Recommended data path:

1. Blockly workspace JSON lives in the web editor.
2. Blockly generators produce `.osccontrol` script text.
3. WinForms receives script text from WebView2.
4. Existing compiler diagnostics validate the script.
5. Existing runtime and packaging paths run the compiled script.

The initial bridge should avoid making Blockly workspace JSON the only source of truth. Store it as optional editor metadata only after the script generation path is stable.

## Mapping To Existing Model

The first block set should match the current `BlockDocument` feature set:

- endpoint blocks: OSC UDP input/output, WebSocket client/server, VRChat endpoint.
- variable blocks: declare state, read state, store state.
- trigger blocks: startup, receive, VRChat avatar change, VRChat parameter.
- action blocks: log, send, stop, VRChat param, VRChat input, VRChat chatbox, VRChat typing.
- control blocks: if, else, while, break, continue.
- expression blocks: literals, identifiers, comparisons, boolean operators, common helper functions.

Avoid custom functions and full expression round-tripping in the first milestone. Keep advanced expressions as raw text fields when necessary.

## Milestones

### Milestone 1: Static Prototype

- add a local web asset folder for the Blockly editor.
- define a minimal toolbox with startup, log, endpoint, and send blocks.
- generate a valid `.osccontrol` script from a small workspace.
- render the generated script in a preview pane.

### Milestone 2: Desktop Bridge

- add WebView2 to `OSCControl.DesktopHost`.
- host the local Blockly editor in the existing `Blocks` tab.
- pass generated script text from JavaScript to C#.
- wire the existing `Check` and `Apply To Script` behavior to the generated script.

### Milestone 3: Current Blocks Parity

- cover every current `BlockStepKind`.
- support importing the current simple `BlockDocument` cases into a Blockly workspace where practical.
- keep unsupported script constructs visible as raw expression/script blocks rather than silently dropping them.

### Milestone 4: Persistence

- decide whether `.osccontrol` remains the only saved artifact or whether a sidecar workspace file is needed.
- if using a sidecar, prefer `<script>.blocks.json` so generated script remains readable and editable without Blockly.

## Stop Conditions

Stop or re-scope the Blockly integration if any of these become true:

- it requires replacing the OSCControl compiler/runtime.
- generated scripts become less readable than the current generator output.
- WebView2 distribution becomes a larger problem than the editor value.
- workspace JSON becomes mandatory before script generation is reliable.

## References

- https://developers.google.com/blockly
- https://developers.google.com/blockly/guides/create-custom-blocks/overview
- https://developers.google.com/blockly/guides/create-custom-blocks/code-generation/overview
- https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/winforms