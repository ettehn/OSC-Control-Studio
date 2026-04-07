using System.Text;

namespace OSCControl.DesktopHost;

internal static class Program
{
    private static readonly string LocalLogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OSCControl.DesktopHost");
    private static readonly string LocalLogPath = Path.Combine(LocalLogDirectory, "desktop-host.log");
    private static readonly string BaseDirectoryLogPath = Path.Combine(AppContext.BaseDirectory, "desktop-host.log");

    [STAThread]
    private static void Main()
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        TryCreateDirectory(LocalLogDirectory);
        Log("Process starting.");

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => HandleUnhandled("Application.ThreadException", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => HandleUnhandled("AppDomain.UnhandledException", args.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception object."));
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            HandleUnhandled("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        try
        {
            ApplicationConfiguration.Initialize();
            Log("ApplicationConfiguration initialized.");
            using var mainForm = new MainForm();
            Log("MainForm constructed.");
            Application.Run(mainForm);
            Log("Application.Run returned normally.");
        }
        catch (Exception ex)
        {
            HandleUnhandled("Main", ex);
        }
    }

    internal static void Log(string message)
    {
        var line = $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}";
        TryAppend(LocalLogPath, line);
        TryAppend(BaseDirectoryLogPath, line);
    }

    private static void HandleUnhandled(string source, Exception exception)
    {
        Log($"Unhandled exception in {source}:{Environment.NewLine}{exception}");
        try
        {
            MessageBox.Show(
                DesktopLocalization.UseSimplifiedChinese() ? $"OSCControl 桌面宿主在 {source} 中崩溃。\r\n\r\n{exception.Message}\r\n\r\n日志: {BaseDirectoryLogPath}" : $"OSCControl Desktop Host crashed in {source}.\r\n\r\n{exception.Message}\r\n\r\nLog: {BaseDirectoryLogPath}",
                DesktopLocalization.UseSimplifiedChinese() ? "OSCControl 桌面宿主" : "OSCControl Desktop Host",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
        }
    }

    private static void TryCreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch
        {
        }
    }

    private static void TryAppend(string path, string line)
    {
        try
        {
            File.AppendAllText(path, line, Encoding.UTF8);
        }
        catch
        {
        }
    }
}

