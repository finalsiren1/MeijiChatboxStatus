using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using MeijiChatboxStatus.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MeijiChatboxStatus;

/// <summary>
/// Hosts the WebUI (embedded HTML/CSS/JS served from memory into WebView2), the tray icon,
/// and wires the UI to the core services through JSON messages.
/// </summary>
public sealed class MainForm : Form
{
    private const string AppOrigin = "https://app.example/"; // reserved domain: never reaches the network
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Logger _log = new();
    private readonly SettingsStore _store = new();
    private readonly OscService _osc;
    private readonly MediaWatcher _media;
    private readonly StatusEngine _engine;
    private readonly VrcWatcher _vrc = new();
    private readonly WebView2 _web;
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _trayOpen, _trayToggle, _trayExit;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer _updateTimer = new() { Interval = 10_000 }; // first check shortly after start
    private readonly Updater _updater;
    private readonly Dictionary<string, string> _resources;
    private readonly string _version;

    private AppSettings _settings;
    private CoreWebView2Environment? _env;
    private bool _webReady, _exiting, _countsDirty, _trayHintShown, _servicesStarted;
    /// <summary>Running from a downloaded copy: the window only shows the install/update screen, nothing is sent.</summary>
    private readonly bool _setupMode;
    private List<OldItem>? _oldItems;
    private int _tick;
    private string _lastState = "";
    private string? _lastArt;
    private Version? _updateBalloonFor;

    public MainForm(EventWaitHandle showEvent, bool setupMode)
    {
        _setupMode = setupMode;
        _version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "2.0.0";
        _resources = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .ToDictionary(n => n.Replace('\\', '/'), n => n, StringComparer.OrdinalIgnoreCase);

        Text = "Meiji Chatbox Status";
        Icon = LoadIcon();
        ClientSize = new Size(1040, 720);
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(255, 245, 249);

        _settings = _store.Load(_log);
        _osc = new OscService(_log);
        if (!_setupMode) _osc.Configure(_settings.Osc);
        _media = new MediaWatcher(_log) { ExtraSources = _settings.Media.ExtraSources };
        _engine = new StatusEngine(_settings, _osc, _media, _log);
        _engine.CountsChanged += () => _countsDirty = true;
        _engine.RunningChanged += UpdateTray;
        _vrc.Changed += OnVrcChanged;
        _updater = new Updater(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(2, 0, 1), _log);
        _updater.Changed += OnUpdateChanged;
        _log.Added += entry => { if (IsHandleCreated && !IsDisposed) BeginInvoke(() => Post(new { type = "log", entry = LogDto(entry) })); };

        _web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = BackColor };
        Controls.Add(_web);

