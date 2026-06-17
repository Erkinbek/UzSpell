// Node smoke-test: dvigatel diskdan lugʻ atlarni yuklab, matnni tekshira oladimi?
// Ishga tushirish: node vscode/smoke-test.mjs
import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createEngine } from '../extension/src/engine.js';

const root = dirname(fileURLToPath(import.meta.url));
const dicDir = join(root, '..', 'uz-hunspell');

const fetchBuf = async (relPath) => {
  const name = relPath.split('/').pop();
  return new Uint8Array(readFileSync(join(dicDir, name)));
};

const engine = createEngine({ fetchBuf });

const text = 'Bu yerda hatolik bor. Men maktabga boradi. togri yozing.';
const errors = await engine.check(text, { script: 'auto', grammar: true });
console.log(`Topildi: ${errors.length} ta`);
for (const e of errors) {
  const sug = (e.suggestions || []).slice(0, 4).join(', ');
  console.log(`  ${e.type}\t"${e.word}"\t${e.message || ''}\t→ ${sug}`);
}
if (errors.length === 0) {
  console.error('XATO: hech narsa topilmadi — dvigatel ishlamayapti?');
  process.exit(1);
}
console.log('OK — dvigatel Node muhitida ishladi.');
