// Grammatika qoidalari — desktop GrammarChecker'dan koʻchirilgan (lotin yozuvi uchun).
// Yuqori aniqlikka moʻljallangan; shubhali holatlarda jim qoladi.

import { tokenize, normalizeToken, isNumeral, isApostropheLike } from './core.js';

const OKINA = 'ʻ';
const REQUIRE_DAN = new Set(['keyin', 'soʻng', 'buyon', 'beri', 'tashqari']);
const REQUIRE_GA = new Set(['qadar', 'koʻra', 'binoan', 'muvofiq', 'qaramay', 'qaramasdan']);
const COMPLEX_MARKERS = new Set(['va', 'hamda', 'yoki', 'lekin', 'ammo', 'biroq', 'bilan', 'ki', 'chunki', 'agar', 'deb', 'degan', 'esa']);
const REPEAT_EXCL = new Set(['u', 'ha']);
const PRONOUNS = { men: 0, sen: 1, biz: 2, siz: 3, u: 4, ular: 5 };

const ENDING_FAMILIES = [
  ['dim', 'ding', 'dik', 'dingiz', 'di', 'dilar'],
  ['aman', 'asan', 'amiz', 'asiz', 'adi', 'adilar'],
  ['yman', 'ysan', 'ymiz', 'ysiz', 'ydi', 'ydilar'],
  ['yapman', 'yapsan', 'yapmiz', 'yapsiz', 'yapti', 'yaptilar'],
  ['moqdaman', 'moqdasan', 'moqdamiz', 'moqdasiz', 'moqda', 'moqdalar'],
  ['ganman', 'gansan', 'ganmiz', 'gansiz', 'gan', 'ganlar'],
  ['sam', 'sang', 'sak', 'sangiz', 'sa', 'salar'],
  ['man', 'san', 'miz', null, null, null],
];

const SORTED_ENDINGS = (() => {
  const list = [];
  for (let f = 0; f < ENDING_FAMILIES.length; f++)
    for (let p = 0; p < ENDING_FAMILIES[f].length; p++)
      if (ENDING_FAMILIES[f][p]) list.push({ suffix: ENDING_FAMILIES[f][p], family: f, person: p });
  list.sort((a, b) => b.suffix.length - a.suffix.length);
  return list;
})();

const ends = (s, suf) => s.endsWith(suf);

export function checkGrammar(text, opts) {
  const morph = opts.morph;
  const isCorrect = opts.isCorrect || (() => false);
  const issues = [];

  const tokens = tokenize(text);
  const norms = tokens.map((t) => normalizeToken(t.text).toLowerCase());

  // Gaplarni token indekslari boʻyicha ajratish
  const sentences = [];
  let from = 0;
  for (let i = 0; i < tokens.length; i++) {
    let boundary = i === tokens.length - 1;
    if (!boundary) {
      const gapStart = tokens[i].start + tokens[i].length;
      const gapEnd = tokens[i + 1].start;
      for (let g = gapStart; g < gapEnd; g++) {
        const c = text[g];
        if (c === '.' || c === '!' || c === '?' || c === '…' || c === ';' || c === '\n') { boundary = true; break; }
      }
    }
    if (boundary) { sentences.push([from, i + 1]); from = i + 1; }
  }

  let first = true;
  for (const [a, b] of sentences) {
    checkSentence(text, tokens, norms, a, b, first, issues, morph, isCorrect);
    first = false;
  }
  checkPunctuation(text, issues);
  issues.sort((x, y) => x.start - y.start);
  return issues;
}

function detectPersonEnding(word, morph) {
  if (morph.isNominalStem(word) || morph.isVerbStem(word)) return null;
  for (const e of SORTED_ENDINGS)
    if (word.length >= e.suffix.length + 2 && ends(word, e.suffix)) return e;
  return null;
}

function checkSentence(text, tokens, norms, from, to, isFirst, issues, morph, isCorrect) {
  if (to - from === 0) return;
  checkCapitalization(text, tokens, from, isFirst, issues);
  for (let i = from; i < to; i++) {
    if (i > from) {
      checkRepeated(tokens, norms, i, issues);
      checkPostposition(tokens, norms, i, issues, morph, isCorrect);
      checkStandaloneMi(text, tokens, norms, i, issues, isCorrect);
    }
    if (i + 1 < to) checkNumeralPlural(tokens, norms, i, to, issues, morph);
  }
  checkPersonAgreement(tokens, norms, from, to, issues, morph, isCorrect);
}

