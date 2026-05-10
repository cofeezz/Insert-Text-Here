using Microsoft.Win32;

static class Startup
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Name    = "ProcessSuspenderMonitor";

    static string MonitorCmd =>
        $"\"{Environment.ProcessPath}\" --monitor";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
        return key?.GetValue(Name) is not null;
    }

    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true)!;
        key.SetValue(Name, MonitorCmd, RegistryValueKind.String);
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
            key?.DeleteValue(Name, throwOnMissingValue: false);
        }
        catch { }
    }

    public static bool IsMonitorRunning()
    {
        return System.Diagnostics.Process
                     .GetProcessesByName("ProcessSuspender")
                     .Any(p =>
                     {
                         try { return p.Id != Environment.ProcessId; }
                         catch { return false; }
                     });
    }

    public static System.Diagnostics.Process StartMonitor()
    {
        var si = new System.Diagnostics.ProcessStartInfo
        {
            FileName        = Environment.ProcessPath ?? "ProcessSuspender.exe",
            Arguments       = "--monitor",
            CreateNoWindow  = true,
            UseShellExecute = false,
        };
        return System.Diagnostics.Process.Start(si)!;
    }

    public static void StopMonitor()
    {
        foreach (var p in System.Diagnostics.Process.GetProcessesByName("ProcessSuspender"))
        {
            try
            {
                if (p.Id == Environment.ProcessId) continue;
                p.Kill();
            }
            catch { }
        }
    }
}