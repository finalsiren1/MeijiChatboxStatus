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
- `--tray` = start hidden (used by the HKCU Run entry from `Autorun`, registered in Release builds only); WebView2 loads only when the window is first shown.
- Advanced settings must carry a hint saying "if it already works, don't touch this".
- Testing without disturbing the game: change `sendPort` in settings.json to another port and listen on UDP (change it back to 9000 afterwards).
- Careful: if VRChat is running, the app starts sending to the real chatbox immediately (`AutoWithVrc`) — stop it and clear the chatbox after testing.
- Find unused UI strings/CSS: `node tools\audit-webui.mjs src/MeijiChatboxStatus/wwwroot`.

## Decisions made by the user (don't revert on your own)

- Name: **Meiji Chatbox Status (MCS)**, exe `MeijiChatboxStatus.exe`, settings in `%APPDATA%\MeijiChatboxStatus\`.
- Font **Noto Sans Thai** (not a cute font like Mali); the chatbox preview has no character/tail.
- Delete buttons are text labels (`delPage` / `del` in `I18N`), not trash-can icons.
- Stopping/exiting **always clears the chatbox** (no option); no notification sound (always send `,sTF`).
- One option "Open automatically with VRChat" (`AutoWithVrc`, on by default) — no "start sending when the app opens".
- X button → popup asking exit/hide with a "don't ask again" checkbox (`CloseAction`: ask/tray/exit).
- Music: only music apps/sites (`MediaFilter`); TikTok/IG/Facebook/NSFW must never leak — when unsure, hide.
- UI wording must be plain and easy for anyone: say what happens, avoid technical jargon.

## Old code (VrcAfkNative — removed)

Only `LegacyImport` remains, to import `vrc_afk_settings.txt`. Old problems already fixed: line-based settings file, silently swallowed errors,
port 9001 conflicts (→ OSCQuery), unlimited resending, the 144-char cut breaking emoji.
