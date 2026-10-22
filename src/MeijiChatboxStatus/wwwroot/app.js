'use strict';

/* ════════════════════════ i18n ════════════════════════ */
const I18N = {
  th: {
    verLatest: 'เป็นเวอร์ชันล่าสุด', verUpdate: 'มีอัปเดต', saved: 'บันทึกแล้ว ✓',
    start: 'เริ่มส่ง', stop: 'หยุดส่ง',
    previewTitle: 'ตัวอย่างบน Chatbox', notSending: 'ยังไม่ได้ส่ง', sending: 'กำลังส่ง ♡',
    chars: 'ตัวอักษร', lines: 'บรรทัด',
    nothingPlaying: 'ไม่มีเพลงเล่นอยู่', paused: 'หยุดชั่วคราว',
    notAfk: 'ไม่ได้ AFK', afkFor: 'AFK อยู่ {0}', noTrigger: 'ยังไม่มีใครแตะ', lastTrigger: 'ล่าสุด: {0}',
    tabStatus: 'ข้อความ', tabMusic: 'เพลง', tabAfk: 'AFK', tabCounters: 'นับการแตะ', tabSettings: 'ตั้งค่า', tabLog: 'บันทึก',
    tipOsc: 'อย่าลืมเปิด OSC ใน VRChat: <b>Action Menu ▸ Options ▸ OSC ▸ Enabled</b>',
    pagesTitle: 'ข้อความ Status', addPage: 'เพิ่มหน้า', pagesHint: 'ถ้ามีหลายหน้า จะสลับโชว์ทีละหน้าวนไปเรื่อย ๆ',
    rotateEvery: 'สลับหน้าทุก', seconds: 'วินาที', page: 'หน้า {0}',
    insertVar: 'แตะเพื่อแทรกตัวแปร', insertEmoji: 'อีโมจิน่ารัก ๆ',
    musicTitle: 'เพลงที่กำลังเล่น',
    musicHint: 'ดึงเพลงจาก Spotify, YouTube และแอปอื่นที่แสดงบนแผงเพลงของ Windows ใส่ <code>{music}</code> ในหน้าข้อความเพื่อโชว์',
    showPaused: 'โชว์เพลงตอนหยุดเล่นด้วย', barStyle: 'สไตล์แถบเวลา', barWidth: 'ความยาวแถบ',
    afkTitle: 'ข้อความตอน AFK',
    afkHint: 'VRChat จะบอกแอปเองเมื่อคุณ AFK (ถอดแว่น/สลับหน้าต่าง) ใส่ <code>{afk}</code> ในหน้าข้อความเพื่อโชว์',
    countersTitle: 'นับการแตะ / Parameter', resetAll: 'รีเซ็ตทั้งหมด', confirmReset: 'แน่ใจนะ? กดอีกที', addCounter: 'เพิ่มตัวนับ',
    countersHint: 'เมื่อ Avatar Parameter (เช่น Contact Receiver) ทำงาน จะนับ +1 แล้วโชว์ข้อความแทรกชั่วคราว ใช้ <code>{count}</code> แทนจำนวนครั้ง และใช้ <code>{count:ชื่อParameter}</code> ในหน้าข้อความได้ด้วย',
    noCounters: 'ยังไม่มีตัวนับ ลองกด “เพิ่มตัวนับ” ดูนะ ♡',
    seenParams: 'Parameter ที่ได้รับล่าสุด', refresh: 'รีเฟรช',
    seenHint: 'เข้า VRChat แล้วลองให้คนแตะ Avatar ชื่อ Parameter จะโผล่ตรงนี้ แตะเพื่อเพิ่มเป็นตัวนับ',
    noParams: 'ยังไม่ได้รับ Parameter จาก VRChat', added: 'เพิ่ม {0} แล้ว ♡', alreadyAdded: 'มี {0} อยู่แล้วน้า',
    paramPh: 'ชื่อ Parameter', msgPh: 'ข้อความ เช่น โดนแตะหัว {count} ครั้ง!', defaultCounterMsg: '💗 โดนแตะไปแล้ว {count} ครั้ง!',
    test: 'ลองดู', reset: 'รีเซ็ต', del: 'ลบ', delPage: 'ลบหน้า', moveUp: 'เลื่อนขึ้น', testing: 'ลองโชว์ในตัวอย่างแล้ว ✨',
    appearance: 'หน้าตา', language: 'ภาษา', theme: 'ธีม',
    interval: 'อัปเดตทุก', behavior: 'การทำงาน',
    autoWithVrc: 'เปิดโปรแกรมเองเมื่อเปิด VRChat',
    autoWithVrcHint: 'เปิด VRChat เมื่อไหร่ โปรแกรมจะเปิดขึ้นมาและเริ่มส่ง Status ให้เลย ทุกครั้ง (ตอนเปิดเครื่องจะไม่มีหน้าต่างหรือไอคอนโผล่ขึ้นมา)',
    closeWithVrcHint: 'ปิด VRChat แล้วโปรแกรมจะล้าง Chatbox และปิดตัวเองไปด้วย',
    advanced: 'ขั้นสูง',
    advancedHint: '<b>ถ้าใช้งานได้ปกติอยู่แล้ว ไม่ต้องแก้ส่วนนี้</b> — สำหรับคนที่เข้าใจ OSC หรือใช้แอป OSC หลายตัวพร้อมกัน ถ้าแก้แล้วมีปัญหา ให้กลับไปใช้ค่าเดิม: เปิด OSCQuery, IP 127.0.0.1, พอร์ตส่ง 9000, พอร์ตรับ 9001, อัปเดตทุก 2s',
    closeWithVrc: 'ปิดโปรแกรมเมื่อปิด VRChat',
    closeAction: 'เมื่อกดปิดหน้าต่าง (X)', closeAsk: 'ถามทุกครั้ง', closeTray: 'ซ่อนไว้ที่ถาด', closeExit: 'ปิดโปรแกรม',
    closeTitle: 'จะปิดโปรแกรม หรือซ่อนไว้?', closeText: 'ถ้าซ่อนไว้ แอปจะทำงานต่อที่ถาดไอคอนมุมขวาล่าง และยังส่ง Status ได้ตามปกติ ถ้าปิดโปรแกรม จะล้างข้อความบน Chatbox ให้ด้วย',
    dontAsk: 'จำตัวเลือกนี้ ไม่ต้องถามอีก (เปลี่ยนได้ในตั้งค่า)', cancel: 'ยกเลิก', closeTrayBtn: 'ซ่อนไว้ที่ถาดไอคอน',
    sourcesTitle: 'แหล่งเพลงที่โชว์ได้',
    sourcesHint: 'โชว์เฉพาะแอปและเว็บเพลงเท่านั้น วิดีโอจาก TikTok, IG, Facebook หรือเว็บอื่น ๆ จะไม่ขึ้นบน Chatbox ถ้าเปิดเพลงในเบราว์เซอร์ จะสลับไปแท็บอื่นก็ได้ แอปยังหาเพลงเจอ',
    extraSources: 'เพิ่มแอป/เว็บเพลงเอง (คั่นด้วย ,)', extraPh: 'เช่น MusicApp, Niconico', notMusic: 'ไม่ใช่แอป/เว็บเพลง — ไม่โชว์บน Chatbox',
    useOscQuery: 'ใช้ OSCQuery <em>(แนะนำ — ไม่ชนกับแอป OSC อื่น)</em>',
    ip: 'IP ของ VRChat', sendPort: 'พอร์ตส่ง', listenPort: 'พอร์ตรับ',
    oscQueryOn: 'กำลังรับข้อมูลผ่าน OSCQuery (UDP {0})', oscPortOn: 'กำลังรับข้อมูลที่พอร์ต {0}', oscOff: 'ไม่ได้รับข้อมูลจาก VRChat',
    oscReceiving: 'ได้รับข้อมูลจาก VRChat ♡', oscWaiting: 'รอข้อมูลจาก VRChat…',
    vrcOn: 'VRChat เปิดอยู่', vrcOff: 'ยังไม่เจอ VRChat',
    aboutText: 'ฟรีและโอเพนซอร์ส (MIT) แชร์ให้เพื่อนใช้ได้เลย ♡', openData: 'เปิดโฟลเดอร์ตั้งค่า', oscDocs: 'คู่มือ OSC ของ VRChat', exitApp: 'ออกจากแอป',
    logTitle: 'บันทึกการทำงาน',
    updates: 'อัปเดต', updateNow: 'อัปเดต', updCheck: 'เช็คอัปเดต',
    updAvailTitle: 'มีเวอร์ชันใหม่ v{0} ✨', updYouHave: 'ตอนนี้ใช้ v{0} อยู่',
    updChecking: 'กำลังเช็คอัปเดต…', updLatest: 'เป็นเวอร์ชันล่าสุดแล้ว ♡', updIdle: 'ยังไม่ได้เช็คอัปเดต',
    updError: 'เช็คอัปเดตไม่สำเร็จ: {0}', updFailed: 'อัปเดตไม่สำเร็จ: {0}',
    updInfo: 'เวอร์ชันที่ใช้อยู่ v{0}', updCheckedAt: 'เช็คล่าสุด {0} น.',
    checkUpdates: 'เช็คอัปเดตให้อัตโนมัติ',
    checkUpdatesHint: 'เช็คตอนเปิดโปรแกรมและทุก 6 ชั่วโมง ถ้ามีเวอร์ชันใหม่จะแจ้งไว้ด้านซ้าย จะอัปเดตก็ต่อเมื่อคุณกดยืนยันเท่านั้น',
    updModalTitle: 'อัปเดตเป็น v{0} เลยไหม?',
    updModalText: 'แอปจะดาวน์โหลดเวอร์ชันใหม่ (ประมาณ {0} MB) แล้วปิดและเปิดตัวเองใหม่ภายในไม่กี่วินาที การตั้งค่าและตัวนับยังอยู่ครบ ระหว่างนั้นข้อความบน Chatbox จะหายไปแป๊บนึง',
    updNotes: 'มีอะไรใหม่', updPage: 'ดูบน GitHub',
    updDownloading: 'กำลังดาวน์โหลด… {0}%', updInstalling: 'กำลังติดตั้ง… เดี๋ยวแอปจะเปิดขึ้นมาใหม่เองนะ ♡',
    setupTitleNew: 'ติดตั้ง Meiji Chatbox Status', setupTitleUpdate: 'อัปเดต Meiji Chatbox Status',
    setupIntro: 'โปรแกรมจะติดตั้งลงเครื่อง สร้างทางลัดที่ Start Menu และหน้าจอ แล้วลบเวอร์ชันเก่าที่เจอด้านล่างทั้งหมด การตั้งค่าและตัวนับเดิมจะถูกเก็บไว้',
    setupScanning: 'กำลังค้นหาเวอร์ชันเก่าในเครื่อง…', setupFound: 'เจอของเก่า {0} รายการ — จะลบทั้งหมด', setupNone: 'ไม่เจอเวอร์ชันเก่าในเครื่อง ✓',
    setupWhere: 'ติดตั้งที่', setupInstall: 'ติดตั้ง', setupUpdate: 'อัปเดต', setupError: 'ติดตั้งไม่สำเร็จ: {0}',
    step_closing: 'กำลังปิดโปรแกรมเก่า…', step_importing: 'กำลังนำเข้าการตั้งค่าเดิม…', step_removing: 'กำลังลบเวอร์ชันเก่า…',
    step_copying: 'กำลังติดตั้ง…', step_shortcuts: 'กำลังสร้างทางลัด…', step_done: 'เสร็จแล้ว! กำลังเปิดโปรแกรม ♡',
    kind_process: 'โปรแกรมเก่าที่เปิดอยู่ (จะปิดให้)', kind_folder: 'โฟลเดอร์โปรแกรมเก่า', kind_app: 'ไฟล์โปรแกรมเก่า', kind_file: 'ไฟล์เก่า',
    kind_settings: 'ไฟล์ตั้งค่าเก่า (นำเข้าก่อนลบ)', kind_shortcut: 'ทางลัดเก่า', kind_autorun: 'รายการเปิดอัตโนมัติเก่า', kind_data: 'ข้อมูลเก่า',
    v_time: 'เวลา', v_time12: 'เวลา 12 ชม.', v_date: 'วันที่', v_day: 'วัน', v_music: 'เพลง', v_afk: 'AFK',
    v_session: 'เวลาที่ออนไลน์', v_count: 'จำนวนนับ', v_title: 'ชื่อเพลง', v_artist: 'ศิลปิน', v_bar: 'แถบเวลา',
    v_pos: 'เวลาปัจจุบัน', v_dur: 'ความยาวเพลง', v_icon: '▶/⏸', v_source: 'แอปที่เล่น', v_afktime: 'AFK มานาน',
    bar_line: 'เส้น', bar_dots: 'จุด', bar_blocks: 'บล็อก', bar_hearts: 'หัวใจ', bar_classic: 'คลาสสิก',
    th_sakura: 'ซากุระ', th_lavender: 'ลาเวนเดอร์', th_mint: 'มิ้นต์', th_sky: 'ท้องฟ้า', th_night: 'กลางคืน',
  },
  en: {
    verLatest: 'Up to date', verUpdate: 'Update available', saved: 'Saved ✓',
    start: 'Start', stop: 'Stop',
    previewTitle: 'Chatbox preview', notSending: 'Not sending', sending: 'Sending ♡',
    chars: 'Characters', lines: 'Lines',
    nothingPlaying: 'Nothing playing', paused: 'Paused',
    notAfk: 'Not AFK', afkFor: 'AFK for {0}', noTrigger: 'No touches yet', lastTrigger: 'Last: {0}',
    tabStatus: 'Status', tabMusic: 'Music', tabAfk: 'AFK', tabCounters: 'Counters', tabSettings: 'Settings', tabLog: 'Log',
    tipOsc: 'Remember to enable OSC in VRChat: <b>Action Menu ▸ Options ▸ OSC ▸ Enabled</b>',
    pagesTitle: 'Status messages', addPage: 'Add page', pagesHint: 'With several pages, they take turns on the chatbox.',
    rotateEvery: 'Switch page every', seconds: 'seconds', page: 'Page {0}',
    insertVar: 'Tap to insert a variable', insertEmoji: 'Cute emoji',
    musicTitle: 'Now playing',
    musicHint: 'Reads Spotify, YouTube and any app shown in the Windows media flyout. Put <code>{music}</code> in a status page to show it.',
    showPaused: 'Show music while paused', barStyle: 'Progress bar style', barWidth: 'Bar length',
    afkTitle: 'AFK message',
    afkHint: 'VRChat tells the app when you go AFK (headset off / window unfocused). Put <code>{afk}</code> in a status page to show it.',
    countersTitle: 'Touch / parameter counters', resetAll: 'Reset all', confirmReset: 'Sure? Click again', addCounter: 'Add counter',
    countersHint: 'When an avatar parameter (e.g. a Contact Receiver) turns on, it counts +1 and briefly shows a message. Use <code>{count}</code> for the number, or <code>{count:ParamName}</code> in status pages.',
    noCounters: 'No counters yet — try “Add counter” ♡',
    seenParams: 'Recently received parameters', refresh: 'Refresh',
    seenHint: 'Join VRChat and get someone to touch your avatar — parameter names appear here. Tap one to add it as a counter.',
    noParams: 'No parameters received from VRChat yet', added: 'Added {0} ♡', alreadyAdded: '{0} is already there',
    paramPh: 'Parameter name', msgPh: 'Message, e.g. Headpats: {count}!', defaultCounterMsg: '💗 Touched {count} times!',
    test: 'Try it', reset: 'Reset', del: 'Delete', delPage: 'Delete page', moveUp: 'Move up', testing: 'Shown in the preview ✨',
    appearance: 'Appearance', language: 'Language', theme: 'Theme',
    interval: 'Update every', behavior: 'Behavior',
    autoWithVrc: 'Open automatically with VRChat',
    autoWithVrcHint: 'Every time you start VRChat, the app opens and starts your status (nothing pops up when your PC starts).',
    closeWithVrcHint: 'When VRChat closes, the app clears your chatbox and closes too.',
    advanced: 'Advanced',
    advancedHint: '<b>If everything works, leave this alone.</b> For people who know OSC or run several OSC apps at once. If something breaks, go back to the defaults: OSCQuery on, IP 127.0.0.1, send port 9000, receive port 9001, update every 2s.',
    closeWithVrc: 'Exit the app when VRChat closes',
    closeAction: 'When closing the window (X)', closeAsk: 'Ask every time', closeTray: 'Hide to tray', closeExit: 'Exit app',
    closeTitle: 'Exit the app, or hide it?', closeText: 'Hidden, the app keeps running in the tray (bottom-right) and keeps sending your status. Exiting also clears your chatbox.',
    dontAsk: 'Remember my choice, don\'t ask again (change it in Settings)', cancel: 'Cancel', closeTrayBtn: 'Hide to tray',
    sourcesTitle: 'Allowed music sources',
    sourcesHint: 'Only music apps and music sites are shown. Videos from TikTok, IG, Facebook or any other site never reach your chatbox. Music in a browser is found even when its tab isn\'t the one you\'re looking at.',
    extraSources: 'Add your own players/sites (comma separated)', extraPh: 'e.g. MusicApp, Niconico', notMusic: 'Not a music app/site — hidden from chatbox',
    useOscQuery: 'Use OSCQuery <em>(recommended — no port conflicts with other OSC apps)</em>',
    ip: 'VRChat IP', sendPort: 'Send port', listenPort: 'Receive port',
    oscQueryOn: 'Receiving via OSCQuery (UDP {0})', oscPortOn: 'Receiving on port {0}', oscOff: 'Not receiving from VRChat',
    oscReceiving: 'Receiving data from VRChat ♡', oscWaiting: 'Waiting for VRChat…',
    vrcOn: 'VRChat is running', vrcOff: 'VRChat not found',
    aboutText: 'Free & open source (MIT) — share it with your friends ♡', openData: 'Open settings folder', oscDocs: 'VRChat OSC guide', exitApp: 'Exit app',
    logTitle: 'Activity log',
    updates: 'Updates', updateNow: 'Update', updCheck: 'Check for updates',
    updAvailTitle: 'Version {0} is out ✨', updYouHave: 'You have v{0}',
    updChecking: 'Checking for updates…', updLatest: 'You\'re on the latest version ♡', updIdle: 'Not checked yet',
    updError: 'Couldn\'t check for updates: {0}', updFailed: 'Update failed: {0}',
    updInfo: 'Installed version v{0}', updCheckedAt: 'last checked {0}',
    checkUpdates: 'Check for updates automatically',
    checkUpdatesHint: 'Checks when the app opens and every 6 hours. A new version shows up on the left — nothing is installed until you confirm.',
    updModalTitle: 'Update to v{0}?',
    updModalText: 'The app downloads the new version (about {0} MB), then closes and reopens itself within a few seconds. Your settings and counters are kept. Your chatbox goes blank for a moment meanwhile.',
    updNotes: 'What\'s new', updPage: 'View on GitHub',
    updDownloading: 'Downloading… {0}%', updInstalling: 'Installing… the app will reopen by itself ♡',
    setupTitleNew: 'Install Meiji Chatbox Status', setupTitleUpdate: 'Update Meiji Chatbox Status',
    setupIntro: 'The app will be installed for your account with Start Menu and desktop shortcuts, and every older version listed below will be removed. Your existing settings and counters are kept.',
    setupScanning: 'Looking for older versions…', setupFound: 'Found {0} old item(s) — all will be removed', setupNone: 'No older versions found ✓',
    setupWhere: 'Install location', setupInstall: 'Install', setupUpdate: 'Update', setupError: 'Installation failed: {0}',
    step_closing: 'Closing old versions…', step_importing: 'Importing your old settings…', step_removing: 'Removing old versions…',
    step_copying: 'Installing…', step_shortcuts: 'Creating shortcuts…', step_done: 'Done! Opening the app ♡',
    kind_process: 'Running old app (will be closed)', kind_folder: 'Old app folder', kind_app: 'Old app files', kind_file: 'Old file',
    kind_settings: 'Old settings file (imported first)', kind_shortcut: 'Old shortcut', kind_autorun: 'Old startup entry', kind_data: 'Old app data',
    v_time: 'time', v_time12: '12h time', v_date: 'date', v_day: 'weekday', v_music: 'music', v_afk: 'AFK',
    v_session: 'time online', v_count: 'counter', v_title: 'title', v_artist: 'artist', v_bar: 'progress bar',
    v_pos: 'position', v_dur: 'length', v_icon: '▶/⏸', v_source: 'player app', v_afktime: 'AFK duration',
    bar_line: 'Line', bar_dots: 'Dots', bar_blocks: 'Blocks', bar_hearts: 'Hearts', bar_classic: 'Classic',
    th_sakura: 'Sakura', th_lavender: 'Lavender', th_mint: 'Mint', th_sky: 'Sky', th_night: 'Night',
  },
};

