# UzSpell Word add-in o'chirish skripti
# HKLM va HKCU dagi ro'yxatlarning ikkalasini ham tozalaydi.
#
# Foydalanish:
#   powershell -ExecutionPolicy Bypass -File scripts\uninstall-word-addin.ps1

$ErrorActionPreference = 'SilentlyContinue'

$progId = 'UzSpell.WordAddin'
$clsid = '{A1B2C3D4-E5F6-47A8-9B0C-1D2E3F4A5B6C}'

Write-Host "Word add-in ro'yxatdan o'chirilmoqda..."
Remove-Item "HKCU:\Software\Microsoft\Office\Word\Addins\$progId" -Recurse -Force

Write-Host "COM ro'yxati tozalanmoqda..."
# RegAsm bilan to'g'ri o'chirishga urinish (HKLM uchun)
$target = Join-Path $env:LOCALAPPDATA 'UzSpell\WordAddin'
$dll = Join-Path $target 'UzSpell.WordAddin.dll'
$regasm = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'
if ((Test-Path $regasm) -and (Test-Path $dll)) {
    & $regasm $dll /unregister 2>&1 | Out-Null
}

# Qolgan izlarni qo'lda tozalash (HKLM va HKCU)
foreach ($root in 'HKCU:\Software\Classes', 'HKLM:\Software\Classes', 'HKLM:\Software\WOW6432Node\Classes') {
    Remove-Item "$root\$progId" -Recurse -Force
    Remove-Item "$root\CLSID\$clsid" -Recurse -Force
}

if (Test-Path $target) {
    Write-Host "Fayllar o'chirilmoqda: $target"
    Remove-Item $target -Recurse -Force
}

Write-Host "O'chirildi. Word'ni qayta oching." -ForegroundColor Green
