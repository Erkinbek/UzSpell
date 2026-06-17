# UzSpell — VS Code kengaytmasi

Oʻzbek tili (lotin va kirill) uchun **100% oflayn** imlo va grammatika tekshiruvchi
hamda **lotin⇄kirill transliteratsiya**. [uz-hunspell](https://github.com/u2b3k/uz-hunspell)
lugʻatlari (95 000+ soʻz) va `hunspell-asm` (WASM) asosida — internet talab qilinmaydi.

## Imkoniyatlar

- **Imlo tekshiruvi** — xato soʻzlar tagiga chiziladi (Muammolar panelida koʻrinadi),
  tezkor tuzatish (Quick Fix, `Ctrl+.`) takliflari oʻzbekcha xatolar boʻyicha tartiblangan
  (x↔h, oʻ↔o, gʻ↔g; kirillda х↔ҳ, қ↔к, ў↔у, ғ↔г).
- **Grammatika** (lotin) — ega-kesim moslashuvi, koʻmakchi kelishigi, son+birlik, takror soʻz va h.k.
- **Tezkor amallar** — taklifni qoʻllash, «eʼtiborsiz qoldirish» (sessiya), «lugʻatga qoʻshish» (doimiy).
- **Transliteratsiya** — belgilangan matn yoki butun hujjatni lotin⇄kirill (buyruqlar palitrasida).

## Sozlamalar

| Sozlama | Tavsif | Standart |
|---|---|---|
| `uzspell.enable` | Tekshiruvni yoqish | `true` |
| `uzspell.script` | `auto` / `latin` / `cyrillic` | `auto` |
| `uzspell.grammar` | Grammatikani ham tekshirish | `true` |
| `uzspell.checkAllCaps` | BOSH HARFLI qisqartmalarni tekshirish | `false` |
| `uzspell.maxSuggestions` | Takliflar soni | `6` |
| `uzspell.languages` | Qaysi fayl turlarida | `["plaintext","markdown"]` |

## Buyruqlar (Ctrl+Shift+P)

- **UzSpell: Hujjatni tekshirish**
- **UzSpell: Lotindan kirillga oʻgirish**
- **UzSpell: Kirilldan lotinga oʻgirish**
- **UzSpell: Tekshiruvni yoqish/oʻchirish**

## Qurish

```bash
cd vscode
npm install
npm run build        # dist/extension.js + dictionaries/ hosil qiladi
```

VS Code'da `vscode/` papkasini ochib **F5** bilan sinab koʻrish mumkin (Extension Development Host).
Doʻkonga paket: `npx @vscode/vsce package` (lugʻatlar va `hunspell-asm` paketga kiradi).

## Litsenziya

GPL-3.0 (uz-hunspell lugʻatlari shu litsenziyada).
