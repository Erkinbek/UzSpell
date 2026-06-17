# UzSpell — modulli oʻrnatuvchi

Bitta `Setup.exe` ichida UzSpell'ning **barcha modullari**. Oʻrnatish vaqtida
foydalanuvchi qaysi modullarni xohlasa, oʻshalarni belgilab oʻrnatadi
(istalgan bittasi, bir nechtasi yoki hammasi). Tanlangan modullar darhol
ishlashga moslab roʻyxatga olinadi.

## Modullar

| Modul | Tavsifi | Oʻrnatishda nima boʻladi |
|-------|---------|---------------------------|
| **Ish stoli dasturi** | WPF GUI (imlo, grammatika, transliteratsiya) | Self-contained nusxa + Start menyu / ish stoli yorligʻi |
| **CLI** | `uzspell` buyruq qatori vositasi | Self-contained nusxa + (ixtiyoriy) PATH ga qoʻshish |
| **Word qoʻshimchasi** | Microsoft Word lentasidagi *UzSpell* boʻlimi | `regasm /codebase` + Word add-in registratsiyasi (HKCU) |
| **VS Code kengaytmasi** | `.vsix` | VS Code topilsa `code --install-extension` bilan oʻrnatiladi |
| **LibreOffice kengaytmasi** | `.oxt` lugʻatlar | LibreOffice topilsa `unopkg add` bilan oʻrnatiladi |
| **Brauzer kengaytmasi** | Chrome / Firefox | Fayllar koʻchiriladi + qoʻlda yuklash yoʻriqnomasi (brauzerlar avtomatik oʻrnatishga ruxsat bermaydi) |

Modul bor, lekin uning host dasturi (VS Code / LibreOffice) topilmasa, fayllar
`{app}` ichiga saqlanadi va foydalanuvchiga keyin qoʻlda oʻrnatish yoʻli
koʻrsatiladi.

## Yigʻish

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

Talablar: **.NET 10 SDK**, **Node.js + npm**, **winget** (Inno Setup'ni
avtomatik oʻrnatadi). Natija: `dist\UzSpell-Setup-<versiya>-x64.exe`.

Faqat kompilyatsiya (modullar allaqachon `staging\` da boʻlsa):

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1 -SkipBuild
```

## Fayllar

- `UzSpell.iss` — Inno Setup skripti (komponentlar, registratsiya, oʻchirish).
- `build-installer.ps1` — barcha modullarni yigʻib `staging\` ga joylaydi va ISCC bilan kompilyatsiya qiladi.
- `browser-yoriqnoma.txt` — brauzer kengaytmasini qoʻlda yuklash yoʻriqnomasi (oʻrnatuvchi ichiga kiradi).
- `staging\` — yigʻilgan modullar (git'ga kirmaydi).

## Oʻchirish

Boshqaruv paneli → Dasturlar orqali. Oʻchirishda Word COM sinfi bekor
qilinadi, VS Code / LibreOffice kengaytmalari olib tashlanadi, CLI PATH'dan
chiqariladi.
