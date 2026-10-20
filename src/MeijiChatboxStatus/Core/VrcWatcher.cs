using System.Diagnostics;

namespace MeijiChatboxStatus.Core;

/// <summary>Detects whether VRChat is running so the app can start/stop/close along with it.</summary>
public sealed class VrcWatcher
{
    public bool IsRunning { get; private set; }

    /// <summary>Raised with the new state when VRChat starts or exits.</summary>
    public event Action<bool>? Changed;

    public void Check()
    {
        bool running;
        var processes = Process.GetProcessesByName("VRChat");
        running = processes.Length > 0;
        foreach (var p in processes) p.Dispose();

        if (running == IsRunning) return;
        IsRunning = running;
        Changed?.Invoke(running);
    }
}
