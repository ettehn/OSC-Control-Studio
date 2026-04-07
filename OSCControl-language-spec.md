# OSCControl Language Spec v0.1

## Goal

OSCControl is a small control language for Windows desktop apps that need to:

- receive OSC messages
- send OSC messages
- receive WebSocket messages
- send WebSocket messages
- transform payloads
- route events between inputs and outputs
- expose a minimal state model for UI binding

The language is intentionally narrow. It is not a general-purpose programming language. It is a routing and transformation language for control systems.

## Design Approach

We start from runtime methods instead of syntax.

If the runtime only needs a small set of behaviors, the grammar should express only those behaviors.

For readability, OSCControl uses mixed bracket roles:

- `()` for function calls and grouped expressions
- `[]` for executable blocks
- `[[ ]]` for list literals
- `{}` for objects and configuration data

This keeps each bracket shape tied to one mental model.

## Core Runtime Objects

### `endpoint`

Represents an OSC or WebSocket connection point.

Fields:

- `name`
- `kind` (`osc.udp`, `ws.client`, `ws.server`)
- `host`
- `port`
- `path`
- `mode` (`input`, `output`, `duplex`)
- `codec` (`osc`, `json`, `text`, `bytes`)

### `message`

Represents a unit of traffic flowing through the runtime.

Fields:

- `source`
- `target`
- `address`
- `args`
- `body`
- `headers`
- `timestamp`
- `tags`

### `state`

Stores named values shared by rules and UI.

Supported value types:

- `number`
- `string`
- `bool`
- `list`
- `object`
- `null`

### `rule`

Represents a trigger plus a block of actions.

Fields:

- `name`
- `trigger`
- `filter`
- `actions`

## Required Runtime Methods

These are the methods the engine needs before we worry about parser details.

### Endpoint methods

- `listen(name, config)`
- `connect(name, config)`
- `close(name)`
- `reopen(name)`
- `status(name)`
- `listEndpoints()`

### Message methods

- `emit(target, message)`
- `forward(target, message)`
- `reply(message)`
- `drop(message)`
- `clone(message)`

### Transform methods

- `set(path, value)`
- `map(fromPath, toPath)`
- `append(path, value)`
- `remove(path)`
- `rename(fromPath, toPath)`
- `tag(name)`
- `untag(name)`

### Matching and control-flow methods

- `match(predicate)`
- `when(condition)`
- `otherwise()`
- `stop()`
- `call(functionName, args)`

### State methods

- `store(name, value)`
- `load(name)`
- `exists(name)`
- `clear(name)`

### Utility methods

- `log(level, value)`
- `wait(milliseconds)`
- `now()`
- `parse(format, raw)`
- `encode(format, value)`

## Built-In Functions

These should be available as expressions inside the language.

### Data access

- `arg(index)`
- `body(path)`
- `header(name)`
- `state(name)`
- `source()`
- `target()`
- `address()`
- `count(value)`

### Data conversion

- `int(value)`
- `float(value)`
- `string(value)`
- `bool(value)`
- `json(value)`

### String helpers

- `concat(a, b, ...)`
- `contains(text, part)`
- `startsWith(text, part)`
- `endsWith(text, part)`
- `replace(text, find, with)`

### Numeric helpers

- `min(a, b)`
- `max(a, b)`
- `clamp(value, low, high)`
- `round(value)`

### Time helpers

- `now()`
- `timestamp()`

## Statements We Need

If we map the runtime methods above into language features, the basic statements become:

- endpoint declaration
- state declaration
- function declaration
- rule declaration
- send action
- set action
- store action
- log action
- conditional action
- stop action

That suggests a very small top-level grammar.

## Bracket Roles

OSCControl should keep bracket usage consistent:

- `()` means "evaluate something"
- `[]` means "run these steps"
- `[[ ]]` means "this is a list"
- `{}` means "this is data"

Single `[]` may also appear after an expression for indexing, for example `msg.args[0]`.

Examples:

```osccontrol
arg(0)
(value + 1) * 0.5

on receive oscIn [
    log info "received"
]

[[1, 2, 3]]

{
    type: "note_on",
    note: 64
}
```

## Minimal Grammar

### Top-level structure

```ebnf
program         = { declaration } ;

declaration     = endpointDecl
                | stateDecl
                | functionDecl
                | ruleDecl ;
```

### Endpoint declarations

```ebnf
endpointDecl    = "endpoint" identifier ":" endpointType
                  configBlock ;

endpointType    = "osc.udp"
                | "ws.client"
                | "ws.server" ;

configBlock     = "{" { configEntry } "}" ;

configEntry     = "mode" ":" modeValue
                | "codec" ":" codecValue
                | "host" ":" stringLiteral
                | "port" ":" numberLiteral
                | "path" ":" stringLiteral ;

modeValue       = "input" | "output" | "duplex" ;
codecValue      = "osc" | "json" | "text" | "bytes" ;
```

### State declarations

```ebnf
stateDecl       = "state" identifier "=" expression ;
```

### Functions

```ebnf
functionDecl    = "func" identifier "(" [ parameterList ] ")" execBlock ;

parameterList   = identifier { "," identifier } ;
```

### Rules

```ebnf
ruleDecl        = "on" trigger [ "when" expression ] execBlock ;

trigger         = "receive" identifier
                | "address" stringLiteral
                | "timer" numberLiteral
                | "startup" ;
```

### Blocks and statements

