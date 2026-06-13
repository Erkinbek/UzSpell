// UzSpell brauzer kengaytmasi — umumiy yadro.
// Desktop (C#) UzSpell.Core'dan JS'ga koʻchirilgan: normalizatsiya, tokenizatsiya,
// transliteratsiya, son shakllari va grammatika qoidalari.
// Imlo tekshiruvi (Hunspell) alohida — popup'da WASM orqali.

export const OKINA = 'ʻ'; // oʻ/gʻ belgisi
export const TUTUQ = 'ʼ'; // tutuq belgisi

const APOSTROPHE_LIKES = ["'", '`', '‘', '’', 'ʹ', '′'];

export function isApostropheLike(c) {
  return c === OKINA || c === TUTUQ || APOSTROPHE_LIKES.includes(c);
}

// Lotin apostrof variantlarini kanonik belgilarga keltiradi (belgilar soni oʻzgarmaydi).
export function normalizeToken(token) {
  let chars = null;
  for (let i = 0; i < token.length; i++) {
    const c = token[i];
    if (c === OKINA || c === TUTUQ) continue;
    if (!APOSTROPHE_LIKES.includes(c)) continue;
    if (chars === null) chars = token.split('');
    const prev = i > 0 ? token[i - 1].toLowerCase() : '\0';
    chars[i] = prev === 'o' || prev === 'g' ? OKINA : TUTUQ;
  }
  return chars === null ? token : chars.join('');
}

// ---------------- Yozuv aniqlash ----------------

export const SCRIPT_LATIN = 'latin';
export const SCRIPT_CYRILLIC = 'cyrillic';

export function detectScript(token) {
  let latin = 0, cyr = 0;
  for (const c of token) {
    const code = c.codePointAt(0);
    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) latin++;
    else if (code >= 0x0400 && code <= 0x04FF) cyr++;
  }
  if (latin === 0 && cyr === 0) return null;
  return cyr > latin ? SCRIPT_CYRILLIC : SCRIPT_LATIN;
}

// ---------------- Tokenizatsiya ----------------

function isCoreLetter(c) {
  return /\p{L}/u.test(c) && !isApostropheLike(c);
}

function isWordChar(s, i) {
  const c = s[i];
  if (isCoreLetter(c)) return true;
  if (isApostropheLike(c) || c === '-') return i > 0 && isCoreLetter(s[i - 1]);
  return false;
}

// Matnni soʻzlarga ajratadi: [{text, start, length}]
export function tokenize(text) {
  const tokens = [];
  const n = text.length;
  let i = 0;
  while (i < n) {
    if (!isCoreLetter(text[i])) { i++; continue; }
    const start = i;
    while (i < n && isWordChar(text, i)) i++;
    let end = i;
    while (end > start) {
      const c = text[end - 1];
      if (isCoreLetter(c)) break;
      if (isApostropheLike(c) && end - 1 > start) {
        const prev = text[end - 2].toLowerCase();
        if (prev === 'o' || prev === 'g') break;
      }
      end--;
    }
    if (end > start) tokens.push({ text: text.slice(start, end), start, length: end - start });
  }
  return tokens;
}

// ---------------- Son shakllari ----------------

const NUMBER_WORDS = new Set([
  'bir', 'ikki', 'uch', 'toʻrt', 'besh', 'olti', 'yetti', 'sakkiz',
  'toʻqqiz', 'oʻn', 'yigirma', 'oʻttiz', 'qirq', 'ellik', 'oltmish',
  'yetmish', 'sakson', 'toʻqson', 'yuz', 'ming', 'million', 'milliard',
  'necha', 'yarim',
]);
const SPECIAL_TA = new Set(['bitta', 'nechta']);

function allDigits(s) {
  if (!s.length) return false;
  for (const c of s) if (c < '0' || c > '9') return false;
  return true;
}

export function isNumeral(norm) {
  if (!norm.length) return false;
  if (allDigits(norm) || NUMBER_WORDS.has(norm) || SPECIAL_TA.has(norm)) return true;
  if (norm.length > 2 && norm.endsWith('ta')) {
    const head = norm.slice(0, -2);
    return allDigits(head) || NUMBER_WORDS.has(head);
  }
  return false;
}

// ---------------- Transliteratsiya ----------------

