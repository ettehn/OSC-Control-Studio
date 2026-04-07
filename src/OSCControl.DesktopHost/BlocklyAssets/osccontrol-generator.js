(function () {
  if (!window.Blockly) {
    return;
  }

  const generator = new Blockly.Generator('OSCControl');
  generator.forBlock = generator.forBlock || Object.create(null);
  generator.INDENT = '    ';
  generator.ORDER_ATOMIC = 0;
  generator.ORDER_NONE = 99;

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

  generator.forBlock.osc_endpoint_ws = function (block) {
    const name = identifier(block.getFieldValue('NAME'), 'wsOut');
    const transport = block.getFieldValue('TRANSPORT') === 'ws.server' ? 'ws.server' : 'ws.client';
    const mode = block.getFieldValue('MODE') === 'input' ? 'input' : 'output';
    const host = stringLiteral(block.getFieldValue('HOST') || '127.0.0.1');
    const port = portNumber(block.getFieldValue('PORT'), 8080);
    const path = stringLiteral(block.getFieldValue('PATH') || '/osc');
    const codec = barewordOrString(block.getFieldValue('CODEC') || 'json');
    return `endpoint ${name}: ${transport} {\n    mode: ${mode}\n    host: ${host}\n    port: ${port}\n    path: ${path}\n    codec: ${codec}\n}\n\n`;
  };

  generator.forBlock.vrchat_endpoint = function (block) {
    const host = stringLiteral(block.getFieldValue('HOST') || '127.0.0.1');
    const inputPort = portNumber(block.getFieldValue('INPUT_PORT'), 9000);
    const outputPort = portNumber(block.getFieldValue('OUTPUT_PORT'), 9001);
    return `vrchat.endpoint {\n    host: ${host}\n    inputPort: ${inputPort}\n    outputPort: ${outputPort}\n}\n\n`;
  };

  generator.forBlock.osc_variable = function (block) {
    const name = identifier(block.getFieldValue('NAME'), 'count');
    const value = expressionOrString(block.getFieldValue('VALUE'));
    return `var ${name} = ${value}\n\n`;
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

  generator.forBlock.osc_receive_rule_when = function (block) {
    const endpoint = identifier(block.getFieldValue('ENDPOINT'), 'oscIn');
    const address = stringLiteral(block.getFieldValue('ADDRESS') || '/example');
    const condition = expression(block.getFieldValue('CONDITION'), 'true');
    const body = generator.statementToCode(block, 'STACK') || '    log info "got message"\n';
    return `on receive ${endpoint} when msg.address == ${address} and ${condition} [\n${body}]\n\n`;
  };

  generator.forBlock.vrchat_avatar_rule = function (block) {
    const body = generator.statementToCode(block, 'STACK') || '    log info "avatar changed"\n';
    return `on vrchat.avatar_change [\n${body}]\n\n`;
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

  generator.forBlock.osc_store = function (block) {
    const name = identifier(block.getFieldValue('NAME'), 'count');
    const value = expression(block.getFieldValue('VALUE'), 'null');
    return `store ${name} = ${value}\n`;
  };

  generator.forBlock.osc_send_simple = function (block) {
    const target = identifier(block.getFieldValue('TARGET'), 'oscOut');
    const address = stringLiteral(block.getFieldValue('ADDRESS') || '/hello');
    const args = argsList(block.getFieldValue('ARGS') || '[[1]]');
    return `send ${target} {\n    address: ${address}\n    args: ${args}\n}\n`;
  };

  generator.forBlock.osc_send_body = function (block) {
    const target = identifier(block.getFieldValue('TARGET'), 'wsOut');
    const address = stringLiteral(block.getFieldValue('ADDRESS') || '/hello');
    const body = expressionOrString(block.getFieldValue('BODY'));
    return `send ${target} {\n    address: ${address}\n    body: ${body}\n}\n`;
  };

  generator.forBlock.osc_if = function (block) {
    const condition = expression(block.getFieldValue('CONDITION'), 'true');
    const body = generator.statementToCode(block, 'THEN') || '    log info "then"\n';
    return `if ${condition} [\n${body}]\n`;
  };

  generator.forBlock.osc_if_else = function (block) {
    const condition = expression(block.getFieldValue('CONDITION'), 'true');
    const thenBody = generator.statementToCode(block, 'THEN') || '    log info "then"\n';
    const elseBody = generator.statementToCode(block, 'ELSE') || '    log info "else"\n';
    return `if ${condition} [\n${thenBody}]\nelse [\n${elseBody}]\n`;
  };

  generator.forBlock.osc_while = function (block) {
    const condition = expression(block.getFieldValue('CONDITION'), 'true');
    const body = generator.statementToCode(block, 'DO') || '    break\n';
    return `while ${condition} [\n${body}]\n`;
  };

  generator.forBlock.osc_stop = function () {
    return 'stop\n';
  };

  generator.forBlock.osc_break = function () {
    return 'break\n';
  };

  generator.forBlock.osc_continue = function () {
    return 'continue\n';
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

  generator.forBlock.vrchat_typing = function (block) {
    const value = block.getFieldValue('VALUE') === 'false' ? 'false' : 'true';
    return `vrchat.typing ${value}\n`;
  };

  generator.forBlock.osc_raw_step = function (block) {
    const source = (block.getFieldValue('SOURCE') || 'stop').trim();
    return `${source}\n`;
  };

  generator.forBlock.osc_expr_number = function (block) {
    return [String(Number(block.getFieldValue('VALUE')) || 0), generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_string = function (block) {
    return [stringLiteral(block.getFieldValue('VALUE') || ''), generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_boolean = function (block) {
    return [block.getFieldValue('VALUE') === 'false' ? 'false' : 'true', generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_identifier = function (block) {
    return [identifier(block.getFieldValue('NAME'), 'count'), generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_arg = function (block) {
    const index = Math.max(0, Number(block.getFieldValue('INDEX')) || 0);
    return [`arg(${index})`, generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_msg_field = function (block) {
    const field = identifier(block.getFieldValue('FIELD'), 'address');
    return [`msg.${field}`, generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_binary = function (block) {
    const left = generator.valueToCode(block, 'LEFT', generator.ORDER_NONE) || '0';
    const right = generator.valueToCode(block, 'RIGHT', generator.ORDER_NONE) || '0';
    const op = block.getFieldValue('OP') || '+';
    return [`(${left} ${op} ${right})`, generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_unary = function (block) {
    const value = generator.valueToCode(block, 'VALUE', generator.ORDER_NONE) || 'false';
    const op = block.getFieldValue('OP') === '-' ? '-' : 'not';
    return [`(${op} ${value})`, generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_call1 = function (block) {
    const func = identifier(block.getFieldValue('FUNC'), 'count');
    const arg = generator.valueToCode(block, 'ARG', generator.ORDER_NONE) || 'null';
    return [`${func}(${arg})`, generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_expr_raw = function (block) {
    return [expression(block.getFieldValue('SOURCE'), 'arg(0)'), generator.ORDER_ATOMIC];
  };

  generator.forBlock.osc_log_expr = function (block) {
    const level = identifier(block.getFieldValue('LEVEL'), 'info');
    const value = generator.valueToCode(block, 'VALUE', generator.ORDER_NONE) || '""';
    return `log ${level} ${value}\n`;
  };

  generator.forBlock.osc_store_expr = function (block) {
    const name = identifier(block.getFieldValue('NAME'), 'count');
    const value = generator.valueToCode(block, 'VALUE', generator.ORDER_NONE) || 'null';
    return `store ${name} = ${value}\n`;
  };

  generator.forBlock.osc_if_expr = function (block) {
    const condition = generator.valueToCode(block, 'CONDITION', generator.ORDER_NONE) || 'true';
    const body = generator.statementToCode(block, 'THEN') || '    log info "then"\n';
    return `if ${condition} [\n${body}]\n`;
  };

  generator.forBlock.osc_if_else_expr = function (block) {
    const condition = generator.valueToCode(block, 'CONDITION', generator.ORDER_NONE) || 'true';
    const thenBody = generator.statementToCode(block, 'THEN') || '    log info "then"\n';
    const elseBody = generator.statementToCode(block, 'ELSE') || '    log info "else"\n';
    return `if ${condition} [\n${thenBody}]\nelse [\n${elseBody}]\n`;
  };

  generator.forBlock.osc_while_expr = function (block) {
    const condition = generator.valueToCode(block, 'CONDITION', generator.ORDER_NONE) || 'true';
    const body = generator.statementToCode(block, 'DO') || '    break\n';
    return `while ${condition} [\n${body}]\n`;
  };

  generator.forBlock.vrchat_input_expr = function (block) {
    const input = identifier(block.getFieldValue('INPUT'), 'Jump');
    const value = generator.valueToCode(block, 'VALUE', generator.ORDER_NONE) || '1';
    return `vrchat.input ${input} = ${value}\n`;
  };

  generator.forBlock.vrchat_param_expr = function (block) {
    const parameter = identifier(block.getFieldValue('PARAM'), 'GestureLeft');
    const value = generator.valueToCode(block, 'VALUE', generator.ORDER_NONE) || '0';
    return `vrchat.param ${parameter} = ${value}\n`;
  };

  generator.forBlock.vrchat_typing_expr = function (block) {
    const value = generator.valueToCode(block, 'VALUE', generator.ORDER_NONE) || 'true';
    return `vrchat.typing ${value}\n`;
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

  function barewordOrString(value) {
    const trimmed = (value || '').trim();
    return /^[A-Za-z_][A-Za-z0-9_.-]*$/.test(trimmed) ? trimmed : stringLiteral(trimmed || 'json');
  }

  function argsList(value) {
    const trimmed = (value || '').trim();
    if (!trimmed) {
      return '[[]]';
    }

    return trimmed.startsWith('[[') ? trimmed : `[[${trimmed}]]`;
  }

  function expression(value, fallback) {
    const trimmed = (value || '').trim();
    return trimmed || fallback;
  }

  function expressionOrString(value) {
    const trimmed = (value || '').trim();
    if (!trimmed) {
      return '""';
    }

    if (/^".*"$/.test(trimmed) || /^(true|false|null)$/i.test(trimmed) || /^-?\d+(\.\d+)?$/.test(trimmed) || /^[A-Za-z_][A-Za-z0-9_]*(\(.*\))?$/.test(trimmed) || /^[{\[(]/.test(trimmed) || /[=<>!+\-*/.:]/.test(trimmed)) {
      return trimmed;
    }

    return stringLiteral(trimmed);
  }

  window.OSCControlBlocklyGenerator = generator;
}());