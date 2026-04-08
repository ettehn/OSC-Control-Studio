(function () {
  if (!window.Blockly) {
    return;
  }

  const zh = /^zh\\b/i.test((document.documentElement.lang || navigator.language || '').toLowerCase());
  const blockDefinitions = [
    {
      "type": "osc_endpoint_udp",
      "message0": "OSC UDP endpoint %1 mode %2 host %3 port %4",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "oscIn" },
        { "type": "field_dropdown", "name": "MODE", "options": [["input", "input"], ["output", "output"], ["duplex", "duplex"]] },
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "PORT", "value": 9000, "min": 1, "max": 65535, "precision": 1 }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
      "colour": 300,
      "tooltip": "Declare a top-level state variable from an expression.",
      "helpUrl": ""
    },
    {
      "type": "osc_startup_rule",
      "message0": "when app starts %1 %2",
      "args0": [
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
      "colour": 190,
      "tooltip": "Run steps when a WebSocket endpoint receives a matching address and condition.",
      "helpUrl": ""
    },
    {
      "type": "vrchat_avatar_rule",
      "message0": "when VRChat avatar changes %1 %2",
      "args0": [
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
        { "type": "input_statement", "name": "THEN", "check": "Step" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
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
        { "type": "input_statement", "name": "THEN", "check": "Step" },
        { "type": "input_statement", "name": "ELSE", "check": "Step" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
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
        { "type": "input_statement", "name": "DO", "check": "Step" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 120,
      "tooltip": "Repeat nested steps while a condition is true.",
      "helpUrl": ""
    },
    {
      "type": "osc_stop",
      "message0": "stop rule",
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 120,
      "tooltip": "Stop the current rule.",
      "helpUrl": ""
    },
    {
      "type": "osc_break",
      "message0": "break loop",
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 120,
      "tooltip": "Break out of the current loop.",
      "helpUrl": ""
    },
    {
      "type": "osc_continue",
      "message0": "continue loop",
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 30,
      "tooltip": "Set VRChat chatbox typing state.",
      "helpUrl": ""
    },
    {
      "type": "dglab_socket_endpoint",
      "message0": "DG-LAB socket endpoint %1 mode %2 host %3 port %4 path %5 secure %6",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "dglab" },
        { "type": "field_dropdown", "name": "MODE", "options": [["duplex", "duplex"], ["output", "output"], ["input", "input"]] },
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "PORT", "value": 5678, "min": 1, "max": 65535, "precision": 1 },
        { "type": "field_input", "name": "PATH", "text": "/" },
        { "type": "field_checkbox", "name": "SECURE", "checked": false }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
      "colour": 20,
      "tooltip": "Declare a DG-LAB socket endpoint that uses the built-in dglab.socket transport.",
      "helpUrl": ""
    },
    {
      "type": "dglab_send_strength",
      "message0": "DG-LAB send strength to %1 channel %2 action %3 value %4",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "dglab" },
        { "type": "field_dropdown", "name": "CHANNEL", "options": [["A", "A"], ["B", "B"]] },
        { "type": "field_dropdown", "name": "ACTION", "options": [["set", "set"], ["increase", "increase"], ["decrease", "decrease"]] },
        { "type": "field_number", "name": "VALUE", "value": 50, "min": 0, "max": 200, "precision": 1 }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 20,
      "tooltip": "Send a DG-LAB strength command.",
      "helpUrl": ""
    },
    {
      "type": "dglab_clear_queue",
      "message0": "DG-LAB clear pulse queue on %1 channel %2",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "dglab" },
        { "type": "field_dropdown", "name": "CHANNEL", "options": [["A", "A"], ["B", "B"]] }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 20,
      "tooltip": "Clear the queued DG-LAB pulse data for one channel.",
      "helpUrl": ""
    },
    {
      "type": "dglab_raw_command",
      "message0": "DG-LAB advanced raw command to %1 text %2",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "dglab" },
        { "type": "field_input", "name": "COMMAND", "text": "strength-1+2+50" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 20,
      "tooltip": "Send a DG-LAB raw command through the explicit advanced path with allowUnsafeRaw.",
      "helpUrl": ""
    },
    {
      "type": "dglab_send_pulse",
      "message0": "DG-LAB send pulse to %1 channel %2 payload %3",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "dglab" },
        { "type": "field_dropdown", "name": "CHANNEL", "options": [["A", "A"], ["B", "B"]] },
        { "type": "field_input", "name": "PAYLOAD", "text": "[[10,20,30]]" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 20,
      "tooltip": "Send a DG-LAB pulse queue payload to one channel.",
      "helpUrl": ""
    },
    {
      "type": "dglab_bind_rule",
      "message0": "when DG-LAB %1 bind status %2 %3 %4",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "dglab" },
        { "type": "field_input", "name": "STATUS", "text": "200" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
      "colour": 190,
      "tooltip": "Run steps when a DG-LAB bind event arrives. Leave status empty to match any bind event.",
      "helpUrl": ""
    },
    {
      "type": "dglab_feedback_rule",
      "message0": "when DG-LAB %1 feedback and %2 %3 %4",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "dglab" },
        { "type": "field_input", "name": "CONDITION", "text": "arg(0) == 0" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
      "colour": 190,
      "tooltip": "Run steps when a DG-LAB feedback event arrives and the condition matches.",
      "helpUrl": ""
    },
    {
      "type": "dglab_strength_rule",
      "message0": "when DG-LAB %1 strength and %2 %3 %4",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "dglab" },
        { "type": "field_input", "name": "CONDITION", "text": "true" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK", "check": "Step" }
      ],
      "previousStatement": "TopLevel",
      "nextStatement": "TopLevel",
      "colour": 190,
      "tooltip": "Run steps when a DG-LAB strength event arrives and the condition matches.",
      "helpUrl": ""
    },    {
      "type": "osc_raw_step",
      "message0": "raw step %1",
      "args0": [
        { "type": "field_input", "name": "SOURCE", "text": "stop" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
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
        { "type": "field_dropdown", "name": "KEY", "options": [["UTC time", "time.utc"], ["local time", "time.local"], ["time-short", "time-short"], ["date", "date"], ["date-short", "date-short"], ["timestamp", "time.timestamp"], ["process CPU %", "process.cpuPercent"], ["process memory bytes", "process.memoryBytes"], ["process thread count", "process.threadCount"], ["system memory load %", "system.memoryLoadPercent"], ["system available memory bytes", "system.memoryAvailableBytes"], ["processor count", "system.processorCount"], ["OS", "system.os"], ["architecture", "system.arch"], ["TCP listener count", "tcp.listenerCount"]] }
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
        { "type": "input_statement", "name": "THEN", "check": "Step" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
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
        { "type": "input_statement", "name": "THEN", "check": "Step" },
        { "type": "input_statement", "name": "ELSE", "check": "Step" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
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
        { "type": "input_statement", "name": "DO", "check": "Step" }
      ],
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
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
      "previousStatement": "Step",
      "nextStatement": "Step",
      "colour": 30,
      "tooltip": "Set VRChat chatbox typing state from an expression.",
      "helpUrl": ""
    }
  ];

  if (zh) {
    localizeBlockDefinitions(blockDefinitions);
  }

  Blockly.defineBlocksWithJsonArray(blockDefinitions);

  function localizeBlockDefinitions(blocks) {
    const translations = {
      osc_endpoint_udp: { message0: 'OSC UDP ?? %1 ?? %2 ?? %3 ?? %4', tooltip: '???? OSC UDP ???' },
      osc_endpoint_ws: { message0: 'WebSocket %1 ?? %2 ?? %3 ?? %4 ?? %5 ?? %6 ?? %7', tooltip: '???? WebSocket ???' },
      ws_client_endpoint: { message0: 'WebSocket ??? %1 ?? %2 ?? %3 ?? %4 ?? %5 ?? %6', tooltip: '???? WebSocket ???????????????????????' },
      ws_server_endpoint: { message0: 'WebSocket ??? %1 ?? %2 ?? %3 ?? %4 ?? %5 ?? %6', tooltip: '???? WebSocket ?????????????????????????????????' },
      vrchat_endpoint: { message0: 'VRChat ?? ?? %1 ?? %2 ?? %3', tooltip: '???? VRChat OSC ???' },
      osc_variable: { message0: '???? %1 = %2', tooltip: '???????????' },
      osc_variable_expr: { message0: '???? %1 = %2', tooltip: '???????????????' },
      osc_startup_rule: { message0: '?????? %1 %2', tooltip: '???????????' },
      osc_receive_rule: { message0: '? %1 ???? %2 ? %3 %4', tooltip: '????????? OSC ????????' },
      osc_receive_rule_when: { message0: '? %1 ???? %2 ? %3 ? %4 %5', tooltip: '?????????????????????' },
      ws_receive_rule: { message0: '? WebSocket %1 ???? %2 ? %3 %4', tooltip: '? WebSocket ?????????? JSON envelope ??????' },
      ws_receive_rule_when: { message0: '? WebSocket %1 ???? %2 ? %3 ? %4 %5', tooltip: '? WebSocket ????????????????????' },
      vrchat_avatar_rule: { message0: '? VRChat Avatar ??? %1 %2', tooltip: '? VRChat ?? Avatar ????????' },
      vrchat_builtin_param_rule: { message0: '? VRChat ?????? %1 ??? %2 %3', tooltip: '?? VRChat ?? Avatar ???????? VRChat ????????? OSCControl ???' },
      vrchat_param_rule: { message0: '? VRChat ????? %1 ??? %2 %3', tooltip: '? VRChat ??? Avatar ????????????????????????' },
      osc_log: { message0: '???? ??/??? %1 %2', tooltip: '???????????????? DSL?' },
      osc_store: { message0: '?? %1 = %2', tooltip: '?????????' },
      osc_send_simple: { message0: '?? OSC args ? %1 ?? %2 ?? %3', tooltip: '???? OSC args ???' },
      osc_send_body: { message0: '?? body ? %1 ?? %2 body %3', tooltip: '??????? body?' },
      ws_send_json: { message0: '?? WebSocket JSON ? %1 ?? %2 body %3', tooltip: '???? JSON WebSocket envelope??? ws.server ????????????? ws.client ????????' },
      ws_send_text: { message0: '?? WebSocket ??? %1 ?? %2', tooltip: '???? WebSocket body??????? DSL?????????? text codec ???' },
      ws_send_json_expr: { message0: '?? WebSocket JSON ? %1 ?? %2 body %3', tooltip: '???????? body ? JSON WebSocket envelope?' },
      osc_if: { message0: '?? %1 %2 %3', tooltip: '????????????' },
      osc_if_else: { message0: '?? %1 %2 ? %3 ?? %4', tooltip: '???????????????????' },
      osc_while: { message0: '? %1 ??? %2 %3', tooltip: '???????????????' },
      osc_stop: { message0: '????', tooltip: '???????' },
      osc_break: { message0: '????', tooltip: '???????' },
      osc_continue: { message0: '????', tooltip: '?????????????' },
      vrchat_chat: { message0: 'VRChat ??? ??/??? %1 ?? %2 ?? %3', tooltip: '? VRChat ?????????????????? DSL?' },
      vrchat_input: { message0: 'VRChat ?? %1 = %2', tooltip: '???? VRChat ????' },
      vrchat_param: { message0: 'VRChat ?? %1 = %2', tooltip: '???? VRChat ??? Avatar ???VRChat ?? Avatar ???????' },
      vrchat_typing: { message0: 'VRChat typing %1', tooltip: '?? VRChat ??? typing ???' },
      dglab_socket_endpoint: { message0: 'DG-LAB socket ?? %1 ?? %2 ?? %3 ?? %4 ?? %5 secure %6', tooltip: '???????? dglab.socket transport ? DG-LAB socket ???' },
      dglab_send_strength: { message0: 'DG-LAB ????? %1 ?? %2 ?? %3 ?? %4', tooltip: '???? DG-LAB ?????' },
      dglab_clear_queue: { message0: 'DG-LAB ?????? ?? %1 ?? %2', tooltip: '?????????? DG-LAB pulse ???' },
      dglab_send_pulse: { message0: 'DG-LAB ?? pulse ? %1 ?? %2 ?? %3', tooltip: '???? DG-LAB pulse ?????' },
      dglab_bind_rule: { message0: '? DG-LAB bind ?? %1 ?? %2 ? %3 %4', tooltip: '???? DG-LAB bind ????????' },
      dglab_feedback_rule: { message0: '? DG-LAB feedback ?? %1 ? %2 ? %3 %4', tooltip: '???? DG-LAB feedback ?????????????' },
      dglab_strength_rule: { message0: '? DG-LAB strength ?? %1 ? %2 ? %3 %4', tooltip: '???? DG-LAB strength ?????????????' },
      dglab_raw_command: { message0: 'DG-LAB ?? raw ??? %1 ?? %2', tooltip: '?? allowUnsafeRaw ????????? DG-LAB ?????' },
      osc_raw_step: { message0: '???? %1', tooltip: '? Blockly ?????????????? OSCControl ???' },
      osc_expr_number: { message0: '?? %1', tooltip: '??????' },
      osc_expr_string: { message0: '?? %1', tooltip: '???????' },
      osc_expr_boolean: { message0: '?? %1', tooltip: '??????' },
      osc_expr_identifier: { message0: '???? %1', tooltip: '????????????' },
      osc_expr_variable: { message0: '???? %1', tooltip: '???????????????' },
      osc_expr_arg: { message0: '?? %1', tooltip: '??????? OSC ???' },
      osc_expr_msg_field: { message0: '?? %1', tooltip: '???????' },
      osc_expr_binary: { message0: '%1 %2 %3', tooltip: '?????????' },
      osc_expr_unary: { message0: '%1 %2', tooltip: '?????????' },
      osc_expr_call1: { message0: '%1 ( %2 )', tooltip: '????????????' },
      osc_expr_env: { message0: '?? %1', tooltip: '??????????' },
      osc_expr_tcp_listening: { message0: 'TCP ?? %1 ??? %2 ???', tooltip: '??? TCP ??????? true???????????????????' },
      osc_expr_env_snapshot: { message0: '????', tooltip: '??????????????? TCP ??????????' },
      osc_expr_raw: { message0: '????? %1', tooltip: '???? OSCControl ????' },
      osc_log_expr: { message0: '???? %1 %2', tooltip: '??????????????' },
      osc_store_expr: { message0: '?? %1 = %2', tooltip: '?????????????' },
      osc_if_expr: { message0: '?? %1 %2 %3', tooltip: '??????????????' },
      osc_if_else_expr: { message0: '?? %1 %2 ? %3 ?? %4', tooltip: '????????????????????' },
      osc_while_expr: { message0: '? %1 ??? %2 %3', tooltip: '????????????????' },
      vrchat_input_expr: { message0: 'VRChat ?? %1 = %2', tooltip: '???????? VRChat ????' },
      vrchat_param_expr: { message0: 'VRChat ?? %1 = %2', tooltip: '???????? VRChat ??? Avatar ???VRChat ?? Avatar ???????' },
      vrchat_typing_expr: { message0: 'VRChat typing %1', tooltip: '?????? VRChat ??? typing ???' }
    };

    const optionTranslations = {
      osc_endpoint_udp: { MODE: { input: '??', output: '??', duplex: '??' } },
      osc_endpoint_ws: { TRANSPORT: { 'ws.client': '???', 'ws.server': '???' }, MODE: { input: '??', output: '??', duplex: '??' } },
      ws_client_endpoint: { MODE: { duplex: '??', input: '??', output: '??' }, CODEC: { json: 'json', text: '??', bytes: '??' } },
      ws_server_endpoint: { MODE: { duplex: '??', input: '??', output: '??' }, CODEC: { json: 'json', text: '??', bytes: '??' } },
      osc_log: { LEVEL: { info: '??', warn: '??', error: '??' } },
      osc_log_expr: { LEVEL: { info: '??', warn: '??', error: '??' } },
      dglab_send_strength: { ACTION: { set: '??', increase: '??', decrease: '??' } },
      dglab_bind_rule: { STATUS: { any: '??', targetId: 'targetId', '200': '200' } },
      osc_expr_env: { KEY: { 'time.utc': 'UTC ??', 'time.local': '????', 'time-short': 'time-short', date: 'date', 'date-short': 'date-short', 'time.timestamp': 'timestamp', 'process.cpuPercent': '?? CPU %', 'process.memoryBytes': '???????', 'process.threadCount': '?????', 'system.memoryLoadPercent': '?????? %', 'system.memoryAvailableBytes': '?????????', 'system.processorCount': '?????', 'system.os': '????', 'system.arch': '??', 'tcp.listenerCount': 'TCP ????' } }
    };

    for (const block of blocks) {
      const translation = translations[block.type];
      if (translation) {
        if (translation.message0) {
          block.message0 = translation.message0;
        }
        if (translation.tooltip) {
          block.tooltip = translation.tooltip;
        }
      }

      const fieldTranslations = optionTranslations[block.type];
      if (!fieldTranslations || !Array.isArray(block.args0)) {
        continue;
      }

      for (const arg of block.args0) {
        const options = fieldTranslations[arg.name];
        if (!options || !Array.isArray(arg.options)) {
          continue;
        }

        arg.options = arg.options.map(([label, value]) => [options[value] || label, value]);
      }
    }
  }
}());
