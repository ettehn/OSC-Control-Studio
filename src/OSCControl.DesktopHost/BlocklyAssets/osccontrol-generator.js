(function () {
  if (!window.Blockly) {
    return;
  }

  const generator = new Blockly.Generator('OSCControl');
  generator.forBlock = generator.forBlock || Object.create(null);
  generator.INDENT = '    ';

  generator.init = function () {};
  generator.finish = function (code) {
    return code.trimEnd() + '\n';
  };

  generator.scrub_ = function (block, code, thisOnly) {
    const nextBlock = block.nextConnection && block.nextConnection.targetBlock();
    const nextCode = thisOnly || !nextBlock ? '' : generator.blockToCode(nextBlock);
    return code + nextCode;
  };

  generator.forBlock.osc_endpoint_udp = function (block) {
    const name = identifier(block.getFieldValue('NAME'), 'oscIn');
    const mode = block.getFieldValue('MODE') === 'output' ? 'output' : 'input';
    const host = stringLiteral(block.getFieldValue('HOST') || '127.0.0.1');
    const port = portNumber(block.getFieldValue('PORT'), 9000);
    return `endpoint ${name}: osc.udp {\n    mode: ${mode}\n    host: ${host}\n    port: ${port}\n    codec: osc\n}\n\n`;
  };

  generator.forBlock.vrchat_endpoint = function (block) {
    const host = stringLiteral(block.getFieldValue('HOST') || '127.0.0.1');
    const inputPort = portNumber(block.getFieldValue('INPUT_PORT'), 9000);
    const outputPort = portNumber(block.getFieldValue('OUTPUT_PORT'), 9001);
    return `vrchat.endpoint {\n    host: ${host}\n    inputPort: ${inputPort}\n    outputPort: ${outputPort}\n}\n\n`;
  };

  generator.forBlock.osc_startup_rule = function (block) {
    const body = generator.statementToCode(block, 'STACK') || '    log info "ready"\n';
    return `on startup [\n${body}]\n\n`;
  };

  generator.forBlock.osc_receive_rule = function (block) {
    const endpoint = identifier(block.getFieldValue('ENDPOINT'), 'oscIn');
    const address = stringLiteral(block.getFieldValue('ADDRESS') || '/example');
    const body = generator.statementToCode(block, 'STACK') || '    log info "got message"\n';
    return `on receive ${endpoint} when msg.address == ${address} [\n${body}]\n\n`;
  };

  generator.forBlock.vrchat_param_rule = function (block) {
    const parameter = identifier(block.getFieldValue('PARAM'), 'GestureLeft');
    const body = generator.statementToCode(block, 'STACK') || '    log info arg(0)\n';
    return `on vrchat.param ${parameter} [\n${body}]\n\n`;
  };

  generator.forBlock.osc_log = function (block) {
    const level = identifier(block.getFieldValue('LEVEL'), 'info');
    const value = expressionOrString(block.getFieldValue('VALUE'));
    return `log ${level} ${value}\n`;
  };

  generator.forBlock.osc_send_simple = function (block) {
    const target = identifier(block.getFieldValue('TARGET'), 'oscOut');
    const address = stringLiteral(block.getFieldValue('ADDRESS') || '/hello');
    const args = block.getFieldValue('ARGS') || '[]';
    return `send ${target} {\n    address: ${address}\n    args: ${args}\n}\n`;
  };

  generator.forBlock.vrchat_chat = function (block) {
    const text = stringLiteral(block.getFieldValue('TEXT') || 'Hello from OSCControl');
    const send = block.getFieldValue('SEND') === 'TRUE' ? 'true' : 'false';
    const notify = block.getFieldValue('NOTIFY') === 'TRUE' ? 'true' : 'false';
    return `vrchat.chat ${text} send=${send} notify=${notify}\n`;
  };

  generator.forBlock.vrchat_input = function (block) {
    const input = identifier(block.getFieldValue('INPUT'), 'Jump');
    const value = expressionOrString(block.getFieldValue('VALUE'));
    return `vrchat.input ${input} = ${value}\n`;
  };

  generator.forBlock.vrchat_param = function (block) {
    const parameter = identifier(block.getFieldValue('PARAM'), 'GestureLeft');
    const value = expressionOrString(block.getFieldValue('VALUE'));
    return `vrchat.param ${parameter} = ${value}\n`;
  };

  generator.forBlock.osc_raw_step = function (block) {
    const source = (block.getFieldValue('SOURCE') || 'stop').trim();
    return `${source}\n`;
  };

  function identifier(value, fallback) {
    const trimmed = (value || '').trim();
    return /^[A-Za-z_][A-Za-z0-9_]*$/.test(trimmed) ? trimmed : fallback;
  }

  function portNumber(value, fallback) {
    return Math.min(65535, Math.max(1, Number(value) || fallback));
  }

  function stringLiteral(value) {
    const escaped = String(value).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
    return `"${escaped}"`;
  }

  function expressionOrString(value) {
    const trimmed = (value || '').trim();
    if (!trimmed) {
      return '""';
    }

    if (/^".*"$/.test(trimmed) || /^(true|false|null)$/i.test(trimmed) || /^-?\d+(\.\d+)?$/.test(trimmed) || /^[A-Za-z_][A-Za-z0-9_]*(\(.*\))?$/.test(trimmed)) {
      return trimmed;
    }

    return stringLiteral(trimmed);
  }

  window.OSCControlBlocklyGenerator = generator;
}());