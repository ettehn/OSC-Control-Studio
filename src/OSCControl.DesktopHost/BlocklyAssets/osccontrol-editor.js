(function () {
  const zh = /^zh\\b/i.test((document.documentElement.lang || navigator.language || '').toLowerCase());
  const T = (en, zhHans) => zh ? zhHans : en;
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
      </xml>`,
    'dglab-strength': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="dglab_socket_endpoint" x="32" y="32">
          <field name="NAME">dglab</field>
          <field name="MODE">duplex</field>
          <field name="HOST">127.0.0.1</field>
          <field name="PORT">5678</field>
          <field name="PATH">/</field>
          <field name="SECURE">FALSE</field>
          <next>
            <block type="osc_startup_rule">
              <statement name="STACK">
                <block type="dglab_send_strength">
                  <field name="TARGET">dglab</field>
                  <field name="CHANNEL">A</field>
                  <field name="ACTION">set</field>
                  <field name="VALUE">50</field>
                  <next>
                    <block type="dglab_clear_queue">
                      <field name="TARGET">dglab</field>
                      <field name="CHANNEL">A</field>
                      <next>
                        <block type="dglab_send_pulse">
                          <field name="TARGET">dglab</field>
                          <field name="CHANNEL">A</field>
                          <field name="PAYLOAD">[[10,20,30]]</field>
                        </block>
                      </next>
                    </block>
                  </next>
                </block>
              </statement>
            </block>
          </next>
        </block>
      </xml>`,
    'dglab-events': `
      <xml xmlns="https://developers.google.com/blockly/xml">
        <block type="dglab_socket_endpoint" x="32" y="32">
          <field name="NAME">dglab</field>
          <field name="MODE">duplex</field>
          <field name="HOST">127.0.0.1</field>
          <field name="PORT">5678</field>
          <field name="PATH">/</field>
          <field name="SECURE">FALSE</field>
          <next>
            <block type="dglab_bind_rule">
              <field name="ENDPOINT">dglab</field>
              <field name="STATUS">200</field>
              <statement name="STACK">
                <block type="dglab_send_strength">
                  <field name="TARGET">dglab</field>
                  <field name="CHANNEL">A</field>
                  <field name="ACTION">set</field>
                  <field name="VALUE">30</field>
                </block>
              </statement>
              <next>
                <block type="dglab_feedback_rule">
                  <field name="ENDPOINT">dglab</field>
                  <field name="CONDITION">arg(0) == 0</field>
                  <statement name="STACK">
                    <block type="osc_log">
                      <field name="LEVEL">info</field>
                      <field name="VALUE">body("message")</field>
                    </block>
                  </statement>
                  <next>
                    <block type="dglab_strength_rule">
                      <field name="ENDPOINT">dglab</field>
                      <field name="CONDITION">true</field>
                      <statement name="STACK">
                        <block type="osc_log">
                          <field name="LEVEL">info</field>
                          <field name="VALUE">body("message")</field>
                        </block>
                      </statement>
                    </block>
                  </next>
                </block>
              </next>
            </block>
          </next>
        </block>
      </xml>`
  };

  const state = {
    workspace: null,
    source: '',
    postTimer: 0,
    suppressEvents: false,
    pendingWorkspaceJson: ''
  };

  window.addEventListener('load', () => {
    applyLocalizedChrome();
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
    setStatus(T('Initializing Blockly...', '正在初始化 Blockly...'));
    postButton.addEventListener('click', () => postToHost('manual'));
    loadTemplateButton.addEventListener('click', loadSelectedTemplate);
    if (hasWebViewHost()) {
      window.chrome.webview.addEventListener('message', handleHostMessage);
    }

    if (!window.Blockly || !window.OSCControlBlocklyGenerator) {
      status.textContent = T('Blockly vendor files are missing. Add them under BlocklyAssets/vendor/blockly/.', '缺少 Blockly vendor 文件。请将它们放到 BlocklyAssets/vendor/blockly/ 下。');
      document.getElementById('scriptPreview').textContent = T('Blockly is not loaded yet. The OSCControl scenario editor is ready, but it needs the Blockly vendor bundle before it can render blocks.', 'Blockly 还没有加载完成。OSCControl 场景编辑器已经准备好，但还需要 Blockly vendor 资源后才能渲染积木。');
      return;
    }

    setStatus(T('Injecting Blockly workspace...', '正在装载 Blockly 工作区...'));
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

    setStatus(T('Loading starter scenario...', '正在加载起始场景...'));
    loadTemplate('startup-log');
    restorePendingWorkspace();
    setStatus(hasWebViewHost() ? T('Ready and synced to desktop', '已就绪，并已同步到桌面端') : T('Ready without WebView2 host', '已就绪，但未连接 WebView2 宿主'));
    postReadyToHost();
    reportDiagnostic('info', `Blockly editor initialized in ${Math.round(performance.now() - startupTime)} ms`);
  }


  function applyLocalizedChrome() {
    document.title = T('OSCControl Blockly Editor', 'OSCControl Blockly 编辑器');
    setText('pageTitle', T('OSCControl Blockly Editor', 'OSCControl Blockly 编辑器'));
    setText('scenarioLabel', T('Scenario', '场景'));
    setText('loadTemplateButton', T('Load Scenario', '加载场景'));
    setText('postButton', T('Apply To Desktop', '应用到桌面端'));
    setText('previewTitle', T('Generated .osccontrol', '生成的 .osccontrol'));
    setText('hostHint', T('Changes auto-sync to the desktop host when WebView2 is attached. Use Check, Save, or Start in the desktop toolbar.', '连接 WebView2 宿主后，变更会自动同步到桌面端。使用桌面工具栏里的 Check、Save 或 Start。'));

    localizeTemplateOption('startup-log', T('Startup log', '启动日志'));
    localizeTemplateOption('environment-log', T('Environment log', '环境日志'));
    localizeTemplateOption('osc-forward', T('OSC receive to OSC send', 'OSC 接收到 OSC 发送'));
    localizeTemplateOption('vrchat-chatbox', T('VRChat startup chatbox', 'VRChat 启动聊天框'));
    localizeTemplateOption('vrchat-param-input', T('VRChat param to input', 'VRChat 参数到输入'));
    localizeTemplateOption('stateful-loop', T('Stateful loop', '状态循环'));
    localizeTemplateOption('ws-forward', T('OSC to WebSocket body', 'OSC 到 WebSocket body'));
    localizeTemplateOption('ws-duplex', T('WebSocket duplex echo', 'WebSocket 双工回显'));
    localizeTemplateOption('vrchat-avatar-typing', T('VRChat avatar typing', 'VRChat Avatar typing'));
    localizeTemplateOption('dglab-strength', T('DG-LAB strength', 'DG-LAB 强度'));

    localizeCategory('Endpoints', T('Endpoints', '端点'));
    localizeCategory('Variables', T('Variables', '变量'));
    localizeCategory('Expressions', T('Expressions', '表达式'));
    localizeCategory('Rules', T('Rules', '规则'));
    localizeCategory('Actions', T('Actions', '动作'));
    localizeCategory('Control', T('Control', '控制'));
    localizeCategory('Raw', T('Raw', '原始'));
  }

  function setText(id, text) {
    const node = document.getElementById(id);
    if (node) {
      node.textContent = text;
    }
  }

  function localizeTemplateOption(value, text) {
    const option = document.querySelector(`#templateSelect option[value="${value}"]`);
    if (option) {
      option.textContent = text;
    }
  }

  function localizeCategory(from, to) {
    const category = document.querySelector(`#toolbox category[name="${from}"]`);
    if (category) {
      category.setAttribute('name', to);
    }
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

  function handleHostMessage(event) {
    const message = event.data || {};
    if (message.kind !== 'osccontrol-blockly-load-workspace') {
      return;
    }

    state.pendingWorkspaceJson = message.workspaceJson || '';
    restorePendingWorkspace();
  }

  function postReadyToHost() {
    if (!hasWebViewHost()) {
      return;
    }

    window.chrome.webview.postMessage({
      kind: 'osccontrol-blockly-ready'
    });
  }

  function restorePendingWorkspace() {
    if (!state.pendingWorkspaceJson || !state.workspace) {
      return;
    }

    const workspaceJson = state.pendingWorkspaceJson;
    state.pendingWorkspaceJson = '';
    loadWorkspaceJson(workspaceJson);
  }

  function loadWorkspaceJson(workspaceJson) {
    if (!workspaceJson || !Blockly.serialization || !Blockly.serialization.workspaces) {
      return;
    }

    try {
      const data = JSON.parse(workspaceJson);
      state.suppressEvents = true;
      try {
        state.workspace.clear();
        Blockly.serialization.workspaces.load(data, state.workspace);
        if (typeof state.workspace.cleanUp === 'function') {
          state.workspace.cleanUp();
        }
      } finally {
        state.suppressEvents = false;
      }

      updatePreview();
      postToHost('restore');
      setStatus(T('Restored Blockly workspace', '已恢复 Blockly 工作区'));
    } catch (error) {
      state.suppressEvents = false;
      reportDiagnostic('restore', 'Failed to restore Blockly workspace', error);
    }
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
      document.getElementById('status').textContent = reason === 'manual' ? T('Applied to desktop', '已应用到桌面端') : T('Synced to desktop', '已同步到桌面端');
      return;
    }

    document.getElementById('status').textContent = T('No WebView2 host attached', '未连接 WebView2 宿主');
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
