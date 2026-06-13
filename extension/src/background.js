// Kontekst menyu: belgilangan matnni lotin <-> kirill oʻgirish.
// Oʻgirilgan matn tahrirlanadigan maydonga qoʻyiladi; imkonsiz boʻlsa nusxa olinadi.
import { toCyrillic, toLatin } from './core.js';

const api = typeof browser !== 'undefined' ? browser : chrome;

api.runtime.onInstalled.addListener(() => {
  api.contextMenus.removeAll(() => {
    api.contextMenus.create({ id: 'uzspell', title: 'UzSpell', contexts: ['selection', 'editable'] });
    api.contextMenus.create({ id: 'uz-cyr', parentId: 'uzspell', title: 'Lotin → Kirill', contexts: ['selection', 'editable'] });
    api.contextMenus.create({ id: 'uz-lat', parentId: 'uzspell', title: 'Kirill → Lotin', contexts: ['selection', 'editable'] });
  });
});

api.contextMenus.onClicked.addListener(async (info, tab) => {
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

// Sahifa kontekstida ishlaydigan funksiya
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
  // Tahrirlab boʻlmasa — nusxaga olamiz
  navigator.clipboard.writeText(replacement).catch(() => {});
}
