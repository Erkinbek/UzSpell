// UzSpell dvigatelining Node (VS Code) uchun moslamasi.
// Brauzer kengaytmasidagi bir xil engine.js'ni qayta ishlatadi — faqat
// fayllarni diskdan oʻqiydigan fetchBuf beriladi.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { createEngine } from '../../extension/src/engine.js';
import { toCyrillic, toLatin } from '../../extension/src/core.js';

export { toCyrillic, toLatin };

// extensionPath ichida `dictionaries/` papkasi boʻladi (build.mjs koʻchiradi).
export function createNodeEngine(extensionPath) {
  const fetchBuf = async (relPath) => {
    const abs = join(extensionPath, relPath);
    return new Uint8Array(readFileSync(abs));
  };
  return createEngine({ fetchBuf });
}
