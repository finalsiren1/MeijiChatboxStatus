using System.Diagnostics;
using System.Globalization;
using MeijiChatboxStatus.Core;
using Microsoft.Web.WebView2.Core;

namespace MeijiChatboxStatus;

internal static class Program
{
    private const string MutexName = "MeijiChatboxStatus.SingleInstance";
    public const string ShowEventName = "MeijiChatboxStatus.ShowWindow";
    private const string WebView2Download = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    [STAThread]
    private static void Main(string[] args)
    {
        // VRChat.OSCQuery uses culture-sensitive string.LastIndexOf("/"); under th-TH (ICU) that never finds "/"
        // and AddEndpoint recurses until a stack overflow. Invariant culture also keeps logs in Gregorian years.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (args.Length >= 2 && args[0] == ChatboxGuard.GuardArg && int.TryParse(args[1], out int parentId))
        {
            ChatboxGuard.RunGuard(parentId);
            return;
        }
        if (args.Contains(Installer.InstallArg) && !Installer.RunningInstalled)
        {
            // Unattended install/update: same steps as the Install button, then open the installed app.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var log = new Logger();
            string exe = Installer.Install(Installer.Scan(cts.Token), log, step => log.Info($"install: {step}"));
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Installer.InstallDir })?.Dispose();
            return;
        }
        if (args.Contains(Installer.UninstallArg))
        {
            ApplicationConfiguration.Initialize();
            RunUninstall();
            return;
        }
        bool startHidden = args.Contains(Autorun.TrayArg);
        // Any copy that isn't the installed one (e.g. the downloaded exe) acts as the installer/updater.
        bool setupMode = !Installer.RunningInstalled;

        using var mutex = new Mutex(true, setupMode ? MutexName + ".Setup" : MutexName, out bool isFirst);
        if (!isFirst)
        {
            // Already running (maybe hidden in the tray): ask that instance to show itself.
            if (startHidden || setupMode) return;
            try { using var ev = EventWaitHandle.OpenExisting(ShowEventName); ev.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Crash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Crash(e.ExceptionObject as Exception);

        if (!HasWebView2Runtime()) return;

        using var showEvent = setupMode
            ? new EventWaitHandle(false, EventResetMode.AutoReset)
            : new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        Application.Run(new MainForm(showEvent, startHidden && !setupMode, setupMode));
    }

    private static void RunUninstall()
    {
        const string title = "Meiji Chatbox Status";
        if (MessageBox.Show("ถอนการติดตั้ง Meiji Chatbox Status ใช่ไหม?\nUninstall Meiji Chatbox Status?",
                title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
        bool removeSettings = MessageBox.Show(
            "ลบการตั้งค่าและตัวนับด้วยไหม? (กด No เพื่อเก็บไว้ ถ้าจะติดตั้งใหม่ภายหลัง)\nAlso delete your settings and counters? (No keeps them for a later reinstall)",
            title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        Installer.Uninstall(removeSettings);
        MessageBox.Show("ถอนการติดตั้งเรียบร้อยแล้ว\nMeiji Chatbox Status has been uninstalled.", title,
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Last resort: clear the chatbox, tell the user, and quit instead of leaving a stale status behind.</summary>
    private static void Crash(Exception? e)
    {
        ChatboxGuard.Clear(SettingsStore.ReadOscSettings());
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeijiChatboxStatus");
            File.AppendAllText(Path.Combine(dir, "app.log"), $"[{DateTime.Now:HH:mm:ss}] CRASH {e}{Environment.NewLine}");
        }
        catch (IOException) { }
        MessageBox.Show($"แอปเจอข้อผิดพลาดและต้องปิดตัว (ล้าง Chatbox ให้แล้ว)\nThe app hit an error and must close.\n\n{e?.Message}",
            "Meiji Chatbox Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(1);
    }

    private static bool HasWebView2Runtime()
    {
        try
        {
            CoreWebView2Environment.GetAvailableBrowserVersionString();
            return true;
        }
        catch (WebView2RuntimeNotFoundException)
        {
            var answer = MessageBox.Show(
                "แอปนี้ต้องใช้ Microsoft Edge WebView2 Runtime (Windows 10/11 ส่วนใหญ่มีอยู่แล้ว)\n" +
                "This app needs the Microsoft Edge WebView2 Runtime.\n\nเปิดหน้าดาวน์โหลดเลยไหม? / Open the download page?",
                "Meiji Chatbox Status", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (answer == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(WebView2Download) { UseShellExecute = true });
            return false;
        }
    }
}
