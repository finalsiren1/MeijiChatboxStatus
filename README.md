# 💬 Meiji Chatbox Status (MCS)

แอปโชว์ **สถานะน่ารัก ๆ บน VRChat Chatbox** ค้างไว้ตลอด — เวลา, เพลงที่กำลังฟัง, สถานะ AFK และตัวนับการโดนแตะ Avatar
ส่งผ่าน OSC ไม่ต้องติดตั้งอะไรใน Avatar (ยกเว้นตัวนับ ที่ใช้ Parameter ของ Avatar)

*English summary at the bottom.*

---

## ✨ ฟีเจอร์

| | |
|---|---|
| 💬 **ข้อความ Status** | เขียนข้อความเอง ใส่ตัวแปรได้ มีหลายหน้าให้สลับวนโชว์ |
| 🎵 **เพลงที่กำลังเล่น** | Spotify, YouTube, YouTube Music, Apple Music, SoundCloud ฯลฯ พร้อมแถบเวลา 5 สไตล์ — **โชว์เฉพาะแหล่งเพลง** ไม่จับ TikTok / IG / Facebook / เว็บอื่น |
| 😴 **AFK** | VRChat บอกเองเมื่อถอดแว่น/สลับหน้าต่าง โชว์ว่า AFK มานานเท่าไหร่ |
| 💗 **นับการแตะ** | นับเมื่อ Avatar Parameter ทำงาน (เช่น Contact Receiver ที่หัว) แล้วโชว์ข้อความแทรก |
| 🔌 **OSCQuery** | VRChat หาแอปเจอเอง ไม่ชนพอร์ตกับแอป OSC อื่น (VRCFT, VRCOSC ฯลฯ) |
| 🎮 **เปิดเองเมื่อเปิด VRChat** | เปิด VRChat ก็เริ่มส่งให้เลย ปิด VRChat ก็หยุด (ปิดตัวเลือกนี้ได้ในตั้งค่า) |
| 🧹 **ไม่ทิ้งข้อความค้าง** | หยุดส่งหรือปิดโปรแกรมแบบไหนก็ล้าง Chatbox ให้เสมอ รวมถึง End task และโปรแกรมแครช |
| 🎀 **UI น่ารัก** | 5 ธีม (ซากุระ, ลาเวนเดอร์, มิ้นต์, ท้องฟ้า, กลางคืน), ภาษาไทย/อังกฤษ, ดูตัวอย่าง Chatbox แบบสด |

## 🚀 วิธีใช้

