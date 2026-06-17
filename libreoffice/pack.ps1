# UzSpell LibreOffice imlo lugʻati kengaytmasini (.oxt) yigʻadi.
# .oxt — bu oddiy ZIP; LibreOffice'da ikki marta bosib oʻrnatiladi.
# Foydalanish:  powershell -ExecutionPolicy Bypass -File libreoffice\pack.ps1

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root    = Split-Path -Parent $PSScriptRoot          # repo ildizi
$srcDir  = $PSScriptRoot                              # libreoffice\
$dicDir  = Join-Path $root 'uz-hunspell'
$outDir  = Join-Path $root 'dist'
$oxtPath = Join-Path $outDir 'UzSpell-libreoffice.oxt'

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
if (Test-Path $oxtPath) { Remove-Item $oxtPath -Force }

# (ZIP ichidagi yoʻl) => (diskdagi manba fayl)
$entries = [ordered]@{
    'description.xml'        = Join-Path $srcDir 'description.xml'
    'META-INF/manifest.xml'  = Join-Path $srcDir 'META-INF\manifest.xml'
    'dictionaries.xcu'       = Join-Path $srcDir 'dictionaries.xcu'
    'desc-uz.txt'            = Join-Path $srcDir 'desc-uz.txt'
    'desc-en.txt'            = Join-Path $srcDir 'desc-en.txt'
    'LICENSE.txt'            = Join-Path $dicDir 'LICENSE'
    'uz_UZ.aff'              = Join-Path $dicDir 'uz_UZ.aff'
    'uz_UZ.dic'              = Join-Path $dicDir 'uz_UZ.dic'
    'uz_UZ_Cyrl.aff'         = Join-Path $dicDir 'uz_UZ_Cyrl.aff'
    'uz_UZ_Cyrl.dic'         = Join-Path $dicDir 'uz_UZ_Cyrl.dic'
}

foreach ($src in $entries.Values) {
    if (-not (Test-Path $src)) {
        throw "Manba fayl topilmadi: $src  (submodule yuklanganmi? git submodule update --init)"
    }
}

$zip = [System.IO.Compression.ZipFile]::Open($oxtPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in $entries.Keys) {
        # Forward-slash entry nomlari (validatorlar uchun ishonchli)
        $entry = $zip.CreateEntry($name, [System.IO.Compression.CompressionLevel]::Optimal)
        $stream = $entry.Open()
        try {
            $bytes = [System.IO.File]::ReadAllBytes($entries[$name])
            $stream.Write($bytes, 0, $bytes.Length)
        } finally {
            $stream.Dispose()
        }
    }
} finally {
    $zip.Dispose()
}

$sizeKb = [math]::Round((Get-Item $oxtPath).Length / 1KB)
Write-Output "Tayyor: $oxtPath ($sizeKb KB)"
Write-Output "Oʻrnatish: LibreOffice'da faylni oching yoki Tools > Extension Manager > Add."
