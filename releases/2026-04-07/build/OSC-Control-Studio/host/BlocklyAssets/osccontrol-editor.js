(function () {
  const templates = {
    'startup-log': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="osc_startup_rule" x="32" y="32">
          <statement name="STACK">
            <block type="osc_log">
              <field name="LEVEL">info</field>
              <field name="VALUE">&quot;ready&quot;</field>
            </block>
          </statement>
        </block>
      </xml>`,
    'environment-log': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="osc_startup_rule" x="32" y="32">
          <statement name="STACK">
            <block type="osc_log">
              <field name="LEVEL">info</field>
              <field name="VALUE">concat(&quot;time=&quot;, env(&quot;time.local&quot;))</field>
              <next>
                <block type="osc_log">
                  <field name="LEVEL">info</field>
                  <field name="VALUE">concat(&quot;cpu=&quot;, string(env(&quot;process.cpuPercent&quot;)), &quot;%&quot;)</field>
                  <next>
                    <block type="osc_log">
                      <field name="LEVEL">info</field>
                      <field name="VALUE">concat(&quot;memory=&quot;, string(env(&quot;system.memoryLoadPercent&quot;)), &quot;%&quot;)</field>
                      <next>
                        <block type="osc_log">
                          <field name="LEVEL">info</field>
                          <field name="VALUE">concat(&quot;tcp9000=&quot;, string(env(&quot;tcp.listening&quot;, 9000)))</field>
                        </block>
                      </next>
                    </block>
                  </next>
                </block>
              </next>
            </block>
          </statement>
        </block>
      </xml>`,
    'osc-forward': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="osc_endpoint_udp" x="32" y="32">
          <field name="NAME">oscIn</field>
          <field name="MODE">input</field>
          <field name="HOST">127.0.0.1</field>
          <field name="PORT">9000</field>
          <next>
            <block type="osc_endpoint_udp">
              <field name="NAME">oscOut</field>
              <field name="MODE">output</field>
              <field name="HOST">127.0.0.1</field>
              <field name="PORT">9001</field>
              <next>
                <block type="osc_receive_rule">
                  <field name="ENDPOINT">oscIn</field>
                  <field name="ADDRESS">/note/on</field>
                  <statement name="STACK">
                    <block type="osc_send_simple">
                      <field name="TARGET">oscOut</field>
                      <field name="ADDRESS">/hello</field>
                      <field name="ARGS">[[arg(0)]]</field>
                    </block>
                  </statement>
                </block>
              </next>
            </block>
          </next>
        </block>
      </xml>`,
    'vrchat-chatbox': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="vrchat_endpoint" x="32" y="32">
          <field name="HOST">127.0.0.1</field>
          <field name="INPUT_PORT">9000</field>
          <field name="OUTPUT_PORT">9001</field>
          <next>
            <block type="osc_startup_rule">
              <statement name="STACK">
                <block type="vrchat_chat">
                  <field name="TEXT">&quot;Hello from OSCControl&quot;</field>
                  <field name="SEND">TRUE</field>
                  <field name="NOTIFY">FALSE</field>
                </block>
              </statement>
            </block>
          </next>
        </block>
      </xml>`,
    'vrchat-param-input': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="vrchat_endpoint" x="32" y="32">
          <field name="HOST">127.0.0.1</field>
          <field name="INPUT_PORT">9000</field>
          <field name="OUTPUT_PORT">9001</field>
          <next>
            <block type="vrchat_builtin_param_rule">
              <field name="PARAM">GestureLeft</field>
              <statement name="STACK">
                <block type="vrchat_input">
                  <field name="INPUT">Jump</field>
                  <field name="VALUE">arg(0)</field>
                </block>
              </statement>
            </block>
          </next>
        </block>
      </xml>`,
    'stateful-loop': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="osc_variable" x="32" y="32">
          <field name="NAME">count</field>
          <field name="VALUE">0</field>
          <next>
            <block type="osc_startup_rule">
              <statement name="STACK">
                <block type="osc_while">
                  <field name="CONDITION">count &lt; 3</field>
                  <statement name="DO">
                    <block type="osc_log">
                      <field name="LEVEL">info</field>
                      <field name="VALUE">count</field>
                      <next>
                        <block type="osc_store">
                          <field name="NAME">count</field>
                          <field name="VALUE">count + 1</field>
                        </block>
                      </next>
                    </block>
                  </statement>
                </block>
              </statement>
            </block>
          </next>
        </block>
      </xml>`,
    'ws-forward': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="osc_endpoint_udp" x="32" y="32">
          <field name="NAME">oscIn</field>
          <field name="MODE">input</field>
          <field name="HOST">127.0.0.1</field>
          <field name="PORT">9000</field>
          <next>
            <block type="osc_endpoint_ws">
              <field name="TRANSPORT">ws.client</field>
              <field name="NAME">wsOut</field>
              <field name="MODE">output</field>
              <field name="HOST">127.0.0.1</field>
              <field name="PORT">8080</field>
              <field name="PATH">/osc</field>
              <field name="CODEC">json</field>
              <next>
                <block type="osc_receive_rule_when">
                  <field name="ENDPOINT">oscIn</field>
                  <field name="ADDRESS">/note/on</field>
                  <field name="CONDITION">arg(0) &gt; 0</field>
                  <statement name="STACK">
                    <block type="osc_send_body">
                      <field name="TARGET">wsOut</field>
                      <field name="ADDRESS">/note/on</field>
                      <field name="BODY">{value: arg(0)}</field>
                    </block>
                  </statement>
                </block>
              </next>
            </block>
          </next>
        </block>
      </xml>`,
    'ws-duplex': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="ws_server_endpoint" x="32" y="32">
          <field name="NAME">wsServer</field>
          <field name="MODE">duplex</field>
          <field name="HOST">127.0.0.1</field>
          <field name="PORT">8081</field>
          <field name="PATH">/control</field>
          <field name="CODEC">json</field>
          <next>
            <block type="ws_receive_rule">
              <field name="ENDPOINT">wsServer</field>
              <field name="ADDRESS">/ping</field>
              <statement name="STACK">
                <block type="ws_send_json">
                  <field name="TARGET">wsServer</field>
                  <field name="ADDRESS">/pong</field>
                  <field name="BODY">{value: body(&quot;value&quot;)}</field>
                </block>
              </statement>
            </block>
          </next>
        </block>
      </xml>`,
    'vrchat-avatar-typing': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="vrchat_endpoint" x="32" y="32">
          <field name="HOST">127.0.0.1</field>
          <field name="INPUT_PORT">9000</field>
          <field name="OUTPUT_PORT">9001</field>
          <next>
            <block type="vrchat_avatar_rule">
              <statement name="STACK">
                <block type="vrchat_typing">
                  <field name="VALUE">true</field>
                  <next>
                    <block type="vrchat_chat">
                      <field name="TEXT">&quot;Avatar changed&quot;</field>
                      <field name="SEND">TRUE</field>
                      <field name="NOTIFY">FALSE</field>
                      <next>
                        <block type="vrchat_typing">
                          <field name="VALUE">false</field>
                        </block>
                      </next>
                    </block>
                  </next>
                </block>
              </statement>
            </block>
          </next>
        </block>
      </xml>`
  };

  const state = {
    workspace: null,
    source: '',
    postTimer: 0,
    suppressEvents: false
  };

  window.addEventListener('load', () => {
    try {
      initializeEditor();
    } catch (error) {
      reportDiagnostic('startup', 'Blockly editor initialization failed', error);
    }
  });

  function initializeEditor() {
    const startupTime = performance.now();
    const status = document.getElementById('status');
    const postButton = document.getElementById('postButton');
    const loadTemplateButton = document.getElementById('loadTemplateButton');
    setStatus('Initializing Blockly...');
    postButton.addEventListener('click', () => postToHost('manual'));
    loadTemplateButton.addEventListener('click', loadSelectedTemplate);

    if (!window.Blockly || !window.OSCControlBlocklyGenerator) {
      status.textContent = 'Blockly vendor files are missing. Add them under BlocklyAssets/vendor/blockly/.';
      document.getElementById('scriptPreview').textContent = 'Blockly is not loaded yet. The OSCControl scenario editor is ready, but it needs the Blockly vendor bundle before it can render blocks.';
      return;
    }

    setStatus('Injecting Blockly workspace...');
    state.workspace = Blockly.inject('blocklyDiv', {
      toolbox: document.getElementById('toolbox'),
      trashcan: true,
      scrollbars: true,
      sounds: false
    });

    state.workspace.addChangeListener(event => {
      const isUiEvent = typeof event.isUiEvent === 'function' ? event.isUiEvent() : event.isUiEvent;
      if (state.suppressEvents || isUiEvent) {
        return;
      }

      updatePreview();
      schedulePostToHost();
    });

    setStatus('Loading starter scenario...');
    loadTemplate('startup-log');
    setStatus(hasWebViewHost() ? 'Ready and synced to desktop' : 'Ready without WebView2 host');
    reportDiagnostic('info', `Blockly editor initialized in ${Math.round(performance.now() - startupTime)} ms`);
  }

  function setStatus(message) {
    const status = document.getElementById('status');
    if (status) {
      status.textContent = message;
    }
  }

  function reportDiagnostic(level, message, error) {
    const detail = error ? `${message}: ${formatError(error)}` : message;
    if (level !== 'info') {
      setStatus(detail);
    }

    if (hasWebViewHost()) {
      window.chrome.webview.postMessage({
        kind: 'osccontrol-blockly-diagnostic',
        level,
        message: detail
      });
    }
  }

  function formatError(error) {
    if (!error) {
      return 'unknown error';
    }

    if (error.stack) {
      return error.stack;
    }

    if (error.message) {
      return error.message;
    }

    return String(error);
  }
  function loadSelectedTemplate() {
    const select = document.getElementById('templateSelect');
    loadTemplate(select.value);
  }

  function loadTemplate(name) {
    const xmlText = templates[name] || templates['startup-log'];
    const parser = new DOMParser();
    const parsed = parser.parseFromString(xmlText, 'text/xml');
    const parseError = parsed.querySelector('parsererror');
    if (parseError) {
      throw new Error(parseError.textContent || `Template XML parse failed: ${name}`);
    }
    const xml = parsed.documentElement;

    state.suppressEvents = true;
    try {
      state.workspace.clear();
      Blockly.Xml.domToWorkspace(xml, state.workspace);
      if (typeof state.workspace.cleanUp === 'function') {
        state.workspace.cleanUp();
      }
    } finally {
      state.suppressEvents = false;
    }

    updatePreview();
    postToHost('template');
  }

  function updatePreview() {
    try {
      state.source = window.OSCControlBlocklyGenerator.workspaceToCode(state.workspace);
      document.getElementById('scriptPreview').textContent = state.source;
    } catch (error) {
      reportDiagnostic('generator', 'Failed to generate OSCControl script', error);
      state.source = '';
      document.getElementById('scriptPreview').textContent = '';
    }
  }

  function schedulePostToHost() {
    if (!hasWebViewHost()) {
      return;
    }

    window.clearTimeout(state.postTimer);
    state.postTimer = window.setTimeout(() => postToHost('auto'), 350);
  }

  function postToHost(reason) {
    const workspaceJson = serializeWorkspace();
    const message = {
      kind: 'osccontrol-blockly-generated-script',
      reason,
      source: state.source,
      workspaceJson
    };

    if (hasWebViewHost()) {
      window.chrome.webview.postMessage(message);
      document.getElementById('status').textContent = reason === 'manual' ? 'Applied to desktop' : 'Synced to desktop';
      return;
    }

    document.getElementById('status').textContent = 'No WebView2 host attached';
  }

  function hasWebViewHost() {
    return Boolean(window.chrome && window.chrome.webview);
  }

  function serializeWorkspace() {
    if (!state.workspace || !Blockly.serialization || !Blockly.serialization.workspaces) {
      return '';
    }

    return JSON.stringify(Blockly.serialization.workspaces.save(state.workspace));
  }
}());