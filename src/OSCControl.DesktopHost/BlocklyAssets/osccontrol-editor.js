(function () {
  const templates = {
    'startup-log': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="osc_startup_rule" x="32" y="32">
          <statement name="STACK">
            <block type="osc_log">
              <field name="LEVEL">info</field>
              <field name="VALUE">ready</field>
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
                  <field name="TEXT">Hello from OSCControl</field>
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
            <block type="vrchat_param_rule">
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
      </xml>`
  };

  const state = {
    workspace: null,
    source: '',
    postTimer: 0,
    suppressEvents: false
  };

  window.addEventListener('load', () => {
    const status = document.getElementById('status');
    const postButton = document.getElementById('postButton');
    const loadTemplateButton = document.getElementById('loadTemplateButton');
    postButton.addEventListener('click', () => postToHost('manual'));
    loadTemplateButton.addEventListener('click', loadSelectedTemplate);

    if (!window.Blockly || !window.OSCControlBlocklyGenerator) {
      status.textContent = 'Blockly vendor files are missing. Add them under BlocklyAssets/vendor/blockly/.';
      document.getElementById('scriptPreview').textContent = 'Blockly is not loaded yet. The OSCControl scenario editor is ready, but it needs the Blockly vendor bundle before it can render blocks.';
      return;
    }

    state.workspace = Blockly.inject('blocklyDiv', {
      toolbox: document.getElementById('toolbox'),
      trashcan: true,
      scrollbars: true,
      sounds: false
    });

    state.workspace.addChangeListener(event => {
      if (state.suppressEvents || event.isUiEvent) {
        return;
      }

      updatePreview();
      schedulePostToHost();
    });

    loadTemplate('startup-log');
    status.textContent = hasWebViewHost() ? 'Ready and synced to desktop' : 'Ready without WebView2 host';
  });

  function loadSelectedTemplate() {
    const select = document.getElementById('templateSelect');
    loadTemplate(select.value);
  }

  function loadTemplate(name) {
    const xmlText = templates[name] || templates['startup-log'];
    const parser = new DOMParser();
    const parsed = parser.parseFromString(xmlText, 'text/xml');
    const xml = parsed.documentElement;

    state.suppressEvents = true;
    try {
      state.workspace.clear();
      Blockly.Xml.domToWorkspace(xml, state.workspace);
      state.workspace.cleanUp();
    } finally {
      state.suppressEvents = false;
    }

    updatePreview();
    postToHost('template');
  }

  function updatePreview() {
    state.source = window.OSCControlBlocklyGenerator.workspaceToCode(state.workspace);
    document.getElementById('scriptPreview').textContent = state.source;
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