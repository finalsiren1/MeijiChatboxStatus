using System.Globalization;

namespace MeijiChatboxStatus.Core;

/// <summary>
/// Builds the chatbox text and decides when to send it. All members run on the UI thread;
/// OSC input arrives on a background thread and is marshalled here through <see cref="SynchronizationContext"/>.
/// </summary>
public sealed class StatusEngine
{
    /// <summary>VRChat drops chatbox messages sent too quickly; never send faster than this.</summary>
    private static readonly TimeSpan HardMinGap = TimeSpan.FromSeconds(1.2);
    /// <summary>Re-send an unchanged message this often so it stays visible.</summary>
    private static readonly TimeSpan KeepAlive = TimeSpan.FromSeconds(6);

    private static readonly string[] ThaiDays = ["อาทิตย์", "จันทร์", "อังคาร", "พุธ", "พฤหัส", "ศุกร์", "เสาร์"];
    private static readonly string[] EnDays = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    private readonly OscService _osc;
    private readonly MediaWatcher _media;
    private readonly Logger _log;
    private readonly SynchronizationContext _ui;

    private AppSettings _settings;
    private volatile HashSet<string> _watched = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _paramState = new(StringComparer.Ordinal);

    private string? _interruptText;
    private DateTime _interruptUntil;
    private int _pageIndex;
    private DateTime _pageSince = DateTime.Now;
    private string _lastSent = "";
    private DateTime _lastSendTime = DateTime.MinValue;
    private DateTime _runningSince;

    public bool IsRunning { get; private set; }
    public bool IsAfk { get; private set; }
    public DateTime AfkSince { get; private set; }
    public string Preview { get; private set; } = "";
    public string? LastTriggered { get; private set; }

    /// <summary>Raised when counter values change (so the host can persist them).</summary>
    public event Action? CountsChanged;
    public event Action? RunningChanged;

    public StatusEngine(AppSettings settings, OscService osc, MediaWatcher media, Logger log)
    {
        _settings = settings;
        _osc = osc;
        _media = media;
        _log = log;
        _ui = SynchronizationContext.Current ?? throw new InvalidOperationException("StatusEngine must be created on the UI thread");
        _osc.Received += OnOscBackground;
        UpdateWatched();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        UpdateWatched();
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        _runningSince = DateTime.Now;
        _lastSendTime = DateTime.MinValue; // send right away
        _lastSent = "";
        _log.Info("▶ เริ่มส่ง Status เข้า Chatbox");
        RunningChanged?.Invoke();
        Tick();
    }

