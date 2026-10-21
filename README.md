# 💬 Meiji Chatbox Status (MCS)

Keep an **always-on status in your VRChat chatbox** — the time, the song you're listening to, your AFK status and how many times your avatar got headpatted.
Everything is sent over OSC, so your avatar needs no changes (except for touch counters, which read an avatar parameter).

---

## ✨ Features

| | |
|---|---|
| 💬 **Status pages** | Write your own text with variables; several pages can take turns on the chatbox |
| 🎵 **Now playing** | Spotify, YouTube, YouTube Music, Apple Music, SoundCloud and more, with 5 progress-bar styles — **music sources only**, never TikTok / Instagram / Facebook / random sites |
| 😴 **AFK** | VRChat reports when you take off your headset or unfocus the game; shows how long you've been away |
| 💗 **Touch counters** | Counts whenever an avatar parameter turns on (e.g. a Contact Receiver on your head) and briefly shows a message |
| 🔌 **OSCQuery** | VRChat finds the app by itself — no port conflicts with other OSC apps (VRCFT, VRCOSC, …) |
| 🎮 **Opens with VRChat** | Start VRChat and your status starts; close it and the status stops (can be turned off in Settings) |
| 🧹 **Never leaves a stale message** | Stopping or exiting always clears the chatbox — even after "End task" or a crash |
| 🎁 **Built-in updates** | Tells you when a new version is out and updates itself in one click (always asks first) |
| 🎀 **Friendly UI** | 5 themes (Sakura, Lavender, Mint, Sky, Night), Thai / English, live chatbox preview |

## 🚀 Getting started