1. ดาวน์โหลด `MeijiChatboxStatus.exe` จากหน้า [**Releases**](https://github.com/finalsiren1/MeijiChatboxStatus/releases/latest) แล้วดับเบิลคลิกเปิด (ไม่ต้องติดตั้ง)
   - ถ้า Windows ขึ้น *"Windows protected your PC"* ให้กด **More info → Run anyway** (เพราะไฟล์ไม่ได้ซื้อใบรับรอง ไม่ใช่ไวรัส)
2. ใน VRChat เปิด OSC: **Action Menu ▸ Options ▸ OSC ▸ Enabled**
3. กด **▶ เริ่มส่ง** (หรือปล่อยให้เริ่มเองตอนเปิด VRChat) — เสร็จ! ♡

หลังเปิดครั้งแรก ทุกครั้งที่เปิด VRChat โปรแกรมจะเริ่มส่งให้เอง (มันรอเงียบ ๆ ที่ถาดไอคอนตั้งแต่เปิดเครื่อง)
ถ้าไม่ต้องการ ปิดได้ที่ **ตั้งค่า ▸ การทำงาน ▸ เปิดโปรแกรมเองเมื่อเปิด VRChat**

> ใน Task Manager จะเห็น MeijiChatboxStatus 2 ตัว ตัวที่สองเป็นตัวเฝ้าเล็ก ๆ (แรมไม่กี่ MB) ไว้ล้าง Chatbox ถ้าโปรแกรมหลักถูกปิดแบบกะทันหัน

> ต้องมี **Microsoft Edge WebView2 Runtime** ซึ่ง Windows 10/11 ส่วนใหญ่มีอยู่แล้ว ถ้าไม่มี แอปจะพาไปหน้าดาวน์โหลดให้เอง

## 🧩 ตัวแปร

**ในหน้าข้อความ Status**

| ตัวแปร | ความหมาย | ตัวอย่าง |
|---|---|---|
| `{time}` / `{time12}` | เวลา | `21:30` / `9:30 PM` |
| `{date}` / `{day}` | วันที่ / วัน | `21/10` / `อังคาร` |
| `{music}` | เพลงที่กำลังเล่น (ตั้งรูปแบบในแท็บเพลง) | |
| `{afk}` | ข้อความ AFK (โชว์เฉพาะตอน AFK) | |
| `{session}` | เวลาที่เปิดส่งมาแล้ว | `1:02:05` |
| `{count:ชื่อParameter}` | จำนวนนับของตัวนับนั้น | `12` |

**ในแท็บเพลง:** `{title}` `{artist}` `{bar}` `{pos}` `{dur}` `{icon}` `{source}`
**ในแท็บ AFK:** `{afktime}` **ในข้อความของตัวนับ:** `{count}`

บรรทัดที่ตัวแปรไม่มีอะไรจะโชว์ (เช่น `{music}` ตอนไม่ได้เปิดเพลง) จะถูกซ่อนให้อัตโนมัติ
ข้อความถูกจำกัดที่ 144 ตัวอักษร / 9 บรรทัดตามที่ VRChat รองรับ โดยไม่ตัดอีโมจิหรือสระไทยให้แหว่ง

## 💗 ตั้งค่าตัวนับการแตะ

1. ใน Avatar ต้องมี Parameter ที่เปลี่ยนเป็น `true` (หรือมากกว่า 0) เมื่อโดนแตะ — ปกติคือ **VRC Contact Receiver** ที่ตั้ง Parameter ไว้
   และ Parameter นั้นต้องอยู่ใน *Expression Parameters* ของ Avatar
2. เปิดแท็บ **นับการแตะ** แล้วให้เพื่อนแตะ — ชื่อ Parameter จะโผล่ในกล่อง *Parameter ที่ได้รับล่าสุด* แตะชื่อนั้นเพื่อเพิ่มได้เลย
3. แก้ข้อความได้ตามใจ เช่น `🐰 โดนลูบหัวไปแล้ว {count} ครั้ง!` — จำนวนนับถูกจำไว้ข้ามการเปิดปิดแอป

> ถ้าเปลี่ยน Avatar แล้ว Parameter ไม่มา: ลบโฟลเดอร์ `%USERPROFILE%\AppData\LocalLow\VRChat\VRChat\OSC` แล้วเปลี่ยน Avatar ใหม่ (VRChat จะสร้าง config ใหม่)

## ❓ ปัญหาที่พบบ่อย

- **Chatbox ไม่ขึ้น** — เช็กว่าเปิด OSC ใน VRChat แล้ว และในแท็บตั้งค่า IP เป็น `127.0.0.1` พอร์ตส่ง `9000`
- **จุด OSC เป็นสีเหลือง** — แอปพร้อมรับแล้ว แต่ VRChat ยังไม่ได้ส่งอะไรมา (VRChat ส่งเฉพาะตอน Parameter เปลี่ยน ลองขยับตัวดู)
- **จุด OSC เป็นสีแดง** — พอร์ตรับถูกแอปอื่นใช้อยู่ ให้เปิด OSCQuery (ค่าเริ่มต้น) หรือเปลี่ยนพอร์ตรับ
- **เพลงไม่ขึ้น** — แอปโชว์เฉพาะแอป/เว็บเพลง (ดูรายชื่อในแท็บเพลง) ถ้าฟังในเบราว์เซอร์ แท็บเพลงต้องเป็นแท็บที่เปิดอยู่ของหน้าต่างนั้น (Windows ไม่บอกชื่อเว็บ แอปเลยดูจากชื่อแท็บ) ถ้าใช้แอป/เว็บเพลงที่ไม่อยู่ในรายชื่อ เพิ่มเองได้ในช่อง *เพิ่มแอป/เว็บเพลงเอง*
- **ดู log** — แท็บ *บันทึก* ในแอป หรือไฟล์ `%LOCALAPPDATA%\MeijiChatboxStatus\app.log`

## 📁 ไฟล์ของแอป

| | |
|---|---|
| การตั้งค่า | `%APPDATA%\MeijiChatboxStatus\settings.json` (ย้ายเครื่องได้โดยก๊อปไฟล์นี้) |
| log + แคชของ WebView2 | `%LOCALAPPDATA%\MeijiChatboxStatus\` |
| เปิดเองเมื่อเปิด VRChat | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → `MeijiChatboxStatus` |

ถ้าเคยใช้แอปเวอร์ชันเก่า (VrcAfkNative) ให้วาง `vrc_afk_settings.txt` ไว้ข้าง ๆ exe ตอนเปิดครั้งแรก — ระบบจะนำเข้าข้อความและตัวนับให้อัตโนมัติ

## 🛠️ Build เอง

ต้องมี [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
dotnet run --project src\MeijiChatboxStatus          # รันแบบ dev
dotnet test tests\MeijiChatboxStatus.Tests           # รันชุดทดสอบ
powershell -ExecutionPolicy Bypass -File .\publish.ps1 [-Lite]   # สร้างไฟล์แจกใน .\dist
```

โครงสร้าง:

```
src/MeijiChatboxStatus/
  Program.cs           จุดเริ่ม: เปิดได้ทีละหน้าต่าง, เช็ก WebView2
  MainForm.cs          หน้าต่าง WebView2 + ถาดไอคอน + สะพานเชื่อม JS ⇄ C#
  Core/
    Osc.cs             เข้ารหัส/ถอดรหัส OSC
    OscService.cs      ส่ง/รับ OSC + OSCQuery
    MediaWatcher.cs    อ่านเพลงจาก Windows
    StatusEngine.cs    ประกอบข้อความ + จังหวะการส่ง + ตัวนับ + AFK
    TextFormat.cs      ตัวแปร, จัดบรรทัด, ตัด 144 ตัวอักษร, แถบเวลา
    Settings.cs        การตั้งค่า (JSON) + นำเข้าจากเวอร์ชันเก่า
  wwwroot/             WebUI (HTML/CSS/JS ล้วน ฝังอยู่ใน exe เปิด offline ได้)
tests/MeijiChatboxStatus.Tests/
tools/make-icon.ps1    สร้างไอคอนแอป
```

WebUI เปิดดีไซน์ในเบราว์เซอร์ได้เลย (เปิด `src/MeijiChatboxStatus/wwwroot/index.html` จะใช้ข้อมูลจำลอง)

## 📜 License

[MIT](LICENSE) — ใช้ฟรี แก้ไข แจกต่อได้ตามสบาย

## 💖 เครดิต

- ฟอนต์ [Noto Sans Thai](https://github.com/notofonts/thai) — SIL Open Font License 1.1 (`wwwroot/fonts/OFL.txt`)
- [vrc-oscquery-lib](https://github.com/vrchat-community/vrc-oscquery-lib) — VRChat
- [Microsoft Edge WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)

---

## English summary

**Meiji Chatbox Status** keeps a cute, always-on status in your VRChat chatbox over OSC: clock, now playing (Spotify/YouTube/any Windows media session) with a progress bar, AFK time, and avatar-parameter counters (e.g. headpat counter via a Contact Receiver). It uses OSCQuery so it never fights other OSC apps over port 9001, starts/stops with VRChat, lives in the tray, and has a pastel WebUI (Thai/English, 5 themes) with a live chatbox preview.

1. Download `MeijiChatboxStatus.exe` from [Releases](https://github.com/finalsiren1/MeijiChatboxStatus/releases/latest) and run it (no install; if SmartScreen appears: *More info → Run anyway*).
2. In VRChat: **Action Menu ▸ Options ▸ OSC ▸ Enabled**.
3. Press **▶ Start**.

Settings: `%APPDATA%\MeijiChatboxStatus\settings.json` · Log: `%LOCALAPPDATA%\MeijiChatboxStatus\app.log`