```ebnf
execBlock       = "[" { statement } "]" ;

statement       = sendStmt
                | setStmt
                | storeStmt
                | logStmt
                | ifStmt
                | callStmt
                | stopStmt
                | letStmt ;
```

### Action statements

```ebnf
sendStmt        = "send" identifier [ sendBlock ] ;

sendBlock       = "{"
                    { sendEntry }
                  "}" ;

sendEntry       = "address" ":" expression
                | "args" ":" expression
                | "body" ":" expression
                | "headers" ":" expression ;

setStmt         = "set" lvalue "=" expression ;

storeStmt       = "store" identifier "=" expression ;

logStmt         = "log" [ logLevel ] expression ;

callStmt        = "call" identifier "(" [ argumentList ] ")" ;

stopStmt        = "stop" ;

letStmt         = "let" identifier "=" expression ;

lvalue          = identifier
                | memberAccess
                | indexAccess ;

logLevel        = "trace" | "debug" | "info" | "warn" | "error" ;
```

### Conditionals

```ebnf
ifStmt          = "if" expression execBlock [ "else" execBlock ] ;
```

### Expressions

```ebnf
expression      = logicalOr ;

logicalOr       = logicalAnd { "or" logicalAnd } ;
logicalAnd      = equality { "and" equality } ;
equality        = comparison { ( "==" | "!=" ) comparison } ;
comparison      = additive { ( "<" | "<=" | ">" | ">=" ) additive } ;
additive        = multiplicative { ( "+" | "-" ) multiplicative } ;
multiplicative  = unary { ( "*" | "/" | "%" ) unary } ;
unary           = [ "not" | "-" ] primary ;
primary         = literal
                | identifier
                | functionCall
                | memberAccess
                | indexAccess
                | "(" expression ")"
                | listLiteral
                | objectLiteral ;

functionCall    = identifier "(" [ argumentList ] ")" ;
argumentList    = expression { "," expression } ;
memberAccess    = primary "." identifier ;
indexAccess     = primary "[" expression "]" ;
listLiteral     = "[[" [ argumentList ] "]]" ;
objectLiteral   = "{" [ objectPropertyList ] "}" ;
objectPropertyList = objectProperty { "," objectProperty } ;
objectProperty  = objectKey ":" expression ;
objectKey       = identifier | stringLiteral ;
```

## Recommended Runtime Aliases

To keep the language ergonomic, the runtime should expose a current-message context:

- `msg.address`
- `msg.args`
- `msg.body`
- `msg.source`
- `msg.target`

This lets rules stay short.

## Suggested First-Phase Subset

For version `0.1`, we should implement only this subset:

- `endpoint`
- `state`
- `on receive`
- `when`
- `send`
- `set`
- `store`
- `log`
- `if`
- `stop`
- expressions with literals, identifiers, function calls, lists, and objects

`v0.1` uses a forward-compatible syntax policy:

- the parser may accept a wider surface grammar than `v0.1` executes
- unsupported features should be rejected by the validator with a clear versioned error
- this lets us keep syntax stable while phasing runtime features in gradually

We can postpone:

- custom functions
- timers
- reconnection policy syntax
- advanced pattern matching
- binary payload editing

## Example 1: OSC in, WebSocket out

```osccontrol
endpoint oscIn: osc.udp {
    mode: input
    host: "127.0.0.1"
    port: 9000
    codec: osc
}

endpoint wsOut: ws.client {
    mode: output
    host: "127.0.0.1"
    port: 8080
    path: "/ws"
    codec: json
}

state lastNote = 0

on receive oscIn when msg.address == "/note/on" [
    store lastNote = arg(0)
    send wsOut {
        body: {
            type: "note_on",
            note: arg(0),
            velocity: arg(1),
            at: timestamp()
        }
    }
]
```

## Example 2: WebSocket in, OSC out

```osccontrol
endpoint wsIn: ws.server {
    mode: input
    host: "0.0.0.0"
    port: 7000
    path: "/control"
    codec: json
}

endpoint oscOut: osc.udp {
    mode: output
    host: "127.0.0.1"
    port: 9001
    codec: osc
}

on receive wsIn when body("action") == "fader" [
    send oscOut {
        address: "/mixer/fader"
        args: [[body("channel"), clamp(body("value"), 0.0, 1.0)]]
    }
]
```

## Mapping Grammar to Runtime

Each statement should compile into a small execution node:

- `endpoint` -> endpoint registration
- `state` -> state initialization
- `on receive` -> subscription rule
- `when` -> predicate node
- `send` -> transport emit node
- `store` -> state write node
- `if` -> branch node
- `stop` -> terminal node

This matters because we should implement an AST and interpreter first, not a source-to-source transpiler.

## Next Step

Before building the parser, we should lock down:

1. whether `address "/foo"` should be valid only inside `send`, or also as a trigger
2. whether `body("x")` should be the official accessor, or whether `msg.body.x` is enough
3. whether user-defined functions should land in `v0.1` or wait until `v0.2`

My recommendation:

- keep `address` as both a send field and a rule trigger
- support both `body("x")` and `msg.body.x`
- postpone user-defined functions until the core routing model is stable

## Versioning Note

OSCControl `v0.1` should distinguish between:

- syntax the parser recognizes
- features the validator/runtime officially supports

That means constructs like `func`, `timer`, or other future-facing forms may parse successfully in `v0.1`, but should fail validation with messages such as:

- `Functions are parsed but not supported in OSCControl v0.1`
- `Timer triggers are parsed but not supported in OSCControl v0.1`
