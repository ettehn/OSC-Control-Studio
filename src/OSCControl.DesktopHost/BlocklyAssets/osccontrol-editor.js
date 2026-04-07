(function () {
  const state = {
    workspace: null,
    source: ''
  };

  window.addEventListener('load', () => {
    const status = document.getElementById('status');
    const postButton = document.getElementById('postButton');
    postButton.addEventListener('click', postToHost);

    if (!window.Blockly || !window.OSCControlBlocklyGenerator) {
      status.textContent = 'Blockly vendor files are missing. Add them under BlocklyAssets/vendor/blockly/.';
      document.getElementById('scriptPreview').textContent = 'Blockly is not loaded yet. The OSCControl block definitions and generator are ready for WebView2 integration.';
      return;
    }

    state.workspace = Blockly.inject('blocklyDiv', {
      toolbox: document.getElementById('toolbox'),
      trashcan: true,
      scrollbars: true,
      sounds: false
    });

    Blockly.Xml.domToWorkspace(document.getElementById('startBlocks'), state.workspace);
    state.workspace.addChangeListener(updatePreview);
    status.textContent = 'Ready';
    updatePreview();
  });

  function updatePreview() {
    state.source = window.OSCControlBlocklyGenerator.workspaceToCode(state.workspace);
    document.getElementById('scriptPreview').textContent = state.source;
  }

  function postToHost() {
    const workspaceJson = serializeWorkspace();
    const message = {
      kind: 'osccontrol-blockly-generated-script',
      source: state.source,
      workspaceJson
    };

    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage(message);
      document.getElementById('status').textContent = 'Sent to host';
      return;
    }

    document.getElementById('status').textContent = 'No WebView2 host attached';
  }

  function serializeWorkspace() {
    if (!state.workspace || !Blockly.serialization || !Blockly.serialization.workspaces) {
      return '';
    }

    return JSON.stringify(Blockly.serialization.workspaces.save(state.workspace));
  }
}());