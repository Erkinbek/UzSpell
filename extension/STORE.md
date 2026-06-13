# UzSpell — Do'konga chiqarish bo'yicha qo'llanma (Chrome Web Store + Firefox AMO)

Bu fayl kengaytmani rasmiy do'konlarga yuklash uchun kerakli **hamma matn va
ko'rsatmalarni** o'z ichiga oladi. Paketlar tayyor: `extension/store/UzSpell-chrome.zip`
va `extension/store/UzSpell-firefox.zip` (versiya 1.4.0).

Paketni qayta yasash:
```bash
cd extension
npm install
npm run build           # dist/chrome va dist/firefox
node pack.mjs           # store/UzSpell-chrome.zip va UzSpell-firefox.zip
```

---

## 1. Umumiy listing matni

**Nomi (Name):**
> UzSpell — oʻzbekcha imlo va transliteratsiya

**Qisqa tavsif (Summary, ≤132 belgi):**
> Oʻzbek tili uchun 100% oflayn imlo va grammatika tekshiruvi. Inputlardagi xatolarni avtomatik belgilaydi. Lotin⇄kirill transliteratsiya.

**Kategoriya:** Productivity (Unumdorlik)
**Til:** Uzbek (oʻzbek)

**To'liq tavsif (Description):**
```
UzSpell — oʻzbek tili uchun toʻliq OFLAYN imlo, grammatika va transliteratsiya yordamchisi.
Internet umuman talab qilinmaydi: lugʻat va Hunspell dvigateli kengaytma ichida.

✦ INPUTLARDA AVTOMATIK TEKSHIRUV
Istalgan saytdagi matn maydoniga (izoh, xabar, email, forma) yozganda imlo va grammatika
xatolari avtomatik tagiga toʻlqinli chiziq bilan chiziladi — imlo qizil, grammatika koʻk.
Chiziqqa bossangiz, tuzatish takliflari chiqadi va bir bosishda almashtiradi.

✦ POPUP TEKSHIRUVCHI
Kengaytma belgisini bosib, matnni yozing yoki qoʻying — toʻliq tekshiruv va takliflar.

✦ LOTIN ⇄ KIRILL TRANSLITERATSIYA
Matnni belgilab, oʻng tugma → UzSpell → Lotin↔Kirill. Popup'da ham tugmalari bor.

✦ NIMALARNI USHLAYDI
• Imlo xatolari (95 000+ soʻzlik uz-hunspell lugʻati, lotin va kirill)
• Apostrof normalizatsiyasi (oʻzbek, o'zbek, o‘zbek — hammasi toʻgʻri)
• Ega–kesim moslashuvi (Men maktabga boradi → boraman)
• Koʻmakchi bilan kelishik (dars keyin → darsdan keyin)
• Son + ot birlikda (beshta olmalar → beshta olma)
• «-mi» yuklamasi, takror soʻz, bosh harf, punktuatsiya

✦ MAXFIYLIK
100% oflayn. Hech qanday maʼlumot yigʻilmaydi yoki yuborilmaydi. Tarmoq soʻrovlari yoʻq.

Lugʻatlar: uz-hunspell (Alisher «U2B3K» Jalolov, Bilolbek Normoʻminov) — GPL.
Imlo dvigateli: hunspell-asm (WASM).
Manba kodi: github.com/Erkinbek/UzSpell
```

---

## 2. Chrome Web Store

**Yuklash:** https://chrome.google.com/webstore/devconsole  (bir martalik $5 dasturchi toʻlovi)
**Paket:** `extension/store/UzSpell-chrome.zip`

**Single purpose (yagona maqsad):**
> Oʻzbek tili uchun oflayn imlo/grammatika tekshiruvi va lotin–kirill transliteratsiya.

**Ruxsatlar izohi (Permission justification) — har biri uchun:**

| Ruxsat | Izoh (review formasiga yoziladi) |
|---|---|
| `storage` | Foydalanuvchi sozlamasini (avtomatik tekshiruv yoqilgan/oʻchirilgani) va popup matnini shu qurilmada saqlash uchun. |
| `contextMenus` | Oʻng tugma menyusida transliteratsiya va tekshiruvni yoqish/oʻchirish tugmalari uchun. |
| `activeTab` | Foydalanuvchi menyudan tanlaganda faqat oʻsha faol varaqdagi belgilangan matnni almashtirish uchun. |
| `scripting` | Belgilangan matnni (transliteratsiya natijasini) varaqdagi maydonga qoʻyish uchun. |
| Host access `<all_urls>` (content script) | Jonli imlo/grammatika tekshiruvi istalgan saytdagi matn maydonida ishlashi uchun. Maʼlumot yigʻilmaydi — hammasi qurilmada, oflayn. |

**Remote code:** Yoʻq. Barcha kod paket ichida (WASM base64 inline). Tarmoq soʻrovlari yoʻq.
**Data usage:** "Does not collect user data" — belgilang. Hech narsa yigʻilmaydi/yuborilmaydi.
**Privacy policy URL:** `https://github.com/Erkinbek/UzSpell/blob/main/extension/PRIVACY.md`

---

## 3. Firefox AMO (addons.mozilla.org)

**Yuklash:** https://addons.mozilla.org/developers/  (bepul)
**Paket:** `extension/store/UzSpell-firefox.zip`
**Extension ID:** `uzspell@pardayev.uz` (manifest'da belgilangan)

**MUHIM — manba kodi talabi:** AMO bundlangan (esbuild) kodni qabul qilganda **manba
kodini** ham soʻraydi. Tayyor: `extension/store/UzSpell-source.zip` (yoki repo havolasi).
Build koʻrsatmasi (review uchun):
```
node 20+, npm install, npm run build  →  dist/firefox
```
Bu `src/*.js` ni esbuild bilan `*.bundle.js` ga aylantiradi. Minifikatsiya yoʻq (oʻqishga oson).

**Privacy policy:** PRIVACY.md matnini AMO maxfiylik maydoniga qoʻying (yoki havola).
**Notes to reviewer:**
> 100% offline. Engine = hunspell-asm (WASM, base64-inline). No network requests, no
> data collection. Dictionaries (uz-hunspell, GPL) bundled under dictionaries/.

---

## 4. Skrinshotlar (ikkala do'kon uchun, 1280×800 yoki 640×400)

Tavsiya etilgan 4 ta kadr:
1. Biror saytdagi `textarea`'da xato so'z tagida qizil to'lqinli chiziq + ochilgan taklif oynasi
2. Grammatika xatosi (ko'k chiziq), masalan "Men maktabga **boradi**"
3. Popup oynasi: matn + imlo/grammatika natijalari ro'yxati
4. O'ng tugma menyusi: UzSpell → Lotin↔Kirill / avtomatik tekshiruv toggle

Promo ikonka: `extension/icons/icon-128.png`.

---

## 5. Yuklashdan oldin tekshiruv ro'yxati (checklist)

- [ ] `npm run build` xatosiz
- [ ] `dist/chrome` va `dist/firefox` da `content.bundle.js`, `content.css`, `dictionaries/` bor
- [ ] Chrome: `chrome://extensions` → Load unpacked → inputda yozib, chiziq chiqishini ko'rish
- [ ] Firefox: `about:debugging` → Load Temporary Add-on → xuddi shunday sinash
- [ ] Versiya 1.4.0 ikkala manifest'da bir xil
- [ ] Privacy policy havolasi ishlayapti
- [ ] Skrinshotlar tayyor
