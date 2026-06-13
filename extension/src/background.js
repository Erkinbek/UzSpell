// UzSpell fon skripti (Chrome: service worker, Firefox: background page).
// 1) Kontekst menyu: belgilangan matnni lotin <-> kirill oʻgirish.
// 2) Dvigatel shu yerda yashaydi: kontent skript yuborgan matnni tekshirib,
//    xatolar roʻyxatini qaytaradi (har bir sahifada WASM qayta yuklanmaydi).
import { toCyrillic, toLatin } from './core.js';
import { createEngine } from './engine.js';

const api = typeof browser !== 'undefined' ? browser : chrome;
const url = (p) => api.runtime.getURL(p);

// ---------------- Dvigatel (kechiktirilgan) ----------------
const engine = createEngine({
  fetchBuf: async (path) => {
    const res = await fetch(url(path));
    return new Uint8Array(await res.arrayBuffer());
  },
});

// ---------------- Kontekst menyu ----------------
function buildMenus() {
  api.contextMenus.removeAll(() => {
    api.contextMenus.create({ id: 'uzspell', title: 'UzSpell', contexts: ['selection', 'editable'] });
    api.contextMenus.create({ id: 'uz-cyr', parentId: 'uzspell', title: 'Lotin → Kirill', contexts: ['selection', 'editable'] });
    api.contextMenus.create({ id: 'uz-lat', parentId: 'uzspell', title: 'Kirill → Lotin', contexts: ['selection', 'editable'] });
    api.contextMenus.create({ id: 'sep', parentId: 'uzspell', type: 'separator', contexts: ['selection', 'editable'] });
    api.contextMenus.create({
      id: 'autocheck', parentId: 'uzspell', type: 'checkbox', checked: true,
      title: 'Inputlarda avtomatik tekshiruv', contexts: ['all'],
    });
  });
}

api.runtime.onInstalled.addListener(async () => {
  buildMenus();
  // Boshlangʻich holat: avtomatik tekshiruv YOQILGAN
  const r = await api.storage.local.get('autocheck');
  if (typeof r.autocheck === 'undefined') await api.storage.local.set({ autocheck: true });
  syncMenuChecked();
});
api.runtime.onStartup?.addListener?.(buildMenus);

async function syncMenuChecked() {
  try {
    const r = await api.storage.local.get('autocheck');
    api.contextMenus.update('autocheck', { checked: r.autocheck !== false });
  } catch { /* menyu hali yoʻq boʻlishi mumkin */ }
}

api.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === 'autocheck') {
    await api.storage.local.set({ autocheck: !!info.checked });
    return;
  }
  if (!info.selectionText || !tab || tab.id == null) return;
  const converted = info.menuItemId === 'uz-cyr'
    ? toCyrillic(info.selectionText)
    : info.menuItemId === 'uz-lat' ? toLatin(info.selectionText) : null;
  if (converted === null) return;

  try {
    await api.scripting.executeScript({
      target: { tabId: tab.id },
      func: replaceSelectionInPage,
      args: [converted],
    });
  } catch (e) {
    console.error('UzSpell:', e);
  }
});

// ---------------- Kontent skript bilan aloqa ----------------
api.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (!msg || msg.type !== 'uzspell-check') return false;
  engine.check(msg.text || '', { script: msg.script || 'auto' })
    .then((errors) => sendResponse({ ok: true, errors }))
    .catch((e) => sendResponse({ ok: false, error: String(e && e.message || e) }));
  return true; // asinxron javob
});

// Sahifa kontekstida ishlaydigan funksiya (transliteratsiya almashtirish)
function replaceSelectionInPage(replacement) {
  const el = document.activeElement;
  if (el && (el.tagName === 'TEXTAREA' || el.tagName === 'INPUT')) {
    const s = el.selectionStart, e = el.selectionEnd;
    if (s != null && e != null && e > s) {
      el.value = el.value.slice(0, s) + replacement + el.value.slice(e);
      el.selectionStart = s;
      el.selectionEnd = s + replacement.length;
      el.dispatchEvent(new Event('input', { bubbles: true }));
      return;
    }
  }
  const sel = window.getSelection();
  if (sel && sel.rangeCount && !sel.isCollapsed) {
    const range = sel.getRangeAt(0);
    const editable = range.commonAncestorContainer.parentElement?.closest('[contenteditable=""],[contenteditable="true"]');
    if (editable) {
      range.deleteContents();
      range.insertNode(document.createTextNode(replacement));
      sel.collapseToEnd();
      return;
    }
  }
  navigator.clipboard.writeText(replacement).catch(() => {});
}