const STATUS_VARS = ['time', 'time12', 'date', 'day', 'music', 'afk', 'session', 'count'];
const MUSIC_VARS = ['title', 'artist', 'bar', 'pos', 'dur', 'icon', 'source'];
const AFK_VARS = ['afktime'];
const EMOJI = ['✨', '🌸', '💖', '💗', '🎀', '⭐', '🌙', '☁️', '🐰', '🐱', '🦊', '🍓', '🍰', '🧸', '💤', '🎵', '🎧', '🎮', '💬', '♡', '☆', '♪', '｡･:*', '(*´▽`*)', '(≧◡≦)', 'ฅ^•ﻌ•^ฅ'];
const BAR_STYLES = ['line', 'dots', 'blocks', 'hearts', 'classic'];
const THEMES = { sakura: ['#ff6fa5', '#b48cff'], lavender: ['#9a78ff', '#ff8fc8'], mint: ['#2fbf9f', '#6cb8ff'], sky: ['#4aa3ff', '#a58bff'], night: ['#2a2540', '#ff8fc0'] };

/* ════════════════════════ helpers ════════════════════════ */
const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];
const fmt = (s, ...args) => s.replace(/\{(\d)\}/g, (_, i) => args[i] ?? '');

let S = null;           // settings (source of truth for the form; the app owns counter values)
let state = {};         // live state pushed by the app
let lang = 'th';
let lastTextarea = null;
let saveTimer = 0;
let paramsTimer = 0;