1. Download `MeijiChatboxStatus.exe` from [**Releases**](https://github.com/finalsiren1/MeijiChatboxStatus/releases/latest) and double-click it.
   - If Windows shows *"Windows protected your PC"*, click **More info → Run anyway** (the file simply isn't code-signed).
2. The setup screen quickly looks for **older versions** on your PC (the old `VrcAfkNative` app, leftover copies, shortcuts and startup entries)
   and lists what it found. Click **Install** — old versions are removed, their settings and counters are imported, and the app is installed
   for your account (no admin rights needed) with Start Menu and desktop shortcuts.
3. In VRChat, enable OSC: **Action Menu ▸ Options ▸ OSC ▸ Enabled**.
4. Press **▶ Start** (or just let it start when VRChat opens) — done! ♡

### Updating

From v2.0.1 on, the app checks GitHub Releases when it starts and every 6 hours. When a new version is out, a notice appears at the top left —
click **Update**, confirm, and the app downloads it (checked against the release's SHA-256), closes, and reopens on the new version with your
settings kept. You can also check by hand in **Settings ▸ Updates**, or turn automatic checking off there. Nothing is installed without your OK.

Running a newer downloaded exe by hand updates the same way (that's also how to get from v2.0.0 to v2.0.1).

To uninstall, use **Windows Settings ▸ Apps ▸ Installed apps ▸ Meiji Chatbox Status** (you'll be asked whether to keep your settings).

The interface opens in Thai; switch to English in **Settings ▸ Appearance ▸ Language**.

After the first run, the app starts your status every time you open VRChat (it waits quietly in the system tray from Windows startup).
To turn this off: **Settings ▸ Behavior ▸ Open automatically with VRChat**.

> Task Manager shows two `MeijiChatboxStatus` processes. The second one is a tiny watcher (a few MB of RAM) that clears your chatbox if the main app is closed abruptly.

> Requires the **Microsoft Edge WebView2 Runtime**, which most Windows 10/11 PCs already have. If it's missing, the app offers to open the download page.

## 🧩 Variables

**In status pages**

| Variable | Meaning | Example |
|---|---|---|
| `{time}` / `{time12}` | Current time | `21:30` / `9:30 PM` |
| `{date}` / `{day}` | Date / weekday (in the UI language) | `21/10` / `Tue` |
| `{music}` | Now playing (format it in the Music tab) | |
| `{afk}` | AFK message (only while AFK) | |
| `{session}` | How long the status has been running | `1:02:05` |
| `{count:ParameterName}` | Value of that touch counter | `12` |

**Music tab:** `{title}` `{artist}` `{bar}` `{pos}` `{dur}` `{icon}` `{source}`
**AFK tab:** `{afktime}` · **Counter messages:** `{count}`

Lines whose variables have nothing to show (e.g. `{music}` when nothing is playing) are hidden automatically.
Messages are limited to VRChat's 144 characters / 9 lines without ever cutting an emoji or a Thai character cluster in half.

## 💗 Setting up a touch counter

1. Your avatar needs a parameter that becomes `true` (or above 0) when touched — usually a **VRC Contact Receiver** with a parameter set,
   and that parameter must be in the avatar's *Expression Parameters*.
2. Open the **Counters** tab and have a friend touch you — the parameter name appears under *Recently received parameters*. Click it to add a counter.
3. Edit the message however you like, e.g. `🐰 Headpats: {count}!` — counts are remembered between sessions.

> If parameters stop arriving after changing avatars, delete `%USERPROFILE%\AppData\LocalLow\VRChat\VRChat\OSC` and switch avatars again (VRChat regenerates its OSC config).

## ❓ Troubleshooting

- **Nothing shows in the chatbox** — make sure OSC is enabled in VRChat, and that Settings ▸ Advanced uses IP `127.0.0.1` and send port `9000`.
- **The OSC dot is yellow** — the app is ready but VRChat hasn't sent anything yet (VRChat only sends parameters when they change — try moving).
- **The OSC dot is red** — the receive port is taken by another app. Enable OSCQuery (the default) or pick another receive port.
- **Music doesn't show** — only music apps and music sites are shown (see the list in the Music tab). In a browser, the app reads the tab titles (Windows doesn't say which site is playing), so the music can be in any tab — even one the browser didn't report to Windows, as long as the tab shows it's playing audio (English browser UI). With a browser in another language, if the music isn't picked up, switch to the music tab once. Using a player or site that isn't listed? Add it under *Add your own players/sites*.
- **Logs** — the *Log* tab in the app, or `%LOCALAPPDATA%\MeijiChatboxStatus\app.log`.

## 📁 Where things are stored

| | |
|---|---|
| Program | `%LOCALAPPDATA%\Programs\MeijiChatboxStatus\` |
| Settings | `%APPDATA%\MeijiChatboxStatus\settings.json` (copy it to move your setup to another PC) |
| Log + WebView2 cache | `%LOCALAPPDATA%\MeijiChatboxStatus\` |
| Downloaded updates | `%LOCALAPPDATA%\MeijiChatboxStatus\updates\` (removed after the update) |
| Open with VRChat | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → `MeijiChatboxStatus` |

The old version's `vrc_afk_settings.txt` is imported automatically when it's found during setup.

Command line (for scripts): `MeijiChatboxStatus.exe --install` installs/updates without the setup screen; `--uninstall` uninstalls.

## 🛠️ Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet run --project src\MeijiChatboxStatus                     # run in development
dotnet test tests\MeijiChatboxStatus.Tests                      # run the tests
powershell -ExecutionPolicy Bypass -File .\publish.ps1 [-Lite]  # build the release exe into .\dist
```

Layout:

```
src/MeijiChatboxStatus/
  Program.cs           entry point: single instance, WebView2 check, crash handling
  MainForm.cs          WebView2 window + tray icon + JS ⇄ C# bridge
  Core/
    Osc.cs             OSC encoding/decoding
    OscService.cs      OSC send/receive + OSCQuery
    MediaWatcher.cs    now playing from Windows media sessions
    MediaFilter.cs     music-only source filter
    StatusEngine.cs    builds the message, send pacing, counters, AFK
    TextFormat.cs      variables, line tidying, 144-char limit, progress bars
    Settings.cs        JSON settings + import from the old version
    ChatboxGuard.cs    clears the chatbox however the app exits
    Autorun.cs         "open with VRChat" (Windows startup entry)
    Installer.cs       per-user install/update/uninstall + quick scan for old versions
    Updater.cs         checks GitHub Releases, downloads + verifies the new exe, hands over to it
  wwwroot/             WebUI (plain HTML/CSS/JS, embedded in the exe, works offline)
tests/MeijiChatboxStatus.Tests/
tools/
  make-icon.ps1        generates the app icon
  audit-webui.mjs      finds unused UI strings / CSS
```

The WebUI can be designed in a normal browser: open `src/MeijiChatboxStatus/wwwroot/index.html` and it runs with mock data
(`index.html#update` pretends a new version is available).

Releasing: bump `<Version>` in the `.csproj`, run `publish.ps1`, and attach `dist\MeijiChatboxStatus.exe` (exactly that name) to a GitHub
release tagged `vX.Y.Z`. Installed apps pick it up from `releases/latest`; drafts and pre-releases are ignored.

## 📜 License

[MIT](LICENSE) — free to use, modify and share.

## 💖 Credits

- [Noto Sans Thai](https://github.com/notofonts/thai) font — SIL Open Font License 1.1 (`wwwroot/fonts/OFL.txt`)
- [vrc-oscquery-lib](https://github.com/vrchat-community/vrc-oscquery-lib) — VRChat
- [Microsoft Edge WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)
