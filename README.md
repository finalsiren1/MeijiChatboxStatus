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
| 🎀 **Friendly UI** | 5 themes (Sakura, Lavender, Mint, Sky, Night), Thai / English, live chatbox preview |

## 🚀 Getting started

1. Download `MeijiChatboxStatus.exe` from [**Releases**](https://github.com/finalsiren1/MeijiChatboxStatus/releases/latest) and double-click it (no install needed).
   - If Windows shows *"Windows protected your PC"*, click **More info → Run anyway** (the file simply isn't code-signed).
2. In VRChat, enable OSC: **Action Menu ▸ Options ▸ OSC ▸ Enabled**.
3. Press **▶ Start** (or just let it start when VRChat opens) — done! ♡

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
- **Music doesn't show** — only music apps and music sites are shown (see the list in the Music tab). In a browser, the music tab must be the active tab of its window, because Windows doesn't say which site is playing, so the app checks the tab title. Using a player or site that isn't listed? Add it under *Add your own players/sites*.
- **Logs** — the *Log* tab in the app, or `%LOCALAPPDATA%\MeijiChatboxStatus\app.log`.

## 📁 Where things are stored

| | |
|---|---|
| Settings | `%APPDATA%\MeijiChatboxStatus\settings.json` (copy it to move your setup to another PC) |
| Log + WebView2 cache | `%LOCALAPPDATA%\MeijiChatboxStatus\` |
| Open with VRChat | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → `MeijiChatboxStatus` |

Coming from the old version (VrcAfkNative)? Put `vrc_afk_settings.txt` next to the exe before the first run and your message and counters are imported automatically.

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
  wwwroot/             WebUI (plain HTML/CSS/JS, embedded in the exe, works offline)
tests/MeijiChatboxStatus.Tests/
tools/
  make-icon.ps1        generates the app icon
  audit-webui.mjs      finds unused UI strings / CSS
```

The WebUI can be designed in a normal browser: open `src/MeijiChatboxStatus/wwwroot/index.html` and it runs with mock data.

## 📜 License

[MIT](LICENSE) — free to use, modify and share.

## 💖 Credits

- [Noto Sans Thai](https://github.com/notofonts/thai) font — SIL Open Font License 1.1 (`wwwroot/fonts/OFL.txt`)
- [vrc-oscquery-lib](https://github.com/vrchat-community/vrc-oscquery-lib) — VRChat
- [Microsoft Edge WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)
