using System.Diagnostics;
using Microsoft.Win32;

namespace MeijiChatboxStatus.Core;

/// <summary>
/// "Open with VRChat". Windows can't start an app when another one launches (not without admin), so a tiny watcher
/// process (no window, no tray icon, no OSC) runs from Windows login via the HKCU Run key and opens the app each time
/// VRChat starts. It is separate from the app: exiting the app leaves it waiting for the next VRChat launch.
/// </summary>
public static class Autorun
{
    public const string WatchArg = "--watch";
    /// <summary>Run-key argument of v2.0.4 and older; treated as <see cref="WatchArg"/>.</summary>
    public const string LegacyTrayArg = "--tray";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MeijiChatboxStatus";
    private const string WatcherMutexName = "MeijiChatboxStatus.Watcher";
    private const string StopEventName = "MeijiChatboxStatus.StopWatcher";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    /// <summary>Registers (and starts) or removes (and stops) the watcher.</summary>
    public static void Apply(bool enabled, Logger log)
    {
#if DEBUG
        // Never register a dev build from bin\Debug as the startup app.
        _ = enabled; _ = log;
#else
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (enabled)
            {
                // Re-written every launch so it follows the exe if the user moves it.
                string value = $"\"{Environment.ProcessPath}\" {WatchArg}";
                if (key.GetValue(ValueName) as string != value) key.SetValue(ValueName, value);
            }
            else if (key.GetValue(ValueName) is not null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            log.Warn($"ตั้งค่าเปิดโปรแกรมพร้อม VRChat ไม่สำเร็จ: {e.Message}");
        }
        if (enabled) StartWatcher(log);
        else StopWatcher();
#endif
    }

    /// <summary>Starts the watcher now (so the option works without signing out) unless one is already running.</summary>
    private static void StartWatcher(Logger log)
    {
        if (Mutex.TryOpenExisting(WatcherMutexName, out var existing)) { existing.Dispose(); return; }
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, WatchArg)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            })?.Dispose();
        }
        catch (Exception e)
        {
            log.Warn($"เปิดตัวเฝ้า VRChat ไม่สำเร็จ (จะลองใหม่ตอนเปิดเครื่องครั้งหน้า): {e.Message}");
        }
    }

    private static void StopWatcher()
    {
        if (EventWaitHandle.TryOpenExisting(StopEventName, out var stop))
            using (stop) stop.Set();
    }

    /// <summary>
    /// Entry point of the watcher process: opens the app whenever VRChat goes from closed to running. VRChat already
    /// running when the watcher starts doesn't count, so closing the app mid-session doesn't reopen it.
    /// </summary>
    public static void RunWatcher(string appMutexName, Logger log)
    {
        using var mutex = new Mutex(true, WatcherMutexName, out bool isFirst);
        if (!isFirst) return;
        using var stop = new EventWaitHandle(false, EventResetMode.AutoReset, StopEventName);

        bool wasRunning = VrcWatcher.IsVrcRunning();
        // This process mostly sleeps: hand its startup memory back to Windows.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        ChatboxGuard.TrimWorkingSet();

        while (!stop.WaitOne(PollInterval))
        {
            bool running = VrcWatcher.IsVrcRunning();
            if (running && !wasRunning && !AppRunning(appMutexName))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(Environment.ProcessPath!)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(Environment.ProcessPath!)!,
                    })?.Dispose();
                }
                catch (Exception e)
                {
                    log.Warn($"เปิดโปรแกรมตอน VRChat เปิดไม่สำเร็จ: {e.Message}");
                }
                ChatboxGuard.TrimWorkingSet(); // launching through the shell pulls in memory the watcher won't need again
            }
            wasRunning = running;
        }
    }

    private static bool AppRunning(string appMutexName)
    {
        if (!Mutex.TryOpenExisting(appMutexName, out var m)) return false;
        m.Dispose();
        return true;
    }
}
