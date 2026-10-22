using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeijiChatboxStatus.Core;

public sealed class AppSettings
{
    public int Version { get; set; } = CurrentVersion;
    public const int CurrentVersion = 2;
    public string Language { get; set; } = "th";
    public string Theme { get; set; } = "sakura";

    public OscSettings Osc { get; set; } = new();
    public ChatboxSettings Chatbox { get; set; } = new();
    public List<StatusPage> Pages { get; set; } = [new() { Text = "🌸 {time} | ชิลล์ ๆ อยู่น้า~ 🌸\n{afk}\n{music}" }];
    public MediaSettings Media { get; set; } = new();
    public AfkSettings Afk { get; set; } = new();
    public List<CounterRule> Counters { get; set; } = [];
    public BehaviorSettings Behavior { get; set; } = new();

    /// <summary>Clamps values coming from the UI or a hand-edited file into safe ranges.</summary>
    public void Normalize()
    {
        Osc ??= new(); Chatbox ??= new(); Media ??= new(); Afk ??= new(); Behavior ??= new();
        Pages ??= []; Counters ??= [];
        Pages.RemoveAll(p => p is null);
        Counters.RemoveAll(c => c is null);
        if (Pages.Count == 0) Pages.Add(new StatusPage());
        if (Pages.Count > 20) Pages.RemoveRange(20, Pages.Count - 20);
        if (Counters.Count > 30) Counters.RemoveRange(30, Counters.Count - 30);

        Language = Language is "en" ? "en" : "th";
        Theme = string.IsNullOrWhiteSpace(Theme) ? "sakura" : Theme;
        Osc.Ip = string.IsNullOrWhiteSpace(Osc.Ip) ? "127.0.0.1" : Osc.Ip.Trim();
        Osc.SendPort = Math.Clamp(Osc.SendPort, 1, 65535);
        Osc.ListenPort = Math.Clamp(Osc.ListenPort, 1, 65535);
        Chatbox.IntervalSeconds = Math.Clamp(Chatbox.IntervalSeconds, ChatboxSettings.MinInterval, 30);
        Chatbox.RotateSeconds = Math.Clamp(Chatbox.RotateSeconds, 3, 600);
        Media.BarWidth = Math.Clamp(Media.BarWidth, 4, 20);
        foreach (var p in Pages) p.Text ??= "";
        foreach (var c in Counters)
        {
            c.Parameter = (c.Parameter ?? "").Trim();
            c.Message ??= "";
            c.Seconds = Math.Clamp(c.Seconds, 1, 30);
            c.Count = Math.Max(0, c.Count);
        }
        Media.Template ??= ""; Afk.Template ??= ""; Media.ExtraSources ??= "";
        Behavior.CloseAction = Behavior.CloseAction is "tray" or "exit" ? Behavior.CloseAction : "ask";
    }
}

public sealed class OscSettings
{
    public string Ip { get; set; } = "127.0.0.1";
    public int SendPort { get; set; } = 9000;
    /// <summary>Used only when OSCQuery is off (or fails). With OSCQuery a free port is chosen automatically.</summary>
    public int ListenPort { get; set; } = 9001;
    public bool UseOscQuery { get; set; } = true;
}

public sealed class ChatboxSettings
{
    public const double MinInterval = 1.5;
    public double IntervalSeconds { get; set; } = 2;
    /// <summary>Seconds each status page stays before rotating to the next one.</summary>
    public double RotateSeconds { get; set; } = 10;
}

public sealed class StatusPage
{
    public bool Enabled { get; set; } = true;
    public string Text { get; set; } = "";
}

public sealed class MediaSettings
{
    public bool Enabled { get; set; } = true;
    public string Template { get; set; } = "🎵 {title} - {artist}\n{bar} {pos}/{dur}";
    public bool ShowWhenPaused { get; set; } = false;
    public string BarStyle { get; set; } = "line";
    public int BarWidth { get; set; } = 10;
    /// <summary>Extra allowed players/sites (comma separated keywords) on top of <see cref="MediaFilter"/>'s built-in list.</summary>
    public string ExtraSources { get; set; } = "";
}

public sealed class AfkSettings
{
    public bool Enabled { get; set; } = true;
    public string Template { get; set; } = "💤 AFK อยู่น้า ({afktime})";
}

public sealed class CounterRule
{
    public bool Enabled { get; set; } = true;
    public string Parameter { get; set; } = "";
    public string Message { get; set; } = "";
    public double Seconds { get; set; } = 4;
    public int Count { get; set; }
}

