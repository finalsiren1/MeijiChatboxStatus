using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace MeijiChatboxStatus.Core;

/// <summary>
/// Makes sure the chatbox is cleared however the app ends. A normal exit clears it directly; for cases no process
/// can intercept (Task Manager "End task", crashes) a tiny second process waits for the app to vanish and clears it.
/// </summary>
public static class ChatboxGuard
{
    public const string GuardArg = "--guard";

    /// <summary>Starts the watcher process for the current app instance.</summary>
    public static void Launch(Logger log)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"{GuardArg} {Environment.ProcessId}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            })?.Dispose();
        }
        catch (Exception e)
        {
            log.Warn($"เปิดตัวเฝ้าล้าง Chatbox ไม่สำเร็จ (ปิดแอปแบบปกติยังล้างได้): {e.Message}");
        }
    }

    /// <summary>Entry point of the watcher process: block until the app exits, then clear the chatbox.</summary>
    public static void RunGuard(int parentId)
    {
        try
        {
            using var parent = Process.GetProcessById(parentId);
            // This process only sleeps until the app exits: hand its startup memory back to Windows.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            parent.WaitForExit();
        }
        catch (ArgumentException) { } // already gone
        catch (InvalidOperationException) { }
        Clear(SettingsStore.ReadOscSettings());
    }

    /// <summary>Sends an empty chatbox message. Safe to call from any thread, even while crashing.</summary>
    public static void Clear(OscSettings osc)
    {
        try
        {
            var ip = IPAddress.TryParse(osc.Ip, out var parsed) ? parsed : IPAddress.Loopback;
            byte[] packet = Osc.Encode("/chatbox/input", "", true, false);
            using var udp = new UdpClient(AddressFamily.InterNetwork);
            udp.Send(packet, packet.Length, new IPEndPoint(ip, osc.SendPort));
        }
        catch (Exception) { } // nothing more we can do on the way out
    }

    [System.Runtime.InteropServices.DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr process);
}
