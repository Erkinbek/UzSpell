# UzSpell Word add-in o'rnatish skripti
#
# Administrator bo'lsa -> COM sinfi HKLM (mashina) ga yoziladi: Word'ning ham
#   oddiy, ham administrator rejimida ishlaydi.
# Administrator bo'lmasa -> HKCU (joriy foydalanuvchi) ga yoziladi: oddiy
#   (elevated bo'lmagan) Word'da ishlaydi. Bu eng keng tarqalgan holat.
#
# Foydalanish:
#   powershell -ExecutionPolicy Bypass -File scripts\install-word-addin.ps1
#
# Oldindan dist\WordAddin papkasi tayyor bo'lishi kerak:
#   dotnet publish src\UzSpell.WordAddin -c Release -o dist\WordAddin -f net48

param(
    [string]$SourceDir
)

$ErrorActionPreference = 'Stop'

$dllName = 'UzSpell.WordAddin.dll'
$progId = 'UzSpell.WordAddin'

# Manba papkani aniqlash: parametr -> skript yonida -> repo tartibi (dist\WordAddin)
if (-not $SourceDir) {
    if (Test-Path (Join-Path $PSScriptRoot $dllName)) {
        $SourceDir = $PSScriptRoot
    } else {
        $SourceDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'dist\WordAddin'
    }
}

if (-not (Test-Path (Join-Path $SourceDir $dllName))) {
    Write-Host "XATO: $SourceDir papkasida $dllName topilmadi." -ForegroundColor Red
    Write-Host "Avval quyidagini bajaring:"
    Write-Host "  dotnet publish src\UzSpell.WordAddin -c Release -o dist\WordAddin -f net48"
    exit 1
}

# 1. Fayllarni doimiy joyga nusxalash
$target = Join-Path $env:LOCALAPPDATA 'UzSpell\WordAddin'
Write-Host "Fayllar nusxalanmoqda: $target"
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item (Join-Path $SourceDir '*') $target -Recurse -Force

$dllPath = Join-Path $target $dllName

# RegAsm.exe ni topish
$regasm = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'
if (-not (Test-Path $regasm)) {
    $regasm = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\RegAsm.exe'
}
if (-not (Test-Path $regasm)) {
    Write-Host "XATO: RegAsm.exe topilmadi (.NET Framework 4.x kerak)." -ForegroundColor Red
    exit 1
}

# Administrator huquqini aniqlash
$isAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# 2. COM sinfini ro'yxatga olish
# RegAsm imzosiz assembly uchun stderr ga ogohlantirish yozadi; ErrorActionPreference
# = Stop bunda skriptni uzib qo'ymasligi uchun shu blokda Continue ga o'tkazamiz.
$prevEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
if ($isAdmin) {
    Write-Host "Administrator aniqlandi -> COM sinfi mashina darajasida (HKLM) ro'yxatga olinmoqda..."
    & $regasm $dllPath /codebase | Out-Null
} else {
    Write-Host "COM sinfi joriy foydalanuvchi uchun (HKCU) ro'yxatga olinmoqda..."
    $regFile = Join-Path $env:TEMP 'uzspell-addin.reg'
    & $regasm $dllPath /codebase /regfile:$regFile | Out-Null
    # HKEY_CLASSES_ROOT -> HKEY_CURRENT_USER\Software\Classes (admin huquqisiz ishlashi uchun)
    (Get-Content $regFile) -replace '\[HKEY_CLASSES_ROOT', '[HKEY_CURRENT_USER\Software\Classes' |
        Set-Content $regFile
    $null = cmd /c "reg import `"$regFile`" 2>&1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "XATO: registrga import muvaffaqiyatsiz." -ForegroundColor Red
        exit 1
    }
    Remove-Item $regFile -Force
}
$ErrorActionPreference = $prevEAP

# 3. Word add-in sifatida ro'yxatga olish (har doim HKCU - joriy foydalanuvchi)
$addinKey = "HKCU:\Software\Microsoft\Office\Word\Addins\$progId"
Write-Host "Word add-in ro'yxatga olinmoqda..."
New-Item -Path $addinKey -Force | Out-Null
New-ItemProperty -Path $addinKey -Name 'FriendlyName' -PropertyType String -Force `
    -Value "UzSpell - o'zbek imlo va grammatika" | Out-Null
New-ItemProperty -Path $addinKey -Name 'Description' -PropertyType String -Force `
    -Value "O'zbek tili uchun oflayn imlo va grammatika tekshiruvchisi (uz-hunspell asosida)" | Out-Null
New-ItemProperty -Path $addinKey -Name 'LoadBehavior' -PropertyType DWord -Value 3 -Force | Out-Null

Write-Host ""
Write-Host "Tayyor! Word'ni yopib qayta oching - lentada 'UzSpell' bo'limi paydo bo'ladi." -ForegroundColor Green
Write-Host ""
Write-Host "Agar bo'lim ko'rinmasa: Fayl -> Parametrlar -> Qo'shimchalar (Add-ins) ->"
Write-Host "pastda 'COM Add-ins' -> O'tish (Go) -> UzSpell belgilangan bo'lishini tekshiring."
