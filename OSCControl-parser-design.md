# OSCControl Parser Design v0.1

## Goal

This document turns the OSCControl language spec into an implementation-ready parser design.

It defines:

- lexical rules
- token kinds
- AST node types
- parser shape
- ambiguity rules
- error handling rules

It assumes the current mixed-bracket grammar:

- `()` for calls and expression grouping
- `[]` for executable blocks
- `[[ ]]` for list literals
- `{}` for data/config objects

## Parsing Strategy

Use a two-stage frontend:

1. `Tokenizer`
2. `RecursiveDescentParser`

OSCControl `v0.1` follows a forward-compatible syntax strategy:

- the parser accepts some constructs that are not yet executable in `v0.1`
- the validator is responsible for reporting version-specific feature errors
- this keeps the syntax stable while the runtime grows over time

This is a better fit than parser generators for `v0.1` because:

- the grammar is small
- context-sensitive interpretation is limited and manageable
- we want strong custom diagnostics
- we want easy future evolution

## Source Model

The frontend should preserve source locations for all nodes.

Recommended source types:

```text
SourceSpan
- startOffset
- endOffset
- startLine
- startColumn
- endLine
- endColumn
```

Every token and AST node should carry a `SourceSpan`.

## Lexical Rules

### Whitespace

Whitespace is ignored except as a separator.

Recognized whitespace:

- space
- tab
- carriage return
- line feed

Line endings do not affect parsing in `v0.1`.

## Comments

Support two comment styles:

- line comment: `# comment`
- line comment: `// comment`

Comment tokens do not need to be preserved unless the editor wants them later.

## Identifiers

Identifiers should follow these rules:

- first character: letter or `_`
- remaining characters: letter, digit, or `_`

Examples:

- `oscIn`
- `msg`
- `_temp`
- `last_note`

### Important lexer rule

Member access and endpoint type names both contain dots in source, but they should not be treated the same way.

Recommended rule:

- allow dotted identifiers only for known endpoint/type-like symbols in declaration position
- tokenize regular member access as `identifier`, `.`, `identifier`

Practical choice for `v0.1`:

- lexer treats `osc.udp`, `ws.client`, and `ws.server` as reserved keywords
- normal identifiers do not include `.`

This keeps the parser simple.

## Keywords

The tokenizer should emit dedicated keyword tokens for:

- `endpoint`
- `state`
- `func`
- `on`
- `receive`
- `when`
- `timer`
- `startup`
- `send`
- `address`
- `args`
- `body`
- `headers`
- `set`
- `store`
- `log`
- `call`
- `stop`
- `let`
- `if`
- `else`
- `trace`
- `debug`
- `info`
- `warn`
- `error`
- `and`
- `or`
- `not`
- `true`
- `false`
- `null`
- `mode`
- `codec`
- `host`
- `port`
- `path`
- `input`
- `output`
- `duplex`
- `osc`
- `json`
- `text`
- `bytes`
- `osc.udp`
- `ws.client`
- `ws.server`

## Literals

### Number literals

Support:

- integers: `1`
- decimals: `3.14`
- negative numbers via unary operator, not tokenized as part of the number

Do not support exponent syntax in `v0.1` unless needed.

### String literals

Support double-quoted strings:

```text
"hello"
"/note/on"
"127.0.0.1"
```

Escapes to support:

- `\"`
- `\\`
- `\n`
- `\r`
- `\t`

Single-quoted strings can wait until later.

## Punctuation Tokens

The tokenizer should emit:

- `(` `)`
- `[` `]`
- `[[` `]]`
- `{` `}`
- `:` `,`
- `.` `=`
- `+` `-` `*` `/` `%`
- `==` `!=` `<` `<=` `>` `>=`

## Token Model

Recommended token structure:

```text
Token
- kind
- text
- value
- span
```

Where:

- `kind` is the enum value
- `text` is the raw source slice
- `value` is decoded for literals when applicable
- `span` is the source range

## Token Kinds

Suggested enum shape:

```text
EndOfFile
Identifier
Number
String

LeftParen
RightParen
LeftBracket
RightBracket
LeftDoubleBracket
RightDoubleBracket
LeftBrace
RightBrace
Colon
Comma
Dot
Equal
Plus
Minus
Star
Slash
Percent
EqualEqual
BangEqual
Less
LessEqual
Greater
GreaterEqual

KeywordEndpoint
KeywordState
KeywordFunc
KeywordOn
KeywordReceive
KeywordWhen
KeywordTimer
KeywordStartup
KeywordSend
KeywordAddress
KeywordArgs
KeywordBody
KeywordHeaders
KeywordSet
KeywordStore
KeywordLog
KeywordCall
KeywordStop
KeywordLet
KeywordIf
KeywordElse
KeywordTrace
KeywordDebug
KeywordInfo
KeywordWarn
KeywordError
KeywordAnd
KeywordOr
KeywordNot
KeywordTrue
KeywordFalse
KeywordNull
KeywordMode
KeywordCodec
KeywordHost
KeywordPort
KeywordPath
KeywordInput
KeywordOutput
KeywordDuplex
KeywordOsc
KeywordJson
KeywordText
KeywordBytes
KeywordOscUdp
KeywordWsClient
KeywordWsServer
```

