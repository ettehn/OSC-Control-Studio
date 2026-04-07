#if OSCCONTROL_BLOCKLY_WEBVIEW2
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OSCControl.DesktopHost;

internal sealed class BlocklyWebViewHost : UserControl
{

    private const string PageDiagnosticsScript = """
        (() => {
          const stringify = value => {
            if (value instanceof Error) {
              return value.stack || value.message;
            }

            if (typeof value === 'string') {
              return value;
            }

            try {
              return JSON.stringify(value);
            } catch {
              return String(value);
            }
          };

          const send = (level, args) => {
            try {
              if (!window.chrome || !window.chrome.webview) {
                return;
              }

              window.chrome.webview.postMessage({
                kind: 'osccontrol-blockly-diagnostic',
                level,
                message: Array.from(args || []).map(stringify).join(' ')
              });
            } catch {
            }
          };

          window.addEventListener('error', event => {
            send('window.error', [event.message, event.filename, `${event.lineno}:${event.colno}`]);
          });

          window.addEventListener('unhandledrejection', event => {
            send('unhandledrejection', [event.reason]);
          });

          const originalError = console.error;
          console.error = (...args) => {
            send('console.error', args);
            originalError.apply(console, args);
          };
        })();
        """;

    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly string _indexPath;
    private bool _initializationStarted;
    private bool _pageReady;
    private string _pendingWorkspaceJson = string.Empty;
    private string _userDataFolder = string.Empty;
    private static readonly object EnvironmentSetupSync = new();
    private static Task<WebView2EnvironmentSetup>? s_environmentSetupTask;

    public BlocklyWebViewHost(string indexPath)
    {
        _indexPath = indexPath;
        Dock = DockStyle.Fill;
        Controls.Add(_webView);
        _ = StartEnvironmentWarmup();
    }

    public event EventHandler<BlocklyGeneratedScript>? GeneratedScriptReceived;

    public Task LoadWorkspaceJsonAsync(string workspaceJson)
    {
        if (string.IsNullOrWhiteSpace(workspaceJson))
        {
            return Task.CompletedTask;
        }

        _pendingWorkspaceJson = workspaceJson;
        return TryPostPendingWorkspaceAsync();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        BeginInitializationIfReady();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        BeginInitializationIfReady();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        BeginInitializationIfReady();
    }

