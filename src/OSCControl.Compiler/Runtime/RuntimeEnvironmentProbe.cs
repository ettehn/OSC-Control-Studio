using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace OSCControl.Compiler.Runtime;

internal static class RuntimeEnvironmentProbe
{
    private static readonly object CpuGate = new();
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    private static TimeSpan? LastProcessorTime;
    private static DateTimeOffset? LastWallTime;

    public static object? Evaluate(IReadOnlyList<object?> arguments, IRuntimeClock clock)
    {
        if (arguments.Count == 0)
        {
            return Snapshot(clock);
        }

        var key = RuntimeValueHelpers.ToStringValue(arguments[0]).Trim();
        return key switch
        {
            "time.utc" => clock.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            "time.local" => clock.UtcNow.ToLocalTime().ToString("O", CultureInfo.InvariantCulture),
            "time.timestamp" or "timestamp" => clock.UtcNow.ToUnixTimeMilliseconds(),
            "process.id" => Environment.ProcessId,
            "process.name" => AppDomain.CurrentDomain.FriendlyName,
            "process.cpuPercent" => GetProcessCpuPercent(clock.UtcNow),
            "process.memoryBytes" => Environment.WorkingSet,
            "process.threadCount" => GetProcessThreadCount(),
            "system.processorCount" => Environment.ProcessorCount,
            "system.os" => RuntimeInformation.OSDescription,
            "system.arch" => RuntimeInformation.ProcessArchitecture.ToString(),
            "system.memoryLoadPercent" => GetMemoryLoadPercent(),
            "system.memoryAvailableBytes" => GetAvailableMemoryBytes(),
            "tcp.listenerCount" => GetTcpListenerCount(),
            "tcp.listening" => IsTcpListening(arguments),
            _ => null
        };
    }

    private static Dictionary<string, object?> Snapshot(IRuntimeClock clock) => new(StringComparer.Ordinal)
    {
        ["time.utc"] = clock.UtcNow.ToString("O", CultureInfo.InvariantCulture),
        ["time.local"] = clock.UtcNow.ToLocalTime().ToString("O", CultureInfo.InvariantCulture),
        ["time.timestamp"] = clock.UtcNow.ToUnixTimeMilliseconds(),
        ["process.id"] = Environment.ProcessId,
        ["process.name"] = AppDomain.CurrentDomain.FriendlyName,
        ["process.cpuPercent"] = GetProcessCpuPercent(clock.UtcNow),
        ["process.memoryBytes"] = Environment.WorkingSet,
        ["process.threadCount"] = GetProcessThreadCount(),
        ["system.processorCount"] = Environment.ProcessorCount,
        ["system.os"] = RuntimeInformation.OSDescription,
        ["system.arch"] = RuntimeInformation.ProcessArchitecture.ToString(),
        ["system.memoryLoadPercent"] = GetMemoryLoadPercent(),
        ["system.memoryAvailableBytes"] = GetAvailableMemoryBytes(),
        ["tcp.listenerCount"] = GetTcpListenerCount()
    };

    private static double GetProcessCpuPercent(DateTimeOffset now)
    {
        lock (CpuGate)
        {
            CurrentProcess.Refresh();
            var processorTime = CurrentProcess.TotalProcessorTime;
            if (LastProcessorTime is null || LastWallTime is null)
            {
                LastProcessorTime = processorTime;
                LastWallTime = now;
                return 0d;
            }

            var cpuDelta = (processorTime - LastProcessorTime.Value).TotalMilliseconds;
            var wallDelta = (now - LastWallTime.Value).TotalMilliseconds;
            LastProcessorTime = processorTime;
            LastWallTime = now;

            if (wallDelta <= 0)
            {
                return 0d;
            }

            var percent = cpuDelta / wallDelta / Math.Max(1, Environment.ProcessorCount) * 100d;
            return Math.Round(Math.Clamp(percent, 0d, 100d), 2);
        }
    }

    private static int GetProcessThreadCount()
    {
        CurrentProcess.Refresh();
        return CurrentProcess.Threads.Count;
    }

    private static double GetMemoryLoadPercent()
    {
        var info = GC.GetGCMemoryInfo();
        if (info.HighMemoryLoadThresholdBytes <= 0)
        {
            return 0d;
        }

        return Math.Round(Math.Clamp((double)info.MemoryLoadBytes / info.HighMemoryLoadThresholdBytes * 100d, 0d, 100d), 2);
    }

    private static long GetAvailableMemoryBytes()
    {
        var info = GC.GetGCMemoryInfo();
        return Math.Max(0, info.HighMemoryLoadThresholdBytes - info.MemoryLoadBytes);
    }

    private static int GetTcpListenerCount() => GetTcpListeners().Length;

    private static bool IsTcpListening(IReadOnlyList<object?> arguments)
    {
        if (arguments.Count < 2)
        {
            return false;
        }

        var port = Convert.ToInt32(RuntimeValueHelpers.ToNumber(arguments[1]), CultureInfo.InvariantCulture);
        var host = arguments.Count >= 3 ? RuntimeValueHelpers.ToStringValue(arguments[2]).Trim() : string.Empty;
        IPAddress? expectedAddress = null;
        if (!string.IsNullOrWhiteSpace(host) && !IPAddress.TryParse(host, out expectedAddress))
        {
            return false;
        }

        foreach (var endpoint in GetTcpListeners())
        {
            if (endpoint.Port != port)
            {
                continue;
            }

            if (expectedAddress is null || endpoint.Address.Equals(expectedAddress) || endpoint.Address.Equals(IPAddress.Any) || endpoint.Address.Equals(IPAddress.IPv6Any))
            {
                return true;
            }
        }

        return false;
    }

    private static IPEndPoint[] GetTcpListeners()
    {
        try
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        }
        catch (NetworkInformationException)
        {
            return [];
        }
        catch (PlatformNotSupportedException)
        {
            return [];
        }
    }
}