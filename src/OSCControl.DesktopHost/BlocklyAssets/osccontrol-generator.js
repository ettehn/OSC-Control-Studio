(function () {
  if (!window.Blockly) {
    return;
  }

  const generator = new Blockly.Generator('OSCControl');
  generator.forBlock = generator.forBlock || Object.create(null);

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
    const port = Math.max(1, Number(block.getFieldValue('PORT')) || 9000);
    return `endpoint ${name}: osc.udp {\n    mode: ${mode}\n    host: ${host}\n    port: ${port}\n    codec: osc\n}\n\n`;
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

  generator.forBlock.osc_raw_step = function (block) {
    const source = (block.getFieldValue('SOURCE') || 'stop').trim();
    return `${source}\n`;
  };

  function identifier(value, fallback) {
    const trimmed = (value || '').trim();
    return /^[A-Za-z_][A-Za-z0-9_]*$/.test(trimmed) ? trimmed : fallback;
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

    if (/^".*"$/.test(trimmed) || /^-?\d+(\.\d+)?$/.test(trimmed) || /^[A-Za-z_][A-Za-z0-9_]*(\(.*\))?$/.test(trimmed)) {
      return trimmed;
    }

    return stringLiteral(trimmed);
  }

  window.OSCControlBlocklyGenerator = generator;
}());