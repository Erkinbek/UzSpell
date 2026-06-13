// Do'kon paketlarini yasaydi: store/UzSpell-chrome.zip, UzSpell-firefox.zip
// va AMO review uchun UzSpell-source.zip. Avval `npm run build` ishlatilgan boʻlsin.
//   node pack.mjs
// ZIP entry'lari forward-slash bilan yoziladi (do'kon validatorlari uchun).
import { execFileSync } from 'child_process';
import { existsSync, mkdirSync, rmSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';
import { platform } from 'os';

const root = dirname(fileURLToPath(import.meta.url));
const store = join(root, 'store');
mkdirSync(store, { recursive: true });

if (platform() === 'win32') {
  // Windows: pack.ps1 .NET ZipArchive bilan forward-slash entry yozadi
  execFileSync('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', join(root, 'pack.ps1')],
    { stdio: 'inherit' });
} else {
  // posix: `zip` CLI forward-slash ishlatadi
  const z = (cwd, out, args) => {
    if (existsSync(out)) rmSync(out, { force: true });
    execFileSync('zip', ['-r', '-q', out, ...args], { cwd, stdio: 'inherit' });
  };
  z(join(root, 'dist', 'chrome'), join(store, 'UzSpell-chrome.zip'), ['.']);
  z(join(root, 'dist', 'firefox'), join(store, 'UzSpell-firefox.zip'), ['.']);
  z(root, join(store, 'UzSpell-source.zip'),
    ['src', 'icons', 'build.mjs', 'pack.mjs', 'pack.ps1', 'manifest.base.json',
      'package.json', 'package-lock.json', 'README.md', 'PRIVACY.md', 'STORE.md', '../uz-hunspell']);
  console.log('Tayyor: store/UzSpell-chrome.zip, UzSpell-firefox.zip, UzSpell-source.zip');
}