const t = (key) => I18N[lang][key] ?? I18N.th[key] ?? key;

function getPath(obj, path) { return path.split('.').reduce((o, k) => o?.[k], obj); }
function setPath(obj, path, value) {
  const keys = path.split('.');
  const last = keys.pop();
  keys.reduce((o, k) => o[k], obj)[last] = value;
}

function el(tag, props = {}, ...children) {
  const node = document.createElement(tag);
  for (const [k, v] of Object.entries(props)) {
    if (k === 'class') node.className = v;
    else if (k.startsWith('on')) node.addEventListener(k.slice(2), v);
    else if (k in node) node[k] = v;
    else node.setAttribute(k, v);
  }
  node.append(...children);
  return node;
}

function toast(text) {
  const box = $('#toast');
  box.textContent = text;
  box.classList.add('show');
  clearTimeout(toast.timer);
  toast.timer = setTimeout(() => box.classList.remove('show'), 2200);
}

function storageGet(key) { try { return localStorage.getItem(key); } catch { return null; } }
function storageSet(key, value) { try { localStorage.setItem(key, value); } catch { /* storage unavailable */ } }

/* ════════════════════════ bridge ════════════════════════ */
const webview = window.chrome?.webview;
function send(type, data = {}) {
  const msg = { type, ...data };
  if (webview) webview.postMessage(msg);
  else Mock.handle(msg);
}

