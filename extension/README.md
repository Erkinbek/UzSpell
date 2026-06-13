# UzSpell — brauzer kengaytmasi

Oʻzbek tili uchun **100% oflayn** imlo, grammatika va lotin–kirill transliteratsiya
brauzer kengaytmasi. Chrome va Firefox uchun (Manifest V3). Internet talab qilinmaydi —
lugʻat va Hunspell dvigateli (WASM) kengaytma ichida.

## Imkoniyatlar

- **Inputlarda avtomatik tekshiruv (yangi)** — istalgan sahifadagi matn maydoniga
  (`<textarea>`, `<input>`, `contenteditable`) yozayotganda imlo va grammatika xatolari
  **avtomatik** tagiga toʻlqinli chiziq bilan chiziladi (imlo — qizil, grammatika — koʻk).
  Chiziqqa bosilsa — taklif oynasi chiqadi, bir bosishda tuzatadi. Internet talab qilinmaydi.
  Yoqib/oʻchirish: oʻng tugma → **UzSpell → «Inputlarda avtomatik tekshiruv»**.
- **Popup tekshiruvchi** — kengaytma belgisini bosing, matn yozing/qoʻying:
  imlo xatolari qizil, grammatika koʻk toʻlqinli chiziq bilan belgilanadi, takliflar beriladi
- **Transliteratsiya** — popup'da Lot→Kir / Kir→Lot tugmalari
- **Kontekst menyu** — istalgan sahifada matnni belgilab, oʻng tugma → **UzSpell →
  Lotin↔Kirill**; tahrirlanadigan maydonda almashtiriladi, aks holda nusxaga olinadi
- Lotin va kirill yozuvlari (avto-aniqlash), 95 000+ soʻzlik uz-hunspell lugʻati

## Qanday ishlaydi (jonli tekshiruv)

Dvigatel (Hunspell WASM + grammatika) **bitta joyda** — fon skriptida (Chrome: service
worker, Firefox: background page) ishlaydi; shuning uchun har bir sahifada WASM qayta
yuklanmaydi. Sahifadagi kontent skripti faqat matnni fon skriptiga yuboradi va qaytgan
xatolarni chizadi:

- `<textarea>`/`<input>` — koʻrinmas «mirror» div orqali har bir xato soʻzning aniq
  joylashuvi oʻlchanadi va ustiga toʻlqinli chiziq qoʻyiladi (scroll bilan birga suriladi)
- `contenteditable` — `Range.getClientRects()` orqali xato boʻlaklarining rect'lari topiladi

Uslublar manifest orqali qoʻyilgani uchun qatʼiy CSP'li saytlarda ham ishlaydi.

## Qurish

```bash
cd extension
npm install
npm run build
```

Natija: `dist/chrome/` va `dist/firefox/`.

## Yuklash (developer rejimi)

- **Chrome/Edge:** `chrome://extensions` → *Developer mode* → *Load unpacked* → `dist/chrome`
- **Firefox:** `about:debugging` → *This Firefox* → *Load Temporary Add-on* → `dist/firefox/manifest.json`

## Tuzilishi

| Fayl | Vazifa |
|---|---|
| `src/core.js` | Normalizatsiya, tokenizatsiya, transliteratsiya, son shakllari, morfologiya |
| `src/grammar.js` | Grammatika qoidalari (desktop bilan bir xil) |
| `src/engine.js` | Umumiy dvigatel (hunspell-asm WASM + grammatika) — popup va background uchun |
| `src/popup.*` | Tekshiruvchi oyna |
| `src/background.js` | Dvigatel + kontekst menyu + `uzspell-check` xabar ishlovchisi |
| `src/content.js` + `content.css` | Sahifa inputlarida jonli tekshiruv (toʻlqinli chiziq + taklif) |
| `build.mjs` | esbuild bilan Chrome/Firefox paketlarini yasaydi |

## Texnik eslatma

Imlo dvigateli — [hunspell-asm](https://github.com/kwonoj/hunspell-asm) (haqiqiy Hunspell
WASM'ga kompilyatsiya qilingan). Lugʻat ~60 ms da yuklanadi, 1000 soʻz ~2 ms da tekshiriladi.
WASM uchun manifest CSP'sida `wasm-unsafe-eval` ruxsati bor.

© 2026 Erkin Pardayev. Lugʻatlar: uz-hunspell (GPL).
