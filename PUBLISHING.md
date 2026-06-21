# UzSpell — Doʻkonlarga nashr qilish qoʻllanmasi

Bu hujjat uchchala kengaytmani rasmiy doʻkonlarga chiqarishning **toʻliq, qadamba-qadam**
yoʻriqnomasi:

| Kengaytma | Papka | Doʻkon | Hisob narxi |
|---|---|---|---|
| **VS Code** | [vscode/](vscode/) | Visual Studio Marketplace | bepul |
| **Google Chrome** | [extension/](extension/) | Chrome Web Store | bir martalik **$5** |
| **Mozilla Firefox** | [extension/](extension/) | Firefox Add-ons (AMO) | bepul |

> Brauzer doʻkonlari (Chrome + Firefox) uchun listing matni, ruxsatlar izohi, skrinshot
> va checklist allaqachon [extension/STORE.md](extension/STORE.md) faylida tayyor. Bu yerda
> ularni qisqa takrorlab, asosiy eʼtiborni **hisob ochish va yuklash jarayoniga** qaratamiz.
> VS Code Marketplace uchun esa toʻliq yoʻriqnoma faqat shu yerda.

---

## 0. Bir martalik tayyorgarlik (har uchchasiga umumiy)

Kerakli vositalar:

```bash
node --version      # 20+ tavsiya etiladi
npm --version
```

Versiyani yangilashni unutmang — har bir yangi yuklashda raqam **oshib borishi** shart
(masalan `1.4.0` → `1.4.1`). Versiya joylari:

- VS Code: [vscode/package.json](vscode/package.json) → `"version"`
- Brauzer: [extension/manifest.base.json](extension/manifest.base.json) → `"version"`
  (build paytida `dist/chrome` va `dist/firefox` ga koʻchiriladi)

---

# 1. VS Code — Visual Studio Marketplace

## 1.1. Publisher (nashriyotchi) yaratish — bir marta

VS Code Marketplace Azure DevOps orqali ishlaydi.

