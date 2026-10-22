# Meiji Chatbox Status — Project Rules

## Core rules (always follow)

1. **Goal**: an app that sends OSC to the VRChat chatbox (`/chatbox/input`) to keep an **always-on status** displayed.
2. **Language**:
   - **Talk to the user in Thai** for the whole project.
   - **Everything published to git is English only — no Thai, ever**: README, release notes, CLAUDE.md, other docs,
     commit messages, PR text, repo description. (Thai is fine *inside the app*: the `th` UI strings in `app.js`, Thai log messages.)
3. **Shareable quality** (MIT License, repo https://github.com/finalsiren1/MeijiChatboxStatus; the exe ships via GitHub Releases, `dist/` is never committed):
   correct, robust and complete; easy to install and run; nothing machine-specific hardcoded; errors reported to the user (no silent `catch { }`);
   settings stored in the proper place; documented.
4. **UI**: pretty and moderately cute, fitting the VRChat vibe — **no native UI** (WinForms/WPF/Tkinter controls) but an **optimized WebUI**
   (lightweight, fast, no CDN at runtime, works offline).

## Architecture (v2, `src/MeijiChatboxStatus/`)

- .NET 8 WinForms is only the host window for **WebView2**; all UI lives in `wwwroot/` (plain HTML/CSS/JS, no framework/CDN),
  embedded as EmbeddedResource and served from memory at `https://app.example/`.
- JS ⇄ C# talk JSON via `chrome.webview.postMessage` (see `MainForm.OnWebMessage` and `onMessage` in `app.js`).
- Core logic in `Core/` runs on the UI thread (OSC receive is on a background thread, marshalled via SynchronizationContext).
- Every UI string needs both `th` and `en` entries in `I18N` in `app.js`.
- `app.js` has a mock mode: open `wwwroot/index.html#<tab>` in a browser without running the app.
- **Do not remove** the `CultureInfo.InvariantCulture` setup in `Program.Main` — VRChat.OSCQuery 0.0.7 uses culture-sensitive
  `LastIndexOf("/")`, which stack-overflows on th-TH machines.
- Commands: `dotnet run --project src\MeijiChatboxStatus`, `dotnet test tests\MeijiChatboxStatus.Tests`, `.\publish.ps1 [-Lite]` → `dist\`.
- Every way of exiting must clear the chatbox: normal exit → `OnFormClosing`, crash → handlers in `Program`,
  End task → the `--guard <pid>` process (`ChatboxGuard`).
- Install model: any copy that isn't `%LOCALAPPDATA%\Programs\MeijiChatboxStatus\MeijiChatboxStatus.exe` runs in **setup mode**
  (WebUI install screen, no OSC/services): quick depth-limited scan for old versions (`Installer.Scan`), import old settings, delete old items,
  copy itself, shortcuts, HKCU Uninstall entry, then launch the installed copy. Debug builds always count as installed.
  CLI: `--install` (unattended), `--uninstall` (Apps & Features). The release version is 2.0.7 — don't bump it unless the user says so.
- Updates (`Updater`): checks `releases/latest` on GitHub at startup (+10 s) and every 6 h when `Behavior.CheckUpdates` is on;
  notice at the top of the sidebar + Settings ▸ Updates; installs only after the user confirms in a popup. Downloads the asset
  `MeijiChatboxStatus.exe` (lite build: `MeijiChatboxStatus-lite.exe`) to `%LOCALAPPDATA%\MeijiChatboxStatus\updates\`, verifies size + SHA-256
  (`digest` from the API), runs it with `--install`, then exits. Releases must attach the exe with exactly that name.
- `--watch` = the hidden VRChat watcher (`Autorun.RunWatcher`; no window/tray/OSC), started by the HKCU Run entry (Release builds only)
  and by the app when "open with VRChat" is on; it opens the app (window shown) each time VRChat goes from closed to running and
  survives the app exiting. Turning the option off removes the Run entry and signals the watcher to quit. Old `--tray` = `--watch`.
  WebView2 loads only when the window is first shown.
- Advanced settings must carry a hint saying "if it already works, don't touch this".
- Testing without disturbing the game: change `sendPort` in settings.json to another port and listen on UDP (change it back to 9000 afterwards).
- Careful: if VRChat is running, the app starts sending to the real chatbox immediately (`AutoWithVrc`) — stop it and clear the chatbox after testing.
- Find unused UI strings/CSS: `node tools\audit-webui.mjs src/MeijiChatboxStatus/wwwroot`.

## Decisions made by the user (don't revert on your own)

- Name: **Meiji Chatbox Status (MCS)**, exe `MeijiChatboxStatus.exe`, settings in `%APPDATA%\MeijiChatboxStatus\`.
- Font **Noto Sans Thai** (not a cute font like Mali); the chatbox preview has no character/tail.
- Delete buttons are text labels (`delPage` / `del` in `I18N`), not trash-can icons.
- Stopping/exiting **always clears the chatbox** (no option); no notification sound (always send `,sTF`).
- Two independent options, both on by default: "Open automatically with VRChat" (`AutoWithVrc`) and "Exit the app when VRChat closes"
  (`CloseWithVrc`). Nothing may appear at Windows startup (only the invisible watcher). No "start sending when the app opens".
- X button → popup asking exit/hide with a "don't ask again" checkbox (`CloseAction`: ask/tray/exit).
- Music: only music apps/sites (`MediaFilter`); TikTok/IG/Facebook/NSFW must never leak — when unsure, hide.
  Chrome reports only ONE media session to Windows (often not the music tab), so browser approval also reads every tab
  title via UI Automation (`MediaFilter.BrowserTabTitles`, skips web content) and falls back to a tab marked
  "- Audio playing" on a music site (`PlayingMusicTab`, English browser UI only; Firefox tabs get that marker added
  when their sound button is shown, see `FirefoxTabPlaysSound`).
- UI wording must be plain and easy for anyone: say what happens, avoid technical jargon.

## Old code (VrcAfkNative — removed)

Only `LegacyImport` remains, to import `vrc_afk_settings.txt`. Old problems already fixed: line-based settings file, silently swallowed errors,
port 9001 conflicts (→ OSCQuery), unlimited resending, the 144-char cut breaking emoji.
