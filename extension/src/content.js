// UzSpell — sahifadagi tahrir maydonlarini jonli tekshiruvchi kontent skripti.
// Matnni fon skriptiga (dvigatel shu yerda) yuboradi, qaytgan xatolarni
// toʻlqinli chiziq bilan belgilaydi va taklif oynasini koʻrsatadi.
// Imlo — qizil, grammatika — koʻk. 100% oflayn.
(() => {
  const api = typeof browser !== 'undefined' ? browser : chrome;
  if (!api || !api.runtime || !api.runtime.sendMessage) return;

  const MAX_LEN = 8000;        // jonli tekshiruv uchun matn chegarasi
  const DEBOUNCE = 600;        // yozishdan keyin tekshirishgacha (ms)

  let enabled = true;
  let field = null;            // joriy faol maydon
  let kind = null;             // 'textarea' | 'input' | 'ce'
  let errors = [];
  let lastText = '';
  const ignored = new Set();
  let reqId = 0;

  // ---- Qatlamlar (lazy) ----
  let layer = null;            // toʻlqinli chiziqlar uchun
  let mirror = null;           // textarea/input oʻlchash uchun
  let tip = null;              // taklif oynasi
  let rafId = 0;
  let lastGeom = '';

  // Uslublar manifest.content_scripts.css orqali qoʻyiladi (sahifa CSP'sidan ozod).

  function ensureLayer() {
    if (!layer) { layer = document.createElement('div'); layer.className = 'uzspell-layer'; document.body.appendChild(layer); }
    return layer;
  }

  // ---------------- Maydonni aniqlash ----------------
  function classify(el) {
    if (!el) return null;
    const tag = el.tagName;
    if (tag === 'TEXTAREA') return 'textarea';
    if (tag === 'INPUT') {
      const t = (el.type || 'text').toLowerCase();
      if (['text', 'search', 'url', 'email', ''].includes(t)) return 'input';
      return null;
    }
    if (el.isContentEditable) return 'ce';
    return null;
  }

  function getText() {
    if (kind === 'ce') return buildCEModel(field).text;
    return field.value || '';
  }

  // ---------------- Tekshiruv ----------------
  let debTimer = 0;
  function scheduleCheck() {
    clearTimeout(debTimer);
    debTimer = setTimeout(runCheck, DEBOUNCE);
  }

  function runCheck() {
    if (!field || !enabled) return;
    const text = getText();
    lastText = text;
    if (!text.trim() || text.length > MAX_LEN) { errors = []; redraw(); return; }
    const myId = ++reqId;
    try {
      api.runtime.sendMessage({ type: 'uzspell-check', text, script: 'auto' }, (resp) => {
        if (api.runtime.lastError) return;             // fon uxlab qolgan boʻlishi mumkin
        if (myId !== reqId || !field) return;          // eskirgan javob
        if (!resp || !resp.ok) { errors = []; redraw(); return; }
        errors = (resp.errors || []).filter((e) =>
          !ignored.has(e.type === 'spell' ? e.normalized : 'G:' + e.ruleId + ':' + e.word));
        redraw();
      });
    } catch { /* kontekst yoʻq */ }
  }

  // ---------------- Joylashuvni oʻlchash ----------------
  function copyStyle(src, dst, props) {
    const cs = getComputedStyle(src);
    for (const p of props) dst.style[p] = cs[p];
  }

  function ensureMirror() {
    if (!mirror) { mirror = document.createElement('div'); mirror.className = 'uzspell-mirror'; document.body.appendChild(mirror); }
    return mirror;
  }

  // textarea/input: matnni nusxalovchi mirror orqali har bir xato boʻlagi rect'ini topadi
  function rectsForInput() {
    const m = ensureMirror();
    const r = field.getBoundingClientRect();
    copyStyle(field, m, [
      'fontFamily', 'fontSize', 'fontWeight', 'fontStyle', 'fontVariant', 'letterSpacing',
      'textTransform', 'textIndent', 'lineHeight', 'wordSpacing', 'tabSize',
      'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
      'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth', 'boxSizing',
    ]);
    m.style.position = 'fixed';
    m.style.left = r.left + 'px';
    m.style.top = r.top + 'px';
    m.style.width = r.width + 'px';
    m.style.height = r.height + 'px';
    m.style.whiteSpace = kind === 'input' ? 'pre' : 'pre-wrap';
    m.style.overflow = 'hidden';
    // koʻchirilgan border-kengliklar faqat border STYLE boʻlsa hisobga olinadi:
    // shaffof solid border bilan maydon ramkasini takrorlaymiz (matn joylashuvi mos kelsin)
    m.style.borderStyle = 'solid';
    m.style.borderColor = 'transparent';
    if (kind === 'input') {
      // input matnni vertikal markazlaydi; div esa yoʻq — line-height bilan markazlaymiz
      const cs = getComputedStyle(field);
      const ph = parseFloat(cs.paddingTop) + parseFloat(cs.paddingBottom);
      const bh = parseFloat(cs.borderTopWidth) + parseFloat(cs.borderBottomWidth);
      const ch = field.clientHeight - ph; // ichki matn balandligi
      if (ch > 0) m.style.lineHeight = ch + 'px';
      void bh;
    }

    const text = field.value || '';
    // xato boʻlaklarini <span class="e"> bilan oʻraymiz
    let html = '', pos = 0;
    const sorted = errors.slice().sort((a, b) => a.start - b.start);
    for (let i = 0; i < sorted.length; i++) {
      const e = sorted[i];
      if (e.start < pos) continue;
      html += esc(text.slice(pos, e.start));
      html += `<span class="e" data-i="${i}">` + esc(text.substr(e.start, e.length)) + '</span>';
      pos = e.start + e.length;
    }
    html += esc(text.slice(pos));
    if (text.endsWith('\n')) html += ' ';
    m.innerHTML = html;
    m.scrollTop = field.scrollTop;
    m.scrollLeft = field.scrollLeft;

    const out = [];
    m.querySelectorAll('span.e').forEach((sp) => {
      const i = +sp.dataset.i;
      for (const rc of sp.getClientRects()) {
        if (rc.width < 1) continue;
        out.push({ err: sorted[i], left: rc.left, top: rc.top, width: rc.width, height: rc.height, bottom: rc.bottom });
      }
    });
    return out;
  }

  // contenteditable: matn-tugun xaritasi orqali Range rect'lari
  function buildCEModel(root) {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, null);
    let text = '';
    const node = [], off = [];
    let n, prevBlock = null;
    while ((n = walker.nextNode())) {
      // blok chegaralarida ajratuvchi '\n' (xaritasiz)
      const block = n.parentElement && getComputedStyle(n.parentElement).display;
      if (prevBlock !== null && /block|flex|grid|list-item|table/.test(block || '') && text && !text.endsWith('\n')) {
        text += '\n'; node.push(null); off.push(0);
      }
      prevBlock = block;
      const s = n.nodeValue;
      for (let i = 0; i < s.length; i++) { text += s[i]; node.push(n); off.push(i); }
    }
    return { text, node, off };
  }

  function rangeFor(model, start, end) {
    let sNode = model.node[start], sOff = model.off[start];
    let j = end - 1;
    while (j > start && !model.node[j]) j--;
    const eNode = model.node[j], eOff = model.off[j];
    if (!sNode || !eNode) return null;
    const range = document.createRange();
    range.setStart(sNode, sOff);
    range.setEnd(eNode, eOff + 1);
    return range;
  }

  function rectsForCE() {
    const model = buildCEModel(field);
    const out = [];
    for (const e of errors) {
      const range = rangeFor(model, e.start, e.start + e.length);
      if (!range) continue;
      for (const rc of range.getClientRects()) {
        if (rc.width < 1) continue;
        out.push({ err: e, left: rc.left, top: rc.top, width: rc.width, height: rc.height, bottom: rc.bottom });
      }
    }
    return out;
  }

  // ---------------- Chizish ----------------
  function redraw() {
    if (!field) return;
    const lay = ensureLayer();
    lay.innerHTML = '';
    if (!enabled || !errors.length) return;

    let rects;
    try { rects = kind === 'ce' ? rectsForCE() : rectsForInput(); }
    catch { rects = []; }

    // maydon koʻrinadigan sohasi (ichki scrollda chetga chiqqanini kesish)
    const fr = field.getBoundingClientRect();
    for (const r of rects) {
      // maydon tashqarisidagi (scroll bilan yashiringan) chiziqlarni oʻtkazib yuboramiz
      if (r.bottom < fr.top - 1 || r.top > fr.bottom + 1) continue;
      if (r.left > fr.right + 1 || r.left + r.width < fr.left - 1) continue;
      const bar = document.createElement('div');
      bar.className = 'uzspell-mark ' + (r.err.type === 'spell' ? 'spell' : 'gram');
      bar.style.left = Math.max(r.left, fr.left) + 'px';
      bar.style.top = (r.bottom - 3) + 'px';
      const w = Math.min(r.left + r.width, fr.right) - Math.max(r.left, fr.left);
      bar.style.width = Math.max(0, w) + 'px';
      bar.style.height = '3px';
      bar.addEventListener('mousedown', (ev) => { ev.preventDefault(); ev.stopPropagation(); showTip(r.err, bar); });
      lay.appendChild(bar);
    }
  }

  // ---------------- Taklif oynasi ----------------
  function showTip(err, bar) {
    hideTip();
    tip = document.createElement('div');
    tip.className = 'uzspell-tip';
    const head = document.createElement('div');
    head.className = 'h';
    head.innerHTML = `<span class="dot ${err.type === 'spell' ? 'spell' : 'gram'}"></span>` +
      (err.type === 'spell' ? 'Imlo xatosi' : 'Grammatika');
    tip.appendChild(head);
    if (err.message) { const m = document.createElement('div'); m.className = 'msg'; m.textContent = err.message; tip.appendChild(m); }

    const sugs = err.suggestions || [];
    if (sugs.length) {
      for (const s of sugs) {
        const b = document.createElement('button');
        b.textContent = s;
        b.addEventListener('click', () => { applySuggestion(err, s); hideTip(); });
        tip.appendChild(b);
      }
    } else {
      const none = document.createElement('span');
      none.className = 'none';
      none.textContent = 'taklif yoʻq';
      tip.appendChild(none);
    }
    const ign = document.createElement('button');
    ign.className = 'ign';
    ign.textContent = 'Eʼtiborsiz';
    ign.addEventListener('click', () => {
      ignored.add(err.type === 'spell' ? err.normalized : 'G:' + err.ruleId + ':' + err.word);
      errors = errors.filter((x) => x !== err);
      hideTip(); redraw();
    });
    tip.appendChild(ign);

    document.body.appendChild(tip);
    const br = bar.getBoundingClientRect();
    const tr = tip.getBoundingClientRect();
    let left = br.left;
    let top = br.bottom + 6;
    if (left + tr.width > innerWidth - 8) left = innerWidth - tr.width - 8;
    if (top + tr.height > innerHeight - 8) top = br.top - tr.height - 6;
    tip.style.left = Math.max(8, left) + 'px';
    tip.style.top = Math.max(8, top) + 'px';
    setTimeout(() => document.addEventListener('mousedown', onDocDown, true), 0);
  }

  function onDocDown(ev) {
    if (tip && !tip.contains(ev.target)) hideTip();
  }
  function hideTip() {
    if (tip) { tip.remove(); tip = null; document.removeEventListener('mousedown', onDocDown, true); }
  }

  function applySuggestion(err, sug) {
    if (kind === 'ce') {
      const model = buildCEModel(field);
      if (model.text.substr(err.start, err.length) !== err.word) { runCheck(); return; }
      const range = rangeFor(model, err.start, err.start + err.length);
      if (!range) return;
      range.deleteContents();
      range.insertNode(document.createTextNode(sug));
      field.dispatchEvent(new InputEvent('input', { bubbles: true }));
    } else {
      const v = field.value;
      if (v.substr(err.start, err.length) !== err.word) { runCheck(); return; }
      field.value = v.slice(0, err.start) + sug + v.slice(err.start + err.length);
      const caret = err.start + sug.length;
      try { field.setSelectionRange(caret, caret); } catch { /* ba'zi input turlari */ }
      field.dispatchEvent(new Event('input', { bubbles: true }));
    }
    field.focus();
    scheduleCheck();
  }

  // ---------------- Geometriyani kuzatish ----------------
  function geomKey() {
    if (!field) return '';
    const r = field.getBoundingClientRect();
    return [Math.round(r.left), Math.round(r.top), Math.round(r.width), Math.round(r.height),
      field.scrollTop | 0, field.scrollLeft | 0].join(',');
  }
  function loop() {
    if (!field) { rafId = 0; return; }
    const k = geomKey();
    if (k !== lastGeom) { lastGeom = k; redraw(); }
    rafId = requestAnimationFrame(loop);
  }
  function startLoop() { if (!rafId) { lastGeom = ''; rafId = requestAnimationFrame(loop); } }
  function stopLoop() { if (rafId) { cancelAnimationFrame(rafId); rafId = 0; } }

  // ---------------- Fokus boshqaruvi ----------------
  function attach(el) {
    field = el; kind = classify(el); errors = []; lastText = '';
    ensureLayer();
    startLoop();
    runCheck();
  }
  function detach() {
    stopLoop(); hideTip();
    if (layer) layer.innerHTML = '';
    if (mirror) mirror.innerHTML = '';
    field = null; kind = null; errors = [];
  }

  document.addEventListener('focusin', (ev) => {
    if (!enabled) return;
    const k = classify(ev.target);
    if (!k) { if (field) detach(); return; }
    if (ev.target !== field) { detach(); attach(ev.target); }
  }, true);

  document.addEventListener('focusout', (ev) => {
    if (ev.target === field) setTimeout(() => { if (document.activeElement !== field) detach(); }, 50);
  }, true);

  document.addEventListener('input', (ev) => {
    if (ev.target === field) { hideTip(); scheduleCheck(); }
  }, true);

  // ---------------- Yoqilgan/oʻchirilgan holati ----------------
  function applyEnabled(v) {
    enabled = v !== false;
    if (!enabled) detach();
    else if (document.activeElement && classify(document.activeElement)) attach(document.activeElement);
  }

  try {
    api.storage.local.get('autocheck').then((r) => {
      applyEnabled(typeof r.autocheck === 'undefined' ? true : r.autocheck);
    }).catch(() => {});
    api.storage.onChanged.addListener((ch, area) => {
      if (area === 'local' && ch.autocheck) applyEnabled(ch.autocheck.newValue);
    });
  } catch { /* storage yoʻq */ }

  function esc(s) { return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
})();