1. **Microsoft/Azure hisobi** bilan kiring: <https://aka.ms/vscode-create-publisher>
2. **Create publisher** → ID kiriting. Loyiha allaqachon `pardayev` deb belgilangan
   ([vscode/package.json](vscode/package.json#L6) → `"publisher": "pardayev"`).
   - Bu ID `package.json` dagi `publisher` qiymatiga **aniq mos** boʻlishi shart.
   - Boshqa ID tanlasangiz, `package.json` dagi `publisher` ni ham oʻzgartiring.
3. Display name va logotip qoʻshing (ixtiyoriy, lekin tavsiya etiladi).

## 1.2. Personal Access Token (PAT) olish — bir marta

1. <https://dev.azure.com> ga oʻsha Microsoft hisobi bilan kiring (kerak boʻlsa tashkilot
   yarating).
2. Oʻng yuqori → **User settings** (⚙️) → **Personal access tokens** → **New Token**.
3. Sozlamalar:
   - **Organization:** `All accessible organizations` (muhim!)
   - **Expiration:** 1 yil (yoki Custom)
   - **Scopes:** `Custom defined` → **Marketplace** boʻlimida **Manage** ni belgilang.
4. **Create** → tokenni **nusxalab oling** (boshqa koʻrsatilmaydi, xavfsiz saqlang).

## 1.3. vsce bilan login

```bash
npm install -g @vscode/vsce        # global, bir marta
vsce login pardayev                # PAT soʻraydi — yuqoridagi tokenni qoʻying
```

## 1.4. Build, paketlash va yuklash

```bash
cd vscode
npm install
npm run build                      # dist/extension.js + dictionaries/ hosil qiladi
```

Marketplace **`repository` maydonini** talab qiladi — u allaqachon bor
([vscode/package.json](vscode/package.json#L11)).

**Variant A — toʻgʻridan-toʻgʻri nashr:**
```bash
vsce publish                       # build + upload (vscode:prepublish avtomatik ishlaydi)
```
Versiyani avtomatik oshirish ham mumkin: `vsce publish patch` (1.0.0 → 1.0.1).

**Variant B — avval .vsix yasab, soʻng yuklash (tavsiya — sinab koʻrish uchun):**
```bash
vsce package -o uzspell.vsix       # paketni hosil qiladi
# .vsix ni VS Code'da o'rnatib sinang: Extensions → ··· → Install from VSIX
vsce publish                       # tekshirgach yuklang
```

> Qoʻlda yuklash ham mumkin: <https://marketplace.visualstudio.com/manage> →
> publisher → **New extension** → `.vsix` ni tashlang.

## 1.5. Tavsiyalar va keng tarqalgan muammolar

- **Ikonka tayyor.** [vscode/icon.png](vscode/icon.png) (128×128) qoʻshilgan va
  `package.json` da `"icon": "icon.png"` belgilangan — Marketplace'da koʻrinadi.
- **README listing boʻladi.** [vscode/README.md](vscode/README.md) Marketplace sahifasida
  toʻliq koʻrsatiladi — relativ rasm havolalari ishlamaydi, mutlaq URL ishlating.
- **LICENSE fayli** boʻlsa avtomatik koʻrsatiladi (loyiha GPL-3.0).
- Xato: *"Missing publisher name"* → `vsce login <publisher>` qilinmagan.
- Xato: *"manifest references repository"* → `repository` maydoni borligini tekshiring (bor).
- Versiya allaqachon mavjud boʻlsa, raqamni oshiring.

---

# 2. Google Chrome — Chrome Web Store

## 2.1. Developer hisobi — bir marta

1. <https://chrome.google.com/webstore/devconsole> ga Google hisobi bilan kiring.
2. Bir martalik **$5** dasturchi toʻlovini toʻlang (kartani roʻyxatdan oʻtkazasiz).

## 2.2. Paketni yasash

```bash
cd extension
npm install
npm run build          # dist/chrome va dist/firefox
node pack.mjs          # store/UzSpell-chrome.zip (va firefox, source)
```
Natija: **`extension/store/UzSpell-chrome.zip`** — yuklanadigan fayl.

## 2.3. Yuklash

1. Dev Console → **Add new item** → `UzSpell-chrome.zip` ni tashlang.
2. **Store listing** maydonlarini [STORE.md](extension/STORE.md#L17) dan koʻchiring:
   - Nomi, qisqa tavsif (≤132 belgi), toʻliq tavsif
   - Kategoriya: **Productivity**, Til: **Uzbek**
   - Ikonka 128×128: [extension/icons/icon-128.png](extension/icons/icon-128.png)
   - Skrinshotlar (1280×800 yoki 640×400) — 4 ta kadr tavsiyasi STORE.md da.
3. **Privacy practices:**
   - **Single purpose:** STORE.md dagi matn.
   - **Permission justification:** har bir ruxsat uchun izoh
     ([STORE.md jadvali](extension/STORE.md#L70)).
   - **Data usage:** "Does not collect user data" ni belgilang (100% oflayn).
   - **Privacy policy URL:** `https://github.com/Erkinbek/UzSpell/blob/main/extension/PRIVACY.md`
4. **Remote code:** Yoʻq — barcha kod paket ichida (WASM inline).
5. **Submit for review.** Koʻrib chiqish odatda bir necha soat – bir necha kun.

> `<all_urls>` host ruxsati borligi sababli tekshiruv biroz uzayishi mumkin —
> izohda "jonli imlo tekshiruvi har qanday matn maydonida ishlashi uchun, maʼlumot
> yigʻilmaydi" deb aniq yozing.

---

# 3. Mozilla Firefox — Add-ons (AMO)

## 3.1. Developer hisobi — bir marta

1. <https://addons.mozilla.org/developers/> ga Firefox (Mozilla) hisobi bilan kiring (bepul).

## 3.2. Paketlar

`node pack.mjs` (yuqorida) ikki faylni tayyorlaydi:
- **`extension/store/UzSpell-firefox.zip`** — kengaytmaning oʻzi
- **`extension/store/UzSpell-source.zip`** — AMO talab qiladigan manba kodi

> Firefox manifestida `browser_specific_settings.gecko.id = uzspell@pardayev.uz` va
> `strict_min_version 115.0` belgilangan — bu build paytida avtomatik qoʻshiladi.

## 3.3. Yuklash

1. Dev Hub → **Submit a New Add-on** → **On this site** (AMO da joylashtirish).
2. `UzSpell-firefox.zip` ni yuklang. Validator ogohlantirishlarini koʻrib chiqing.
3. **Manba kodi (muhim!):** AMO bundlangan/minifikatsiyalangan kodni koʻrganda manba
   kodini soʻraydi → `UzSpell-source.zip` ni yuklang.
   **Build koʻrsatmasi (reviewerga):**
   ```
   node 20+, `npm install`, `npm run build`  →  dist/firefox
   esbuild src/*.js → *.bundle.js (minifikatsiyasiz)
   ```
4. **Listing** maydonlari (nomi, tavsif, kategoriya, skrinshot) — STORE.md dan.
5. **Privacy policy:** [extension/PRIVACY.md](extension/PRIVACY.md) matnini qoʻying yoki havola bering.
6. **Notes to reviewer:**
   > 100% offline. Engine = hunspell-asm (WASM, base64-inline). No network requests, no
   > data collection. Dictionaries (uz-hunspell, GPL) bundled under dictionaries/.
7. **Submit.** AMO da koʻrib chiqish odatda Chrome'dan tezroq.

---

# 4. Yakuniy checklist (har bir nashrdan oldin)

- [ ] Versiya raqami oshirildi (VS Code: package.json; brauzer: manifest.base.json)
- [ ] `npm run build` xatosiz oʻtdi (har ikkala papkada)
- [ ] Brauzer: `node pack.mjs` → `store/` da 3 ta zip yangilandi
- [ ] Chrome'da `chrome://extensions` → *Load unpacked* (`dist/chrome`) bilan sinaldi
- [ ] Firefox'da `about:debugging` → *Load Temporary Add-on* (`dist/firefox`) bilan sinaldi
- [ ] VS Code: `.vsix` lokal oʻrnatib sinaldi (`Install from VSIX`)
- [ ] Privacy policy havolasi ochiladi
- [ ] Skrinshotlar tayyor (brauzer doʻkonlari uchun)
- [ ] Git'da versiya commit + tag qoʻyildi

---

## Tezkor buyruqlar

```bash
# VS Code
cd vscode && npm install && npm run build && vsce publish

# Brauzerlar (Chrome + Firefox paketlari)
cd extension && npm install && npm run build && node pack.mjs
# → store/UzSpell-chrome.zip, UzSpell-firefox.zip, UzSpell-source.zip
```

Batafsil brauzer listing matni: [extension/STORE.md](extension/STORE.md).
