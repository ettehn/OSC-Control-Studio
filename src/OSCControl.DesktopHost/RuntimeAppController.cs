using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Runtime;

namespace OSCControl.DesktopHost;

internal sealed class RuntimeAppController : IAsyncDisposable
{
    private readonly CompilerPipeline _pipeline = new();
    private RuntimeEngine? _engine;
    private RuntimeHost? _host;
    private readonly bool _useSimplifiedChinese = DesktopLocalization.UseSimplifiedChinese();

    public event Action<string>? RuntimeOutput;
    public event Action<string>? StatusChanged;

    public bool IsRunning => _host is not null;

    public CompilationResult Compile(string source) => _pipeline.Compile(source);

    public async Task<CompilationResult> StartAsync(string source, CancellationToken cancellationToken)
    {
        await StopAsync();

        var result = _pipeline.Compile(source);
        if (result.HasErrors || result.Plan is null)
        {
            StatusChanged?.Invoke(_useSimplifiedChinese ? "编译失败" : "Compile failed");
            return result;
        }

        var logSink = new CallbackRuntimeLogSink(entry =>
            RuntimeOutput?.Invoke($"[{entry.Timestamp:HH:mm:ss}] log/{entry.Level}: {FormatValue(entry.Value)}"));

        var errorSink = new CallbackRuntimeHostErrorSink(error =>
            RuntimeOutput?.Invoke($"[{error.Timestamp:HH:mm:ss}] host/{error.Stage} {error.EndpointName} ({error.TransportKind}): {error.Exception.Message}"));

        _engine = new RuntimeEngine(result.Plan, new RuntimeEngineOptions
        {
            LogSink = logSink
        });

        _host = new RuntimeHost(_engine, new RuntimeHostOptions
        {
            ErrorSink = errorSink
        });

        await _host.StartAsync(cancellationToken);
        RuntimeOutput?.Invoke(_useSimplifiedChinese ? $"运行时已启动，共加载 {result.Plan.Rules.Count} 条规则。" : $"Runtime started with {result.Plan.Rules.Count} rule(s).");
        StatusChanged?.Invoke(_useSimplifiedChinese ? "运行中" : "Running");
        return result;
    }

    public async Task StopAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
            _host = null;
        }

        if (_engine is not null)
        {
            await _engine.DisposeAsync();
            _engine = null;
        }

        StatusChanged?.Invoke(_useSimplifiedChinese ? "已停止" : "Stopped");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "null",
        string text => text,
        _ => System.Text.Json.JsonSerializer.Serialize(value)
    };

    private sealed class CallbackRuntimeLogSink : IRuntimeLogSink
    {
        private readonly Action<RuntimeLogEntry> _callback;

        public CallbackRuntimeLogSink(Action<RuntimeLogEntry> callback)
        {
            _callback = callback;
        }

        public void Write(RuntimeLogEntry entry)
        {
            _callback(entry);
        }
    }

    private sealed class CallbackRuntimeHostErrorSink : IRuntimeHostErrorSink
    {
        private readonly Action<RuntimeHostError> _callback;

        public CallbackRuntimeHostErrorSink(Action<RuntimeHostError> callback)
        {
            _callback = callback;
        }

        public void Report(RuntimeHostError error)
        {
            _callback(error);
        }
    }
}


