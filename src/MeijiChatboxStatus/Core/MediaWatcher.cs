using Windows.Media.Control;
using Windows.Storage.Streams;
using PlaybackStatus = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus;

namespace MeijiChatboxStatus.Core;

public sealed record MediaInfo(
    string Title, string Artist, string Source, bool Playing,
    TimeSpan Position, TimeSpan Duration, string? Thumbnail, bool Allowed);

/// <summary>
/// Reads "now playing" from Windows (Spotify, browsers/YouTube, Apple Music, ...) via the System Media Transport Controls.
/// Apps only report their position occasionally, so the position is extrapolated between reports.
/// </summary>
public sealed class MediaWatcher
{
    private const int MaxThumbnailBytes = 400_000;

    private readonly Logger _log;
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private bool _initFailed;
    private bool _busy;

    private string _trackKey = "";
    private readonly Dictionary<string, string?> _thumbnails = []; // per track; null = no artwork
    private readonly Dictionary<string, (bool Allowed, DateTime Checked)> _approval = [];
    private List<string> _tabTitles = [];
    private DateTime _tabTitlesAt = DateTime.MinValue;
    private static readonly TimeSpan TabTitlesMaxAge = TimeSpan.FromSeconds(5);

    /// <summary>User-added allowed players/sites (see <see cref="MediaSettings.ExtraSources"/>).</summary>
    public string? ExtraSources { get; set; }
    private TimeSpan _lastReported = TimeSpan.MinValue;
    private TimeSpan _estimate;
    private DateTime _lastPoll = DateTime.UtcNow;

    public MediaInfo? Current { get; private set; }

    public MediaWatcher(Logger log) => _log = log;

    public async Task InitAsync()
    {
        try { _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(); }
        catch (Exception e)
        {
            _initFailed = true;
            _log.Error($"เข้าถึงระบบเพลงของ Windows ไม่ได้ ฟีเจอร์เพลงจะใช้ไม่ได้: {e.Message}");
        }
    }

    /// <summary>Refreshes <see cref="Current"/>. Safe to call on a timer; overlapping calls are skipped.</summary>
    public async Task PollAsync()
    {
        if (_manager is null || _initFailed || _busy) return;
        _busy = true;
        try
        {
            // Look at every session, not just the one Windows calls "current": a TikTok tab shouldn't hide Spotify.
            var candidates = new List<Candidate>();
            var titles = new TitleLookup();
            var current = _manager.GetCurrentSession();
            foreach (var s in _manager.GetSessions())
            {
                var p = await s.TryGetMediaPropertiesAsync();
                var pb = s.GetPlaybackInfo();
                string t = p?.Title?.Trim() ?? "";
                if (p is null || pb is null || t.Length == 0) continue;
                string a = (p.Artist ?? "").Trim();
                if (a.Length == 0) a = (p.AlbumArtist ?? "").Trim();
                bool allowed = await IsAllowedAsync(s.SourceAppUserModelId, t, a, titles);
                candidates.Add(new Candidate(s, p, pb, t, a, allowed,
                    pb.PlaybackStatus == PlaybackStatus.Playing, current is not null && s.SourceAppUserModelId == current.SourceAppUserModelId));
            }
            // Several tabs/apps can hold a session (e.g. a paused YouTube tab next to the one playing): prefer what's playing.
            var pick = candidates
                .OrderByDescending(c => c.Allowed && c.Playing)
                .ThenByDescending(c => c.Allowed)
                .ThenByDescending(c => c.Playing)
                .ThenByDescending(c => c.IsCurrent)
                .FirstOrDefault();

            // Nothing approved is playing, but a browser is involved: the music may be in a tab the browser didn't report.
            if (pick is not { Allowed: true, Playing: true } && candidates.FirstOrDefault(c => MediaFilter.IsBrowser(c.Session.SourceAppUserModelId)) is { } browser)
            {
                titles.Tabs ??= await GetTabTitlesAsync();
                if (MediaFilter.PlayingMusicTab(titles.Tabs, ExtraSources) is { } tabTitle)
                {
                    _trackKey = "tab|" + tabTitle;
                    Current = new MediaInfo(tabTitle, "", FriendlySource(browser.Session.SourceAppUserModelId), true,
                        TimeSpan.Zero, TimeSpan.Zero, null, true);
                    return;
                }
            }
            if (pick is null) { Current = null; return; }

            var session = pick.Session;
            var props = pick.Props;
            var playback = pick.Playback;
            var timeline = session.GetTimelineProperties();
            if (timeline is null) { Current = null; return; }
            string title = pick.Title, artist = pick.Artist;

            bool playing = pick.Playing;
            double rate = playback.PlaybackRate is > 0 and < 8 ? playback.PlaybackRate.Value : 1.0;
            var duration = timeline.EndTime - timeline.StartTime;
            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;

            var now = DateTime.UtcNow;
            var delta = now - _lastPoll;
            _lastPoll = now;

            string key = $"{session.SourceAppUserModelId}|{title}|{artist}|{(int)duration.TotalSeconds}";
            bool newTrack = key != _trackKey;
            if (newTrack) _trackKey = key;
            // Artwork of unapproved media (could be anything) is never loaded or shown.
            string? thumbnail = null;
            if (pick.Allowed && !_thumbnails.TryGetValue(key, out thumbnail))
            {
                if (_thumbnails.Count > 30) _thumbnails.Clear();
                _thumbnails[key] = thumbnail = await ReadThumbnailAsync(props.Thumbnail);
            }

            var reported = timeline.Position - timeline.StartTime;
            if (newTrack || reported != _lastReported)
            {
                _lastReported = reported;
                _estimate = reported;
                // LastUpdatedTime says when the app took that reading; use it if it looks sane.
                var age = now - timeline.LastUpdatedTime.UtcDateTime;
                if (playing && age > TimeSpan.Zero && age < TimeSpan.FromMinutes(10)) _estimate += age * rate;
            }
            else if (playing)
            {
                _estimate += delta * rate;
            }

            if (duration > TimeSpan.Zero && _estimate > duration) _estimate = duration;
            if (_estimate < TimeSpan.Zero) _estimate = TimeSpan.Zero;

            Current = new MediaInfo(title, artist, FriendlySource(session.SourceAppUserModelId), playing, _estimate, duration,
                thumbnail, pick.Allowed);
        }
        catch (Exception e)
        {
            // Sessions can vanish mid-call when the player closes; that's normal, just report "nothing playing".
            Current = null;
            if (e is not (InvalidOperationException or System.Runtime.InteropServices.COMException))
                _log.Warn($"อ่านข้อมูลเพลงผิดพลาด: {e.Message}");
        }
        finally { _busy = false; }
    }

