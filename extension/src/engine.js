// UzSpell umumiy dvigateli — popup va background (service worker) uchun bir manba.
// hunspell-asm (WASM) imlo + core.js morfologiya + grammar.js qoidalari.
// DOMsiz muhitda (Chrome MV3 service worker) ham ishlashi tasdiqlangan.
import { loadModule } from 'hunspell-asm';
import {
  tokenize, normalizeToken, detectScript, isNumeral,
  buildMorphology, SCRIPT_LATIN, SCRIPT_CYRILLIC,
} from './core.js';
import { checkGrammar } from './grammar.js';
import { refineSuggestions } from './suggest.js';

// fetchBuf(path) => Promise<Uint8Array>  (kengaytma ichidagi faylni oʻqiydi)
export function createEngine({ fetchBuf }) {
  let latin = null;     // {spell, isCorrect}
  let cyrillic = null;  // {spell:null, ensure}
  let morph = null;
  let ready = false;
  let initPromise = null;

  async function init() {
    if (ready) return;
    if (initPromise) return initPromise;
    initPromise = (async () => {
      const factory = await loadModule();
      const make = async (affPath, dicPath) => {
        const aff = factory.mountBuffer(await fetchBuf(affPath), affPath.split('/').pop());
        const dic = factory.mountBuffer(await fetchBuf(dicPath), dicPath.split('/').pop());
        return factory.create(aff, dic);
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

      // Kirill lugʻati — kerak boʻlganda (kechiktirilgan)
      cyrillic = {
        spell: null,
        ensure: async () => {
          if (!cyrillic.spell) {
            cyrillic.spell = await make('dictionaries/uz_UZ_Cyrl.aff', 'dictionaries/uz_UZ_Cyrl.dic');
          }
          return cyrillic.spell;
        },
      };

      ready = true;
    })();
    return initPromise;
  }

  // text ni tekshiradi -> errors[] (har biri start/length/word/suggestions bilan).
  // opts: { script: 'auto'|'latin'|'cyrillic', suggestions: bool, grammar: bool, ignored: Set }
  async function check(text, opts = {}) {
    await init();
    const forced = opts.script || 'auto';
    const withSug = opts.suggestions !== false;
    const withGram = opts.grammar !== false;
    const checkAllCaps = opts.checkAllCaps === true;
    const ignored = opts.ignored || new Set();
    const errors = [];

    // ---- Imlo ----
    for (const tok of tokenize(text)) {
      if (tok.length < 2) continue;
      if (!checkAllCaps && /^[\p{Lu}]{2,}$/u.test(tok.text)) continue; // BOSH HARFLI qisqartmalar
      const script = forced === 'auto'
        ? detectScript(tok.text)
        : (forced === 'latin' ? SCRIPT_LATIN : SCRIPT_CYRILLIC);
      if (!script) continue;

      let engine = latin;
      if (script === SCRIPT_CYRILLIC) {
        await cyrillic.ensure();
        engine = { isCorrect: (w) => cyrillic.spell.spell(w) };
      }
      const norm = script === SCRIPT_LATIN ? normalizeToken(tok.text) : tok.text;
      if (ignored.has(norm)) continue;
      if (!engine.isCorrect(tok.text)) {
        const sugEngine = script === SCRIPT_LATIN ? latin.spell : cyrillic.spell;
        const max = opts.maxSuggestions || 6;
        // Hunspell'dan kengroq roʻyxat olib, oʻzbekcha chalkashliklar boʻyicha qayta tartiblaymiz
        const raw = withSug ? sugEngine.suggest(norm).slice(0, max * 3) : [];
        const sugs = withSug
          ? refineSuggestions(norm, script, raw, (w) => sugEngine.spell(w), max)
          : [];
        errors.push({
          type: 'spell', word: tok.text, normalized: norm,
          start: tok.start, length: tok.length, script, suggestions: sugs,
        });
      }
    }

    // ---- Grammatika (lotin) ----
    if (withGram) {
      const gIssues = checkGrammar(text, { morph, isCorrect: latin.isCorrect });
      for (const it of gIssues) {
        const frag = text.substr(it.start, it.length);
        if (ignored.has('G:' + it.ruleId + ':' + frag)) continue;
        errors.push({
          type: 'gram', word: frag, start: it.start, length: it.length,
          message: it.message, suggestions: it.suggestions, ruleId: it.ruleId,
        });
      }
    }

    errors.sort((a, b) => a.start - b.start);
    return errors;
  }

  return {
    init,
    check,
    isReady: () => ready,
    ensureCyrillic: () => cyrillic && cyrillic.ensure(),
  };
}
