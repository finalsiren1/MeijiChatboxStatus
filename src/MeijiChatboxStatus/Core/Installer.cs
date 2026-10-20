using System.Diagnostics;
using Microsoft.Win32;

namespace MeijiChatboxStatus.Core;

/// <summary>Something left behind by an older version (or an older copy) that a clean install removes.</summary>
public sealed record OldItem(string Kind, string Path, string? Detail = null);

/// <summary>
/// Per-user install (no admin): the downloaded exe installs itself into %LOCALAPPDATA%\Programs, removes older versions
/// it finds in the usual places, creates shortcuts and an Apps &amp; Features entry. Also handles uninstall.
/// </summary>
public static class Installer
{
    public const string InstallArg = "--install";
    public const string UninstallArg = "--uninstall";
    private const string AppName = "Meiji Chatbox Status";
    private const string ExeName = "MeijiChatboxStatus.exe";
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MeijiChatboxStatus";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static string InstallDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MeijiChatboxStatus");
    public static string InstalledExe { get; } = Path.Combine(InstallDir, ExeName);
    public static bool IsInstalled => File.Exists(InstalledExe);

    /// <summary>True when this process is the installed copy (dev builds always count as installed).</summary>
    public static bool RunningInstalled
    {
        get
        {
#if DEBUG
            return true;
#else
            return string.Equals(Path.GetFullPath(Environment.ProcessPath!), InstalledExe, StringComparison.OrdinalIgnoreCase);
#endif
        }
    }

    // Executables of previous versions: the original WinForms app and pre-release builds.
    private static readonly string[] OldExeNames = ["VrcAfkNative.exe", "ChatboxStatus.exe", "ChatboxStatus-lite.exe"];
    private static readonly string[] OldProcessNames = ["VrcAfkNative", "ChatboxStatus", "ChatboxStatus-lite", "MeijiChatboxStatus"];
    private static readonly string[] OldNameHints = ["vrcafknative", "chatboxstatus", "chatbox status"];
    // Files that ship next to the old non-single-file build; a folder holding only these is the old app's own folder.
    private static readonly string[] OldCompanionFiles =
        ["Microsoft.Windows.SDK.NET.dll", "WinRT.Runtime.dll", "vrc_afk_settings.txt"];

    // ───────────────────────────── Scan ─────────────────────────────

    /// <summary>Quick scan of the places people keep downloads (depth-limited, a few seconds at most).</summary>
    public static List<OldItem> Scan(CancellationToken ct)
    {
        var items = new List<OldItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(OldItem item) { if (seen.Add(item.Kind + "|" + item.Path)) items.Add(item); }

        string self = Path.GetFullPath(Environment.ProcessPath!);
        foreach (var p in OldProcessNames.SelectMany(Process.GetProcessesByName))
        {
            using (p)
            {
                if (p.Id == Environment.ProcessId) continue;
                string path = TryPath(p) ?? p.ProcessName;
                if (string.Equals(path, self, StringComparison.OrdinalIgnoreCase)) continue;
                Add(new OldItem("process", path, p.Id.ToString()));
            }
        }

        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new (string Dir, int Depth)[]
        {
            (Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), 3),
            (Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), 2),
            (Path.Combine(profile, "Downloads"), 3),
            (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 3),
            (profile, 1),
        };
        foreach (var (dir, depth) in roots)
        {
            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
            foreach (var file in SafeEnumerate(dir, depth, ct))
            {
                string name = Path.GetFileName(file);
                string full = Path.GetFullPath(file);
                if (string.Equals(full, self, StringComparison.OrdinalIgnoreCase) || full.StartsWith(InstallDir, StringComparison.OrdinalIgnoreCase)) continue;

                if (OldExeNames.Contains(name, StringComparer.OrdinalIgnoreCase)) Add(OldAppFolderOrFile(full));
                else if (name.Equals("vrc_afk_settings.txt", StringComparison.OrdinalIgnoreCase)) Add(new OldItem("settings", full));
                else if (name.StartsWith("MeijiChatboxStatus", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    Add(new OldItem("file", full));
                else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && name.Contains("VrcAfkNative", StringComparison.OrdinalIgnoreCase))
                    Add(new OldItem("file", full));
                else if (name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && IsOldName(name))
                    Add(new OldItem("shortcut", full));
            }
        }

        foreach (string dir in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                     Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                 })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var lnk in SafeEnumerate(dir, 2, ct).Where(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && IsOldName(Path.GetFileName(f))))
                Add(new OldItem("shortcut", lnk));
        }

        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            foreach (string name in run?.GetValueNames() ?? [])
                if (IsOldName(name))
                    Add(new OldItem("autorun", name, run!.GetValue(name) as string));
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException) { }

        foreach (string dir in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatboxStatus"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatboxStatus"),
                 })
            if (Directory.Exists(dir)) Add(new OldItem("data", dir));

        return items;
    }

    /// <summary>Names hinting at an older version. This app's own shortcut/autorun names are excluded (they're rewritten anyway).</summary>
    private static bool IsOldName(string s)
    {
        if (s.Equals(AppName + ".lnk", StringComparison.OrdinalIgnoreCase) || s.Equals("MeijiChatboxStatus", StringComparison.OrdinalIgnoreCase)) return false;
        string lower = s.ToLowerInvariant();
        return OldNameHints.Any(lower.Contains);
    }

    /// <summary>If the exe sits in a folder that holds nothing but the old app, the whole folder goes; otherwise only its files.</summary>
    private static OldItem OldAppFolderOrFile(string exePath)
    {
        string dir = Path.GetDirectoryName(exePath)!;
        string stem = Path.GetFileNameWithoutExtension(exePath);
        try
        {
            var entries = Directory.GetFileSystemEntries(dir);
            bool dedicated = entries.Length <= 40 && entries.All(e =>
            {
                string n = Path.GetFileName(e);
                return File.Exists(e) && (n.StartsWith(stem, StringComparison.OrdinalIgnoreCase)
                    || OldCompanionFiles.Contains(n, StringComparer.OrdinalIgnoreCase));
            });
            if (dedicated && !IsWellKnownFolder(dir)) return new OldItem("folder", dir);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return new OldItem("app", exePath);
    }

    private static bool IsWellKnownFolder(string dir)
    {
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] known =
        [
            profile, Path.Combine(profile, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        ];
        return known.Any(k => string.Equals(Path.TrimEndingDirectorySeparator(k), Path.TrimEndingDirectorySeparator(dir), StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> SafeEnumerate(string root, int depth, CancellationToken ct)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = depth > 0,
            MaxRecursionDepth = depth,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System, // no junction loops, no cloud downloads
        };
        IEnumerator<string> e;
        try { e = Directory.EnumerateFiles(root, "*", options).GetEnumerator(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { yield break; }
        using (e)
        {
            while (!ct.IsCancellationRequested)
            {
                bool next;
                try { next = e.MoveNext(); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { yield break; }
                if (!next) yield break;
                yield return e.Current;
            }
        }
    }

    private static string? TryPath(Process p)
    {
        try { return p.MainModule?.FileName; }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { return null; }
    }

    // ───────────────────────────── Install ─────────────────────────────

    /// <summary>Removes the old items, installs this exe, registers it, and returns the installed exe path.</summary>
    public static string Install(IReadOnlyList<OldItem> oldItems, Logger log, Action<string> progress)
    {
        progress("closing");
        StopOtherInstances(oldItems);
        ChatboxGuard.Clear(SettingsStore.ReadOscSettings());

        progress("importing");
        ImportOldSettings(oldItems, log);

        progress("removing");
        foreach (var item in oldItems) Remove(item, log);

        progress("copying");
        Directory.CreateDirectory(InstallDir);
        CopyWithRetry(Environment.ProcessPath!, InstalledExe);

        progress("shortcuts");
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk"), log);
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk"), log);
        RegisterUninstall();
        log.Info($"ติดตั้งเรียบร้อย: {InstalledExe}");
        return InstalledExe;
    }

    private static void StopOtherInstances(IReadOnlyList<OldItem> oldItems)
    {
        var ids = oldItems.Where(i => i.Kind == "process" && int.TryParse(i.Detail, out _)).Select(i => int.Parse(i.Detail!)).ToHashSet();
        // Also any copy of this app running from the install folder (an update replaces it).
        foreach (var p in Process.GetProcessesByName("MeijiChatboxStatus"))
            using (p) if (p.Id != Environment.ProcessId) ids.Add(p.Id);
        foreach (int id in ids)
        {
            try
            {
                using var p = Process.GetProcessById(id);
                p.Kill(entireProcessTree: true);
                p.WaitForExit(5000);
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }
    }

    /// <summary>Carries the newest old settings over, but never overwrites settings the new app already has.</summary>
    private static void ImportOldSettings(IReadOnlyList<OldItem> oldItems, Logger log)
    {
        if (File.Exists(SettingsStore.SettingsFile)) return;
        string oldJson = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatboxStatus", "settings.json");
        if (File.Exists(oldJson))
        {
            Directory.CreateDirectory(SettingsStore.DataDir);
            File.Copy(oldJson, SettingsStore.SettingsFile);
            return;
        }
        var txt = oldItems.Where(i => i.Kind == "settings").Select(i => i.Path)
            .Concat(oldItems.Where(i => i.Kind is "folder").Select(i => Path.Combine(i.Path, "vrc_afk_settings.txt")))
            .Concat(oldItems.Where(i => i.Kind is "app").Select(i => Path.Combine(Path.GetDirectoryName(i.Path)!, "vrc_afk_settings.txt")))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (txt is null) return;
        var imported = LegacyImport.FromFile(txt, log);
        if (imported is null) return;
        imported.Normalize();
        new SettingsStore().Save(imported, log);
    }

    private static void Remove(OldItem item, Logger log)
    {
        try
        {
            switch (item.Kind)
            {
                case "folder":
                case "data":
                    if (Directory.Exists(item.Path)) Directory.Delete(item.Path, recursive: true);
                    break;
                case "app":
                    // Only the old app's own files in a shared folder.
                    string dir = Path.GetDirectoryName(item.Path)!;
                    string stem = Path.GetFileNameWithoutExtension(item.Path);
                    foreach (var f in Directory.GetFiles(dir, stem + ".*")) File.Delete(f);
                    break;
                case "file":
                case "settings":
                case "shortcut":
                    if (File.Exists(item.Path)) File.Delete(item.Path);
                    break;
                case "autorun":
                    using (var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true))
                        run?.DeleteValue(item.Path, throwOnMissingValue: false);
                    break;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            log.Warn($"ลบ {item.Path} ไม่สำเร็จ: {e.Message}");
        }
    }

    private static void CopyWithRetry(string from, string to)
    {
        for (int attempt = 0; ; attempt++)
        {
            try { File.Copy(from, to, overwrite: true); return; }
            catch (IOException) when (attempt < 10) { Thread.Sleep(300); } // the old copy may still be exiting
        }
    }

    private static void CreateShortcut(string lnkPath, Logger log)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell unavailable");
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic link = shell.CreateShortcut(lnkPath);
            link.TargetPath = InstalledExe;
            link.WorkingDirectory = InstallDir;
            link.IconLocation = InstalledExe + ",0";
            link.Description = AppName;
            link.Save();
        }
        catch (Exception e)
        {
            log.Warn($"สร้าง shortcut ไม่สำเร็จ ({lnkPath}): {e.Message}");
        }
    }

    private static void RegisterUninstall()
    {
        using var key = Registry.CurrentUser.CreateSubKey(UninstallKey, writable: true);
        string version = typeof(Installer).Assembly.GetName().Version?.ToString(3) ?? "2.0.0";
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", version);
        key.SetValue("Publisher", "finalsiren1");
        key.SetValue("DisplayIcon", InstalledExe + ",0");
        key.SetValue("InstallLocation", InstallDir);
        key.SetValue("UninstallString", $"\"{InstalledExe}\" {UninstallArg}");
        key.SetValue("URLInfoAbout", "https://github.com/finalsiren1/MeijiChatboxStatus");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", (int)(new FileInfo(InstalledExe).Length / 1024), RegistryValueKind.DWord);
    }

    // ───────────────────────────── Uninstall ─────────────────────────────

    /// <summary>Removes everything the installer created. The running exe deletes itself after exiting.</summary>
    public static void Uninstall(bool removeSettings)
    {
        foreach (var p in Process.GetProcessesByName("MeijiChatboxStatus"))
            using (p)
                if (p.Id != Environment.ProcessId)
                {
                    try { p.Kill(entireProcessTree: true); p.WaitForExit(5000); }
                    catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
                }
        ChatboxGuard.Clear(SettingsStore.ReadOscSettings());

        Autorun.Apply(false, new Logger());
        foreach (string lnk in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk"),
                 })
            TryDelete(() => File.Delete(lnk));
        TryDelete(() => Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, throwOnMissingSubKey: false));
        TryDelete(() => Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeijiChatboxStatus"), true));
        if (removeSettings) TryDelete(() => Directory.Delete(SettingsStore.DataDir, true));

        // A running exe can't delete itself: let cmd remove the install folder once we're gone.
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c ping 127.0.0.1 -n 3 >nul & rmdir /s /q \"{InstallDir}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetTempPath(),
        })?.Dispose();
    }

    private static void TryDelete(Action delete)
    {
        try { delete(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException) { }
    }
}
