# UzSpell — oʻzbek tili uchun oflayn imlo tekshiruvchi

[uz-hunspell](https://github.com/u2b3k/uz-hunspell) lugʻatlari (95 000+ soʻz, lotin va kirill)
asosida qurilgan, **100% oflayn** ishlaydigan imlo tekshiruvchi. Internet umuman talab qilinmaydi.

## Tarkibi

| Loyiha | Tavsif |
|---|---|
| `src/UzSpell.Core` | Tekshiruv mexanizmi: tokenizer, apostrof normalizatsiyasi, lotin/kirill avto-aniqlash, Hunspell tekshiruvi va takliflar |
| `src/UzSpell.App` | Oynali dastur (WPF): xatolar qizil toʻlqinli chiziq bilan koʻrsatiladi, takliflar paneli, oʻng tugma menyusi, **MS Word integratsiyasi** |
| `src/UzSpell.Cli` | Terminal vositasi: fayl yoki stdin orqali tekshirish |
| `uz-hunspell/` | Asl lugʻat fayllari (klonlangan repo) |
| `dist/` | Tayyor Release fayllar |

## Ishga tushirish

Tayyor fayllar:

- **Oynali dastur:** `dist\UzSpell\UzSpell.exe`
- **Terminal:** `dist\uzspell-cli\uzspell.exe matn.txt`

Manbadan qurish (.NET 10 SDK kerak):

```powershell
dotnet build
dotnet run --project src\UzSpell.App      # oynali dastur
dotnet run --project src\UzSpell.Cli -- namuna.txt
```

## Imkoniyatlar

- **Lotin va kirill** yozuvlari, har bir soʻz uchun avtomatik aniqlanadi (yoki qoʻlda tanlanadi)
- **Apostrof normalizatsiyasi** — `o'zbek`, `o`zbek`, `o‘zbek` kabi yozilishlar ham toʻgʻri deb qabul qilinadi
  (lugʻatdagi kanonik belgilar: `ʻ` U+02BB va tutuq `ʼ` U+02BC); takliflar kanonik koʻrinishda beriladi
- **Juft soʻzlar** (`katta-katta`) qoʻllab-quvvatlanadi
- **Takliflar** — har bir xato uchun 6 tagacha tuzatish varianti
- **Shaxsiy lugʻat** — `%APPDATA%\UzSpell\custom_words.txt` ga doimiy saqlanadi
- **.docx oʻqish** — Word fayli matnini Word'siz ham ochib tekshiradi
- **BOSH HARFLI** qisqartmalar (AQSH, BMT) sukut boʻyicha tekshirilmaydi

## MS Word integratsiyasi

1. Word'da hujjatni oching
2. UzSpell'da **«📝 Word hujjatini tekshirish»** tugmasini bosing
3. Xato soʻzlar hujjatda **qizil toʻlqinli chiziq** bilan belgilanadi
4. Oʻng paneldagi taklif tugmasi bosilsa, soʻz hujjatda **hamma joyda almashtiriladi**
5. **«🧹 Word belgilarini tozalash»** belgilashlarni olib tashlaydi

Integratsiya COM orqali ishlaydi — Word oʻrnatilgan boʻlishi kifoya, qoʻshimcha plagin talab qilinmaydi.

## Terminal (CLI)

```
uzspell <fayl.txt> [parametrlar]

  --lotin       Faqat lotin lugʻati
  --kirill      Faqat kirill lugʻati
  --taklifsiz   Takliflarsiz (tezroq)
  --allcaps     Qisqartmalarni ham tekshirish
```

Chiqish kodi: `0` — xato yoʻq, `1` — xato topildi, `2` — notoʻgʻri chaqiruv.
Namuna chiqish:

```
1:29    Kitobb    → Kitob, Kitoba, Kitobi
2:20    hatolik   → xatolik
5:36    togri     → toʻgʻri, togʻi, toʻri
```

## Eslatma

Hunspell **imlo** (orfografiya) xatolarini aniqlaydi — soʻz lugʻatda bor-yoʻqligini va
qoʻshimchalar toʻgʻri qoʻshilganini tekshiradi. Gap qurilishi darajasidagi **grammatik**
tahlil (kelishik moslashuvi, soʻz tartibi) uchun alohida qoidalar mexanizmi kerak boʻladi.
