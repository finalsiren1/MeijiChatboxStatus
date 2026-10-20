# OSC-VRC — Project Rules

## กฎหลัก (ต้องทำตามเสมอ)

1. **เป้าหมายของโปรเจกต์**: แอปส่ง OSC เข้า VRChat Chatbox (`/chatbox/input`) เพื่อแสดงเป็น **Status ค้างไว้บน Chatbox ตลอดเวลา**
2. **ภาษา**: คุยกับผู้ใช้เป็น **ภาษาไทยตลอดทั้งโปรเจกต์** (โค้ด, ชื่อตัวแปร, commit message ใช้ภาษาอังกฤษได้ตามปกติ)
3. **คุณภาพระดับส่งต่อได้** (MIT License, repo: https://github.com/finalsiren1/MeijiChatboxStatus — แจก exe ผ่าน GitHub Releases ไม่ commit `dist/`): โค้ดเดิม (VrcAfkNative) เก่ามาก ต้อง rework ให้ **ถูกต้อง ใช้งานได้ดี สมบูรณ์ที่สุด** และ **แจกจ่ายให้คนอื่นใช้ได้** — ติดตั้ง/เปิดใช้ง่าย, ไม่ hardcode ค่าเฉพาะเครื่อง, มี error handling ที่บอกผู้ใช้ได้จริง (ห้าม `catch { }` เงียบ), เก็บ settings ในที่ที่ถูกต้อง, มีเอกสารวิธีใช้
4. **UI**: Rework UI ใหม่ให้ **สวย น่ารักพอดี ๆ เหมาะกับบรรยากาศ VRChat** — **ไม่ใช้ Native UI** (WinForms/WPF/Tkinter ฯลฯ) แต่ใช้ **WebUI ที่ Optimize แล้ว** (เบา, โหลดเร็ว, ไม่พึ่ง CDN ตอนรัน, รองรับ offline)

## โครงสร้างเวอร์ชันใหม่ (v2, `src/MeijiChatboxStatus/`)

- .NET 8 WinForms ใช้เป็นแค่หน้าต่าง host สำหรับ **WebView2**; UI ทั้งหมดอยู่ใน `wwwroot/` (HTML/CSS/JS ล้วน ไม่มี framework/CDN) ฝังเป็น EmbeddedResource และเสิร์ฟจาก memory ที่ `https://app.example/`
- JS ⇄ C# คุยกันด้วย JSON ผ่าน `chrome.webview.postMessage` (ดู `MainForm.OnWebMessage` และ `onMessage` ใน `app.js`)
- Core logic อยู่ใน `Core/` ทำงานบน UI thread ทั้งหมด (OSC receive อยู่ background แล้ว marshal ผ่าน SynchronizationContext)
- ข้อความ UI ต้องเพิ่มทั้ง `th` และ `en` ใน `I18N` ของ `app.js`
- `app.js` มี Mock mode: เปิด `wwwroot/index.html#<tab>` ในเบราว์เซอร์ได้โดยไม่ต้องรันแอป
- **ห้ามลบ** การตั้ง `CultureInfo.InvariantCulture` ใน `Program.Main` — VRChat.OSCQuery 0.0.7 ใช้ `LastIndexOf("/")` แบบ culture-sensitive ทำให้ stack overflow บนเครื่องที่ตั้ง th-TH
- คำสั่ง: `dotnet run --project src\MeijiChatboxStatus`, `dotnet test tests\MeijiChatboxStatus.Tests`, `.\publish.ps1 [-Lite]` → `dist\`
- ปิดแอปทุกรูปแบบต้องล้าง Chatbox: ปิดปกติ → `OnFormClosing`, crash → handler ใน `Program`, End task → โปรเซส `--guard <pid>` (`ChatboxGuard`)
- `--tray` = เปิดแบบซ่อน (ใช้โดย Autorun ใน HKCU Run, ลงทะเบียนเฉพาะ Release build); WebView2 โหลดเมื่อเปิดหน้าต่างครั้งแรกเท่านั้น
- ส่วนตั้งค่าขั้นสูงต้องมี hint บอกว่า "ถ้าใช้ได้อยู่แล้วไม่ต้องแก้"
- ทดสอบโดยไม่รบกวนเกม: เปลี่ยน sendPort ใน settings.json เป็นพอร์ตอื่นแล้วดักฟัง UDP (อย่าลืมเปลี่ยนกลับเป็น 9000)
- ระวัง: ถ้าเครื่องเปิด VRChat อยู่ แอปจะเริ่มส่งเข้า Chatbox จริงทันที (`AutoWithVrc`) — ทดสอบเสร็จให้ปิดและล้าง Chatbox
- ตรวจ i18n/CSS ที่ไม่ได้ใช้: `node tools\audit-webui.mjs src/MeijiChatboxStatus/wwwroot`

## สิ่งที่ผู้ใช้ตัดสินใจแล้ว (อย่าย้อนกลับเอง)

- ชื่อ: **Meiji Chatbox Status (MCS)**, exe `MeijiChatboxStatus.exe`, settings `%APPDATA%\MeijiChatboxStatus\`
- ฟอนต์ **Noto Sans Thai** (ไม่เอาฟอนต์น่ารักแบบ Mali), ตัวอย่าง Chatbox ไม่มีตัวละคร/หาง
- ปุ่มลบเป็นตัวหนังสือ ("ลบหน้า", "ลบ") ไม่ใช้ไอคอนถังขยะ
- หยุดส่ง/ปิดโปรแกรม **ล้าง Chatbox เสมอ** (ไม่มีตัวเลือก), ไม่มีเสียงแจ้งเตือน (ส่ง `,sTF` เสมอ)
- ตัวเลือกเดียว "เปิดโปรแกรมเองเมื่อเปิด VRChat" (`AutoWithVrc`, ค่าเริ่มต้นเปิด) — ไม่มี "เริ่มส่งทันทีเมื่อเปิดแอป"
- กด X → popup ถาม ปิด/ซ่อน + ติ๊ก "ไม่ต้องถามอีก" (`CloseAction`: ask/tray/exit)
- เพลง: โชว์เฉพาะแอป/เว็บเพลง (`MediaFilter`) ห้ามหลุด TikTok/IG/Facebook/NSFW — ไม่แน่ใจให้ซ่อนไว้ก่อน
- ใช้คำในหน้า UI ให้คนทั่วไปเข้าใจง่าย อธิบายว่ากดแล้วเกิดอะไรขึ้น ไม่ใช้ศัพท์เทคนิค

## โค้ดเดิม (VrcAfkNative — ลบแล้ว)

เหลือแค่ `LegacyImport` สำหรับนำเข้า `vrc_afk_settings.txt` ปัญหาเดิมที่แก้แล้ว: settings แบบเรียงบรรทัด, error ถูกกลืนเงียบ, พอร์ต 9001 ชนกับแอปอื่น (→ OSCQuery), ส่งซ้ำไม่จำกัด, ตัด 144 ตัวทำ emoji แตก
