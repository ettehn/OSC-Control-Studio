(function () {
  if (!window.Blockly) {
    return;
  }

  Blockly.defineBlocksWithJsonArray([
    {
      "type": "osc_endpoint_udp",
      "message0": "endpoint %1 mode %2 host %3 port %4",
      "args0": [
        { "type": "field_input", "name": "NAME", "text": "oscIn" },
        { "type": "field_dropdown", "name": "MODE", "options": [["input", "input"], ["output", "output"]] },
        { "type": "field_input", "name": "HOST", "text": "127.0.0.1" },
        { "type": "field_number", "name": "PORT", "value": 9000, "min": 1, "max": 65535, "precision": 1 }
      ],
      "nextStatement": null,
      "colour": 230,
      "tooltip": "Declare an OSC UDP endpoint.",
      "helpUrl": ""
    },
    {
      "type": "osc_startup_rule",
      "message0": "on startup %1 %2",
      "args0": [
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 160,
      "tooltip": "Run steps when the app starts.",
      "helpUrl": ""
    },
    {
      "type": "osc_receive_rule",
      "message0": "on receive %1 address %2 %3 %4",
      "args0": [
        { "type": "field_input", "name": "ENDPOINT", "text": "oscIn" },
        { "type": "field_input", "name": "ADDRESS", "text": "/example" },
        { "type": "input_dummy" },
        { "type": "input_statement", "name": "STACK" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 160,
      "tooltip": "Run steps when an endpoint receives a matching OSC address.",
      "helpUrl": ""
    },
    {
      "type": "osc_log",
      "message0": "log %1 %2",
      "args0": [
        { "type": "field_dropdown", "name": "LEVEL", "options": [["info", "info"], ["warn", "warn"], ["error", "error"]] },
        { "type": "field_input", "name": "VALUE", "text": "ready" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 60,
      "tooltip": "Write a runtime log entry.",
      "helpUrl": ""
    },
    {
      "type": "osc_send_simple",
      "message0": "send %1 address %2 args %3",
      "args0": [
        { "type": "field_input", "name": "TARGET", "text": "oscOut" },
        { "type": "field_input", "name": "ADDRESS", "text": "/hello" },
        { "type": "field_input", "name": "ARGS", "text": "[[1]]" }
      ],
      "previousStatement": null,
      "nextStatement": null,
      "colour": 20,
      "tooltip": "Send a simple OSC message.",
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
  ]);
}());