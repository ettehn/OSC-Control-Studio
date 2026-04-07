# OSCControl Compiler

This folder contains the first compiler skeleton for OSCControl.

It is aligned with:

- `C:\CodexProjects\OSCControl-language-spec.md`
- `C:\CodexProjects\OSCControl-parser-design.md`

Current scope:

- source positions and spans
- diagnostics
- tokenizer
- AST model
- recursive-descent parser
- validator with version gating
- lowering stub

The project is intentionally conservative:

- parser accepts forward-compatible syntax
- validator reports features not supported in `v0.1`
- lowering only keeps the currently useful declaration categories

## Additional Project Goal

One explicit project goal is enabling a future visual-programming workflow on top of OSCControl.

- the textual language remains the canonical source for precision and versioning
- the compiler pipeline should preserve enough structure for nodes, ports, rules, conditions, and data transforms to be represented in a visual graph editor
- the long-term direction is round-tripping between visual graphs and OSCControl source without losing diagnostics or execution intent

The next step after this scaffold is adding tests and then tightening the parser against real example files.