function checkRepeated(tokens, norms, i, issues) {
  const cur = norms[i];
  if (cur !== norms[i - 1] || cur.length < 2) return;
  if (REPEAT_EXCL.has(cur) || isNumeral(cur)) return;
  const prev = tokens[i - 1], tok = tokens[i];
  issues.push({
    ruleId: 'TAKROR', message: `«${tok.text}» soʻzi ketma-ket takrorlangan`,
    start: prev.start, length: tok.start + tok.length - prev.start, suggestions: [prev.text],
  });
}

function buildDative(stem) {
  const lo = stem.toLowerCase();
  if (ends(lo, 'gʻ')) return stem.slice(0, -2) + 'qqa';
  if (ends(lo, 'k')) return stem + 'ka';
  if (ends(lo, 'q')) return stem + 'qa';
  return stem + 'ga';
}

function checkPostposition(tokens, norms, i, issues, morph, isCorrect) {
  const word = norms[i];
  const needDan = REQUIRE_DAN.has(word);
  const needGa = !needDan && REQUIRE_GA.has(word);
  if (!needDan && !needGa) return;
  const prev = norms[i - 1], prevTok = tokens[i - 1];

  if (needDan) {
    if (ends(prev, 'dan')) return;
    if (!morph.isNominalStem(prev) || prev in PRONOUNS) return;
    const sug = prevTok.text + 'dan';
    issues.push({
      ruleId: 'KELISHIK-DAN',
      message: `«${word}» koʻmakchisi chiqish kelishigini talab qiladi: «${prevTok.text}dan ${tokens[i].text}»`,
      start: prevTok.start, length: prevTok.length, suggestions: isCorrect(sug) ? [sug] : [],
    });
  } else {
    if (ends(prev, 'ga') || ends(prev, 'ka') || ends(prev, 'qa')) return;
    if (!morph.isNominalStem(prev) || prev in PRONOUNS) return;
    const sug = buildDative(prevTok.text);
    issues.push({
      ruleId: 'KELISHIK-GA',
      message: `«${word}» koʻmakchisi joʻnalish kelishigini talab qiladi: «${sug} ${tokens[i].text}»`,
      start: prevTok.start, length: prevTok.length, suggestions: isCorrect(sug) ? [sug] : [],
    });
  }
}

function checkNumeralPlural(tokens, norms, i, to, issues, morph) {
  if (!isNumeral(norms[i])) return;
  let j = i + 1;
  if (j >= to) return;
  if (norms[j] === 'ta') { j++; if (j >= to) return; }
  const noun = norms[j];
  if (noun.length < 5) return;
  const removed = morph.tryRemovePlural(noun);
  if (!removed) return;
  if (j + 1 < to) {
    const next = norms[j + 1];
    if (ends(next, 'i') && detectPersonEnding(next, morph) === null) return;
  }
  const raw = tokens[j].text;
  const sug = raw.slice(0, removed.larIndex) + raw.slice(removed.larIndex + 3);
  issues.push({
    ruleId: 'SON-BIRLIK',
    message: `Sondan keyin ot birlikda qoʻllanadi: «${tokens[i].text} ${sug}»`,
    start: tokens[j].start, length: tokens[j].length, suggestions: [sug],
  });
}

function checkStandaloneMi(text, tokens, norms, i, issues, isCorrect) {
  if (norms[i] !== 'mi') return;
  const prev = tokens[i - 1], cur = tokens[i];
  for (let g = prev.start + prev.length; g < cur.start; g++) if (text[g] !== ' ') return;
  const joinedNorm = norms[i - 1] + 'mi';
  if (!isCorrect(joinedNorm)) return;
  issues.push({
    ruleId: 'MI-QOSHIB', message: '«-mi» yuklamasi soʻzga qoʻshib yoziladi',
    start: prev.start, length: cur.start + cur.length - prev.start, suggestions: [prev.text + 'mi'],
  });
}

