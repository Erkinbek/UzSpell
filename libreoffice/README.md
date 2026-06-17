# UzSpell — LibreOffice imlo lugʻati (.oxt)

Oʻzbek tili (lotin va kirill) imlosini **LibreOffice** (Writer, Calc, Impress…) va
Apache OpenOffice'da tekshirish uchun lugʻat kengaytmasi. [uz-hunspell](https://github.com/u2b3k/uz-hunspell)
lugʻatlari asosida, 100% oflayn.

## Yigʻish

```powershell
powershell -ExecutionPolicy Bypass -File libreoffice\pack.ps1
```

Natija: `dist\UzSpell-libreoffice.oxt`. Skript lugʻat fayllarini `uz-hunspell\`
papkasidan oladi — submodule yuklangan boʻlsin (`git submodule update --init`).

## Oʻrnatish

1. `UzSpell-libreoffice.oxt` faylini ikki marta bosing **yoki**
   LibreOffice'da **Asboblar → Kengaytmalar boshqaruvchisi → Qoʻshish**.
2. LibreOffice'ni qayta ishga tushiring.
3. Matn tili oʻzbekcha (Lotin yoki Kirill) qilib belgilangan boʻlsa
   (**Asboblar → Til → Matn uchun**), imlo avtomatik tekshiriladi.

## Locale belgilari

| Yozuv | Locale |
|---|---|
| Lotin | `uz`, `uz-UZ`, `uz-Latn-UZ` |
| Kirill | `uz-Cyrl`, `uz-Cyrl-UZ` |

## Litsenziya

Lugʻatlar GPL-3.0 (uz-hunspell). `.oxt` ichida `LICENSE.txt` sifatida joylanadi.
