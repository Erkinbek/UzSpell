// UzSpell brauzer kengaytmasi build skripti.
// Chrome (MV3, service_worker) va Firefox (MV3, background.scripts) variantlarini yasaydi.
//   node build.mjs
import * as esbuild from 'esbuild';
import { readFileSync, writeFileSync, mkdirSync, rmSync, cpSync, existsSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';

const root = dirname(fileURLToPath(import.meta.url));
const src = join(root, 'src');
const dist = join(root, 'dist');
const dictSrc = join(root, '..', 'uz-hunspell');

const base = JSON.parse(readFileSync(join(root, 'manifest.base.json'), 'utf8'));

async function bundle(entry, outfile) {
  await esbuild.build({
    entryPoints: [join(src, entry)],
    bundle: true,
    format: 'iife',
    platform: 'browser',
    target: 'es2020',
    outfile,
    legalComments: 'none',
    minify: false,
    // hunspell-asm ESM build'ida nanoid namespace-call bug bor; CJS build toza
    // (browser field hali ham node->browser hunspell.js ni almashtiradi).
    mainFields: ['browser', 'main'],
  });
}

async function buildFor(browser, manifest) {
  const out = join(dist, browser);
  rmSync(out, { recursive: true, force: true });
  mkdirSync(out, { recursive: true });

  // JS bundling
  await bundle('popup.js', join(out, 'popup.bundle.js'));
  await bundle('background.js', join(out, 'background.bundle.js'));

  // Statik fayllar
  cpSync(join(src, 'popup.html'), join(out, 'popup.html'));
  cpSync(join(src, 'popup.css'), join(out, 'popup.css'));
  cpSync(join(root, 'icons'), join(out, 'icons'), { recursive: true });

  // Lugʻatlar
  const dd = join(out, 'dictionaries');
  mkdirSync(dd, { recursive: true });
  for (const f of ['uz_UZ.aff', 'uz_UZ.dic', 'uz_UZ_Cyrl.aff', 'uz_UZ_Cyrl.dic']) {
    cpSync(join(dictSrc, f), join(dd, f));
  }

  writeFileSync(join(out, 'manifest.json'), JSON.stringify(manifest, null, 2));
  console.log(`✓ ${browser}: ${out}`);
}

// popup.html dagi <script src> ni bundle nomiga moslash uchun nusxa olamiz
function fixPopupHtml(out) {
  const p = join(out, 'popup.html');
  let html = readFileSync(p, 'utf8');
  html = html.replace('src="../icons/icon-32.png"', 'src="icons/icon-32.png"');
  writeFileSync(p, html);
}

const chrome = {
  ...base,
  background: { service_worker: 'background.bundle.js' },
};

const firefox = {
  ...base,
  background: { scripts: ['background.bundle.js'] },
  browser_specific_settings: {
    gecko: { id: 'uzspell@erkinbek.uz', strict_min_version: '115.0' },
  },
};

await buildFor('chrome', chrome);
fixPopupHtml(join(dist, 'chrome'));
await buildFor('firefox', firefox);
fixPopupHtml(join(dist, 'firefox'));
console.log('\nBitdi. Yuklash:');
console.log('  Chrome:  chrome://extensions -> Developer mode -> Load unpacked -> dist/chrome');
console.log('  Firefox: about:debugging -> This Firefox -> Load Temporary Add-on -> dist/firefox/manifest.json');
