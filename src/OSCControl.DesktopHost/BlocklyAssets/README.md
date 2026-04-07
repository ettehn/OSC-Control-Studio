# Blockly Assets

This folder is the WebView2 asset root for the future Blockly editor.

The initial branch intentionally does not vendor Blockly or enable the WebView2 NuGet package by default, because the repository verification path is designed to avoid new network restores.

Expected vendor layout for the next step:

```text
BlocklyAssets/
  vendor/
    blockly/
      blockly_compressed.js
      blocks_compressed.js
      msg/en.js
```

Once those files are present, open `index.html` in a browser to test the static prototype, or host it through WebView2 from `OSCControl.DesktopHost`.

## WebView2 Host

The desktop host can compile the WebView2-backed editor path with:

```powershell
dotnet build C:\CodexProjects\src\OSCControl.DesktopHost\OSCControl.DesktopHost.csproj /p:EnableBlocklyWebView2=true
```

This requires the `Microsoft.Web.WebView2` NuGet package to be restorable in the local environment.
