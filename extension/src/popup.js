import { loadModule } from 'hunspell-asm';
import {
  toCyrillic, toLatin, tokenize, normalizeToken, detectScript,
  isNumeral, buildMorphology, SCRIPT_LATIN, SCRIPT_CYRILLIC,
} from './core.js';
import { checkGrammar } from './grammar.js';

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

let latin = null;     // {spell, isCorrect}
let cyrillic = null;  // {spell, isCorrect}
let morph = null;
let ready = false;
let lastErrors = [];
const ignored = new Set();

// ---------------- Lugʻat yuklash ----------------

async function fetchBuf(path) {
  const res = await fetch(url(path));
  return new Uint8Array(await res.arrayBuffer());
}

async function init() {
  statusEl.textContent = 'Lugʻat yuklanmoqda…';
  try {
    const factory = await loadModule();
    const make = async (affPath, dicPath) => {
      const aff = factory.mountBuffer(await fetchBuf(affPath), affPath.split('/').pop());
      const dic = factory.mountBuffer(await fetchBuf(dicPath), dicPath.split('/').pop());
      const spell = factory.create(aff, dic);
      return spell;
    };
    const latSpell = await make('dictionaries/uz_UZ.aff', 'dictionaries/uz_UZ.dic');
    latin = {
      spell: latSpell,
      isCorrect: (w) => {
        const norm = normalizeToken(w);
        if (isNumeral(norm.toLowerCase())) return true;
        if (latSpell.spell(norm)) return true;
        if (norm.includes('-')) {
          const parts = norm.split('-').filter(Boolean);
          if (parts.length > 1 && parts.every((p) => p.length > 1 && latSpell.spell(p))) return true;
        }
        return false;
      },
    };
    // Morfologiya — grammatika uchun
    const dicText = new TextDecoder().decode(await fetchBuf('dictionaries/uz_UZ.dic'));
    morph = buildMorphology(dicText);

    // Kirill lugʻ ati — kerak boʻlganda (kechiktirilgan)
    cyrillic = {
      spell: null,
      ensure: async () => {
        if (!cyrillic.spell) cyrillic.spell = await make('dictionaries/uz_UZ_Cyrl.aff', 'dictionaries/uz_UZ_Cyrl.dic');
        return cyrillic.spell;
      },
    };

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
  const forced = scriptSel.value;
  const errors = [];

  // Imlo
  for (const tok of tokenize(text)) {
    if (tok.length < 2) continue;
    if (/^[\p{Lu}]{2,}$/u.test(tok.text)) continue; // BOSH HARFLI qisqartmalar
    let script = forced === 'auto' ? detectScript(tok.text) : (forced === 'latin' ? SCRIPT_LATIN : SCRIPT_CYRILLIC);
    if (!script) continue;
    let engine = latin;
    if (script === SCRIPT_CYRILLIC) {
      await cyrillic.ensure();
      engine = { isCorrect: (w) => cyrillic.spell.spell(w) };
    }
    const norm = script === SCRIPT_LATIN ? normalizeToken(tok.text) : tok.text;
    if (ignored.has(norm)) continue;
    if (!engine.isCorrect(tok.text)) {
      const sugs = (script === SCRIPT_LATIN ? latin.spell : cyrillic.spell).suggest(norm).slice(0, 6);
      errors.push({ type: 'spell', word: tok.text, normalized: norm, start: tok.start, length: tok.length, script, suggestions: sugs });
    }
  }

  // Grammatika (lotin)
  const gIssues = checkGrammar(text, { morph, isCorrect: latin.isCorrect });
  for (const it of gIssues) {
    const frag = text.substr(it.start, it.length);
    if (ignored.has('G:' + it.ruleId + ':' + frag)) continue;
    errors.push({ type: 'gram', word: frag, start: it.start, length: it.length, message: it.message, suggestions: it.suggestions, ruleId: it.ruleId });
  }

  errors.sort((a, b) => a.start - b.start);
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

  errors.forEach((e, idx) => {
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