        _trayOpen = new ToolStripMenuItem("", null, (_, _) => ShowFromTray());
        _trayToggle = new ToolStripMenuItem("", null, (_, _) => ToggleRunning());
        _trayExit = new ToolStripMenuItem("", null, (_, _) => ExitApp());
        _tray = new NotifyIcon
        {
            Icon = Icon,
            Text = "Meiji Chatbox Status",
            Visible = !_setupMode,
            ContextMenuStrip = new ContextMenuStrip { Items = { _trayOpen, _trayToggle, new ToolStripSeparator(), _trayExit } },
        };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowFromTray(); };
        _tray.BalloonTipClicked += (_, _) => ShowFromTray();
        UpdateTray();

        _timer.Tick += OnTimer;
        ThreadPool.RegisterWaitForSingleObject(showEvent,
            (_, _) => { if (IsHandleCreated && !IsDisposed) BeginInvoke(ShowFromTray); }, null, -1, false);
    }

    // Services run as soon as the window handle exists (even hidden); the WebUI loads on first show (OnLoad),
    // so a tray-only session doesn't pay for a browser it never displays.
    protected override async void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_servicesStarted || _setupMode) return;
        _servicesStarted = true;

        ChatboxGuard.Launch(_log);
        Autorun.Apply(_settings.Behavior.AutoWithVrc, _log);
        await _media.InitAsync();
        _vrc.Check();
        _timer.Start();
        _updateTimer.Tick += OnUpdateTimer;
        _updateTimer.Start();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await InitWebAsync();
    }

    // ───────────────────────────── WebView2 ─────────────────────────────

    private async Task InitWebAsync()
    {
        try
        {
            string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeijiChatboxStatus", "WebView2");
            _env = await CoreWebView2Environment.CreateAsync(null, dataDir);
            await _web.EnsureCoreWebView2Async(_env);
            var core = _web.CoreWebView2;

            var s = core.Settings;
            s.IsStatusBarEnabled = false;
            s.IsZoomControlEnabled = false;
            s.IsPasswordAutosaveEnabled = false;
            s.IsGeneralAutofillEnabled = false;
#if DEBUG
            s.AreDevToolsEnabled = true;
#else
            s.AreDevToolsEnabled = false;
            s.AreBrowserAcceleratorKeysEnabled = false; // no F5/Ctrl+P/Ctrl+F; editing shortcuts still work
#endif

            core.AddWebResourceRequestedFilter(AppOrigin + "*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += OnWebResource;
            core.WebMessageReceived += OnWebMessage;
            core.NavigationStarting += (_, a) =>
            {
                if (a.Uri.StartsWith(AppOrigin, StringComparison.OrdinalIgnoreCase)) return;
                a.Cancel = true;
                OpenExternal(a.Uri);
            };
            core.NewWindowRequested += (_, a) => { a.Handled = true; OpenExternal(a.Uri); };
            core.ContextMenuRequested += (_, a) =>
            {
                // Keep only text-editing entries (no "Reload", "Save as", "Print"...).
                string[] keep = ["cut", "copy", "paste", "pasteAndMatchStyle", "selectAll", "undo", "redo", "emoji"];
                for (int i = a.MenuItems.Count - 1; i >= 0; i--)
                    if (!keep.Contains(a.MenuItems[i].Name)) a.MenuItems.RemoveAt(i);
                if (a.MenuItems.Count == 0) a.Handled = true;
            };
            core.ProcessFailed += (_, a) => _log.Error($"WebView2 process failed: {a.ProcessFailedKind}");

            core.Navigate(AppOrigin + "index.html");
        }
        catch (Exception e)
        {
            _log.Error($"เปิดหน้า UI ไม่สำเร็จ: {e.Message}");
            MessageBox.Show(this, $"เปิดหน้า UI ไม่สำเร็จ / Failed to start the UI:\n{e.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnWebResource(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        string path = new Uri(e.Request.Uri).AbsolutePath.TrimStart('/');
        if (path.Length == 0) path = "index.html";
        var stream = _resources.TryGetValue("wwwroot/" + path, out var name)
            ? Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            : null;
        e.Response = stream is null
            ? _env!.CreateWebResourceResponse(null, 404, "Not Found", "")
            : _env!.CreateWebResourceResponse(stream, 200, "OK", $"Content-Type: {MimeType(path)}\r\nCache-Control: no-store");
    }

    private static string MimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".woff2" => "font/woff2",
        ".json" => "application/json",
        _ => "application/octet-stream",
    };

    private void Post(object message)
    {
        if (!_webReady || _web.CoreWebView2 is null) return;
        try { _web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, Json)); }
        catch (InvalidOperationException) { } // WebView is shutting down
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var msg = doc.RootElement;
            int Index() => msg.TryGetProperty("index", out var i) && i.TryGetInt32(out var v) ? v : -1;

            switch (msg.GetProperty("type").GetString())
            {
                case "ready" when _setupMode:
                    _webReady = true;
                    StartSetupScan();
                    break;
                case "install": RunInstall(); break;
                case "cancelSetup": _exiting = true; Close(); break;
                case "ready":
                    _webReady = true;
                    _lastState = ""; _lastArt = null;
                    Post(new
                    {
                        type = "init",
                        settings = _settings,
                        version = _version,
                        log = _log.Snapshot().Select(LogDto),
                        update = UpdateDto(),
                    });
                    PushState();
                    break;
                case "save":
                    ApplySettings(msg.GetProperty("settings").Deserialize<AppSettings>(Json));
                    break;
                case "start": _engine.Start(); PushState(); break;
                case "stop": _engine.Stop(); PushState(); break;
                case "resetCounter": _engine.ResetCounter(Index()); PushState(); break;
                case "resetCounters": _engine.ResetAllCounters(); PushState(); break;
                case "testCounter": _engine.TestCounter(Index()); PushState(); break;
                case "getParams": PostParameters(); break;
                case "openDataFolder": OpenExternal(SettingsStore.DataDir); break;
                case "openUrl":
                    string url = msg.GetProperty("url").GetString() ?? "";
                    if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) OpenExternal(url);
                    break;
                case "exit": ExitApp(); break;
                case "checkUpdate": _ = _updater.CheckAsync(manual: true); break;
                case "installUpdate": InstallUpdate(); break;
                case "cancelUpdate": _updater.CancelDownload(); break;
                case "closeChoice":
                    if (msg.GetProperty("action").GetString() == "exit") ExitApp();
                    else HideToTray();
                    break;
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _log.Error($"ข้อความจาก UI ไม่ถูกต้อง: {ex.Message}");
        }
    }

    // ───────────────────────────── Install / update ─────────────────────────────

    private async void StartSetupScan()
    {
        bool th = File.Exists(SettingsStore.SettingsFile)
            ? _settings.Language == "th"
            : CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "th";
        Post(new { type = "setup", lang = th ? "th" : "en", version = _version, installDir = Installer.InstallDir, update = Installer.IsInstalled });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        _oldItems = await Task.Run(() => Installer.Scan(cts.Token));
        Post(new { type = "setupScan", items = _oldItems.Select(i => new { kind = i.Kind, path = i.Path }) });
    }

    private async void RunInstall()
    {
        if (_oldItems is null) return;
        try
        {
            var items = _oldItems;
            string exe = await Task.Run(() => Installer.Install(items, _log,
                step => BeginInvoke(() => Post(new { type = "setupStep", step }))));
            Post(new { type = "setupStep", step = "done" });
            await Task.Delay(1200);
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Installer.InstallDir })?.Dispose();
            _exiting = true;
            Close();
        }
        catch (Exception e)
        {
            _log.Error($"ติดตั้งไม่สำเร็จ: {e.Message}");
            Post(new { type = "setupError", message = e.Message });
        }
    }

    private void ApplySettings(AppSettings? incoming)
    {
        if (incoming is null) return;
        // Counter values live here, not in the UI: a trigger may have landed while the user was typing.
        foreach (var c in incoming.Counters)
        {
            var existing = _settings.Counters.FirstOrDefault(o => o.Parameter == c.Parameter?.Trim());
            if (existing is not null) c.Count = existing.Count;
        }
        incoming.Normalize();
        bool languageChanged = incoming.Language != _settings.Language;
        bool autorunChanged = incoming.Behavior.AutoWithVrc != _settings.Behavior.AutoWithVrc;
        bool updateCheckTurnedOn = incoming.Behavior.CheckUpdates && !_settings.Behavior.CheckUpdates;

        _settings = incoming;
        if (autorunChanged) Autorun.Apply(_settings.Behavior.AutoWithVrc, _log);
        if (_media.ExtraSources != _settings.Media.ExtraSources)
        {
            _media.ExtraSources = _settings.Media.ExtraSources;
            _media.ResetApprovals();
        }
        _osc.Configure(_settings.Osc);
        _engine.ApplySettings(_settings);
        _store.Save(_settings, _log);
        _countsDirty = false;
        if (languageChanged) UpdateTray();
        if (updateCheckTurnedOn && _updater.State is "idle" or "error") _ = _updater.CheckAsync(manual: false);
        PushState();
    }

    private void PostParameters()
    {
        var now = DateTime.UtcNow;
        var list = _osc.SeenParameters
            .OrderByDescending(p => p.Value.Utc)
            .Take(200)
            .Select(p => new { name = p.Key, value = p.Value.Message.ArgText, ago = (int)(now - p.Value.Utc).TotalSeconds });
        Post(new { type = "params", list });
    }

    // ───────────────────────────── Updates ─────────────────────────────

    private async void OnUpdateTimer(object? sender, EventArgs e)
    {
        bool first = _updateTimer.Interval != (int)Updater.CheckEvery.TotalMilliseconds;
        _updateTimer.Interval = (int)Updater.CheckEvery.TotalMilliseconds;
        if (first) _updater.CleanupDownloads(); // the installer that started this copy has exited by now
        if (_settings.Behavior.CheckUpdates) await _updater.CheckAsync(manual: false);
    }

    private async void InstallUpdate()
    {
        if (await _updater.DownloadAndInstallAsync()) ExitApp(); // the new copy waits for this one, then reopens
    }

    private void OnUpdateChanged()
    {
        Post(new { type = "update", update = UpdateDto() });
        // Hidden in the tray: mention a new version once, without opening the window.
        if (_updater.State == "available" && _updater.Latest is { } r && r.Version != _updateBalloonFor && !Visible)
        {
            _updateBalloonFor = r.Version;
            bool th = _settings.Language == "th";
            _tray.ShowBalloonTip(5000, "Meiji Chatbox Status",
                th ? $"มีเวอร์ชันใหม่ v{r.Version.ToString(3)} แล้ว คลิกเพื่อเปิดหน้าต่างแล้วกดอัปเดต" : $"Version {r.Version.ToString(3)} is available. Click to open the app and update.",
                ToolTipIcon.Info);
        }
    }

    private object UpdateDto()
    {
        var r = _updater.Latest;
        return new
        {
            state = _updater.State,
            current = _updater.Current.ToString(3),
            latest = r?.Version.ToString(3),
            notes = r?.Notes,
            url = r?.PageUrl,
            sizeMb = r is null ? 0 : Math.Round(r.AssetSize / 1048576.0),
            progress = _updater.Progress,
            error = _updater.Error,
            checkedAt = _updater.CheckedAt?.ToString("HH:mm"),
        };
    }

    // ───────────────────────────── Loop ─────────────────────────────

    private async void OnTimer(object? sender, EventArgs e)
    {
        _tick++;
        try
        {
            // Hidden in the tray and not sending: nobody needs media info or UI updates, so skip that work.
            bool visible = Visible && WindowState != FormWindowState.Minimized;
            if (_tick % 4 == 0 && (visible || _engine.IsRunning)) await _media.PollAsync(); // 1 s
            _engine.Tick();                                                                  // 250 ms
            if (_tick % 2 == 0 && visible) PushState();                                      // 500 ms
            if (_tick % 12 == 0) _vrc.Check();               // 3 s
            if (_tick % 20 == 0 && _countsDirty)             // 5 s
            {
                _countsDirty = false;
                _store.Save(_settings, _log);
            }
        }
        catch (Exception ex)
        {
            _log.Error($"เกิดข้อผิดพลาดในลูปหลัก: {ex.Message}");
        }
    }

    private void PushState()
    {
        if (!_webReady) return;
        var m = _media.Current;
        var now = DateTime.Now;
        var state = new
        {
            type = "state",
            running = _engine.IsRunning,
            preview = _engine.Preview,
            vrc = _vrc.IsRunning,
            osc = new
            {
                mode = _osc.ListenMode,
                port = _osc.ListenPort,
                error = _osc.ListenError,
                receiving = DateTime.UtcNow - _osc.LastReceivedUtc < TimeSpan.FromSeconds(10),
            },
            media = m is null ? null : new
            {
                allowed = m.Allowed,
                // Don't even display what unapproved media is (it could be anything).
                title = m.Allowed ? m.Title : "",
                artist = m.Allowed ? m.Artist : "",
                source = m.Source,
                playing = m.Playing,
                pos = TextFormat.Duration(m.Position),
                dur = m.Duration > TimeSpan.Zero ? TextFormat.Duration(m.Duration) : "",
                progress = m.Duration > TimeSpan.Zero ? m.Position / m.Duration : 0,
            },
            afk = _engine.IsAfk,
            afkFor = _engine.IsAfk ? TextFormat.Duration(now - _engine.AfkSince) : "",
            counts = _settings.Counters.Select(c => c.Count),
            lastTriggered = _engine.LastTriggered,
        };
        string json = JsonSerializer.Serialize(state, Json);
        if (json != _lastState)
        {
            _lastState = json;
            try { _web.CoreWebView2?.PostWebMessageAsJson(json); } catch (InvalidOperationException) { }
        }

        // Album art is large, so it only goes over the bridge when it changes.
        string? art = m?.Thumbnail;
        if (art != _lastArt)
        {
            _lastArt = art;
            Post(new { type = "art", src = art });
        }
    }

    // ───────────────────────────── VRChat / tray / lifetime ─────────────────────────────

    private void OnVrcChanged(bool running)
    {
        var b = _settings.Behavior;
        if (running)
        {
            _log.Info("🎮 เจอ VRChat แล้ว");
            if (b.AutoWithVrc && !_engine.IsRunning) _engine.Start();
        }
        else
        {
            _log.Info("🎮 VRChat ปิดแล้ว");
            _engine.ResetAvatarState();
            if (b.CloseWithVrc) { ExitApp(); return; }
            if (b.AutoWithVrc) _engine.Stop();
        }
        PushState();
    }

    private void ToggleRunning()
    {
        if (_engine.IsRunning) _engine.Stop();
        else _engine.Start();
        PushState();
    }

    private void UpdateTray()
    {
        bool th = _settings.Language == "th";
        _trayOpen.Text = th ? "เปิดหน้าต่าง" : "Open window";
        _trayToggle.Text = _engine?.IsRunning == true ? (th ? "⏹ หยุดส่ง" : "⏹ Stop") : (th ? "▶ เริ่มส่ง" : "▶ Start");
        _trayExit.Text = th ? "ออกจากแอป" : "Exit";
        _tray.Text = _engine?.IsRunning == true ? "Meiji Chatbox Status — ON" : "Meiji Chatbox Status";
    }

    private void ShowFromTray()
    {
        Show();
        PushState();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    private void HideToTray()
    {
        Hide();
        if (_trayHintShown) return;
        _trayHintShown = true;
        bool th = _settings.Language == "th";
        _tray.ShowBalloonTip(3000, "Meiji Chatbox Status",
            th ? "แอปยังทำงานอยู่ที่ไอคอนข้างนาฬิกา คลิกขวาที่ไอคอนเพื่อออก" : "Still running in the tray. Right-click the icon to exit.",
            ToolTipIcon.Info);
    }

    private void ExitApp()
    {
        _exiting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_setupMode)
        {
            _tray.Visible = false;
            base.OnFormClosing(e);
            return;
        }

        // X button / Alt+F4 / taskbar "Close window": ask (popup in the WebUI), hide, or exit as the user chose.
        if (!_exiting && e.CloseReason == CloseReason.UserClosing && _settings.Behavior.CloseAction != "exit")
        {
            e.Cancel = true;
            if (_settings.Behavior.CloseAction == "ask" && _webReady) Post(new { type = "askClose" });
            else HideToTray();
            return;
        }

        // Any real exit (window, tray, VRChat closing, Windows shutdown) leaves the chatbox empty — even when not sending,
        // in case an earlier session left something. Kills/crashes are covered by ChatboxGuard.
        _timer.Stop();
        if (_engine.IsRunning) _engine.Stop();
        else ChatboxGuard.Clear(_settings.Osc);
        _store.Save(_settings, _log);
        _tray.Visible = false;
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _updateTimer.Dispose();
            _updater.Dispose();
            _tray.Dispose();
            _osc.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OpenExternal(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception e) { _log.Error($"เปิด {target} ไม่ได้: {e.Message}"); }
    }

    private static object LogDto(LogEntry e) => new { time = e.Time.ToString("HH:mm:ss"), level = e.Level, text = e.Text };

    private static Icon LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
        return stream is null ? SystemIcons.Application : new Icon(stream);
    }
}