## AST Shape

The AST should stay close to the language surface.

## Root Node

```text
ProgramNode
- declarations: List<DeclarationNode>
- span
```

## Declaration Nodes

```text
DeclarationNode
  EndpointDeclarationNode
  StateDeclarationNode
  FunctionDeclarationNode
  RuleDeclarationNode
```

### Endpoint Declaration

```text
EndpointDeclarationNode
- name: IdentifierNode
- endpointType: EndpointType
- config: ObjectLiteralNode
- span
```

`config` should stay as data, not a special map type.

### State Declaration

```text
StateDeclarationNode
- name: IdentifierNode
- value: ExpressionNode
- span
```

### Function Declaration

```text
FunctionDeclarationNode
- name: IdentifierNode
- parameters: List<IdentifierNode>
- body: ExecBlockNode
- span
```

Even if user functions are postponed at runtime, keeping the node now is intentional because `v0.1` parses forward-compatible syntax and rejects unsupported features in validation.

### Rule Declaration

```text
RuleDeclarationNode
- trigger: TriggerNode
- condition: ExpressionNode?
- body: ExecBlockNode
- span
```

## Trigger Nodes

```text
TriggerNode
  ReceiveTriggerNode
  AddressTriggerNode
  TimerTriggerNode
  StartupTriggerNode
```

```text
ReceiveTriggerNode
- endpointName: IdentifierNode
- span

AddressTriggerNode
- value: StringLiteralNode
- span

TimerTriggerNode
- interval: NumberLiteralNode
- span

StartupTriggerNode
- span
```

## Statement Nodes

```text
StatementNode
  SendStatementNode
  SetStatementNode
  StoreStatementNode
  LogStatementNode
  CallStatementNode
  StopStatementNode
  LetStatementNode
  IfStatementNode
```

### Exec Block

```text
ExecBlockNode
- statements: List<StatementNode>
- span
```

### Send Statement

```text
SendStatementNode
- target: IdentifierNode
- payload: ObjectLiteralNode?
- span
```

The payload object can later be normalized into:

- `address`
- `args`
- `body`
- `headers`

### Set Statement

```text
SetStatementNode
- target: AssignableExpressionNode
- value: ExpressionNode
- span
```

### Store Statement

```text
StoreStatementNode
- name: IdentifierNode
- value: ExpressionNode
- span
```

### Log Statement

```text
LogStatementNode
- level: LogLevel?
- value: ExpressionNode
- span
```

### Call Statement

```text
CallStatementNode
- name: IdentifierNode
- arguments: List<ExpressionNode>
- span
```

### Stop Statement

```text
StopStatementNode
- span
```

### Let Statement

```text
LetStatementNode
- name: IdentifierNode
- value: ExpressionNode
- span
```

### If Statement

```text
IfStatementNode
- condition: ExpressionNode
- thenBlock: ExecBlockNode
- elseBlock: ExecBlockNode?
- span
```

## Expression Nodes

```text
ExpressionNode
  IdentifierNode
  NumberLiteralNode
  StringLiteralNode
  BooleanLiteralNode
  NullLiteralNode
  UnaryExpressionNode
  BinaryExpressionNode
  CallExpressionNode
  MemberExpressionNode
  IndexExpressionNode
  ListLiteralNode
  ObjectLiteralNode
  ParenthesizedExpressionNode
```

### Assignable Expressions

Only these may appear on the left side of `set`:

```text
AssignableExpressionNode
  IdentifierNode
  MemberExpressionNode
  IndexExpressionNode
```

### List Literal

```text
ListLiteralNode
- items: List<ExpressionNode>
- span
```

### Object Literal

```text
ObjectLiteralNode
- properties: List<ObjectPropertyNode>
- span

ObjectPropertyNode
- key: ObjectKeyNode
- value: ExpressionNode
- span
```

`ObjectKeyNode` can be:

- identifier
- string literal

### Call Expression

```text
CallExpressionNode
- callee: ExpressionNode
- arguments: List<ExpressionNode>
- span
```

### Member Expression

```text
MemberExpressionNode
- target: ExpressionNode
- member: IdentifierNode
- span
```

### Index Expression

```text
IndexExpressionNode
- target: ExpressionNode
- index: ExpressionNode
- span
```

### Unary Expression

```text
UnaryExpressionNode
- operator: UnaryOperator
- operand: ExpressionNode
- span
```

### Binary Expression

```text
BinaryExpressionNode
- left: ExpressionNode
- operator: BinaryOperator
- right: ExpressionNode
- span
```

## Operator Precedence

Use this precedence table from lowest to highest:

1. `or`
2. `and`
3. `==`, `!=`
4. `<`, `<=`, `>`, `>=`
5. `+`, `-`
6. `*`, `/`, `%`
7. unary `not`, unary `-`
8. postfix member access, index access, call
9. primary

This means:

```osccontrol
msg.body.value + 1 * 2
```

parses as:

```text
msg.body.value + (1 * 2)
```

## Parser Structure

Recommended methods:

