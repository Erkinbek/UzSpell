// VS Code kengaytmasini yigʻadi: esbuild bilan bitta CJS bundle + lugʻatlarni koʻchirish.
// Yadro (engine.js, core.js, grammar.js) brauzer kengaytmasidan qayta ishlatiladi;
// hunspell-asm tashqi qoldiriladi (node_modules ichidan oʻz wasm'ini yuklaydi).
import { build } from 'esbuild';
import { cpSync, mkdirSync, rmSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const dist = join(root, 'dist');
rmSync(dist, { recursive: true, force: true });
mkdirSync(dist, { recursive: true });

await build({
  entryPoints: [join(root, 'src', 'extension.js')],
  bundle: true,
  platform: 'node',
  format: 'cjs',
  target: 'node18',
  outfile: join(dist, 'extension.js'),
  external: ['vscode', 'hunspell-asm'],
  logLevel: 'info',
});

// Lugʻatlarni kengaytma ildizidagi dictionaries/ ga koʻchiramiz
const dicSrc = join(root, '..', 'uz-hunspell');
const dicDst = join(root, 'dictionaries');
mkdirSync(dicDst, { recursive: true });
for (const f of ['uz_UZ.aff', 'uz_UZ.dic', 'uz_UZ_Cyrl.aff', 'uz_UZ_Cyrl.dic']) {
  cpSync(join(dicSrc, f), join(dicDst, f));
}

console.log('UzSpell VS Code: build tayyor (dist/extension.js + dictionaries/).');
