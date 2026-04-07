# OSCControl Internal Advice

## Scope

This note packages the conclusions from the current thread about the desktop UI, the visual model, the runtime packaging direction, and whether the project is "reinventing the wheel."

## Current Product Shape

OSCControl currently behaves like a narrow domain toolchain for:

- OSC UDP messaging
- WebSocket messaging
- VRChat OSC automation
- rule-based runtime execution
- partial visual editing through Blocks

The current stack is effectively:

- visual editor model: `BlockDocument`
- textual canonical source: `.osccontrol`
- compiler pipeline: parse -> lower -> execution plan
- runtime host: load plan and execute endpoints/rules/steps

Relevant code:

- `C:\CodexProjects\src\OSCControl.DesktopHost\BlockDocument.cs`
- `C:\CodexProjects\src\OSCControl.DesktopHost\OSCControlScriptGenerator.cs`
- `C:\CodexProjects\src\OSCControl.DesktopHost\BlockDocumentImporter.cs`
- `C:\CodexProjects\src\OSCControl.Compiler\Compiler\CompilerPipeline.cs`
- `C:\CodexProjects\src\OSCControl.Compiler\Runtime\RuntimeEngine.cs`
- `C:\CodexProjects\src\OSCControl.Compiler\Runtime\RuntimeHost.cs`

## How The Desktop UI Should Graphicalize Scripts

Do not turn the desktop UI into a generic free-form node editor.

The current data model is not a graph IR. It is a structured rule document:

- `Endpoints`
- `Rules`
- ordered `Steps` inside each rule

That means the correct visual form is:

- endpoint/resource area
- rule lanes
- ordered step chain inside each rule
- property inspector for the selected item
- generated script preview

Recommended mapping:

- `endpoint` / `vrchat.endpoint` -> endpoint cards or a top resource strip
- `on ... when ... [ ... ]` -> a rule lane
- each statement in a rule body -> a step block in vertical order

Do not force all expressions into visual nodes yet. Keep these as text fields:

- `WhenExpression`
- complex values and payload expressions

Reason:

- the importer only reliably structuralizes a subset of syntax
- unsupported syntax is currently downgraded or skipped during import
- the project already documents partial round-tripping

This means the right product shape is:

- structure where structure is stable
- text where syntax is still open-ended

## Reinventing The Wheel

Yes, there is real "reinventing the wheel" risk, but it is mixed.

Reasonable custom work:

- the OSC/VRChat/WebSocket domain DSL
- the canonical script format
- the domain-specific compiler/lowering model
- the visual editor model for this exact niche

Highest risk of unnecessary reinvention:

- building a general-purpose graph editor framework from scratch
- building a second parallel application-generation platform on top of this
- duplicating capabilities already present in a higher-level generation system

Practical rule:

- if OSCControl is the domain layer inside a larger software generation system, it is justified
- if OSCControl grows into a separate universal app-builder platform, it is overexpansion

## Best Role Inside A Software Generation System

If this is meant to plug into a broader "generate software from user intent" system, OSCControl should be positioned as a domain submodule, not the entire generation platform.

Recommended role:

- upper system defines the application shell, data, UI, packaging, and publishing
- OSCControl defines event-driven automation logic for OSC/VRChat/WebSocket behavior
- OSCControl script remains the canonical source for that automation slice

Bad role:

- using OSCControl Blocks as the universal app model for everything

Good role:

- using OSCControl as the automation/transport logic module embedded into generated apps

## Recommended Pipeline

The most defensible pipeline discussed in this thread is:

`Visual Model -> OSCControl Script -> Runtime Plan -> Packaged App`

This is better than:

`Visual Model -> Script -> Generate New Source Code -> Client Compiles Whole EXE`

Reason:

- you already have runtime and compiler infrastructure
- packaging a fixed host is simpler than generating and compiling fresh application code every time
- upgrade and support become much easier

## Packaging Direction

Preferred delivery model:

- fixed host executable
- runtime libraries included with the installed app
- generated `app.osccontrol`
- generated `app.plan.json`
- manifest/config/assets

In other words, ship a stable host plus generated payload.

Suggested installed layout:

```text
MyGeneratedApp/
  host/
    AppHost.exe
    OSCControl.Compiler.dll
    ...
  app/
    app.manifest.json
    app.plan.json
    app.osccontrol
    assets/
  data/
  logs/
```

Suggested startup:

1. `AppHost.exe` reads `app.manifest.json`
2. host loads `app.plan.json`
3. host creates runtime engine and runtime host
4. host starts endpoints/rules
5. logs and state go into app-local folders

Fallback is possible:

- if no plan exists, compile `app.osccontrol` at startup

But for production builds, precompiled plan is preferred.

## Recommended Project Split

The clean architecture for the next stage is:

