# Blockly Assets

This folder is the WebView2 asset root for the Blockly editor.

The branch vendors the browser files needed at runtime and keeps npm metadata at the repository root so the vendor payload can be refreshed.

Vendor layout:

```text
BlocklyAssets/
  vendor/
    blockly/
      blockly_compressed.js
      blocks_compressed.js
      msg/en.js
      msg/zh-hans.js
```

Open `index.html` in a browser to test the static prototype, or host it through WebView2 from `OSCControl.DesktopHost`.

## WebView2 Host

The desktop host can compile the WebView2-backed editor path with:

```powershell
dotnet build C:\CodexProjects\src\OSCControl.DesktopHost\OSCControl.DesktopHost.csproj /p:EnableBlocklyWebView2=true
```

This requires the `Microsoft.Web.WebView2` NuGet package to be restorable in the local environment.

To refresh Blockly vendor files:

```powershell
npm install
Copy-Item node_modules\blockly\blockly_compressed.js src\OSCControl.DesktopHost\BlocklyAssets\vendor\blockly\ -Force
Copy-Item node_modules\blockly\blocks_compressed.js src\OSCControl.DesktopHost\BlocklyAssets\vendor\blockly\ -Force
Copy-Item node_modules\blockly\msg\en.js src\OSCControl.DesktopHost\BlocklyAssets\vendor\blockly\msg\ -Force
Copy-Item node_modules\blockly\msg\zh-hans.js src\OSCControl.DesktopHost\BlocklyAssets\vendor\blockly\msg\ -Force
```

## Built-in scenarios

The page includes scenario templates for startup logging, OSC receive-to-send forwarding, VRChat startup Chatbox, and VRChat parameter-to-input mapping.

When hosted through WebView2, workspace changes automatically send generated `.osccontrol` text to the desktop host; the `Apply To Desktop` button sends immediately.
