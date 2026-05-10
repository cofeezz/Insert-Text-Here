// Manages installation paths and the local version saved on disk.

static class InstallPath
{
    // Installation folder: %APPDATA%\ProcessSuspend\
    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProcessSuspender");

    // Main app 
    public static readonly string AppExe     = Path.Combine(Dir, "ProcessSuspender.exe");
    // Temporary file during download
    public static readonly string AppExeTmp  = Path.Combine(Dir, "ProcessSuspender.exe.tmp");
    // Installed version
    public static readonly string VersionFile = Path.Combine(Dir, "version.txt");

    static InstallPath() => Directory.CreateDirectory(Dir);

    public static bool AppExists() => File.Exists(AppExe);

    public static string ReadLocalVersion()
    {
        try   { return File.Exists(VersionFile) ? File.ReadAllText(VersionFile).Trim() : ""; }
        catch { return ""; }
    }

    public static void WriteVersion(string tag)
        => File.WriteAllText(VersionFile, tag);

    public static void ReplaceWithTemp()
    {
        if (File.Exists(AppExe))    File.Delete(AppExe);
        File.Move(AppExeTmp, AppExe);
    }
}