function checkPersonAgreement(tokens, norms, from, to, issues, morph, isCorrect) {
  const count = to - from;
  if (count < 2 || count > 9) return;
  let subjIdx = -1;
  if (norms[from] in PRONOUNS) subjIdx = from;
  else if (count > 2 && norms[from + 1] in PRONOUNS) subjIdx = from + 1;
  if (subjIdx < 0) return;
  const subjPerson = PRONOUNS[norms[subjIdx]];
  const last = to - 1;
  if (last <= subjIdx) return;
  for (let i = subjIdx + 1; i < last; i++) {
    const w = norms[i];
    if (COMPLEX_MARKERS.has(w)) return;
    if (ends(w, 'gan') || ends(w, 'kan') || ends(w, 'qan') || ends(w, 'digan')) return;
  }
  const final = norms[last];
  if (final.includes('-')) return;
  const det = detectPersonEnding(final, morph);
  if (det === null) return;
  const matches = (subjPerson === 4 || subjPerson === 5) ? (det.person === 4 || det.person === 5) : det.person === subjPerson;
  if (matches) return;
  const suggestions = [];
  const targetPerson = subjPerson === 5 ? 4 : subjPerson;
  const target = ENDING_FAMILIES[det.family][targetPerson];
  if (target) {
    const rawStem = tokens[last].text.slice(0, tokens[last].text.length - det.suffix.length);
    const stem = final.slice(0, final.length - det.suffix.length);
    if (isCorrect(stem + target)) suggestions.push(rawStem + target);
  }
  issues.push({
    ruleId: 'SHAXS-SON', message: `Ega («${tokens[subjIdx].text}») bilan kesim shaxs-sonda mos kelmayapti`,
    start: tokens[last].start, length: tokens[last].length, suggestions,
  });
}

function checkCapitalization(text, tokens, from, isFirst, issues) {
  if (isFirst) return;
  const tok = tokens[from];
  if (tok.length < 2) return;
  const first = tok.text[0];
  // Faqat KICHIK harf bilan boshlangan boʻlsa davom etamiz
  const isLower = first === first.toLowerCase() && first !== first.toUpperCase();
  if (!isLower) return;
  let p = tok.start - 1;
  while (p >= 0 && /\s/.test(text[p])) p--;
  if (p < 1 || !'.!?…'.includes(text[p])) return;
  let q = p - 1, letters = 0;
  while (q >= 0 && (/\p{L}/u.test(text[q]) || isApostropheLike(text[q]))) { letters++; q--; }
  if (letters < 3) return;
  issues.push({
    ruleId: 'BOSH-HARF', message: 'Gap bosh harf bilan boshlanadi',
    start: tok.start, length: tok.length, suggestions: [first.toUpperCase() + tok.text.slice(1)],
  });
}

function checkPunctuation(text, issues) {
  for (const m of text.matchAll(/[ \t]+(?=[,;:!?]|\.(?!\.))/g)) {
    issues.push({
      ruleId: 'PUNKT-OLDIN', message: 'Tinish belgisidan oldin boʻshliq qoʻyilmaydi',
      start: m.index, length: m[0].length + 1, suggestions: [text[m.index + m[0].length]],
    });
  }
  for (const m of text.matchAll(/[,;](?=[^\s\d])/g)) {
    issues.push({
      ruleId: 'PUNKT-KEYIN', message: 'Tinish belgisidan keyin boʻshliq qoʻyiladi',
      start: m.index, length: 1, suggestions: [text[m.index] + ' '],
    });
  }
  for (const m of text.matchAll(/(?<=\S)[ ]{2,}(?=\S)/g)) {
    issues.push({
      ruleId: 'PUNKT-BOSHLIQ', message: 'Ortiqcha boʻshliq',
      start: m.index, length: m[0].length, suggestions: [' '],
    });
  }
  for (const m of text.matchAll(/(?<=\p{L}{2})[.!?](?=\p{Lu})/gu)) {
    issues.push({
      ruleId: 'PUNKT-NUQTA', message: 'Gap tugagach boʻshliq qoʻyiladi',
      start: m.index, length: 1, suggestions: [text[m.index] + ' '],
    });
  }
}