    private void BeginInitializationIfReady()
    {
        if (_initializationStarted || !IsHandleCreated || !Visible || Width <= 0 || Height <= 0)
        {
            return;
        }

        if (!_webView.IsHandleCreated)
        {
            _webView.CreateControl();
        }

        if (!_webView.IsHandleCreated)
        {
            Program.Log("Blockly WebView2 initialization deferred: WebView child handle was not created.");
            BeginInvoke(BeginInitializationIfReady);
            return;
        }

        _initializationStarted = true;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (!File.Exists(_indexPath))
        {
            ShowMissingAssetMessage();
            return;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var setup = await StartEnvironmentWarmup();
            _userDataFolder = setup.UserDataFolder;
            Program.Log($"Blockly WebView2 user data folder: {_userDataFolder}");
            Program.Log($"Blockly WebView2 loading index: {_indexPath}");
            Program.Log($"Blockly WebView2 handles: host=0x{Handle.ToInt64():X}; webView=0x{_webView.Handle.ToInt64():X}; size={_webView.Width}x{_webView.Height}; visible={Visible}/{_webView.Visible}");
            await _webView.EnsureCoreWebView2Async(setup.Environment);
            Program.Log($"Blockly WebView2 controller ready in {stopwatch.ElapsedMilliseconds} ms.");
            _webView.CoreWebView2.NavigationStarting += (_, args) =>
            {
                _pageReady = false;
                Program.Log($"Blockly WebView2 navigation starting: {args.Uri}");
            };
            _webView.CoreWebView2.NavigationCompleted += (_, args) => Program.Log($"Blockly WebView2 navigation completed: success={args.IsSuccess}; status={args.WebErrorStatus}");
            _webView.CoreWebView2.DOMContentLoaded += (_, _) => Program.Log("Blockly WebView2 DOM content loaded.");
            _webView.CoreWebView2.ProcessFailed += (_, args) => Program.Log($"Blockly WebView2 process failed: {args.ProcessFailedKind}");
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(PageDiagnosticsScript);
            _webView.CoreWebView2.Navigate(new Uri(_indexPath).AbsoluteUri);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or NotSupportedException or FileNotFoundException or IOException or System.Runtime.InteropServices.COMException)
        {
            Program.Log($"Blockly WebView2 initialization failed: {ex}");
            ShowInitializationError(ex);
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("kind", out var kind))
            {
                return;
            }

            var kindText = kind.GetString();
            if (kindText == "osccontrol-blockly-ready")
            {
                _pageReady = true;
                Program.Log("Blockly WebView2 page ready.");
                _ = TryPostPendingWorkspaceAsync();
                return;
            }

            if (kindText == "osccontrol-blockly-diagnostic")
            {
                var level = root.TryGetProperty("level", out var levelElement) ? levelElement.GetString() ?? string.Empty : string.Empty;
                var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? string.Empty : string.Empty;
                Program.Log($"Blockly WebView2 page diagnostic [{level}]: {message}");
                return;
            }

            if (kindText != "osccontrol-blockly-generated-script")
            {
                return;
            }

            var source = root.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString() ?? string.Empty : string.Empty;
            var reason = root.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() ?? string.Empty : string.Empty;
            var workspaceJson = root.TryGetProperty("workspaceJson", out var workspaceElement) ? workspaceElement.GetString() ?? string.Empty : string.Empty;
            Program.Log($"Blockly WebView2 generated script received: reason={reason}; chars={source.Length}; workspaceJsonChars={workspaceJson.Length}");
            GeneratedScriptReceived?.Invoke(this, new BlocklyGeneratedScript
            {
                Source = source,
                Reason = reason,
                WorkspaceJson = workspaceJson
            });
        }
        catch (JsonException ex)
        {
            Program.Log($"Blockly WebView2 message parse failed: {ex.Message}");
        }
    }

    private async Task TryPostPendingWorkspaceAsync()
    {
        if (!_pageReady || string.IsNullOrWhiteSpace(_pendingWorkspaceJson) || _webView.CoreWebView2 is null)
        {
            return;
        }

        var workspaceJson = _pendingWorkspaceJson;
        _pendingWorkspaceJson = string.Empty;
        var payload = JsonSerializer.Serialize(new
        {
            kind = "osccontrol-blockly-load-workspace",
            workspaceJson
        });

        _webView.CoreWebView2.PostWebMessageAsJson(payload);
        Program.Log($"Blockly WebView2 workspace restore posted: chars={workspaceJson.Length}");
        await Task.CompletedTask;
    }

    private static Task<WebView2EnvironmentSetup> StartEnvironmentWarmup()
    {
        lock (EnvironmentSetupSync)
        {
            if (s_environmentSetupTask is null || s_environmentSetupTask.IsCanceled || s_environmentSetupTask.IsFaulted)
            {
                s_environmentSetupTask = CreateEnvironmentSetupAsync();
            }

            return s_environmentSetupTask;
        }
    }

    private static async Task<WebView2EnvironmentSetup> CreateEnvironmentSetupAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var userDataFolder = ResolveWritableUserDataFolder();
        Program.Log($"Blockly WebView2 environment warmup starting: {userDataFolder}");
        var environmentOptions = CreateEnvironmentOptions();
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder, options: environmentOptions);
        Program.Log($"Blockly WebView2 environment warmup completed in {stopwatch.ElapsedMilliseconds} ms.");
        return new WebView2EnvironmentSetup(userDataFolder, environment);
    }

    private static CoreWebView2EnvironmentOptions CreateEnvironmentOptions()
    {
        var browserArguments = Environment.GetEnvironmentVariable("OSCCONTROL_WEBVIEW2_BROWSER_ARGS");
        if (string.IsNullOrWhiteSpace(browserArguments))
        {
            Program.Log("Blockly WebView2 browser args: (none)");
            return new CoreWebView2EnvironmentOptions();
        }

        Program.Log($"Blockly WebView2 browser args: {browserArguments}");
        return new CoreWebView2EnvironmentOptions(browserArguments);
    }

    private static string ResolveWritableUserDataFolder()
    {
        var candidates = new List<string>();
        AddCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, ".webview2-user-data"));
        AddCandidate(candidates, Path.GetTempPath());

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                Directory.CreateDirectory(candidate);
                var probePath = Path.Combine(candidate, $".write-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probePath, string.Empty);
                File.Delete(probePath);
                return candidate;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or NotSupportedException)
            {
                Program.Log($"Blockly WebView2 user data folder not writable: {candidate}; {ex.GetType().Name}: {ex.Message}");
            }
        }

        throw new UnauthorizedAccessException("No writable WebView2 user data folder was found.");

        static void AddCandidate(List<string> candidates, string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return;
            }

            candidates.Add(Path.Combine(root, "OSCControl.DesktopHost", "WebView2UserData"));
        }
    }

    private void ShowMissingAssetMessage()
    {
        Controls.Clear();
        Controls.Add(CreateMessageLabel($"Blockly editor asset was not found: {_indexPath}"));
    }

    private void ShowInitializationError(Exception ex)
    {
        var userDataFolder = string.IsNullOrWhiteSpace(_userDataFolder) ? "not resolved" : _userDataFolder;
        Controls.Clear();
        Controls.Add(CreateMessageLabel($"WebView2 could not be initialized. {ex.Message}{Environment.NewLine}{Environment.NewLine}User data folder: {userDataFolder}"));
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

    private sealed record WebView2EnvironmentSetup(string UserDataFolder, CoreWebView2Environment Environment);
}
#endif