function onMessage(msg) {
  switch (msg.type) {
    case 'init':
      S = msg.settings;
      $('#version').textContent = 'v' + msg.version;
      $('#hdr-version').textContent = 'v' + msg.version;
      $('#log').replaceChildren();
      msg.log.forEach(addLog);
      upd = msg.update ?? null;
      renderAll();
      break;
    case 'update': applyUpdate(msg.update); break;
    case 'state': applyState(msg); break;
    case 'art': renderArt(msg.src); break;
    case 'log': addLog(msg.entry); break;
    case 'params': renderParams(msg.list); break;
    case 'askClose': openCloseModal(); break;
    case 'setup': showSetup(msg); break;
    case 'setupScan': renderSetupScan(msg.items); break;
    case 'setupStep': setupStatus(t('step_' + msg.step)); break;
    case 'setupError':
      setupStatus(fmt(t('setupError'), msg.message), true);
      $('#setup-install').disabled = false;
      break;
  }
}

/* ── install / update screen ── */
let setupInfo = null;

function showSetup(info) {
  setupInfo = info;
  lang = info.lang;
  document.body.classList.add('setup-mode');
  $('#setup').hidden = false;
  $('#setup-version').textContent = 'v' + info.version;
  $('#setup-dir').textContent = info.installDir;
  applySetupLang();
  setupStatus('');
}

function applySetupLang() {
  $$('#setup [data-i18n]').forEach(n => { n.textContent = t(n.dataset.i18n); });
  $('#setup-title').textContent = t(setupInfo.update ? 'setupTitleUpdate' : 'setupTitleNew');
  $('#setup-install').textContent = t(setupInfo.update ? 'setupUpdate' : 'setupInstall');
  if (!setupInfo.items) $('#setup-scan').textContent = t('setupScanning');
}

function renderSetupScan(items) {
  setupInfo.items = items;
  $('#setup-scan').textContent = items.length ? fmt(t('setupFound'), items.length) : t('setupNone');
  $('#setup-items').replaceChildren(...items.map(i =>
    el('li', {}, el('span', { class: 'kind' }, t('kind_' + i.kind)), el('code', { title: i.path }, i.path))));
  $('#setup-install').disabled = false;
}

function setupStatus(text, isError = false) {
  const box = $('#setup-status');
  box.textContent = text;
  box.classList.toggle('error', isError);
}

function scheduleSave() {
  clearTimeout(saveTimer);
  saveTimer = setTimeout(saveNow, 450);
}
function saveNow() {
  clearTimeout(saveTimer);
  saveTimer = 0;
  send('save', { settings: S });
  const badge = $('#saved');
  badge.classList.add('show');
  clearTimeout(saveNow.timer);
  saveNow.timer = setTimeout(() => badge.classList.remove('show'), 1400);
}
const flushSave = () => { if (saveTimer) saveNow(); };

/* ════════════════════════ render ════════════════════════ */
function renderAll() {
  lang = S.language;
  applyTheme();
  applyLang();
  bindInputs();
  renderPages();
  renderCounters();
  renderChips();
  renderBarStyles();
  renderThemes();
  renderCloseSeg();
  renderSources();
  renderUpdate();
}