    private sealed record Candidate(
        GlobalSystemMediaTransportControlsSession Session, GlobalSystemMediaTransportControlsSessionMediaProperties Props,
        GlobalSystemMediaTransportControlsSessionPlaybackInfo Playback, string Title, string Artist, bool Allowed, bool Playing, bool IsCurrent);

    /// <summary>
    /// Approval is remembered per track once granted (switching browser tabs afterwards doesn't hide it);
    /// a "no" is re-checked every few seconds in case the user brings the music tab to the front.
    /// </summary>
    private async Task<bool> IsAllowedAsync(string appId, string title, string artist, TitleLookup titles)
    {
        string key = $"{appId}|{title}|{artist}";
        var now = DateTime.UtcNow;
        if (_approval.TryGetValue(key, out var cached) && (cached.Allowed || now - cached.Checked < TimeSpan.FromSeconds(3)))
            return cached.Allowed;

        bool allowed;
        if (!MediaFilter.IsBrowser(appId)) allowed = MediaFilter.IsMusicApp(appId, ExtraSources);
        else
        {
            // Active tabs first (cheap); then every tab, so music left playing in a background tab is found too.
            titles.Windows ??= MediaFilter.VisibleWindowTitles();
            allowed = titles.Windows.Any(w => MediaFilter.IsMusicTab(w, title, artist, ExtraSources));
            if (!allowed)
            {
                titles.Tabs ??= await GetTabTitlesAsync();
                allowed = titles.Tabs.Any(w => MediaFilter.IsMusicTab(w, title, artist, ExtraSources));
            }
        }
        if (_approval.Count > 200) _approval.Clear();
        _approval[key] = (allowed, now);
        return allowed;
    }

    private async Task<List<string>> GetTabTitlesAsync()
    {
        if (DateTime.UtcNow - _tabTitlesAt < TabTitlesMaxAge) return _tabTitles;
        _tabTitles = await Task.Run(MediaFilter.BrowserTabTitles);
        _tabTitlesAt = DateTime.UtcNow;
        return _tabTitles;
    }

    /// <summary>Window/tab titles fetched at most once per poll.</summary>
    private sealed class TitleLookup
    {
        public List<string>? Windows;
        public List<string>? Tabs;
    }

    /// <summary>Clears remembered decisions (call when the user edits the allowed-sources list).</summary>
    public void ResetApprovals()
    {
        _approval.Clear();
        _tabTitlesAt = DateTime.MinValue;
    }

    private async Task<string?> ReadThumbnailAsync(IRandomAccessStreamReference? reference)
    {
        if (reference is null) return null;
        try
        {
            using var stream = await reference.OpenReadAsync();
            if (stream.Size == 0 || stream.Size > MaxThumbnailBytes) return null;
            var buffer = new byte[stream.Size];
            using (var input = stream.AsStreamForRead()) await input.ReadExactlyAsync(buffer);
            string type = string.IsNullOrEmpty(stream.ContentType) ? "image/png" : stream.ContentType;
            return $"data:{type};base64,{Convert.ToBase64String(buffer)}";
        }
        catch (Exception) { return null; } // artwork is decorative only
    }

    private static string FriendlySource(string appId)
    {
        string id = appId.ToLowerInvariant();
        if (id.Contains("spotify")) return "Spotify";
        if (id.Contains("chrome")) return "Chrome";
        if (id.Contains("msedge")) return "Edge";
        if (id.Contains("firefox")) return "Firefox";
        if (id.Contains("opera")) return "Opera";
        if (id.Contains("brave")) return "Brave";
        if (id.Contains("zunemusic") || id.Contains("media player")) return "Media Player";
        if (id.Contains("applemusic") || id.Contains("itunes")) return "Apple Music";
        if (id.Contains("youtube")) return "YouTube Music";
        string name = appId.Split('!')[0].Split('\\').Last();
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }
}
