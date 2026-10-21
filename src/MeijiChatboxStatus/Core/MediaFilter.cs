using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace MeijiChatboxStatus.Core;

/// <summary>
/// Decides whether a media session is music worth showing. Windows only says *which app* is playing, so:
/// music apps are matched by app id, and browsers are matched by finding a browser tab whose title contains the track title
/// together with an allowed site name (window titles cover the active tabs; <see cref="BrowserTabTitles"/> covers the rest). Anything unproven is hidden — TikTok, IG, Facebook,
/// random sites and NSFW never reach the chatbox.
/// </summary>
public static partial class MediaFilter
{
    /// <summary>Music players, matched against the session's app id (case-insensitive substring).</summary>
    public static readonly string[] MusicApps =
    [
        "spotify", "applemusic", "itunes", "zunemusic", "tidal", "deezer", "amazonmusic", "amazon music",
        "youtube-music", "youtubemusic", "pear-desktop", "musicbee", "foobar2000", "aimp", "winamp", "cider",
        "joox", "soundcloud", "qobuz", "audirvana", "dopamine", "plexamp", "nuclear",
    ];

    /// <summary>Music sites, matched against the browser's tab title.</summary>
    public static readonly string[] MusicSites =
    [
        "YouTube", "YouTube Music", "Spotify", "Apple Music", "SoundCloud", "Deezer", "TIDAL", "JOOX",
        "Amazon Music", "Bandcamp", "Audiomack", "Mixcloud", "Pandora", "Qobuz",
    ];

    private static readonly string[] Browsers =
        ["chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "arc", "browser", "zen", "floorp", "thorium", "waterfox", "librewolf",
         "308046b0af4a39cb" /* Firefox's app id */];

    public static bool IsBrowser(string appId)
    {
        string id = appId.ToLowerInvariant();
        return Browsers.Any(id.Contains);
    }