/* ── updates ── */
let upd = null;          // update status pushed by the app (Updater.cs)
let updManual = false;   // the user pressed "check": say so even when nothing is new

const updBusy = () => upd?.state === 'downloading' || upd?.state === 'installing';

function applyUpdate(u) {
  const was = upd?.state;
  upd = u;
  if (updManual && was === 'checking' && u.state !== 'checking') {
    updManual = false;
    if (u.state === 'latest') toast(t('updLatest'));
  }
  renderUpdate();
}

function renderUpdate() {
  if (!S || !upd) return;
  const u = upd, has = !!u.latest, busy = updBusy();

  // sidebar notice
  $('#update-card').hidden = !has;
  if (has) {
    $('#update-title').textContent = fmt(t('updAvailTitle'), u.latest);
    $('#update-sub').textContent = u.state === 'downloading' ? fmt(t('updDownloading'), Math.round(u.progress * 100))
      : u.state === 'installing' ? t('updInstalling')
      : u.state === 'error' ? fmt(t('updFailed'), u.error) : fmt(t('updYouHave'), u.current);
    $('#update-btn').disabled = busy;
  }

  // header badge: green check when up to date, orange dot when there's an update (nothing while unknown)
  const badge = $('#ver-badge');
  const badgeState = has ? 'new' : u.state === 'latest' ? 'ok' : '';
  badge.hidden = !badgeState;
  badge.className = 'ver-badge ' + badgeState;
  $('#ver-text').textContent = badgeState ? t(has ? 'verUpdate' : 'verLatest') : '';

  // settings card
  $('#upd-status').textContent = has ? fmt(t('updAvailTitle'), u.latest)
    : u.state === 'checking' ? t('updChecking')
    : u.state === 'latest' ? t('updLatest')
    : u.state === 'error' ? fmt(t('updError'), u.error) : t('updIdle');
  $('#upd-detail').textContent = fmt(t('updInfo'), u.current) + (u.checkedAt ? ' · ' + fmt(t('updCheckedAt'), u.checkedAt) : '');
  const action = $('#upd-action');
  action.textContent = t(has ? 'updateNow' : 'updCheck');
  action.className = 'btn ' + (has ? 'primary' : 'soft');
  action.disabled = busy || u.state === 'checking';

  // popup
  if (!$('#update-modal').hidden) renderUpdateModal();
}