    /// <summary>Stops sending and always clears the chatbox, so no stale status is left behind.</summary>
    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _osc.Send("/chatbox/input", "", true, false);
        _log.Info("⏹ หยุดส่งแล้ว และล้าง Chatbox");
        RunningChanged?.Invoke();
    }

    /// <summary>Called every ~250 ms.</summary>
    public void Tick()
    {
        var now = DateTime.Now;
        if (_interruptText is not null && now >= _interruptUntil) _interruptText = null;

        Preview = _interruptText ?? BuildStatus(now);
        if (!IsRunning) return;

        var sinceLast = now - _lastSendTime;
        bool changed = Preview != _lastSent;
        var minGap = _interruptText is not null
            ? HardMinGap
            : TimeSpan.FromSeconds(Math.Max(_settings.Chatbox.IntervalSeconds, HardMinGap.TotalSeconds));

        if ((changed && sinceLast >= minGap) || sinceLast >= KeepAlive)
        {
            if (_osc.Send("/chatbox/input", Preview, true, false)) _lastSent = Preview;
            _lastSendTime = now; // also on failure, so a missing network doesn't spin
        }
    }

    /// <summary>Forgets AFK/parameter state when VRChat closes (it will resend everything next session).</summary>
    public void ResetAvatarState()
    {
        IsAfk = false;
        _paramState.Clear();
    }

    public void ResetCounter(int index)
    {
        if (index < 0 || index >= _settings.Counters.Count) return;
        _settings.Counters[index].Count = 0;
        CountsChanged?.Invoke();
    }

    public void ResetAllCounters()
    {
        foreach (var c in _settings.Counters) c.Count = 0;
        CountsChanged?.Invoke();
    }

    /// <summary>Lets the user try a counter from the UI without touching their avatar.</summary>
    public void TestCounter(int index)
    {
        if (index >= 0 && index < _settings.Counters.Count) Trigger(_settings.Counters[index], test: true);
    }

    private string BuildStatus(DateTime now)
    {
        var pages = _settings.Pages.Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Text)).ToList();
        if (pages.Count == 0) return "";

        if (now - _pageSince >= TimeSpan.FromSeconds(_settings.Chatbox.RotateSeconds))
        {
            _pageIndex++;
            _pageSince = now;
        }
        if (_pageIndex >= pages.Count) _pageIndex = 0;

        return TextFormat.Clamp(TextFormat.Render(pages[_pageIndex].Text, (name, arg) => Resolve(name, arg, now)));
    }

    private string? Resolve(string name, string? arg, DateTime now)
    {
        bool th = _settings.Language == "th";
        switch (name)
        {
            case "time": return now.ToString("HH:mm", CultureInfo.InvariantCulture);
            case "time12": return now.ToString("h:mm tt", CultureInfo.InvariantCulture);
            case "date": return now.ToString("dd/MM", CultureInfo.InvariantCulture);
            case "day": return (th ? ThaiDays : EnDays)[(int)now.DayOfWeek];
            case "session": return IsRunning ? TextFormat.Duration(now - _runningSince) : "0:00";
            case "music": return RenderMusic();
            case "afk":
                if (!_settings.Afk.Enabled || !IsAfk) return "";
                return TextFormat.Render(_settings.Afk.Template, (n, _) => n == "afktime" ? TextFormat.Duration(now - AfkSince) : null);
            case "count":
                if (arg is null) return null;
                var rule = _settings.Counters.FirstOrDefault(c => c.Parameter.Equals(arg, StringComparison.OrdinalIgnoreCase));
                return rule?.Count.ToString(CultureInfo.InvariantCulture) ?? "0";
            default: return null;
        }
    }

    private string RenderMusic()
    {
        var m = _media.Current;
        var cfg = _settings.Media;
        if (!cfg.Enabled || m is null || !m.Allowed || (!m.Playing && !cfg.ShowWhenPaused)) return "";

        bool hasTime = m.Duration > TimeSpan.Zero;
        return TextFormat.Render(cfg.Template, (name, _) => name switch
        {
            "title" => m.Title,
            "artist" => m.Artist,
            "source" => m.Source,
            "icon" => m.Playing ? "▶" : "⏸",
            "pos" => hasTime ? TextFormat.Duration(m.Position) : "",
            "dur" => hasTime ? TextFormat.Duration(m.Duration) : "",
            "bar" => hasTime ? TextFormat.ProgressBar(cfg.BarStyle, cfg.BarWidth, m.Position / m.Duration) : "",
            _ => null,
        });
    }

    private void UpdateWatched()
    {
        var set = new HashSet<string>(StringComparer.Ordinal) { "AFK" };
        foreach (var c in _settings.Counters)
            if (c.Enabled && c.Parameter.Length > 0) set.Add(c.Parameter);
        _watched = set;
    }

    // Background thread: VRChat streams many parameters per second, so filter before touching the UI thread.
    private void OnOscBackground(OscMessage msg)
    {
        const string prefix = "/avatar/parameters/";
        if (!msg.Address.StartsWith(prefix, StringComparison.Ordinal)) return;
        string param = msg.Address[prefix.Length..];
        if (!_watched.Contains(param)) return;
        bool? value = msg.AsBool(threshold: 0.1f);
        if (value is null) return;
        _ui.Post(_ => OnParameter(param, value.Value), null);
    }

    private void OnParameter(string param, bool value)
    {
        if (param == "AFK")
        {
            if (value && !IsAfk) { IsAfk = true; AfkSince = DateTime.Now; }
            else if (!value) IsAfk = false;
        }

        bool wasActive = _paramState.GetValueOrDefault(param);
        _paramState[param] = value;
        if (!value || wasActive || !IsRunning) return; // count rising edges only, and only while running

        foreach (var rule in _settings.Counters)
            if (rule.Enabled && rule.Parameter == param) Trigger(rule, test: false);
    }

    private void Trigger(CounterRule rule, bool test)
    {
        if (!test)
        {
            rule.Count++;
            CountsChanged?.Invoke();
        }
        string count = rule.Count.ToString(CultureInfo.InvariantCulture);
        string template = string.IsNullOrWhiteSpace(rule.Message) ? $"{rule.Parameter} × {{count}}" : rule.Message;
        _interruptText = TextFormat.Clamp(TextFormat.Render(template.Replace("{0}", "{count}"),
            (n, _) => n == "count" ? count : null));
        _interruptUntil = DateTime.Now.AddSeconds(rule.Seconds);
        LastTriggered = rule.Parameter;
        Tick();
    }
}
