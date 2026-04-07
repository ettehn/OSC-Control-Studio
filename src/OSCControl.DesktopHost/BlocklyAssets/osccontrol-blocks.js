(function () {
  if (!window.Blockly) {
    return;
  }

  Blockly.defineBlocksWithJsonArray([
    {
      "type": "osc_endpoint_udp",
      "message0": "OSC UDP endpoint %1 mode %2 host %3 port %4",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "oscIn" },
        { "type": "field_dropdown", "name": "MODE", "options": [["input", "input"], ["output", "output"], ["duplex", "duplex"]] },
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "PORT", "value": 9000, "min": 1, "max": 65535, "precision": 1 }
      ],
      "nextStatement": null,
      "colour": 230,
      "tooltip": "Declare an OSC UDP endpoint.",
      "helpUrl": ""
    },
    {
      "type": "osc_endpoint_ws",
      "message0": "WebSocket %1 endpoint %2 mode %3 host %4 port %5 path %6 codec %7",
      "args0": [
        { "type": "field_dropdown", "name": "TRANSPORT", "options": [["client", "ws.client"], ["server", "ws.server"]] },
        { "type": "field_input", "name": "NAME", "text": "wsOut" },
        { "type": "field_dropdown", "name": "MODE", "options": [["input", "input"], ["output", "output"], ["duplex", "duplex"]] },
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "PORT", "value": 8080, "min": 1, "max": 65535, "precision": 1 },
        { "type": "field_input", "name": "PATH", "text": "/osc" },
        { "type": "field_input", "name": "CODEC", "text": "json" }
      ],
      "nextStatement": null,
      "colour": 230,
      "tooltip": "Declare a WebSocket endpoint.",
      "helpUrl": ""
    },
    {
      "type": "ws_client_endpoint",
      "message0": "WebSocket client %1 mode %2 connect %3 port %4 path %5 codec %6",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "wsClient" },
        { "type": "field_dropdown", "name": "MODE", "options": [["duplex", "duplex"], ["input", "input"], ["output", "output"]] },
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "PORT", "value": 8080, "min": 1, "max": 65535, "precision": 1 },
        { "type": "field_input", "name": "PATH", "text": "/control" },
        { "type": "field_dropdown", "name": "CODEC", "options": [["json", "json"], ["text", "text"], ["bytes", "bytes"]] }
      ],
      "nextStatement": null,
      "colour": 230,
      "tooltip": "Declare a WebSocket client endpoint. Duplex mode receives from and sends to the remote server on the same connection.",
      "helpUrl": ""
    },
    {
      "type": "ws_server_endpoint",
      "message0": "WebSocket server %1 mode %2 listen %3 port %4 path %5 codec %6",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "wsServer" },
        { "type": "field_dropdown", "name": "MODE", "options": [["duplex", "duplex"], ["input", "input"], ["output", "output"]] },
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "PORT", "value": 8081, "min": 1, "max": 65535, "precision": 1 },
        { "type": "field_input", "name": "PATH", "text": "/control" },
        { "type": "field_dropdown", "name": "CODEC", "options": [["json", "json"], ["text", "text"], ["bytes", "bytes"]] }
      ],
      "nextStatement": null,
      "colour": 230,
      "tooltip": "Declare a WebSocket server endpoint. Duplex mode receives client messages and broadcasts sends to connected clients.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_endpoint",
      "message0": "VRChat endpoint host %1 input %2 output %3",
      "args0": [
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "INPUT_PORT", "value": 9000, "min": 1, "max": 65535, "precision": 1 },
        { "type": "field_number", "name": "OUTPUT_PORT", "value": 9001, "min": 1, "max": 65535, "precision": 1 }
      ],
      "nextStatement": null,
      "colour": 230,
      "tooltip": "Declare a VRChat OSC endpoint.",
      "helpUrl": ""
    },
    {
      "type": "osc_variable",
      "message0": "declare variable %1 = %2",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "count" },
        { "type": "field_input", "name": "VALUE", "text": "0" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 300,
      "tooltip": "Declare a top-level state variable.",
      "helpUrl": ""
    },
    {
      "type": "osc_variable_expr",
      "message0": "declare variable %1 = %2",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "count" },
        { "type": "input_value", "name": "VALUE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 300,
      "tooltip": "Declare a top-level state variable from an expression.",
      "helpUrl": ""
    },
    {
      "type": "osc_startup_rule",
      "message0": "when app starts %1 %2",
      "args0": [
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Run steps when the app starts.",
      "helpUrl": ""
    },
    {
      "type": "osc_receive_rule",
      "message0": "when %1 receives address %2 %3 %4",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "oscIn" },
        { "type": "field_input", "name": "ADDRESS", "text": "/example" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Run steps when an endpoint receives a matching OSC address.",
      "helpUrl": ""
    },
    {
      "type": "osc_receive_rule_when",
      "message0": "when %1 receives address %2 and %3 %4 %5",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "oscIn" },
        { "type": "field_input", "name": "ADDRESS", "text": "/example" },
        { "type": "field_input", "name": "CONDITION", "text": "arg(0) > 0" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Run steps when an endpoint receives a matching address and condition.",
      "helpUrl": ""
    },
    {
      "type": "ws_receive_rule",
      "message0": "when WebSocket %1 receives address %2 %3 %4",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "wsServer" },
        { "type": "field_input", "name": "ADDRESS", "text": "/ping" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Run steps when a WebSocket endpoint receives a JSON envelope with the matching address.",
      "helpUrl": ""
    },
    {
      "type": "ws_receive_rule_when",
      "message0": "when WebSocket %1 receives address %2 and %3 %4 %5",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "wsServer" },
        { "type": "field_input", "name": "ADDRESS", "text": "/ping" },
        { "type": "field_input", "name": "CONDITION", "text": "body(\"value\") > 0" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Run steps when a WebSocket endpoint receives a matching address and condition.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_avatar_rule",
      "message0": "when VRChat avatar changes %1 %2",
      "args0": [
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Run steps when VRChat reports an avatar change.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_builtin_param_rule",
      "message0": "when VRChat read-only built-in param %1 changes %2 %3",
      "args0": [
        { "type": "field_dropdown", "name": "PARAM", "options": [["GestureLeft", "GestureLeft"], ["GestureRight", "GestureRight"], ["GestureLeftWeight", "GestureLeftWeight"], ["GestureRightWeight", "GestureRightWeight"], ["VelocityMagnitude", "VelocityMagnitude"], ["VelocityX", "VelocityX"], ["VelocityY", "VelocityY"], ["VelocityZ", "VelocityZ"], ["Upright", "Upright"], ["Grounded", "Grounded"], ["Seated", "Seated"], ["AFK", "AFK"], ["TrackingType", "TrackingType"], ["VRMode", "VRMode"], ["MuteSelf", "MuteSelf"], ["InStation", "InStation"], ["Earmuffs", "Earmuffs"], ["Viseme", "Viseme"], ["Voice", "Voice"], ["AngularY", "AngularY"], ["IsLocal", "IsLocal"], ["PreviewMode", "PreviewMode"], ["AvatarVersion", "AvatarVersion"], ["IsAnimatorEnabled", "IsAnimatorEnabled"], ["ScaleModified", "ScaleModified"], ["ScaleFactor", "ScaleFactor"], ["ScaleFactorInverse", "ScaleFactorInverse"], ["EyeHeightAsMeters", "EyeHeightAsMeters"], ["EyeHeightAsPercent", "EyeHeightAsPercent"], ["IsOnFriendsList", "IsOnFriendsList"]] },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Listen to a VRChat built-in avatar parameter. Built-in parameters are read-only in VRChat and cannot be written by OSCControl.",
      "helpUrl": "https://creators.vrchat.com/avatars/animator-parameters/"
    },
    {
      "type": "vrchat_param_rule",
      "message0": "when VRChat custom param %1 changes %2 %3",
      "args0": [
        { "type": "field_input", "name": "PARAM", "text": "CustomParam" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 190,
      "tooltip": "Run steps when a custom VRChat avatar parameter changes. For built-in read-only parameters, prefer the dedicated built-in parameter block.",
      "helpUrl": ""
    },
    {
      "type": "osc_log",
      "message0": "log text/expression %1 %2",
      "args0": [
        { "type": "field_dropdown", "name": "LEVEL", "options": [["info", "info"], ["warn", "warn"], ["error", "error"]] },
        { "type": "field_input", "name": "VALUE", "text": "\"ready\"" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 60,
      "tooltip": "Write a runtime log entry. This field is raw DSL: type count to reference a variable, or \"ready\" for a string literal.",
      "helpUrl": ""
    },
    {
      "type": "osc_store",
      "message0": "store %1 = %2",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "count" },
        { "type": "field_input", "name": "VALUE", "text": "count + 1" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 60,
      "tooltip": "Update a state variable.",
      "helpUrl": ""
    },
    {
      "type": "osc_send_simple",
      "message0": "send OSC args to %1 address %2 args %3",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "oscOut" },
        { "type": "field_input", "name": "ADDRESS", "text": "/hello" },
        { "type": "field_input", "name": "ARGS", "text": "[[1]]" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Send an OSC args payload.",
      "helpUrl": ""
    },
    {
      "type": "osc_send_body",
      "message0": "send body to %1 address %2 body %3",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "wsOut" },
        { "type": "field_input", "name": "ADDRESS", "text": "/hello" },
        { "type": "field_input", "name": "BODY", "text": "{value: arg(0)}" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Send a message body to an endpoint.",
      "helpUrl": ""
    },
    {
      "type": "ws_send_json",
      "message0": "send WebSocket JSON to %1 address %2 body %3",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "wsServer" },
        { "type": "field_input", "name": "ADDRESS", "text": "/pong" },
        { "type": "field_input", "name": "BODY", "text": "{value: arg(0)}" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Send a JSON WebSocket envelope. With ws.server, this broadcasts to connected clients; with ws.client, it sends to the remote server.",
      "helpUrl": ""
    },
    {
      "type": "ws_send_text",
      "message0": "send WebSocket text to %1 text %2",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "wsClient" },
        { "type": "field_input", "name": "TEXT", "text": "\"hello\"" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Send a WebSocket body. This field is raw DSL: type count to reference a variable, or \"hello\" for a string literal. Use codec text for raw text payloads.",
      "helpUrl": ""
    },
    {
      "type": "ws_send_json_expr",
      "message0": "send WebSocket JSON to %1 address %2 body %3",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "wsServer" },
        { "type": "field_input", "name": "ADDRESS", "text": "/pong" },
        { "type": "input_value", "name": "BODY" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Send a JSON WebSocket envelope with an expression body.",
      "helpUrl": ""
    },
    {
      "type": "osc_if",
      "message0": "if %1 %2 %3",
      "args0": [
        { "type": "field_input", "name": "CONDITION", "text": "arg(0) > 0" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "THEN" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Run nested steps when a condition is true.",
      "helpUrl": ""
    },
    {
      "type": "osc_if_else",
      "message0": "if %1 %2 then %3 else %4",
      "args0": [
        { "type": "field_input", "name": "CONDITION", "text": "arg(0) > 0" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "THEN" },
        { "type": "input_statement", "name": "ELSE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Run one of two nested step lists based on a condition.",
      "helpUrl": ""
    },
    {
      "type": "osc_while",
      "message0": "while %1 %2 %3",
      "args0": [
        { "type": "field_input", "name": "CONDITION", "text": "count < 3" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "DO" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Repeat nested steps while a condition is true.",
      "helpUrl": ""
    },
    {
      "type": "osc_stop",
      "message0": "stop rule",
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Stop the current rule.",
      "helpUrl": ""
    },
    {
      "type": "osc_break",
      "message0": "break loop",
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Break out of the current loop.",
      "helpUrl": ""
    },
    {
      "type": "osc_continue",
      "message0": "continue loop",
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Continue the current loop.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_chat",
      "message0": "VRChat chatbox text/expression %1 send %2 notify %3",
      "args0": [
        { "type": "field_input", "name": "TEXT", "text": "\"Hello from OSCControl\"" },
        { "type": "field_checkbox", "name": "SEND", "checked": true },
        { "type": "field_checkbox", "name": "NOTIFY", "checked": false }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Send text or an expression to the VRChat chatbox. This field is raw DSL: type count to reference a variable, or \"hello\" for a string literal.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_input",
      "message0": "VRChat input %1 = %2",
      "args0": [
        { "type": "field_input", "name": "INPUT", "text": "Jump" },
        { "type": "field_input", "name": "VALUE", "text": "1" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Set a VRChat input value.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_param",
      "message0": "VRChat param %1 = %2",
      "args0": [
        { "type": "field_input", "name": "PARAM", "text": "CustomParam" },
        { "type": "field_input", "name": "VALUE", "text": "0" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Set a custom VRChat avatar parameter. VRChat built-in avatar parameters such as GestureLeft and VelocityZ are read-only.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_typing",
      "message0": "VRChat typing %1",
      "args0": [
        { "type": "field_dropdown", "name": "VALUE", "options": [["true", "true"], ["false", "false"]] }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Set VRChat chatbox typing state.",
      "helpUrl": ""
    },
    {
      "type": "osc_raw_step",
      "message0": "raw step %1",
      "args0": [
        { "type": "field_input", "name": "SOURCE", "text": "stop" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 0,
      "tooltip": "Emit a raw OSCControl statement while the Blockly mapping is incomplete.",
      "helpUrl": ""
    }
    ,
    {
      "type": "osc_expr_number",
      "message0": "number %1",
      "args0": [
        { "type": "field_number", "name": "VALUE", "value": 0 }
      ],
      "output": null,
      "colour": 260,
      "tooltip": "Number literal.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_string",
      "message0": "text %1",
      "args0": [
        { "type": "field_input", "name": "VALUE", "text": "text" }
      ],
      "output": null,
      "colour": 260,
      "tooltip": "String literal.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_boolean",
      "message0": "boolean %1",
      "args0": [
        { "type": "field_dropdown", "name": "VALUE", "options": [["true", "true"], ["false", "false"]] }
      ],
      "output": null,
      "colour": 260,
      "tooltip": "Boolean literal.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_identifier",
      "message0": "raw name %1",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "count" }
      ],
      "output": null,
      "colour": 260,
      "tooltip": "Reference a raw variable name or local value.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_variable",
      "message0": "reference variable %1",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "count" }
      ],
      "output": null,
      "colour": 300,
      "tooltip": "Reference a variable declared with a top-level variable block as an expression input.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_arg",
      "message0": "arg %1",
      "args0": [
        { "type": "field_number", "name": "INDEX", "value": 0, "min": 0, "precision": 1 }
      ],
      "output": null,
      "colour": 260,
      "tooltip": "Read an OSC argument by index.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_msg_field",
      "message0": "message %1",
      "args0": [
        { "type": "field_dropdown", "name": "FIELD", "options": [["address", "address"], ["body", "body"], ["source", "source"], ["target", "target"]] }
      ],
      "output": null,
      "colour": 260,
      "tooltip": "Read a message property.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_binary",
      "message0": "%1 %2 %3",
      "args0": [
        { "type": "input_value", "name": "LEFT" },
        { "type": "field_dropdown", "name": "OP", "options": [["+", "+"], ["-", "-"], ["*", "*"], ["/", "/"], ["%", "%"], ["==", "=="], ["!=", "!="], ["<", "<"], ["<=", "<="], [">", ">"], [">=", ">="], ["and", "and"], ["or", "or"]] },
        { "type": "input_value", "name": "RIGHT" }
      ],
      "inputsInline": true,
      "output": null,
      "colour": 260,
      "tooltip": "Binary expression operator.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_unary",
      "message0": "%1 %2",
      "args0": [
        { "type": "field_dropdown", "name": "OP", "options": [["not", "not"], ["-", "-"]] },
        { "type": "input_value", "name": "VALUE" }
      ],
      "inputsInline": true,
      "output": null,
      "colour": 260,
      "tooltip": "Unary expression operator.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_call1",
      "message0": "%1 ( %2 )",
      "args0": [
        { "type": "field_dropdown", "name": "FUNC", "options": [["count", "count"], ["int", "int"], ["float", "float"], ["string", "string"], ["bool", "bool"], ["json", "json"], ["round", "round"]] },
        { "type": "input_value", "name": "ARG" }
      ],
      "inputsInline": true,
      "output": null,
      "colour": 260,
      "tooltip": "Call a one-argument built-in function.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_env",
      "message0": "environment %1",
      "args0": [
        { "type": "field_dropdown", "name": "KEY", "options": [["UTC time", "time.utc"], ["local time", "time.local"], ["timestamp", "time.timestamp"], ["process CPU %", "process.cpuPercent"], ["process memory bytes", "process.memoryBytes"], ["process thread count", "process.threadCount"], ["system memory load %", "system.memoryLoadPercent"], ["system available memory bytes", "system.memoryAvailableBytes"], ["processor count", "system.processorCount"], ["OS", "system.os"], ["architecture", "system.arch"], ["TCP listener count", "tcp.listenerCount"]] }
      ],
      "output": null,
      "colour": 200,
      "tooltip": "Read runtime environment data.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_tcp_listening",
      "message0": "TCP port %1 listening on host %2",
      "args0": [
        { "type": "field_number", "name": "PORT", "value": 9000, "min": 0, "max": 65535, "precision": 1 },
        { "type": "field_input", "name": "HOST", "text": "" }
      ],
      "output": null,
      "colour": 200,
      "tooltip": "Return true when a local TCP listener exists on the port. Host is optional; leave blank to match any local address.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_env_snapshot",
      "message0": "environment snapshot",
      "output": null,
      "colour": 200,
      "tooltip": "Return an object containing time, process, system, and TCP listener count fields.",
      "helpUrl": ""
    },
    {
      "type": "osc_expr_raw",
      "message0": "raw expression %1",
      "args0": [
        { "type": "field_input", "name": "SOURCE", "text": "arg(0)" }
      ],
      "output": null,
      "colour": 260,
      "tooltip": "Use a raw OSCControl expression.",
      "helpUrl": ""
    },
    {
      "type": "osc_log_expr",
      "message0": "log %1 %2",
      "args0": [
        { "type": "field_dropdown", "name": "LEVEL", "options": [["info", "info"], ["warn", "warn"], ["error", "error"]] },
        { "type": "input_value", "name": "VALUE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 60,
      "tooltip": "Write a runtime log entry from an expression.",
      "helpUrl": ""
    },
    {
      "type": "osc_store_expr",
      "message0": "store %1 = %2",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "count" },
        { "type": "input_value", "name": "VALUE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 60,
      "tooltip": "Update a state variable from an expression.",
      "helpUrl": ""
    },
    {
      "type": "osc_if_expr",
      "message0": "if %1 %2 %3",
      "args0": [
        { "type": "input_value", "name": "CONDITION" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "THEN" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Run nested steps when an expression is true.",
      "helpUrl": ""
    },
    {
      "type": "osc_if_else_expr",
      "message0": "if %1 %2 then %3 else %4",
      "args0": [
        { "type": "input_value", "name": "CONDITION" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "THEN" },
        { "type": "input_statement", "name": "ELSE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Run one of two nested step lists based on an expression.",
      "helpUrl": ""
    },
    {
      "type": "osc_while_expr",
      "message0": "while %1 %2 %3",
      "args0": [
        { "type": "input_value", "name": "CONDITION" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "DO" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 120,
      "tooltip": "Repeat nested steps while an expression is true.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_input_expr",
      "message0": "VRChat input %1 = %2",
      "args0": [
        { "type": "field_input", "name": "INPUT", "text": "Jump" },
        { "type": "input_value", "name": "VALUE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Set a VRChat input from an expression.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_param_expr",
      "message0": "VRChat param %1 = %2",
      "args0": [
        { "type": "field_input", "name": "PARAM", "text": "CustomParam" },
        { "type": "input_value", "name": "VALUE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Set a custom VRChat avatar parameter from an expression. VRChat built-in avatar parameters are read-only.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_typing_expr",
      "message0": "VRChat typing %1",
      "args0": [
        { "type": "input_value", "name": "VALUE" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 30,
      "tooltip": "Set VRChat chatbox typing state from an expression.",
      "helpUrl": ""
    }
  ]);
}());