    public static IEnumerable<string> ParseExtra(string? extra) =>
        (extra ?? "").Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length >= 2);

    /// <summary>For non-browser apps: is this a known (or user-allowed) music player?</summary>
    public static bool IsMusicApp(string appId, string? extra)
    {
        string id = appId.ToLowerInvariant();
        return MusicApps.Concat(ParseExtra(extra)).Any(k => id.Contains(k.ToLowerInvariant()));
    }

    /// <summary>
    /// For browsers: does a tab title prove the track comes from a music site? The title must contain the track title
    /// and a site name as a separate " - Site" / " | Site" / " on Site" part, so a TikTok page titled "... YouTube ..." doesn't pass.
    /// Spotify's web player titles tabs "Track • Artist" without the site name, so that exact shape is accepted too.
    /// </summary>
    public static bool IsMusicTab(string windowTitle, string trackTitle, string artist, string? extra)
    {
        if (trackTitle.Length == 0 || !windowTitle.Contains(trackTitle, StringComparison.OrdinalIgnoreCase)) return false;

        if (artist.Length > 0 && windowTitle.Contains($"{trackTitle} • {artist}", StringComparison.OrdinalIgnoreCase)) return true;

        foreach (string site in MusicSites.Concat(ParseExtra(extra)))
        {
            foreach (string sep in new[] { " - ", " – ", " — ", " | ", " · ", " on " })
            {
                int i = windowTitle.IndexOf(sep + site, StringComparison.OrdinalIgnoreCase);
                if (i < 0) continue;
                int end = i + sep.Length + site.Length;
                // The site name must end there (next char is the end, a space or another separator), not be a prefix of a word.
                if (end == windowTitle.Length || !char.IsLetterOrDigit(windowTitle[end])) return true;
            }
        }
        return false;
    }

    // Chromium browsers (Chrome, Edge, ...) append this to the accessible name of a tab that is making sound (English UI);
    // CollectTabs adds the first one to Firefox tabs that are playing.
    private static readonly string[] AudioMarkers = [" - Audio playing", " - Playing audio"];
    [GeneratedRegex(@"^\(\d+\+?\)\s*")]
    private static partial Regex NotificationCountRegex();

    /// <summary>
    /// Chrome hands Windows only one media session per browser, often not the tab playing music (e.g. a paused video elsewhere).
    /// This finds a tab that is audibly playing on a music site and returns its track title (site name and markers removed),
    /// or null. The tab title must end in " - Site" right before the marker, so only allowed sites pass.
    /// </summary>
    public static string? PlayingMusicTab(IEnumerable<string> tabTitles, string? extra)
    {
        var sites = MusicSites.Concat(ParseExtra(extra)).ToList();
        foreach (string tab in tabTitles)
        {
            int marker = AudioMarkers.Select(m => tab.IndexOf(m, StringComparison.OrdinalIgnoreCase)).Where(i => i > 0).DefaultIfEmpty(-1).Min();
            if (marker < 0) continue;
            string head = tab[..marker];
            foreach (string site in sites)
            {
                foreach (string sep in new[] { " - ", " – ", " — ", " | ", " · " })
                {
                    if (!head.EndsWith(sep + site, StringComparison.OrdinalIgnoreCase)) continue;
                    string title = NotificationCountRegex().Replace(head[..^(sep.Length + site.Length)], "").Trim();
                    if (title.Length > 0) return title;
                }
            }
        }
        return null;
    }

    /// <summary>Titles of all visible top-level windows (browser windows show their active tab's title).</summary>
    public static List<string> VisibleWindowTitles()
    {
        var titles = new List<string>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            int len = GetWindowTextLength(hwnd);
            if (len <= 0) return true;
            var sb = new StringBuilder(len + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            titles.Add(sb.ToString());
            return true;
        }, IntPtr.Zero);
        return titles;
    }

    /// <summary>
    /// Titles of every tab in open browser windows, read through UI Automation (the tab strip lists all tabs, not just the
    /// active one), so music in a background tab is recognised too. Web page contents are never touched: the walk skips
    /// documents and stops at each tab. Slow-ish (tens of ms per window), so call it off the UI thread and cache the result.
    /// </summary>
    public static List<string> BrowserTabTitles()
    {
        var titles = new List<string>();
        foreach (IntPtr hwnd in BrowserWindows())
        {
            try { CollectTabs(AutomationElement.FromHandle(hwnd), titles); }
            // The window closed or stopped answering mid-walk: its tabs just aren't listed this time.
            catch (Exception e) when (e is ElementNotAvailableException or COMException or InvalidOperationException or TimeoutException) { }
        }
        return titles;
    }

    private static void CollectTabs(AutomationElement window, List<string> titles)
    {
        var walker = TreeWalker.ControlViewWalker;
        var queue = new Queue<(AutomationElement Element, int Depth)>();
        queue.Enqueue((window, 0));
        int visited = 0;
        while (queue.Count > 0 && visited++ < 4000)
        {
            var (element, depth) = queue.Dequeue();
            var type = element.Current.ControlType;
            if (type == ControlType.TabItem)
            {
                string name = element.Current.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;
                // Firefox doesn't put the sound state in the tab name like Chromium does; mark it the same way so
                // PlayingMusicTab treats both alike.
                if (element.Current.ClassName == "tabbrowser-tab" && FirefoxTabPlaysSound(element, walker)) name += AudioMarkers[0];
                titles.Add(name);
                continue;
            }
            if ((type == ControlType.Document && depth > 0) || depth >= 16) continue;
            for (var child = walker.GetFirstChild(element); child is not null; child = walker.GetNextSibling(child))
                queue.Enqueue((child, depth + 1));
        }
    }

    // Names of Firefox's tab sound button when the tab is NOT audibly playing: autoplay blocked ("Play tab") or muted.
    private static readonly string[] FirefoxSilentButtons = ["Play tab", "Unmute tab", "เล่นแท็บ", "เลิกปิดเสียงแท็บ"];

    /// <summary>
    /// Firefox (and its forks) shows a sound button on a tab only while it has media; the close button is the only other button.
    /// The button's name is in the browser's language, so any sound button counts except the known "blocked"/"muted" names.
    /// </summary>
    private static bool FirefoxTabPlaysSound(AutomationElement tab, TreeWalker walker)
    {
        for (var child = walker.GetFirstChild(tab); child is not null; child = walker.GetNextSibling(child))
        {
            var info = child.Current;
            if (info.ControlType != ControlType.Button || info.ClassName.Contains("close", StringComparison.OrdinalIgnoreCase)) continue;
            if (!FirefoxSilentButtons.Contains(info.Name, StringComparer.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>Visible top-level windows that belong to a browser process.</summary>
    private static List<IntPtr> BrowserWindows()
    {
        var windows = new List<IntPtr>();
        var names = new Dictionary<uint, bool>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || GetWindowTextLength(hwnd) <= 0) return true;
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (!names.TryGetValue(pid, out bool isBrowser))
            {
                try { using var p = Process.GetProcessById((int)pid); isBrowser = IsBrowser(p.ProcessName); }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException) { isBrowser = false; } // already gone
                names[pid] = isBrowser;
            }
            if (isBrowser) windows.Add(hwnd);
            return true;
        }, IntPtr.Zero);
        return windows;
    }

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLength(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
}
