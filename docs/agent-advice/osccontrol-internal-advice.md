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

## Historical Review Notes

The detailed issue lists from the two earlier structure reviews have been intentionally collapsed because several of their findings are now fixed. Do not treat this section as the active backlog.

Superseded review: `Current Structure Review After AppHost And Packager Split`

Resolved by later changes:

- `DesktopHost` now has a `Package App...` entry point.
- generated `run.cmd` now prefers `host\OSCControl.AppHost.exe app` and falls back to `dotnet host\OSCControl.AppHost.dll app`.
- manifest DTOs were consolidated through `OSCControl.Packaging.PackagedAppManifest`.
- `Data` and `Logs` are both represented in the manifest.
- `AppHost` no-argument root discovery was improved.

Superseded review: `Follow-Up Structure Review After Packaging Library`

Resolved by later changes:

- repeated packaging now cleans `app` and `host` while preserving `data` and `logs`.
- `PackageBuildRequest.ScriptPath` is now used through `PackagedAppManifest.SourceScript`.
- `PackagedAppBuilder.SanitizeDirectoryName` now handles Windows reserved names and trailing dot/space cases more defensively.
- `AppHost` now validates manifest-relative paths with containment checks.
- `PackagedAppBuilderTests` now cover package creation, plan deserialization, stale artifact cleanup, data preservation, and reserved-name normalization.

Still relevant and carried forward into the active review below:

- build/restore verification remains blocked by NuGet configuration access in this sandbox.
- `SourceScript` may leak local absolute paths in distributable manifests.
- `AppHost` should handle malformed manifest JSON and file I/O failures more gracefully.
- explicit package-root arguments should resolve `<root>\app` consistently.
- `HostSource` should be guarded against overlap with the target `host` folder.
- a solution or single build entry point is still missing.
- release packaging should use an explicit configured/published host directory instead of source-tree probing.

## Latest Review: Current State And Forward Plan

This review supersedes the earlier `Current Structure Review After AppHost And Packager Split` and `Follow-Up Structure Review After Packaging Library` notes above. Those sections are retained only as historical context.

Resolved since the prior advice pass:

- restore/build verification is now driven by tracked repository files: `Directory.Build.props`, `NuGet.Config`, and `Verify.ps1`.
- `OSCControl.sln` now provides a standard solution entry point for the production projects and test harness.
- `PackagedAppManifest.SourceScript` now stores only a source file label, not a full absolute local path.
- `AppHost` now reports malformed manifest JSON and manifest file I/O failures as user-readable startup errors.
- explicit `AppHost <package-root>` now resolves `<package-root>\app\app.manifest.json` consistently.
- `PackagedAppBuilder` now rejects `HostSource` values that overlap the generated target `host` folder, avoiding destructive self-copy cases.
- `PackagedAppBuilderTests` now include an overlap guard test.

Validation performed after these changes:

- `powershell -NoProfile -ExecutionPolicy Bypass -File C:\CodexProjects\Verify.ps1` passed with `22 passed, 0 failed, 1 skipped, 23 total`.
- packaging smoke confirmed `SourceScript` is `sample-plan.osccontrol`, not an absolute path.
- AppHost process smoke confirmed explicit package-root startup loads `app\app.plan.json`.
- malformed manifest process smoke returned exit code `2` with a friendly `Could not start packaged app` error.

Remaining active issues:

- `DesktopHost` still needs a more explicit packaging settings surface for app name, output folder, and host source.
- DesktopHost host binary probing is still development-oriented and should become a release-configured setting before product distribution.
- AppHost root/path logic is still private; process smoke covers it for now, but extracting a testable resolver remains reasonable if this code grows.
- Add plan codec round-trip tests that cover endpoints, states, rules, functions, branch/loop steps, and nested expressions.
- Add a packaging smoke test for host-copy mode and run-command contents.
- The WebSocket server runtime host test remains skipped in this sandbox because `HttpListener` cannot start with the current sandbox handle restrictions.

Suggested forward plan:

1. Productize DesktopHost packaging.
   - Add an explicit packaging settings surface for app name, output folder, and host source.
   - Keep source-tree host probing as a development fallback only.
   - Move packaging orchestration out of `MainForm` once the workflow grows beyond the current button handler.

2. Improve focused coverage.
   - Add plan codec round-trip tests for functions, branches, loops, nested expressions, endpoints, and states.
   - Add AppHost root/path resolution coverage, either through a small extracted resolver or process-level smoke tests.
   - Add host-copy packaging smoke coverage for `run.cmd` and copied host files.

3. Defer larger architecture changes.
   - Do not build a generic graph editor yet.
   - Keep Blocks as the structured editor for the subset that round-trips well.
   - Keep `.osccontrol` as the canonical source and `app.plan.json` as the runtime artifact.
