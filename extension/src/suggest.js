// Hunspell takliflarini oʻzbekcha tipik xatolar boʻyicha qayta tartiblaydi.
// C# UzbekSuggester'dan koʻchirilgan: x↔h, oʻ↔o, gʻ↔g (lotin); х↔ҳ, қ↔к, ў↔у, ғ↔г (kirill).
import { OKINA, TUTUQ, SCRIPT_CYRILLIC, SCRIPT_LATIN } from './core.js';

const LATIN_SWAPS = [['x', 'h']];
const CYR_SWAPS = [['х', 'ҳ'], ['қ', 'к'], ['ў', 'у'], ['ғ', 'г'], ['в', 'ф']];

// word — yozilgan (normalizatsiyalangan) soʻz; script — 'latin'|'cyrillic'
// hunspellSugs — Hunspell nomzodlari; isValid(w) — soʻz lugʻatda bormi
export function refineSuggestions(word, script, hunspellSugs, isValid, max) {
  const upperFirst = word.length > 0 && word[0] !== word[0].toLowerCase();
  const lower = word.toLowerCase();

  // 1) Yuqori ishonchli: bitta chalkashlik bilan lugʻatdagi soʻz
  const high = [];
  for (const cand of confusionVariants(lower, script)) {
    if (cand !== lower && isValid(cand)) high.push(cand);
  }

  // 2) Birlashtirish (dublikatlarsiz)
  const seen = new Set();
  const pool = [];
  const add = (s) => { if (s && !seen.has(s.toLowerCase())) { seen.add(s.toLowerCase()); pool.push(s); } };
  for (const s of high) add(s);
  for (const s of hunspellSugs) add(s);

  // 3) Oʻzbekcha yumshoq masofa boʻyicha saralash
  const scored = pool
    .map((s) => ({ w: s, c: weightedDistance(lower, s.toLowerCase(), script) }))
    .sort((a, b) => a.c - b.c || a.w.length - b.w.length)
    .slice(0, max)
    .map((t) => t.w);

  return upperFirst ? scored.map(capitalize) : scored;
}

function* confusionVariants(lower, script) {
  const swaps = script === SCRIPT_CYRILLIC ? CYR_SWAPS : LATIN_SWAPS;
  for (let i = 0; i < lower.length; i++) {
    for (const [a, b] of swaps) {
      if (lower[i] === a) yield lower.slice(0, i) + b + lower.slice(i + 1);
      else if (lower[i] === b) yield lower.slice(0, i) + a + lower.slice(i + 1);
    }
  }
  if (script === SCRIPT_LATIN) {
    for (let i = 0; i < lower.length; i++) {
      if (lower[i] === 'o' || lower[i] === 'g') {
        const already = i + 1 < lower.length && lower[i + 1] === OKINA;
        if (!already) yield lower.slice(0, i + 1) + OKINA + lower.slice(i + 1);
      }
      if (lower[i] === OKINA) yield lower.slice(0, i) + lower.slice(i + 1);
    }
  }
}

function indelCost(c) { return c === OKINA || c === TUTUQ ? 0.3 : 1.0; }

function subCost(a, b, script) {
  if (a === b) return 0;
  const swaps = script === SCRIPT_CYRILLIC ? CYR_SWAPS : LATIN_SWAPS;
  for (const [x, y] of swaps) if ((a === x && b === y) || (a === y && b === x)) return 0.4;
  return 1.0;
}

function weightedDistance(a, b, script) {
  const n = a.length, m = b.length;
  const d = Array.from({ length: n + 1 }, () => new Array(m + 1).fill(0));
  for (let i = 1; i <= n; i++) d[i][0] = d[i - 1][0] + indelCost(a[i - 1]);
  for (let j = 1; j <= m; j++) d[0][j] = d[0][j - 1] + indelCost(b[j - 1]);
  for (let i = 1; i <= n; i++) {
    for (let j = 1; j <= m; j++) {
      let best = Math.min(
        d[i - 1][j - 1] + subCost(a[i - 1], b[j - 1], script),
        d[i - 1][j] + indelCost(a[i - 1]),
        d[i][j - 1] + indelCost(b[j - 1]),
      );
      if (i > 1 && j > 1 && a[i - 1] === b[j - 2] && a[i - 2] === b[j - 1]) {
        best = Math.min(best, d[i - 2][j - 2] + 0.7);
      }
      d[i][j] = best;
    }
  }
  return d[n][m];
}

function capitalize(s) {
  return s.length === 0 ? s : s[0].toUpperCase() + s.slice(1);
}
