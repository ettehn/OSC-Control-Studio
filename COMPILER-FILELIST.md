# OSCControl Compiler File List

## Current Files

These are the compiler files that now exist in the workspace.

### Docs

- `OSCControl-language-spec.md`
- `OSCControl-parser-design.md`
- `COMPILER-FILELIST.md`
- `VRChat-OSC-sugar.md`
- `src/OSCControl.Compiler/README.md`

### Project

- `src/OSCControl.Compiler/OSCControl.Compiler.csproj`
- `src/oscctlc/oscctlc.csproj`

### Compiler pipeline

- `src/OSCControl.Compiler/Compiler/CompilationResult.cs`
- `src/OSCControl.Compiler/Compiler/CompilerPipeline.cs`

### Source text and spans

- `src/OSCControl.Compiler/Text/SourcePosition.cs`
- `src/OSCControl.Compiler/Text/SourceSpan.cs`
- `src/OSCControl.Compiler/Text/SourceText.cs`

### Diagnostics

- `src/OSCControl.Compiler/Diagnostics/DiagnosticSeverity.cs`
- `src/OSCControl.Compiler/Diagnostics/Diagnostic.cs`
- `src/OSCControl.Compiler/Diagnostics/DiagnosticBag.cs`

### Lexing

- `src/OSCControl.Compiler/Lexing/TokenKind.cs`
- `src/OSCControl.Compiler/Lexing/Token.cs`
- `src/OSCControl.Compiler/Lexing/Tokenizer.cs`

### Syntax and AST

- `src/OSCControl.Compiler/Syntax/SyntaxNodes.cs`
- `src/OSCControl.Compiler/Syntax/Parser.cs`

### Validation

- `src/OSCControl.Compiler/Validation/LanguageVersion.cs`
- `src/OSCControl.Compiler/Validation/Validator.cs`

### Lowering

- `src/OSCControl.Compiler/Lowering/LoweredProgram.cs`
- `src/OSCControl.Compiler/Lowering/Lowerer.cs`

### Execution IR

- `src/OSCControl.Compiler/Execution/ExecutionProgram.cs`
- `src/OSCControl.Compiler/Execution/ExecutionLowerer.cs`

### CLI

- `src/oscctlc/Program.cs`

### Tests

- `tests/OSCControl.Compiler.Tests/OSCControl.Compiler.Tests.csproj`
- `tests/OSCControl.Compiler.Tests/CompilerPipelineTests.cs`

## Module Responsibilities

- `Lexing`: turns source text into tokens, including OSCControl-specific tokens like `[[` and `]]`
- `Syntax`: builds the AST from tokens
- `Validation`: enforces version support rules such as "parses in v0.1 but not executable yet"
- `Lowering`: converts AST into normalized language IR
- `Execution IR`: converts lowered language IR into runtime-oriented execution structures
- `Compiler`: wires the stages together
- `CLI`: exposes `check`, `parse`, `tokens`, `ast`, `lowered`, and `execution`

## Deferred Files

These are intentionally not implemented yet.

- `src/OSCControl.Compiler/Formatting/*`
- `src/OSCControl.Compiler/CodeGen/*`
- `src/OSCControl.Compiler/RuntimeIR/*`

## Next Likely Additions

- parser unit tests for invalid cases
- validator rule tests beyond v0.1 feature gating
- desugaring pass
- command-line options for JSON output
- runtime host or editor integration
- editor-facing schema or IR for future visual programming support

