import { toCyrillic, toLatin } from './core.js';
import { createEngine } from './engine.js';

const api = typeof browser !== 'undefined' ? browser : chrome;
const url = (p) => api.runtime.getURL(p);

const $ = (id) => document.getElementById(id);
const editor = $('editor');
const highlights = $('highlights');
const backdrop = $('backdrop');
const results = $('results');
const statusEl = $('status');
const statsEl = $('stats');
const scriptSel = $('script');

let ready = false;
let lastErrors = [];
const ignored = new Set();

// Dvigatel — lugʻat fayllarini kengaytma ichidan oʻqiydi
const engine = createEngine({
  fetchBuf: async (path) => {
    const res = await fetch(url(path));
    return new Uint8Array(await res.arrayBuffer());
  },
});

// ---------------- Ishga tushirish ----------------

async function init() {
  statusEl.textContent = 'Lugʻat yuklanmoqda…';
  try {
    await engine.init();
    ready = true;
    statusEl.textContent = 'Tayyor';
    check();
  } catch (e) {
    statusEl.textContent = 'Xatolik: ' + e.message;
    console.error(e);
  }
}

// ---------------- Tekshiruv ----------------

function escapeHtml(s) {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

async function check() {
  if (!ready) return;
  const text = editor.value;
  const errors = await engine.check(text, { script: scriptSel.value, ignored });
  lastErrors = errors;
  render(text, errors);
}

function render(text, errors) {
  // Backdrop highlight
  let html = '', pos = 0;
  for (const e of errors) {
    if (e.start < pos) continue;
    html += escapeHtml(text.slice(pos, e.start));
    const cls = e.type === 'spell' ? 'spell' : 'gram';
    html += `<mark class="${cls}">` + escapeHtml(text.substr(e.start, e.length)) + '</mark>';
    pos = e.start + e.length;
  }
  html += escapeHtml(text.slice(pos));
  highlights.innerHTML = html;

  // Results list
  results.innerHTML = '';
  const spellCount = errors.filter((e) => e.type === 'spell').length;
  statsEl.textContent = errors.length === 0
    ? 'Xato topilmadi'
    : `Imlo: ${spellCount} · Grammatika: ${errors.length - spellCount}`;

  errors.forEach((e) => {
    const div = document.createElement('div');
    div.className = 'err ' + e.type;
    const kind = e.type === 'spell' ? 'imlo' : 'grammatika';
    div.innerHTML = `<span class="kind">${kind}</span><span class="word">${escapeHtml(e.word.replace(/\n/g, '⏎'))}</span>` +
      (e.message ? `<div class="msg">${escapeHtml(e.message)}</div>` : '');
    const sugs = document.createElement('div');
    sugs.className = 'sugs';
    if (e.suggestions && e.suggestions.length) {
      for (const s of e.suggestions) {
        const b = document.createElement('button');
        b.textContent = s;
        b.onclick = () => applySuggestion(e, s);
        sugs.appendChild(b);
      }
    } else {
      const none = document.createElement('span');
      none.className = 'none';
      none.textContent = 'taklif topilmadi';
      sugs.appendChild(none);
    }
    const ign = document.createElement('button');
    ign.className = 'ignore';
    ign.textContent = 'Eʼtiborsiz';
    ign.onclick = () => {
      ignored.add(e.type === 'spell' ? e.normalized : 'G:' + e.ruleId + ':' + e.word);
      check();
    };
    sugs.appendChild(ign);
    div.appendChild(sugs);
    results.appendChild(div);
  });
}

function applySuggestion(e, suggestion) {
  const text = editor.value;
  if (text.substr(e.start, e.length) !== e.word) { check(); return; }
  editor.value = text.slice(0, e.start) + suggestion + text.slice(e.start + e.length);
  editor.focus();
  check();
}

// ---------------- Hodisalar ----------------

let debounce;
editor.addEventListener('input', () => {
  highlights.textContent = editor.value; // vaqtincha belgisiz
  clearTimeout(debounce);
  debounce = setTimeout(check, 400);
});
editor.addEventListener('scroll', () => {
  backdrop.scrollTop = editor.scrollTop;
  backdrop.scrollLeft = editor.scrollLeft;
});
scriptSel.addEventListener('change', check);

$('btnCheck').onclick = check;
$('btnCyr').onclick = () => { transform(toCyrillic); };
$('btnLat').onclick = () => { transform(toLatin); };
$('btnCopy').onclick = async () => {
  await navigator.clipboard.writeText(editor.value);
  statusEl.textContent = 'Nusxalandi';
};

function transform(fn) {
  const s = editor.selectionStart, en = editor.selectionEnd;
  if (en > s) {
    const conv = fn(editor.value.slice(s, en));
    editor.value = editor.value.slice(0, s) + conv + editor.value.slice(en);
    editor.setSelectionRange(s, s + conv.length);
  } else {
    editor.value = fn(editor.value);
  }
  check();
}

$('about').onclick = (ev) => {
  ev.preventDefault();
  let box = document.getElementById('about-box');
  if (box) { box.remove(); return; }
  box = document.createElement('div');
  box.id = 'about-box';
  box.innerHTML =
    'UzSpell — oʻzbek tili uchun oflayn imlo, grammatika va transliteratsiya.<br>' +
    '100% oflayn — internet talab qilinmaydi.<br><br>' +
    '© 2026 Erkin Pardayev. Lugʻatlar: <b>uz-hunspell</b> (Alisher ʻU2B3Kʻ Jalolov, ' +
    'Bilolbek Normoʻminov) — GPL. Imlo dvigateli: hunspell-asm (WASM).<br>' +
    'github.com/Erkinbek/UzSpell';
  document.querySelector('footer').after(box);
};

// Saqlangan matnni tiklash
api.storage?.local.get('text').then((r) => { if (r && r.text) { editor.value = r.text; } init(); })
  .catch(() => init());
editor.addEventListener('input', () => { api.storage?.local.set({ text: editor.value }); });

// Tanlangan matn bilan ochilsa (kontekst menyudan)
api.storage?.local.get('pendingText').then((r) => {
  if (r && r.pendingText) { editor.value = r.pendingText; api.storage.local.remove('pendingText'); }
}).catch(() => {});
