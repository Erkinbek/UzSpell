# Do'kon paketlarini ZIP spec'iga mos (forward-slash) yasaydi.
# Windows PowerShell 5.1'ning Compress-Archive backslash ishlatadi — do'kon validatorlari
# rad etishi mumkin. Bu skript .NET ZipArchive bilan entry nomlarini '/' bilan yozadi.
# Avval `npm run build` ishlatilgan boʻlsin.  Ishga tushirish:  powershell -File pack.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null

$root  = $PSScriptRoot
$store = Join-Path $root 'store'
New-Item -ItemType Directory -Force -Path $store | Out-Null

function New-Zip([string]$dst) {
  if (Test-Path $dst) { Remove-Item $dst -Force }
  [System.IO.Compression.ZipFile]::Open($dst, 'Create')
}
function Add-FileToZip($zip, [string]$file, [string]$entry) {
  $e = $entry -replace '\\', '/'
  [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file, $e, 'Optimal') | Out-Null
}

# Bir papka ichidagi hamma fayl (manifest.json ildizda boʻladi)
function Zip-DirContents([string]$srcDir, [string]$dst) {
  if (-not (Test-Path $srcDir)) { throw "Topilmadi: $srcDir (avval: npm run build)" }
  $zip = New-Zip $dst
  try {
    $base = (Resolve-Path $srcDir).Path.TrimEnd('\')
    Get-ChildItem -Path $srcDir -Recurse -File | ForEach-Object {
      $rel = $_.FullName.Substring($base.Length).TrimStart('\')
      Add-FileToZip $zip $_.FullName $rel
    }
  } finally { $zip.Dispose() }
}

# AMO review uchun manba (rebuild qilish uchun kerakli fayllar)
function Zip-Source([string]$dst) {
  $zip = New-Zip $dst
  try {
    $items = @('src', 'icons', 'build.mjs', 'pack.mjs', 'pack.ps1', 'manifest.base.json',
      'package.json', 'package-lock.json', 'README.md', 'PRIVACY.md', 'STORE.md')
    foreach ($it in $items) {
      $p = Join-Path $root $it
      if (Test-Path $p -PathType Container) {
        $base = (Resolve-Path $root).Path.TrimEnd('\')
        Get-ChildItem -Path $p -Recurse -File | ForEach-Object {
          Add-FileToZip $zip $_.FullName $_.FullName.Substring($base.Length).TrimStart('\')
        }
      } elseif (Test-Path $p) {
        Add-FileToZip $zip $p $it
      }
    }
    # Lugʻatlar (uz-hunspell) — build.mjs shulardan dist'ga nusxa oladi
    $dict = (Resolve-Path (Join-Path $root '..\uz-hunspell')).Path.TrimEnd('\')
    foreach ($d in 'uz_UZ.aff', 'uz_UZ.dic', 'uz_UZ_Cyrl.aff', 'uz_UZ_Cyrl.dic') {
      $dp = Join-Path $dict $d
      if (Test-Path $dp) { Add-FileToZip $zip $dp ('uz-hunspell/' + $d) }
    }
  } finally { $zip.Dispose() }
}

Zip-DirContents (Join-Path $root 'dist\chrome')  (Join-Path $store 'UzSpell-chrome.zip')
Zip-DirContents (Join-Path $root 'dist\firefox') (Join-Path $store 'UzSpell-firefox.zip')
Zip-Source (Join-Path $store 'UzSpell-source.zip')
Write-Host 'Tayyor: store/UzSpell-chrome.zip, UzSpell-firefox.zip, UzSpell-source.zip'