```text
ParseProgram()
ParseDeclaration()
ParseEndpointDeclaration()
ParseStateDeclaration()
ParseFunctionDeclaration()
ParseRuleDeclaration()
ParseTrigger()
ParseExecBlock()
ParseStatement()
ParseSendStatement()
ParseSetStatement()
ParseStoreStatement()
ParseLogStatement()
ParseCallStatement()
ParseStopStatement()
ParseLetStatement()
ParseIfStatement()
ParseExpression()
ParseOrExpression()
ParseAndExpression()
ParseEqualityExpression()
ParseComparisonExpression()
ParseAdditiveExpression()
ParseMultiplicativeExpression()
ParseUnaryExpression()
ParsePostfixExpression()
ParsePrimaryExpression()
ParseListLiteral()
ParseObjectLiteral()
```

`ParseListLiteral()` should consume `LeftDoubleBracket ... RightDoubleBracket`.

## Important Ambiguity Rules

### `[]` as exec block vs index access, and `[[ ]]` as list literal

After introducing `[[ ]]` list syntax, the main `[]` ambiguity is removed.

Resolution rule:

- in declaration and statement positions, `[` starts an `ExecBlockNode`
- in expression position, `[[` starts a `ListLiteralNode`
- in expression postfix position, `[` starts an `IndexExpressionNode`

Examples:

```osccontrol
on receive oscIn [
    log info "x"
]
```

Here `[` starts an exec block because the parser is expecting a rule body.

```osccontrol
send oscOut {
    args: [[1, 2, 3]]
}
```

Here `[[` starts a list literal because the parser is inside an expression after `args:`.

This is straightforward in a recursive-descent parser because the current parse function already knows which category is valid.

### `{}` as config block vs object literal

Resolution rule:

- after `endpoint name: type`, `{}` is parsed as config
- in expression position, `{}` is parsed as object literal
- after `send target`, `{}` is parsed as payload object

The AST may represent all three as `ObjectLiteralNode` if desired, but parsing context should validate allowed keys later.

### `address`

`address` can appear as:

- a keyword in trigger syntax
- a key inside send payload
- a built-in function name if we keep `address()`

Recommendation:

- keep `address` keyworded in declarations/statements
- allow `address()` as a built-in function only in expression parsing

That is easy to support because call parsing is contextual.

## Validation Layer

The parser should not enforce all semantic rules.

Use a second pass for validation:

- duplicate endpoint names
- duplicate state names
- invalid config keys for endpoint type
- invalid send payload keys
- invalid assignment targets
- use of unsupported declarations in `v0.1`

For `v0.1`, this explicitly includes features that parse successfully but are not yet supported by execution, for example:

- `func` declarations
- `timer` triggers
- any later feature added to the grammar before the runtime is ready

Recommended diagnostic style:

- `Functions are parsed but not supported in OSCControl v0.1`
- `Timer triggers are parsed but not supported in OSCControl v0.1`

## Error Handling

Use parser diagnostics with recovery so the editor can keep showing multiple errors.

Each diagnostic should include:

- message
- span
- severity
- optional hint

Examples:

```text
Unexpected token '}' while parsing expression.
Expected ']' to close executable block started at line 12.
Unknown send field 'adress'. Did you mean 'address'?
```

### Recovery strategy

When a statement parse fails, skip tokens until one of:

- start of a new top-level declaration
- closing `]`
- closing `}`
- end of file

When an expression parse fails inside an object or list, skip until:

- `,`
- `}`
- `]`

## Normalized Runtime IR

Do not execute directly from the parser AST.

Instead:

1. parse into AST
2. validate AST
3. lower into a normalized runtime graph

Suggested lowered nodes:

- `EndpointDefinition`
- `StateDefinition`
- `RuleDefinition`
- `PredicateStep`
- `StoreStep`
- `SendStep`
- `LogStep`
- `BranchStep`
- `StopStep`

This keeps runtime execution simpler than interpreting every syntax form directly.

## Example Parse

Source:

```osccontrol
on receive oscIn when msg.address == "/note/on" [
    store lastNote = arg(0)
    send wsOut {
        body: {
            type: "note_on",
            note: arg(0)
        }
    }
]
```

High-level AST:

```text
RuleDeclarationNode
  trigger = ReceiveTriggerNode("oscIn")
  condition =
    BinaryExpressionNode("==")
      left = MemberExpressionNode(
        target = IdentifierNode("msg"),
        member = IdentifierNode("address"))
      right = StringLiteralNode("/note/on")
  body = ExecBlockNode
    StoreStatementNode("lastNote", CallExpressionNode("arg", [0]))
    SendStatementNode("wsOut", ObjectLiteralNode(...))
```

## Recommended v0.1 Scope

Implement now:

- tokenizer
- AST types
- recursive-descent parser
- diagnostics
- validator

Delay until later:

- user-defined functions at runtime
- timers
- source formatting
- code completion
- macro system

## Next Implementation Step

Build files in this order:

1. `TokenKind`
2. `Token`
3. `Tokenizer`
4. AST node model
5. `Parser`
6. diagnostics
7. validator

If we keep this order, we can test incrementally without needing the runtime yet.
