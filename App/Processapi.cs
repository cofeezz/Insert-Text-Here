using System.Runtime.InteropServices;
using System.Diagnostics;

static class ProcessApi
{
    const uint PROCESS_SUSPEND_RESUME = 0x0800;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("ntdll.dll")]
    static extern int NtSuspendProcess(IntPtr handle);

    [DllImport("ntdll.dll")]
    static extern int NtResumeProcess(IntPtr handle);

    static IntPtr Open(uint pid)
        => OpenProcess(PROCESS_SUSPEND_RESUME, false, pid);

    public static bool Suspend(uint pid)
    {
        var h = Open(pid);
        if (h == IntPtr.Zero) return false;
        try   { return NtSuspendProcess(h) == 0; }
        finally { CloseHandle(h); }
    }

    public static bool Resume(uint pid)
    {
        var h = Open(pid);
        if (h == IntPtr.Zero) return false;
        try   { return NtResumeProcess(h) == 0; }
        finally { CloseHandle(h); }
    }

    public static uint[] GetPidsByName(string exeName)
    {
        return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName))
                      .Select(p => (uint)p.Id)
                      .ToArray();
    }

    public static void ResumeAllByName(string exeName)
    {
        foreach (var pid in GetPidsByName(exeName))
            Resume(pid);
    }
}