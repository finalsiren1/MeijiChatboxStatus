using System.Runtime.InteropServices;
using System.Text;

namespace MeijiChatboxStatus.Core;

/// <summary>
/// Decides whether a media session is music worth showing. Windows only says *which app* is playing, so:
/// music apps are matched by app id, and browsers are matched by finding a browser window whose title (the active tab)
/// contains the track title together with an allowed site name. Anything unproven is hidden — TikTok, IG, Facebook,
/// random sites and NSFW never reach the chatbox.
/// </summary>
public static class MediaFilter
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

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLength(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
}