public sealed class BehaviorSettings
{
    /// <summary>"Open with VRChat": a hidden watcher started at Windows login (see <see cref="Autorun"/>) opens the app each
    /// time VRChat starts; the app starts sending when it sees VRChat.</summary>
    public bool AutoWithVrc { get; set; } = true;
    /// <summary>Exit the app (clearing the chatbox) when VRChat closes. When off, open-with-VRChat just stops sending.</summary>
    public bool CloseWithVrc { get; set; } = true;
    /// <summary>What the window's X button does: "ask" (popup every time), "tray" (hide) or "exit".</summary>
    public string CloseAction { get; set; } = "ask";
    /// <summary>Look for a newer release on GitHub at startup and every few hours (installing always asks first).</summary>
    public bool CheckUpdates { get; set; } = true;
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext;

public sealed class SettingsStore
{
    public static string DataDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeijiChatboxStatus");

    public static string SettingsFile { get; } = Path.Combine(DataDir, "settings.json");

    public string FilePath => SettingsFile;

    /// <summary>Reads just the OSC target without logging or migrations (used by the exit guard process).</summary>
    public static OscSettings ReadOscSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
                return JsonSerializer.Deserialize(File.ReadAllText(SettingsFile), SettingsJsonContext.Default.AppSettings)?.Osc ?? new();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        return new();
    }

    public AppSettings Load(Logger log)
    {
        try
        {
            // Pre-release builds were named "ChatboxStatus"; carry their settings over once.
            string oldPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatboxStatus", "settings.json");
            if (!File.Exists(FilePath) && File.Exists(oldPath))
            {
                Directory.CreateDirectory(DataDir);
                File.Copy(oldPath, FilePath);
            }

            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize(File.ReadAllText(FilePath), SettingsJsonContext.Default.AppSettings) ?? new();
                s.Normalize();
                if (s.Version < AppSettings.CurrentVersion)
                {
                    // v1 (2.0.4 and older) ignored "close with VRChat" while "open with VRChat" was on; both now default on.
                    if (s.Version < 2) s.Behavior.CloseWithVrc = true;
                    s.Version = AppSettings.CurrentVersion;
                    Save(s, log);
                }
                return s;
            }
            var imported = LegacyImport.TryImport(log);
            if (imported is not null) { imported.Normalize(); Save(imported, log); return imported; }
        }
        catch (Exception e)
        {
            // Keep the broken file so the user doesn't lose it, then start fresh.
            string backup = FilePath + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try { File.Copy(FilePath, backup, true); } catch (IOException) { }
            log.Error($"อ่านไฟล์ตั้งค่าไม่ได้ ใช้ค่าเริ่มต้นแทน (สำรองไฟล์เดิมไว้ที่ {backup}): {e.Message}");
        }
        var fresh = new AppSettings();
        fresh.Normalize();
        return fresh;
    }

    public void Save(AppSettings settings, Logger log)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings));
            File.Move(tmp, FilePath, overwrite: true); // atomic replace: a crash never leaves a half-written file
        }
        catch (Exception e)
        {
            log.Error($"บันทึกการตั้งค่าไม่สำเร็จ: {e.Message}");
        }
    }
}

/// <summary>Imports the line-based settings file of the old VrcAfkNative app.</summary>
internal static class LegacyImport
{
    /// <summary>Looks next to the exe / in the working directory (portable use).</summary>
    public static AppSettings? TryImport(Logger log) =>
        new[] { AppContext.BaseDirectory, Environment.CurrentDirectory }
            .Select(dir => Path.Combine(dir, "vrc_afk_settings.txt"))
            .Where(File.Exists)
            .Select(path => FromFile(path, log))
            .FirstOrDefault(s => s is not null);

    public static AppSettings? FromFile(string path, Logger log)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 12) return null;

        var s = new AppSettings();
        s.Pages = [new() { Text = (lines[0].Trim() + "\n{music}").Trim() }];
        s.Media.Enabled = bool.TryParse(lines[1], out var m) && m;
        for (int i = 0; i < 5; i++)
        {
            string param = lines[2 + i * 2].Trim();
            if (param.Length == 0) continue;
            s.Counters.Add(new CounterRule { Parameter = param, Message = lines[3 + i * 2].Trim().Replace("{0}", "{count}") });
        }
        log.Info($"นำเข้าการตั้งค่าจากแอปเวอร์ชันเก่า ({path}) แล้ว");
        return s;
    }
}