function isLatinVowel(c) { return 'aeiou'.includes((c || '').toLowerCase()); }
function isCyrVowel(c) { return 'аеёиоуўэюяы'.includes((c || '').toLowerCase()); }
function isUpper(c) { return c !== c.toLowerCase() && c === c.toUpperCase(); }

function matchesAt(text, idx, suffix) {
  if (idx + suffix.length > text.length) return false;
  for (let k = 0; k < suffix.length; k++)
    if (text[idx + k].toLowerCase() !== suffix[k]) return false;
  return true;
}
function isTsLoanSuffix(text, idx) {
  return matchesAt(text, idx, 'iya') || matchesAt(text, idx, 'ion');
}

function appendWithCase(out, mapped, src, srcIndex, consumed) {
  const first = src[srcIndex];
  if (!isUpper(first)) { out.push(mapped); return; }
  let restUpper;
  if (consumed >= 2 && /\p{L}/u.test(src[srcIndex + consumed - 1])) {
    restUpper = isUpper(src[srcIndex + consumed - 1]);
  } else {
    let neighbor = '';
    for (let k = srcIndex + consumed; k < src.length; k++) {
      if (/\p{L}/u.test(src[k])) { neighbor = src[k]; break; }
      if (!isApostropheLike(src[k])) break;
    }
    if (!neighbor) for (let k = srcIndex - 1; k >= 0; k--) {
      if (/\p{L}/u.test(src[k])) { neighbor = src[k]; break; }
      if (!isApostropheLike(src[k])) break;
    }
    restUpper = neighbor && isUpper(neighbor);
  }
  let res = mapped[0].toUpperCase();
  for (let k = 1; k < mapped.length; k++) res += restUpper ? mapped[k].toUpperCase() : mapped[k];
  out.push(res);
}

export function toCyrillic(text) {
  const out = [];
  const n = text.length;
  let lastSrc = '\0';
  let i = 0;
  while (i < n) {
    const c = text[i];
    if (isApostropheLike(c)) {
      const between = lastSrc !== '\0' && i + 1 < n && /\p{L}/u.test(text[i + 1]);
      out.push(between ? 'ъ' : c);
      i++; continue;
    }
    if (!/\p{L}/u.test(c)) { out.push(c); lastSrc = '\0'; i++; continue; }

    const lo = c.toLowerCase();
    const next = i + 1 < n ? text[i + 1] : '\0';
    const lon = next === '\0' ? '\0' : next.toLowerCase();
    const wordStart = lastSrc === '\0';
    let mapped = null, consumed = 1;

    if (lo === 'y' && lon === 'o' && i + 2 < n && isApostropheLike(text[i + 2])) { mapped = 'йў'; consumed = 3; }
    else if (lo === 'o' && next !== '\0' && isApostropheLike(next)) { mapped = 'ў'; consumed = 2; }
    else if (lo === 'g' && next !== '\0' && isApostropheLike(next)) { mapped = 'ғ'; consumed = 2; }
    else if (lo === 's' && lon === 'h') { mapped = 'ш'; consumed = 2; }
    else if (lo === 'c' && lon === 'h') { mapped = 'ч'; consumed = 2; }
    else if (lo === 'y' && lon === 'o') { mapped = 'ё'; consumed = 2; }
    else if (lo === 'y' && lon === 'u') { mapped = 'ю'; consumed = 2; }
    else if (lo === 'y' && lon === 'a') { mapped = 'я'; consumed = 2; }
    else if (lo === 'y' && lon === 'e' && wordStart) { mapped = 'е'; consumed = 2; }
    else if (lo === 't' && lon === 's' && isTsLoanSuffix(text, i + 2)) { mapped = 'ц'; consumed = 2; }
    else {
      mapped = ({
        a: 'а', b: 'б', c: 'ц', d: 'д',
        e: (wordStart || isLatinVowel(lastSrc)) ? 'э' : 'е',
        f: 'ф', g: 'г', h: 'ҳ', i: 'и', j: 'ж', k: 'к', l: 'л', m: 'м',
        n: 'н', o: 'о', p: 'п', q: 'қ', r: 'р', s: 'с', t: 'т', u: 'у',
        v: 'в', w: 'в', x: 'х', y: 'й', z: 'з',
      })[lo] || c;
    }

    appendWithCase(out, mapped, text, i, consumed);
    lastSrc = consumed >= 2 ? text[i + consumed - 1] : c;
    if (isApostropheLike(lastSrc)) lastSrc = text[i];
    i += consumed;
  }
  return out.join('');
}

