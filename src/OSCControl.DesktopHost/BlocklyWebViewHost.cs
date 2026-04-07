#if OSCCONTROL_BLOCKLY_WEBVIEW2
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OSCControl.DesktopHost;

internal sealed class BlocklyWebViewHost : UserControl
{
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly string _indexPath;

    public BlocklyWebViewHost(string indexPath)
    {
        _indexPath = indexPath;
        Dock = DockStyle.Fill;
        Controls.Add(_webView);
        Load += OnLoadAsync;
    }

    public event EventHandler<BlocklyGeneratedScript>? GeneratedScriptReceived;

    private async void OnLoadAsync(object? sender, EventArgs e)
    {
        if (!File.Exists(_indexPath))
        {
            ShowMissingAssetMessage();
            return;
        }

        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webView.CoreWebView2.Navigate(new Uri(_indexPath).AbsoluteUri);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or FileNotFoundException)
        {
            ShowInitializationError(ex);
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("kind", out var kind) || kind.GetString() != "osccontrol-blockly-generated-script")
            {
                return;
            }

            var source = root.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString() ?? string.Empty : string.Empty;
            var workspaceJson = root.TryGetProperty("workspaceJson", out var workspaceElement) ? workspaceElement.GetString() ?? string.Empty : string.Empty;
            GeneratedScriptReceived?.Invoke(this, new BlocklyGeneratedScript
            {
                Source = source,
                WorkspaceJson = workspaceJson
            });
        }
        catch (JsonException)
        {
        }
    }

    private void ShowMissingAssetMessage()
    {
        Controls.Clear();
        Controls.Add(CreateMessageLabel($"Blockly editor asset was not found: {_indexPath}"));
    }

    private void ShowInitializationError(Exception ex)
    {
        Controls.Clear();
        Controls.Add(CreateMessageLabel($"WebView2 could not be initialized. {ex.Message}"));
    }

    private static Label CreateMessageLabel(string message)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Text = message,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }
}
#endif