<div align="center">

<h1>⏸ ProcessSuspender</h1>

<p>Automatically suspend Windows processes the moment they start.<br>
Zero dependencies. Single file to distribute. Self-updating via GitHub Releases.</p>

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue?style=flat-square&logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square&logo=dotnet)
![Release](https://img.shields.io/github/v/release/cofeezz/Insert-Text-Here?style=flat-square&color=orange)
![License](https://img.shields.io/github/license/cofeezz/Insert-Text-Here?style=flat-square)


</div>

---

## What is this?

**ProcessSuspender** is a lightweight Windows utility that automatically suspends processes as soon as they start. A suspended process is frozen in memory — it holds its state but consumes zero CPU — until you explicitly resume it.

**Use cases:**
- Prevent startup programs from running without uninstalling them
- Freeze background processes that consume resources unnecessarily
- Control exactly when a specific executable is allowed to run

---

## ⬇️ Download

> You only need to download the Launcher. The main application is downloaded and kept up to date automatically.

**[→ Download Launcher.exe](https://github.com/cofeezz/Insert-Text-Here/releases/latest/download/Launcher.exe)**

Requirements: Windows 10 or 11 (64-bit). No installation required.

---

## Getting Started

**1. Run `Launcher.exe`**

On first launch, the Launcher connects to GitHub, downloads the latest `ProcessSuspender.exe` into `%APPDATA%\ProcessSuspender\`, and opens it. This only happens when there is a new version available — subsequent launches open the app instantly.

**2. Start the monitor**

Click **▶ Start monitor**. The monitor runs silently as a background process with no visible window. You can close the main UI at any time — the monitor keeps running independently.

**3. Add processes to the watchlist**

Click **＋ Add** and select the `.exe` you want to suspend. From that point on, every time that process starts — by you, by Windows, or by another program — it will be suspended within 2 seconds.

**4. Enable autostart** *(recommended)*

Check **"Start monitor automatically with Windows"**. This writes an entry to the Windows registry so the monitor starts silently at login, before you even open the main UI.

---

## UI Reference

| Button | Action |
|---|---|
| ▶ / ⏹ Start · Stop monitor | Toggle the background monitor on or off |
| Start with Windows | Add or remove from the Windows startup registry |
| + Add | Open a file picker to add an `.exe` to the watchlist |
| - Remove | Remove from watchlist and immediately resume the process |
| Resume now | Temporarily resume without removing from the watchlist |
| View log | Open the activity log in Notepad |

---

## How It Works

### Process suspension

Windows does not expose a public API for suspending entire processes — only individual threads. However, `ntdll.dll` exports two undocumented-but-stable functions that have been present since Windows XP:

```
NtSuspendProcess(HANDLE ProcessHandle) → NTSTATUS
NtResumeProcess(HANDLE ProcessHandle)  → NTSTATUS
```

ProcessSuspender calls these via P/Invoke using the `PROCESS_SUSPEND_RESUME` (0x0800) access right:

```csharp
[DllImport("ntdll.dll")]
static extern int NtSuspendProcess(IntPtr handle);

[DllImport("ntdll.dll")]
static extern int NtResumeProcess(IntPtr handle);
```

When a process is suspended this way, all of its threads are frozen simultaneously. The process remains in memory, its handles stay valid, and it can be resumed at any time with full state intact. From the OS scheduler's perspective, the process is alive but allocated zero CPU time.

### The monitor loop

The monitor runs as a separate instance of the same executable, launched with the `--monitor` flag and `CREATE_NO_WINDOW`. Every 2 seconds it:

1. Reads `watchlist.json` from disk — so changes made in the UI take effect immediately without restarting the monitor
2. Enumerates running processes via `Process.GetProcessesByName()`
3. For each PID on the watchlist that is not yet tracked, opens a handle and calls `NtSuspendProcess`
4. Cleans up tracked PIDs for processes that have terminated or been removed from the watchlist, calling `NtResumeProcess` for the latter

```
Monitor loop (every 2s)
        │
        ├─ Read watchlist.json
        │
        ├─ For each name in watchlist:
        │       └─ Get running PIDs by name
        │               └─ PID not yet tracked? → NtSuspendProcess(pid)
        │
        └─ For each tracked PID:
                ├─ Removed from watchlist? → NtResumeProcess(pid) → untrack
                └─ Process terminated?     → untrack
```

### Autostart via registry

When "Start with Windows" is enabled, the monitor registers itself under:

```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
"ProcessSuspenderMonitor" = "C:\...\ProcessSuspender.exe --monitor"
```

Using `HKCU` instead of `HKLM` means no administrator privileges are ever required.

### The Launcher and self-update

The Launcher is a small, separate executable that acts as a bootstrapper and auto-updater. On every launch it:

1. Calls the [GitHub Releases API](https://api.github.com/repos/cofeezz/Insert-Text-Here/releases/latest) to fetch the latest release metadata
2. Parses the `tag_name` field and compares it against the locally stored `version.txt` using semantic versioning
3. If an update is available (or the app is not yet installed), streams the `ProcessSuspender.exe` asset with a live progress bar
4. Downloads to a `.tmp` file first, then atomically replaces the existing `.exe` only after a fully successful download — a corrupted or partial download never overwrites the working binary
5. Saves the new version tag to `version.txt`, launches the app, and exits

```
Launcher starts
        │
        ├─ Fetch GitHub API → latest release tag + asset URL
        │       │
        │       ├─ Unreachable (offline)?
        │       │       ├─ App installed → open local version
        │       │       └─ Not installed → show error
        │       │
        │       └─ Reachable:
        │               ├─ local == remote → open app directly
        │               └─ local < remote (or missing):
        │                       ├─ Stream download → write to .tmp
        │                       ├─ Atomic replace: .tmp → .exe
        │                       ├─ Write version.txt
        │                       └─ Open updated app → exit
        │
        └─ Exit
```

---

##  Architecture

The project is split into two independently compiled executables. This separation means the Launcher never needs to be redistributed — only the app binary changes between versions.

```
ProcessSuspender/
├── Launcher/                     ← bootstrapper + auto-updater
│   ├── Launcher.csproj
│   ├── Program.cs                ← entry point
│   ├── LauncherForm.cs           ← download window with progress bar
│   ├── GitHubRelease.cs          ← GitHub API client, semver comparison
│   └── InstallPath.cs            ← manages %APPDATA%\ProcessSuspender\
│
├── App/                          ← main application
│   ├── ProcessSuspender.csproj
│   ├── Program.cs                ← entry point: GUI mode or --monitor mode
│   ├── MainForm.cs               ← WinForms UI, dark theme
│   ├── Monitor.cs                ← background suspend/resume loop
│   ├── ProcessApi.cs             ← P/Invoke: NtSuspendProcess / NtResumeProcess
│   ├── Config.cs                 ← watchlist.json read/write
│   └── Startup.cs                ← registry autostart + process control
│
└── build.bat                     ← compiles both projects into dist\
```

### Local data (`%APPDATA%\ProcessSuspender\`)

| File | Purpose |
|---|---|
| `ProcessSuspender.exe` | Main application binary (managed by the Launcher) |
| `watchlist.json` | JSON array of watched executable names |
| `version.txt` | Currently installed version tag (e.g. `v1.2.0`) |
| `monitor.log` | Timestamped activity log from the monitor |

### Dual-mode executable

`ProcessSuspender.exe` serves two roles depending on how it is invoked:

```csharp
if (args.Length > 0 && args[0] == "--monitor")
    Monitor.Run();                     // headless background loop
else
    Application.Run(new MainForm());   // WinForms UI
```

The UI launches the monitor by spawning a second instance of itself with `--monitor` and `CREATE_NO_WINDOW`. This avoids shipping two separate binaries for the app itself.

---

## Building from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for Windows x64

### Steps

```batch
git clone https://github.com/cofeezz/Insert-Text-Here.git
cd ProcessSuspender
build.bat
```

Output:

```
dist\
├── launcher\Launcher.exe          ← share this with users
└── app\ProcessSuspender.exe       ← upload to GitHub Releases as an asset
```

Both binaries are fully self-contained (`SelfContained=true`, `PublishSingleFile=true`) — they embed the .NET 8 runtime and require no pre-installed dependencies on the target machine.

### Publishing a new release

1. Make your changes and run `build.bat`
2. Go to your repository → **Releases → Draft a new release**
3. Set the tag to the new version (e.g. `v1.1.0`)
4. Upload `dist\app\ProcessSuspender.exe` as a release asset
5. Publish — all existing users will receive the update automatically on their next Launcher run

> `Launcher.exe` only needs to be uploaded once, in the very first release. It never changes.

---

## FAQ

**Does the process stay suspended permanently?**
Yes, as long as it remains on the watchlist and the monitor is running. If something else resumes the process externally, the monitor will re-suspend it within 2 seconds.

**How do I resume a process for good?**
Select it in the list and click **🗑 Remove**. The process is resumed immediately and will no longer be touched by the monitor.

**Will my antivirus flag this?**
Possibly. Calling `NtSuspendProcess` is legitimate but uncommon for typical applications, which can trigger heuristic detections. Adding the executable to your antivirus exclusions resolves this.

**Can I suspend system processes?**
Strongly discouraged. Suspending critical Windows processes can cause system instability or deadlocks. Only use this with regular user-space applications.

**Does the Launcher ever need to be updated?**
No. The Launcher is intentionally minimal and contains no application logic — it only downloads and opens the app. It is designed to never require redistribution.

**How do I uninstall?**
Remove the monitor from startup via the UI, then delete `%APPDATA%\ProcessSuspender\`. No system files or registry entries outside of the autostart key are ever modified.

---

## License

Distributed under the MIT License. See [`LICENSE`](LICENSE) for details.
