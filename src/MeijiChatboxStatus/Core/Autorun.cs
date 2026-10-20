using Microsoft.Win32;

namespace MeijiChatboxStatus.Core;

/// <summary>Adds/removes the app from Windows startup (HKCU Run key, no admin needed).</summary>
public static class Autorun
{
    public const string TrayArg = "--tray";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MeijiChatboxStatus";

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
                string value = $"\"{Environment.ProcessPath}\" {TrayArg}";
                if (key.GetValue(ValueName) as string != value) key.SetValue(ValueName, value);
            }
            else if (key.GetValue(ValueName) is not null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            log.Warn($"ตั้งค่าเปิดแอปพร้อม Windows ไม่สำเร็จ: {e.Message}");
        }
#endif
    }
}
