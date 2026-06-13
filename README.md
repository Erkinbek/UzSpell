# UzSpell — oʻzbek tili uchun oflayn imlo va grammatika tekshiruvchi

[uz-hunspell](https://github.com/u2b3k/uz-hunspell) lugʻatlari (95 000+ soʻz, lotin va kirill)
asosida qurilgan, **100% oflayn** ishlaydigan imlo va grammatika tekshiruvchi. Internet umuman talab qilinmaydi.

## Tarkibi

| Loyiha | Tavsif |
|---|---|
| `src/UzSpell.Core` | Tekshiruv mexanizmi: tokenizer, apostrof normalizatsiyasi, lotin/kirill avto-aniqlash, Hunspell imlo tekshiruvi, takliflar, **grammatika qoidalari** va **transliteratsiya** |
| `src/UzSpell.App` | Oynali dastur (WPF): xatolar toʻlqinli chiziq bilan koʻrsatiladi (qizil — imlo, koʻk — grammatika), takliflar paneli, oʻng tugma menyusi, **MS Word integratsiyasi** |
| `src/UzSpell.WordAddin` | **Word lentasidagi haqiqiy add-in** — «UzSpell» boʻlimi, tugmalar, takliflar oynasi (COM, VSTO talab qilinmaydi) |
| `src/UzSpell.Cli` | Terminal vositasi: fayl yoki stdin orqali tekshirish |
| `extension/` | **Chrome/Firefox brauzer kengaytmasi** (oflayn, hunspell-asm WASM) — sahifa inputlarida **avtomatik** imlo/grammatika tekshiruvi (xatolar tagiga chiziladi), popup va transliteratsiya — [batafsil](extension/README.md) |
| `uz-hunspell/` | Asl lugʻat fayllari (submodule) |
| `dist/` | Tayyor Release fayllar |

## Yuklab olish (internetsiz kompyuterlar uchun ham)

Tayyor ZIP'lar — [**Releases sahifasida**](https://github.com/Erkinbek/UzSpell/releases/latest):

| Fayl | Nima | Talablar |
|---|---|---|
| [UzSpell-win-x64.zip](https://github.com/Erkinbek/UzSpell/releases/download/v1.0.0/UzSpell-win-x64.zip) | Oynali dastur | **Hech narsa** — .NET runtime ichida |
| [UzSpell-WordAddin.zip](https://github.com/Erkinbek/UzSpell/releases/download/v1.0.0/UzSpell-WordAddin.zip) | Word lenta add-in'i | Word + .NET Framework 4.8 (Windows 10/11 da bor) |
| [uzspell-cli-win-x64.zip](https://github.com/Erkinbek/UzSpell/releases/download/v1.0.0/uzspell-cli-win-x64.zip) | Terminal vositasi | **Hech narsa** — .NET runtime ichida |

Internetsiz kompyuterga oʻrnatish: ZIP'ni fleshkada olib oʻting →
- **Oynali dastur:** ochib `UzSpell\UzSpell.exe` ni ishga tushiring — oʻrnatish shart emas
- **Word add-in:** ochilgan papkada `install-word-addin.ps1` ni ishga tushiring:
  ```powershell
  powershell -ExecutionPolicy Bypass -File install-word-addin.ps1
  ```

> Eslatma: `dist\` dagi oddiy buildlar .NET 10 oʻrnatilgan boʻlishini talab qiladi.
> Releases'dagi ZIP'lar esa **self-contained** — hech qanday qoʻshimcha dastursiz ishlaydi.

## Ishga tushirish (manbadan)

Tayyor fayllar:

- **Oynali dastur:** `dist\UzSpell\UzSpell.exe`
- **Terminal:** `dist\uzspell-cli\uzspell.exe matn.txt`
- **Word add-in:** `scripts\install-word-addin.ps1` (pastga qarang)

Manbadan qurish (.NET 10 SDK kerak; Word add-in uchun .NET Framework 4.8 ham):

```powershell
git clone --recurse-submodules https://github.com/Erkinbek/UzSpell.git
cd UzSpell
dotnet build
dotnet run --project src\UzSpell.App           # oynali dastur
dotnet run --project src\UzSpell.Cli -- namuna.txt
```

## Imkoniyatlar

### Imlo
- **Lotin va kirill** yozuvlari, har bir soʻz uchun avtomatik aniqlanadi (yoki qoʻlda tanlanadi)
- **Apostrof normalizatsiyasi** — `o'zbek`, `o`zbek`, `o‘zbek` kabi yozilishlar ham toʻgʻri deb qabul qilinadi
  (lugʻatdagi kanonik belgilar: `ʻ` U+02BB va tutuq `ʼ` U+02BC); takliflar kanonik koʻrinishda beriladi
- **Juft soʻzlar** (`katta-katta`) va **son shakllari** (`beshta`, `1995-yil`) qoʻllab-quvvatlanadi
- **Takliflar** — har bir xato uchun 6 tagacha tuzatish varianti
- **Shaxsiy lugʻat** — `%APPDATA%\UzSpell\custom_words.txt` ga doimiy saqlanadi
- **BOSH HARFLI** qisqartmalar (AQSH, BMT) sukut boʻyicha tekshirilmaydi

### Grammatika (gap qurilishi)
Lugʻatdagi morfologik maʼlumotdan foydalangan, yuqori aniqlikka moʻljallangan qoidalar:

| Qoida | Misol (xato → tuzatish) |
|---|---|
| Ega–kesim shaxs-son moslashuvi | *Men maktabga **boradi*** → boraman |
| Koʻmakchi bilan kelishik | *dars **keyin*** → darsdan keyin · *ariza **muvofiq*** → arizaga muvofiq |
| Son + ot birlikda | *beshta **olmalar*** → beshta olma |
| «-mi» yuklamasi qoʻshib | *berdi **mi*** → berdimi |
| Takror soʻz | *juda **juda*** → juda |
| Gap bosh harfdan | *…bor. **matn**…* → Matn |
| Punktuatsiya | belgidan oldin/keyin boʻshliq, ortiqcha boʻshliq |

> Eslatma: grammatika qoidalari shubhali holatlarda ataylab jim qoladi (notoʻgʻri ogohlantirishlardan saqlanish uchun).

## MS Word integratsiyasi — ikki usul

### 1-usul: Word lentasidagi add-in (tavsiya etiladi)

Oʻrnatish (Word yopiq boʻlsin):

```powershell
dotnet publish src\UzSpell.WordAddin -c Release -o dist\WordAddin -f net48
powershell -ExecutionPolicy Bypass -File scripts\install-word-addin.ps1
```

Word'ni oching — lentada **«UzSpell»** boʻlimi paydo boʻladi:

- **Tekshirish** — hujjatni tekshiradi, imlo xatolarini qizil, grammatikani koʻk
  toʻlqinli chiziq bilan belgilaydi va takliflar oynasini ochadi
- **Xatolar roʻyxati** — oxirgi natijalar oynasini koʻrsatadi (taklif bosilsa hamma joyda almashtiriladi, ikki marta bosilsa hujjatda topadi)
- **Belgilarni tozalash** — toʻlqinli belgilashlarni olib tashlaydi

Oʻchirish: `powershell -ExecutionPolicy Bypass -File scripts\uninstall-word-addin.ps1`

> Administrator boʻlsangiz COM sinfi mashina darajasida (HKLM), aks holda joriy
> foydalanuvchi uchun (HKCU) roʻyxatga olinadi — ikkala holatda ham oddiy Word'da ishlaydi.
> VSTO yoki Office plagin SDK talab qilinmaydi; faqat Word oʻrnatilgan boʻlsa kifoya.

### 2-usul: oynali dasturdan

UzSpell.exe'da **«📝 Word hujjatini tekshirish»** tugmasi ochiq Word hujjatini
tekshiradi va xuddi shunday belgilaydi/almashtiradi. `.docx` faylni Word'siz ham oʻqiy oladi.

## Terminal (CLI)

```
uzspell <fayl.txt> [parametrlar]

  --lotin          Faqat lotin lugʻati
  --kirill         Faqat kirill lugʻati
  --taklifsiz      Takliflarsiz (tezroq)
  --allcaps        Qisqartmalarni ham tekshirish
  --grammatikasiz  Faqat imlo (grammatika qoidalarisiz)
```

Chiqish kodi: `0` — xato yoʻq, `1` — xato topildi, `2` — notoʻgʻri chaqiruv.
Namuna chiqish (`[imlo]` va `[gram]` belgilari bilan):

```
1:29   [imlo]   Kitobb     → Kitob, Kitoba, Kitobi
2:20   [imlo]   hatolik    → xatolik
1:13   [gram]   boradi     Ega («Men») bilan kesim mos kelmayapti → boraman
3:17   [gram]   olmalar    Sondan keyin ot birlikda → olma
```

## Texnik eslatma

Imlo tekshiruvi [WeCantSpell.Hunspell](https://www.nuget.org/packages/WeCantSpell.Hunspell)
(toza .NET, tashqi bogʻliqliksiz) orqali bajariladi. Grammatika qoidalari lugʻatdagi
soʻz turkumi flaglariga (`X` — feʼl, `V`/`S` — ot/koʻplik) tayanadi. Bu chuqur sintaktik
tahlil emas, balki keng tarqalgan xatolarni yuqori aniqlik bilan ushlaydigan qoidalar toʻplami.

## Minnatdorchilik

- Loyihaning poydevori — [uz-hunspell](https://github.com/u2b3k/uz-hunspell) lugʻatlari.
  Mualliflar **Alisher "U2B3K" Jalolov** va **Bilolbek "itsbilolbek" Normoʻminov**ga
  90 000+ soʻzlik lugʻat hamda OT, SIFAT va FEʼL qoʻshimcha qoidalarini ochiq manba
  qilib ulashganlari uchun katta rahmat — ularsiz bu dastur boʻlmasdi. 🙏
- [WeCantSpell.Hunspell](https://github.com/aarondandy/WeCantSpell.Hunspell) — Hunspell'ning
  sof .NET portati uchun Aaron Dandy'ga rahmat.

## Qanday yaratilgan

Loyiha kodi [Claude Code](https://claude.com/claude-code) (Anthropic) yordamida yozilgan —
**Claude Opus 4.8** va **Claude Fable 5** modellari bilan: arxitektura, imlo/grammatika
mexanizmi, WPF dastur, Word COM add-in va uning tuzatishlari (SAFEARRAY marshaling,
DISPID'lar) shu vositada ishlab chiqilgan.