function renderUpdateModal() {
  const u = upd, busy = updBusy();
  $('#update-modal-title').textContent = fmt(t('updModalTitle'), u.latest);
  $('#update-modal-text').textContent = fmt(t('updModalText'), u.sizeMb || 70);
  // Release notes are Markdown; show them as plain text without the heading/bold markers.
  const notes = (u.notes ?? '').replace(/^#+\s*/gm, '').replace(/\*\*/g, '').trim();
  $('#update-notes-box').hidden = !notes;
  $('#update-notes').textContent = notes;
  $('#update-progress').hidden = !busy;
  $('#update-progress-bar').style.width = (u.state === 'installing' ? 100 : u.progress * 100).toFixed(1) + '%';
  $('#update-progress-text').textContent = u.state === 'installing' ? t('updInstalling') : fmt(t('updDownloading'), Math.round(u.progress * 100));
  const err = $('#update-error');
  err.hidden = u.state !== 'error';
  err.textContent = fmt(t('updFailed'), u.error ?? '');
  $('#update-confirm').disabled = busy;
  $('#update-cancel').disabled = u.state === 'installing';
  $('#update-page').hidden = !u.url;
}

function openUpdateModal() {
  if (!upd?.latest) return;
  $('#update-modal').hidden = false;
  renderUpdateModal();
  $('#update-confirm').focus();
}

function closeUpdateModal() {
  if (upd?.state === 'installing') return;
  if (upd?.state === 'downloading') send('cancelUpdate');
  $('#update-modal').hidden = true;
}

function applyLang() {
  document.documentElement.lang = lang;
  $$('[data-i18n]').forEach(n => { n.textContent = t(n.dataset.i18n); });
  $$('[data-i18n-html]').forEach(n => { n.innerHTML = t(n.dataset.i18nHtml); }); // trusted strings from I18N only
  $$('[data-i18n-ph]').forEach(n => { n.placeholder = t(n.dataset.i18nPh); });
  $$('#lang-seg button').forEach(b => b.classList.toggle('active', b.dataset.lang === lang));
  applyState(state);
}

function applyTheme() {
  document.documentElement.dataset.theme = THEMES[S.theme] ? S.theme : 'sakura';
}

function bindInputs() {
  $$('[data-bind]').forEach(input => {
    const path = input.dataset.bind;
    const value = getPath(S, path);
    if (input.type === 'checkbox') input.checked = !!value;
    else input.value = value ?? '';
    updateRangeLabel(input);
    if (input.bound) return;
    input.bound = true;
    input.addEventListener('input', () => {
      let v = input.value;
      if (input.type === 'checkbox') v = input.checked;
      else if (input.type === 'number' || input.type === 'range') {
        v = parseFloat(v);
        if (!Number.isFinite(v)) return;
      }
      setPath(S, path, v);
      updateRangeLabel(input);
      if (path === 'media.barWidth') renderBarStyles();
      if (path === 'osc.useOscQuery') applyState(state);
      scheduleSave();
    });
    if (input.tagName === 'TEXTAREA') input.addEventListener('focus', () => { lastTextarea = input; });
  });
}

function updateRangeLabel(input) {
  if (input.type !== 'range') return;
  const label = $(`.range-val[data-for="${input.dataset.bind}"]`);
  if (label) label.textContent = input.value + (label.dataset.suffix ?? '');
}

/* ── status pages ── */
function renderPages() {
  const list = $('#pages');
  list.replaceChildren();
  S.pages.forEach((page, i) => {
    const node = $('#tpl-page').content.firstElementChild.cloneNode(true);
    const text = $('.p-text', node);
    const enabled = $('.p-enabled', node);
    node.classList.toggle('off', !page.enabled);
    $('.p-title', node).textContent = fmt(t('page'), i + 1);
    const updateLen = () => { $('.p-len', node).textContent = `${page.text.length} ${t('chars')}`; };
    updateLen();
    text.value = page.text;
    text.addEventListener('input', () => { page.text = text.value; updateLen(); scheduleSave(); });
    text.addEventListener('focus', () => { lastTextarea = text; });
    enabled.checked = page.enabled;
    enabled.addEventListener('change', () => {
      page.enabled = enabled.checked;
      node.classList.toggle('off', !page.enabled);
      updateRotateRow();
      scheduleSave();
    });
    const up = $('.p-up', node);
    up.title = t('moveUp');
    up.hidden = i === 0;
    up.addEventListener('click', () => {
      [S.pages[i - 1], S.pages[i]] = [S.pages[i], S.pages[i - 1]];
      renderPages(); scheduleSave();
    });
    const del = $('.p-del', node);
    del.textContent = t('delPage');
    del.hidden = S.pages.length <= 1;
    del.addEventListener('click', () => { S.pages.splice(i, 1); renderPages(); scheduleSave(); });
    list.append(node);
  });
  updateRotateRow();
}

function updateRotateRow() {
  $('#rotate-row').hidden = S.pages.filter(p => p.enabled).length <= 1;
}

/* ── counters ── */
function renderCounters() {
  const list = $('#counters');
  list.replaceChildren();
  $('#counters-empty').hidden = S.counters.length > 0;
  S.counters.forEach((c, i) => {
    const node = $('#tpl-counter').content.firstElementChild.cloneNode(true);
    node.classList.toggle('off', !c.enabled);
    const enabled = $('.c-enabled', node);
    enabled.checked = c.enabled;
    enabled.addEventListener('change', () => { c.enabled = enabled.checked; node.classList.toggle('off', !c.enabled); scheduleSave(); });

    const param = $('.c-param', node);
    param.placeholder = t('paramPh');
    param.value = c.parameter;
    param.addEventListener('input', () => { c.parameter = param.value.trim(); scheduleSave(); });

    const msg = $('.c-msg', node);
    msg.placeholder = t('msgPh');
    msg.value = c.message;
    msg.addEventListener('input', () => { c.message = msg.value; scheduleSave(); });

    const secs = $('.c-secs input', node);
    secs.value = c.seconds;
    secs.addEventListener('input', () => { const v = parseFloat(secs.value); if (Number.isFinite(v)) { c.seconds = v; scheduleSave(); } });

    $('.c-count', node).textContent = state.counts?.[i] ?? c.count;
    const test = $('.c-test', node), reset = $('.c-reset', node), del = $('.c-del', node);
    test.title = t('test'); reset.title = t('reset'); del.textContent = t('del');
    test.addEventListener('click', () => { flushSave(); send('testCounter', { index: i }); toast(t('testing')); });
    reset.addEventListener('click', () => { flushSave(); send('resetCounter', { index: i }); });
    del.addEventListener('click', () => { S.counters.splice(i, 1); renderCounters(); saveNow(); });
    list.append(node);
  });
}

function addCounter(parameter = '') {
  if (parameter && S.counters.some(c => c.parameter === parameter)) { toast(fmt(t('alreadyAdded'), parameter)); return; }
  S.counters.push({ enabled: true, parameter, message: t('defaultCounterMsg'), seconds: 4, count: 0 });
  renderCounters();
  saveNow();
  if (parameter) toast(fmt(t('added'), parameter));
  else $$('.c-param').at(-1)?.focus();
}

function renderParams(list) {
  const box = $('#params');
  box.replaceChildren();
  if (!list.length) { box.append(el('p', { class: 'empty' }, t('noParams'))); return; }
  for (const p of list) {
    box.append(el('button', {
      type: 'button', class: 'chip' + (p.ago < 5 ? ' fresh' : ''), title: p.name,
      onclick: () => addCounter(p.name),
    }, p.name, el('small', {}, '= ' + p.value)));
  }
  $('#param-list').replaceChildren(...list.map(p => el('option', { value: p.name })));
}

/* ── chips ── */
function insertText(textarea, text) {
  if (!textarea) return;
  textarea.focus();
  textarea.setRangeText(text, textarea.selectionStart, textarea.selectionEnd, 'end');
  textarea.dispatchEvent(new Event('input'));
}

function renderChips() {
  const make = (box, vars, target) => {
    box.replaceChildren(...vars.map(v => {
      const token = v === 'count' ? `{count:${S.counters.find(c => c.parameter)?.parameter ?? 'Param'}}` : `{${v}}`;
      return el('button', { type: 'button', class: 'chip', onmousedown: e => e.preventDefault(), onclick: () => insertText(target(), token) },
        `{${v}}`, el('small', {}, t('v_' + v)));
    }));
  };
  const pageTarget = () => (lastTextarea?.classList.contains('p-text') && lastTextarea.isConnected) ? lastTextarea : $('.p-text');
  make($('#chips-status'), STATUS_VARS, pageTarget);
  make($('#chips-music'), MUSIC_VARS, () => $('[data-bind="media.template"]'));
  make($('#chips-afk'), AFK_VARS, () => $('[data-bind="afk.template"]'));
  $('#chips-emoji').replaceChildren(...EMOJI.map(e =>
    el('button', { type: 'button', class: 'chip', onmousedown: ev => ev.preventDefault(), onclick: () => insertText(pageTarget(), e) }, e)));
}

/* ── bar styles (mirrors TextFormat.ProgressBar in C#) ── */
function progressBar(style, width, fraction) {
  const filled = Math.round(fraction * width);
  const knob = Math.min(width - 1, Math.floor(fraction * width));
  const range = [...Array(width).keys()];
  switch (style) {
    case 'dots': return '●'.repeat(filled) + '○'.repeat(width - filled);
    case 'blocks': return '▓'.repeat(filled) + '░'.repeat(width - filled);
    case 'hearts': return '♥'.repeat(filled) + '♡'.repeat(width - filled);
    case 'classic': return '[' + range.map(i => i === knob ? 'o' : '-').join('') + ']';
    default: return range.map(i => i < knob ? '━' : i === knob ? '◉' : '─').join('');
  }
}

function renderBarStyles() {
  $('#bar-styles').replaceChildren(...BAR_STYLES.map(style =>
    el('button', {
      type: 'button', class: style === S.media.barStyle ? 'active' : '',
      onclick: () => { S.media.barStyle = style; renderBarStyles(); scheduleSave(); },
    }, el('small', {}, t('bar_' + style)), el('code', {}, progressBar(style, S.media.barWidth, 0.4)))));
}

function renderThemes() {
  $('#themes').replaceChildren(...Object.entries(THEMES).map(([name, [a, b]]) => {
    const btn = el('button', {
      type: 'button', title: t('th_' + name), class: name === S.theme ? 'active' : '',
      onclick: () => { S.theme = name; applyTheme(); renderThemes(); scheduleSave(); },
    });
    btn.style.background = `linear-gradient(135deg, ${a}, ${b})`;
    return btn;
  }));
}

/* ── live state ── */
let prevCounts = [];
function applyState(s) {
  state = s ?? {};
  if (!S) return;
  const running = !!state.running;

  const run = $('#btn-run');
  run.classList.toggle('running', running);
  $('.run-icon', run).textContent = running ? '■' : '▶';
  $('.run-label', run).textContent = t(running ? 'stop' : 'start');
  const live = $('#live-badge');
  live.textContent = t(running ? 'sending' : 'notSending');
  live.classList.toggle('live', running);

  // preview + meters
  const preview = state.preview ?? '';
  const box = $('#preview');
  if (box.textContent !== preview) box.textContent = preview;
  const lines = preview ? preview.split('\n').length : 0;
  setMeter('chars', preview.length, 144);
  setMeter('lines', lines, 9);

  // status pills
  const vrc = $('#pill-vrc');
  vrc.className = 'pill' + (state.vrc ? ' on' : '');
  vrc.title = t(state.vrc ? 'vrcOn' : 'vrcOff');
  const osc = state.osc ?? {};
  const oscPill = $('#pill-osc');
  oscPill.className = 'pill' + (osc.mode === 'off' ? ' bad' : osc.receiving ? ' on' : ' warn');
  oscPill.title = osc.error || t(osc.mode === 'off' ? 'oscOff' : osc.receiving ? 'oscReceiving' : 'oscWaiting');
  const status = osc.mode === 'oscquery' ? fmt(t('oscQueryOn'), osc.port) : osc.mode === 'port' ? fmt(t('oscPortOn'), osc.port) : (osc.error || t('oscOff'));
  $('#osc-status').textContent = status + (osc.mode !== 'off' ? ' — ' + t(osc.receiving ? 'oscReceiving' : 'oscWaiting') : '');
  $('#listen-port').disabled = !!S.osc.useOscQuery;
  if (osc.receiving) $('#tip-osc').classList.add('hidden');

  // now playing
  const m = state.media;
  const np = $('#np');
  np.classList.toggle('playing', !!m?.playing);
  $('#np-source').textContent = m ? (m.source + (m.playing ? '' : ' · ' + t('paused'))) : t('nothingPlaying');
  np.classList.toggle('blocked', m ? !m.allowed : false);
  $('#np-title').textContent = m ? (m.allowed ? m.title : t('notMusic')) : '—';
  $('#np-artist').textContent = m?.artist ?? '';
  $('#np-progress').style.width = ((m?.progress ?? 0) * 100).toFixed(1) + '%';
  $('#np-time').textContent = m?.dur ? `${m.pos} / ${m.dur}` : '';

  // AFK + counters
  const afk = $('#afk-state');
  afk.textContent = state.afk ? fmt(t('afkFor'), state.afkFor) : t('notAfk');
  afk.classList.toggle('on', !!state.afk);
  $('#last-trigger').textContent = state.lastTriggered ? fmt(t('lastTrigger'), state.lastTriggered) : t('noTrigger');

  const counts = state.counts ?? [];
  $$('#counters .counter').forEach((row, i) => {
    if (counts[i] === undefined) return;
    $('.c-count', row).textContent = counts[i];
    if (prevCounts[i] !== undefined && counts[i] > prevCounts[i]) {
      row.classList.remove('hit'); void row.offsetWidth; row.classList.add('hit');
      box.classList.remove('flash'); void box.offsetWidth; box.classList.add('flash');
    }
  });
  prevCounts = counts;
}

function setMeter(name, value, max) {
  $(`#m-${name}`).textContent = `${value}/${max}`;
  $(`#m-${name}-bar`).style.width = Math.min(100, value / max * 100) + '%';
  $(`#m-${name}`).parentElement.classList.toggle('full', value >= max);
}

function renderArt(src) {
  const art = $('#np-art');
  art.replaceChildren(src ? el('img', { src, alt: '' }) : el('span', {}, '🎵'));
}

function addLog(entry) {
  const log = $('#log');
  log.prepend(el('li', { class: entry.level }, el('time', {}, entry.time), el('span', {}, entry.text)));
  while (log.children.length > 300) log.lastChild.remove();
}

/* ════════════════════════ events ════════════════════════ */
/* ── close behaviour ── */
const SOURCE_NAMES = ['Spotify', 'Apple Music', 'YouTube', 'YouTube Music', 'SoundCloud', 'Deezer', 'TIDAL', 'JOOX',
  'Amazon Music', 'Bandcamp', 'Audiomack', 'Mixcloud', 'Pandora', 'Qobuz', 'Media Player', 'MusicBee', 'foobar2000', 'AIMP', 'Cider'];

function renderSources() {
  $('#sources').replaceChildren(...SOURCE_NAMES.map(n => el('span', { class: 'chip static' }, n)));
}

function renderCloseSeg() {
  $$('#close-seg button').forEach(b => b.classList.toggle('active', b.dataset.close === S.behavior.closeAction));
}

function openCloseModal() {
  $('#close-remember').checked = false;
  $('#close-modal').hidden = false;
  $('#close-tray').focus();
}

function chooseClose(action) {
  $('#close-modal').hidden = true;
  if (!action) return; // cancelled: keep the window open
  if ($('#close-remember').checked) {
    S.behavior.closeAction = action;
    renderCloseSeg();
    saveNow(); // goes out before closeChoice, so the choice is stored even when exiting
  } else flushSave();
  send('closeChoice', { action });
}

function selectTab(name) {
  $$('.tabs button').forEach(b => b.classList.toggle('active', b.dataset.tab === name));
  $$('.panel').forEach(p => p.classList.toggle('active', p.id === 'tab-' + name));
  clearInterval(paramsTimer);
  if (name === 'counters') {
    send('getParams');
    paramsTimer = setInterval(() => { if (!document.hidden) send('getParams'); }, 3000);
  }
  storageSet('tab', name);
}

function wireEvents() {
  $$('.tabs button').forEach(b => b.addEventListener('click', () => selectTab(b.dataset.tab)));
  $('#btn-run').addEventListener('click', () => { flushSave(); send(state.running ? 'stop' : 'start'); });
  $('#add-page').addEventListener('click', () => {
    S.pages.push({ enabled: true, text: '' });
    renderPages(); scheduleSave();
    $$('.p-text').at(-1).focus();
  });
  $('#add-counter').addEventListener('click', () => addCounter());
  $('#reset-all').addEventListener('click', e => {
    const btn = e.currentTarget;
    if (btn.dataset.armed) {
      delete btn.dataset.armed;
      btn.textContent = t('resetAll');
      send('resetCounters');
      return;
    }
    btn.dataset.armed = '1';
    btn.textContent = t('confirmReset');
    setTimeout(() => { if (btn.dataset.armed) { delete btn.dataset.armed; btn.textContent = t('resetAll'); } }, 3000);
  });
  $('#refresh-params').addEventListener('click', () => send('getParams'));
  $('#setup-install').addEventListener('click', () => {
    $('#setup-install').disabled = true;
    $('#setup-cancel').disabled = true;
    send('install');
  });
  $('#setup-cancel').addEventListener('click', () => send('cancelSetup'));
  $$('#close-seg button').forEach(b => b.addEventListener('click', () => {
    S.behavior.closeAction = b.dataset.close;
    renderCloseSeg(); scheduleSave();
  }));
  $('#close-tray').addEventListener('click', () => chooseClose('tray'));
  $('#close-exit').addEventListener('click', () => chooseClose('exit'));
  $('#close-cancel').addEventListener('click', () => chooseClose(null));
  $('#close-modal').addEventListener('click', e => { if (e.target === e.currentTarget) chooseClose(null); });
  document.addEventListener('keydown', e => {
    if (e.key !== 'Escape') return;
    if (!$('#close-modal').hidden) chooseClose(null);
    else if (!$('#update-modal').hidden) closeUpdateModal();
  });
  $$('#lang-seg button').forEach(b => b.addEventListener('click', () => {
    S.language = b.dataset.lang;
    lang = S.language;
    applyLang(); renderPages(); renderCounters(); renderChips(); renderBarStyles(); renderThemes();
    scheduleSave();
  }));
  $('#open-data').addEventListener('click', () => send('openDataFolder'));
  $('#open-docs').addEventListener('click', () => send('openUrl', { url: 'https://docs.vrchat.com/docs/osc-overview' }));
  $('#exit-app').addEventListener('click', () => { flushSave(); send('exit'); });
  $('#update-btn').addEventListener('click', openUpdateModal);
  $('#ver-badge').addEventListener('click', openUpdateModal);
  $('#upd-action').addEventListener('click', () => {
    if (upd?.latest) { openUpdateModal(); return; }
    updManual = true;
    send('checkUpdate');
  });
  $('#update-confirm').addEventListener('click', () => { flushSave(); send('installUpdate'); });
  $('#update-cancel').addEventListener('click', closeUpdateModal);
  $('#update-page').addEventListener('click', () => send('openUrl', { url: upd.url }));
  $('#update-modal').addEventListener('click', e => { if (e.target === e.currentTarget) closeUpdateModal(); });
  $('#tip-osc-close').addEventListener('click', () => { $('#tip-osc').classList.add('hidden'); storageSet('tipOscHidden', '1'); });
  if (storageGet('tipOscHidden')) $('#tip-osc').classList.add('hidden');
  window.addEventListener('beforeunload', flushSave);
}

/* ════════════════════════ browser-only mock (for designing the UI without the app) ════════════════════════ */
const Mock = {
  handle(msg) {
    if (msg.type === 'ready' && location.hash === '#setup') {
      setTimeout(() => {
        onMessage({ type: 'setup', lang: 'th', version: 'dev', installDir: 'C:\\Users\\you\\AppData\\Local\\Programs\\MeijiChatboxStatus', update: false });
        onMessage({ type: 'setupScan', items: [
          { kind: 'process', path: 'C:\\Users\\you\\Desktop\\VRC\\VrcAfkNative.exe' },
          { kind: 'folder', path: 'C:\\Users\\you\\Desktop\\VRC' },
          { kind: 'file', path: 'C:\\Users\\you\\Downloads\\VrcAfkNative.zip' },
          { kind: 'shortcut', path: 'C:\\Users\\you\\Desktop\\VrcAfkNative.lnk' },
        ] });
      });
      return;
    }
    if (msg.type === 'ready') {
      const settings = {
        version: 1, language: 'th', theme: 'sakura',
        osc: { ip: '127.0.0.1', sendPort: 9000, listenPort: 9001, useOscQuery: true },
        chatbox: { intervalSeconds: 2, rotateSeconds: 10 },
        pages: [{ enabled: true, text: '🌸 {time} | ชิลล์ ๆ อยู่น้า~ 🌸\n{afk}\n{music}' }],
        media: { enabled: true, template: '🎵 {title} - {artist}\n{bar} {pos}/{dur}', showWhenPaused: false, barStyle: 'line', barWidth: 10 },
        afk: { enabled: true, template: '💤 AFK อยู่น้า ({afktime})' },
        counters: [{ enabled: true, parameter: 'HeadPat', message: '💗 โดนลูบหัวไปแล้ว {count} ครั้ง!', seconds: 4, count: 12 }],
        behavior: { autoWithVrc: true, closeWithVrc: true, closeAction: 'ask', checkUpdates: true },
      };
      setTimeout(() => {
        onMessage({ type: 'init', settings, version: '2.0.6', log: [{ time: '12:00:00', level: 'info', text: 'Mock mode' }],
          update: location.hash === '#update' ? this.release('available') : { state: 'latest', current: '2.0.6', checkedAt: '12:00', progress: 0 } });
        this.tick();
        setInterval(() => this.tick(), 1000);
      });
    } else if (msg.type === 'start' || msg.type === 'stop') {
      this.running = msg.type === 'start';
      this.tick();
    } else if (msg.type === 'checkUpdate') {
      onMessage({ type: 'update', update: { state: 'checking', current: '2.0.6', progress: 0 } });
      setTimeout(() => onMessage({ type: 'update', update: this.release('available') }), 800);
    } else if (msg.type === 'installUpdate') {
      let p = 0;
      this.download = setInterval(() => {
        p += 0.08;
        if (p < 1) { onMessage({ type: 'update', update: { ...this.release('downloading'), progress: p } }); return; }
        clearInterval(this.download);
        onMessage({ type: 'update', update: this.release('installing') });
      }, 250);
    } else if (msg.type === 'cancelUpdate') {
      clearInterval(this.download);
      onMessage({ type: 'update', update: this.release('available') });
    } else if (msg.type === 'getParams') {
      onMessage({ type: 'params', list: [{ name: 'HeadPat', value: 'false', ago: 2 }, { name: 'AFK', value: 'false', ago: 30 }, { name: 'VelocityX', value: '0.12', ago: 0 }] });
    }
  },
  running: false,
  pos: 42,
  download: 0,
  release(state) {
    return { state, current: '2.0.6', latest: '2.0.7', sizeMb: 70, progress: 0, checkedAt: '12:00',
      url: 'https://github.com/finalsiren1/MeijiChatboxStatus/releases', notes: '## What\'s new\n- **Auto update**: get new versions from inside the app\n- Small fixes' };
  },
  tick() {
    this.pos = (this.pos + 1) % 200;
    const now = new Date();
    const time = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
    const pos = `${Math.floor(this.pos / 60)}:${String(this.pos % 60).padStart(2, '0')}`;
    onMessage({
      type: 'state', running: this.running, vrc: true,
      preview: `🌸 ${time} | ชิลล์ ๆ อยู่น้า~ 🌸\n🎵 Cute Song - Someone\n${progressBar('line', 10, this.pos / 200)} ${pos}/3:20`,
      osc: { mode: 'oscquery', port: 54321, error: null, receiving: true },
      media: { allowed: true, title: 'Cute Song', artist: 'Someone', source: 'Spotify', playing: true, pos, dur: '3:20', progress: this.pos / 200 },
      afk: false, afkFor: '', counts: [12], lastTriggered: null,
    });
  },
};

/* ════════════════════════ boot ════════════════════════ */
webview?.addEventListener('message', e => onMessage(e.data));
wireEvents();
selectTab(location.hash.slice(1) || storageGet('tab') || 'status');
send('ready');