- `OSCControl.DesktopHost`
  - editor
  - blocks UI
  - script editing
  - preview/import/export
- `OSCControl.AppHost`
  - end-user runtime host
  - loads packaged app payload
- `OSCControl.Packager`
  - bundles host + plan + script + manifest + assets
  - optionally emits installer

This is cleaner than making the current editor also be the final installed runtime.

## Product Constraints To Preserve

To avoid long-term drift, preserve these rules:

- script is the single canonical source
- visual model must compile down to script, not become a separate truth source
- unsupported imported syntax must be surfaced as notes, not silently mangled
- the visual editor should target the subset that round-trips well
- runtime packaging should prefer stable host reuse over full per-app recompilation

## Short Bottom Line

The strongest path forward is:

- keep Blocks as a structured editor for endpoints/rules/steps
- keep `.osccontrol` as canonical source
- compile to a runtime plan
- ship a fixed host with bundled runtime and generated payload
- integrate OSCControl into a larger generator as a domain logic module, not as a second app platform

## Current Structure Review After AppHost And Packager Split

The repository has already moved toward the recommended shape.

Current project split:

- `C:\CodexProjects\src\OSCControl.Compiler`
  - compiler and runtime core
- `C:\CodexProjects\src\OSCControl.DesktopHost`
  - editor, script view, runtime preview, and Blocks UI
- `C:\CodexProjects\src\OSCControl.AppHost`
  - end-user runtime host for packaged apps
- `C:\CodexProjects\src\OSCControl.Packager`
  - packages script, plan, manifest, and optional host files
- `C:\CodexProjects\src\oscctlc`
  - development CLI for diagnostics and runtime inspection

The current pipeline is now close to:

```text
Blocks / Script
  -> CompilerPipeline
  -> RuntimePlanJsonCodec
  -> Packager
  -> app/ + host/ + data/ + logs/
  -> AppHost
```

What is already good:

- `OSCControl.AppHost` exists and is separate from the editor.
- `OSCControl.Packager` exists and produces the intended `app`, `host`, `data`, and `logs` layout.
- packaged artifacts under `C:\CodexProjects\artifacts\packaged-plan-codec` show the target shape working at a file-layout level.
- `AppHost` loads `app.plan.json` first and falls back to compiling `app.osccontrol` if the plan is missing or invalid.
- `Packager` compiles the script before packaging and emits `app.plan.json`, so the production path can avoid startup-time compilation.
- `AppHost` and `Packager` currently build successfully with `dotnet build --no-restore`.

Issues and risks:

- `OSCControl.DesktopHost` does not appear to have a packaging UI entry point yet. The editor can build and run scripts, but it is not yet the user-facing "generate packaged app" workflow.
- `run.cmd` currently calls `dotnet host\OSCControl.AppHost.dll app`. This is acceptable for framework-dependent distribution, but a generated end-user app should prefer `host\OSCControl.AppHost.exe app` when the exe exists.
- `AppHost` expects the manifest in the folder passed as its argument. This works because `run.cmd` passes `app`, but no-arg startup from the packaged root should probably try `app\app.manifest.json` before failing.
- `AppManifest` and `PackagedAppManifest` duplicate the same fields. This is tolerable now, but the duplication will become fragile if manifest fields grow.
- `ConsoleRuntimeSinks` is duplicated between `OSCControl.AppHost` and `oscctlc`. This is low risk, but it is a cleanup candidate.
- The packaged app has `data` and `logs`, but the manifest only models logs today. If persistent state becomes real product behavior, `data` should be represented in the manifest and used consistently by `AppHost`.
- The DesktopHost project still mixes editor UI, Blocks UI, localization, preview, and runtime control in a very large `MainForm.cs`. This is not blocking the packaging direction, but future UI iteration will become harder unless packaging and Blocks logic are kept out of `MainForm`.
- Tests could not be run in the reviewed environment because NuGet restore attempted to access `https://api.nuget.org/v3/index.json` and failed. Build checks for `AppHost` and `Packager` passed, but test coverage was not confirmed.

Recommended next steps:

- Add a DesktopHost command such as `Package App...` that invokes the same packaging path as `OSCControl.Packager`.
- Update generated `run.cmd` to prefer `host\OSCControl.AppHost.exe app`, with a fallback to `dotnet host\OSCControl.AppHost.dll app` if needed.
- Make `AppHost` no-arg startup friendlier by trying both the current base directory and an `app` child directory.
- Move manifest DTOs into a shared location if new manifest fields are added.
- Consider moving console runtime sinks into a shared utility only if duplication starts to affect behavior.
- Keep `OSCControl.Packager` as the CLI source of truth for packaging, and let DesktopHost call or share that logic rather than reimplementing a second packager.
- Add a small package smoke test once dependency restore is stable: package a trivial script, run `AppHost` against the packaged `app` folder, and verify it loads the plan.
