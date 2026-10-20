// Finds unused i18n keys (and keys missing in one language) and CSS classes never referenced in HTML/JS.
import { readFileSync } from 'node:fs';
const dir = process.argv[2];
const js = readFileSync(`${dir}/app.js`, 'utf8');
const html = readFileSync(`${dir}/index.html`, 'utf8');
const css = readFileSync(`${dir}/app.css`, 'utf8');

const block = (lang) => {
  const start = js.indexOf(`  ${lang}: {`);
  const end = js.indexOf('\n  },', start);
  return js.slice(start, end);
};
const keysOf = (src) => new Set([...src.matchAll(/(?:^|[\s,{])([a-zA-Z_][\w]*):\s*'/gm)].map(m => m[1]));
const th = keysOf(block('th')), en = keysOf(block('en'));
const dictEnd = js.indexOf('const STATUS_VARS');
const code = js.slice(dictEnd) + html;
const dynamicPrefixes = ['v_', 'bar_', 'th_', 'kind_', 'step_'];
const unused = [...th].filter(k => !dynamicPrefixes.some(p => k.startsWith(p)) &&
  !new RegExp(`(['"\`])${k}\\1|data-i18n(?:-html|-ph)?="${k}"`).test(code));
console.log('unused i18n:', unused.join(', ') || '(none)');
console.log('missing in en:', [...th].filter(k => !en.has(k)).join(', ') || '(none)');
console.log('missing in th:', [...en].filter(k => !th.has(k)).join(', ') || '(none)');

const classes = new Set([...css.replace(/\/\*[\s\S]*?\*\//g, '').matchAll(/\.([a-zA-Z][\w-]*)/g)].map(m => m[1]));
const deadCss = [...classes].filter(c => !/^(woff2|png|js|css)$/.test(c) && !new RegExp(`[\\s"'.]${c}[\\s"'.)]|['"\`]${c}['"\`]|class(Name)?=["'][^"']*\\b${c}\\b`).test(js + html));
console.log('unused css classes:', deadCss.join(', ') || '(none)');
const ids = [...html.matchAll(/id="([\w-]+)"/g)].map(m => m[1]);
console.log('ids never used in js/css:', ids.filter(id => !js.includes(`#${id}`) && !css.includes(`#${id}`) && !html.includes(`aria-labelledby="${id}"`) && !html.includes(`list="${id}"`)).join(', ') || '(none)');