export function toLatin(text) {
  const out = [];
  const n = text.length;
  let prev = '\0';
  for (let i = 0; i < n; i++) {
    const c = text[i];
    if (!/\p{L}/u.test(c)) { out.push(c); prev = '\0'; continue; }
    const lo = c.toLowerCase();
    const wordStart = prev === '\0';
    const nextLetter = i + 1 < n ? text[i + 1] : '\0';
    let mapped;
    switch (lo) {
      case 'а': mapped = 'a'; break;
      case 'б': mapped = 'b'; break;
      case 'в': mapped = 'v'; break;
      case 'г': mapped = 'g'; break;
      case 'д': mapped = 'd'; break;
      case 'е': mapped = (wordStart || isCyrVowel(prev) || prev === 'ь' || prev === 'ъ') ? 'ye' : 'e'; break;
      case 'ё': mapped = 'yo'; break;
      case 'ж': mapped = 'j'; break;
      case 'з': mapped = 'z'; break;
      case 'и': mapped = 'i'; break;
      case 'й': mapped = 'y'; break;
      case 'к': mapped = 'k'; break;
      case 'л': mapped = 'l'; break;
      case 'м': mapped = 'm'; break;
      case 'н': mapped = 'n'; break;
      case 'о': mapped = 'o'; break;
      case 'п': mapped = 'p'; break;
      case 'р': mapped = 'r'; break;
      case 'с': mapped = 's'; break;
      case 'т': mapped = 't'; break;
      case 'у': mapped = 'u'; break;
      case 'ф': mapped = 'f'; break;
      case 'х': mapped = 'x'; break;
      case 'ц': mapped = isCyrVowel(prev) ? 'ts' : 's'; break;
      case 'ч': mapped = 'ch'; break;
      case 'ш': mapped = 'sh'; break;
      case 'щ': mapped = 'sh'; break;
      case 'ъ': {
        const nl = (nextLetter || '').toLowerCase();
        mapped = (nl === 'е' || nl === 'ё' || nl === 'ю' || nl === 'я') ? null : TUTUQ;
        break;
      }
      case 'ь': mapped = null; break;
      case 'ы': mapped = 'i'; break;
      case 'э': mapped = 'e'; break;
      case 'ю': mapped = 'yu'; break;
      case 'я': mapped = 'ya'; break;
      case 'ў': mapped = 'o' + OKINA; break;
      case 'қ': mapped = 'q'; break;
      case 'ғ': mapped = 'g' + OKINA; break;
      case 'ҳ': mapped = 'h'; break;
      default: mapped = c; break;
    }
    if (mapped !== null) appendWithCase(out, mapped, text, i, 1);
    prev = c;
  }
  return out.join('');
}

// ---------------- Morfologiya (uz_UZ.dic flaglaridan) ----------------

export function buildMorphology(dicText) {
  const verbStems = new Set();
  const nominalStems = new Set();
  const lines = dicText.split('\n');
  for (const raw of lines) {
    const line = raw.replace(/\r$/, '');
    if (!line.length || (line[0] >= '0' && line[0] <= '9')) continue;
    const slash = line.indexOf('/');
    let word, flags;
    if (slash < 0) { word = line.trim(); flags = ''; }
    else { word = line.slice(0, slash).trim(); flags = line.slice(slash + 1).trim(); }
    if (!word.length) continue;
    const lower = word.toLowerCase();
    if (flags.includes('X')) verbStems.add(lower);
    if (flags.includes('V') || flags.includes('S')) nominalStems.add(lower);
  }
  return {
    isVerbStem: (w) => verbStems.has(w),
    isNominalStem: (w) => nominalStems.has(w),
    tryRemovePlural(lowerWord) {
      let idx = lowerWord.indexOf('lar');
      while (idx > 0) {
        const stem = lowerWord.slice(0, idx);
        if (nominalStems.has(stem)) return { stem: stem + lowerWord.slice(idx + 3), larIndex: idx };
        idx = lowerWord.indexOf('lar', idx + 1);
      }
      return null;
    },
  };
}
