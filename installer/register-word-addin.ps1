# UzSpell Word add-in registratsiyasi (oʻrnatuvchi tomonidan elevated ishga tushiriladi).
#
# Word'da add-in koʻrinmasligining eng keng tarqalgan sababi: COM sinfi faqat
# 64-bit registr koʻrinishiga yoziladi, lekin Office 32-bit boʻladi (yoki aksincha).
# Shu sabab COM sinfini HAM 64-bit, HAM 32-bit koʻrinishga roʻyxatga olamiz va
# add-in kalitini mashina darajasida (HKLM, ikkala koʻrinish) + joriy foydalanuvchi
# (HKCU) ga yozamiz — bu barcha holatlarni qamrab oladi.
#
# Foydalanish:
#   powershell -ExecutionPolicy Bypass -File register-word-addin.ps1 -DllPath "<dll>"
#   powershell -ExecutionPolicy Bypass -File register-word-addin.ps1 -DllPath "<dll>" -Unregister

param(
    [string]$DllPath,
    [switch]$Unregister
)

$ErrorActionPreference = 'Continue'
$progId = 'UzSpell.WordAddin'

if (-not $DllPath) { $DllPath = Join-Path $PSScriptRoot 'UzSpell.WordAddin.dll' }
if (-not (Test-Path $DllPath)) {
    Write-Host "XATO: topilmadi: $DllPath"
    exit 1
}

$regasm64 = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'
$regasm32 = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\RegAsm.exe'

function Invoke-Regasm {
    param([string]$Exe, [switch]$Remove)
    if (-not (Test-Path $Exe)) { return }
    if ($Remove) {
        & $Exe "$DllPath" /unregister /nologo 2>&1 | Out-Null
    } else {
        & $Exe "$DllPath" /codebase /nologo 2>&1 | Out-Null
    }
}

# Word add-in kaliti yoziladigan joylar:
#   HKLM 64-bit, HKLM 32-bit (WOW6432Node), HKCU
$addinKeys = @(
    "HKLM:\SOFTWARE\Microsoft\Office\Word\Addins\$progId",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Office\Word\Addins\$progId",
    "HKCU:\SOFTWARE\Microsoft\Office\Word\Addins\$progId"
)

if ($Unregister) {
    Invoke-Regasm -Exe $regasm64 -Remove
    Invoke-Regasm -Exe $regasm32 -Remove
    foreach ($k in $addinKeys) {
        Remove-Item -Path $k -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-Host "UzSpell Word add-in roʻyxatdan chiqarildi."
    exit 0
}

# 1. COM sinfini ikkala registr koʻrinishiga roʻyxatga olish
Invoke-Regasm -Exe $regasm64
Invoke-Regasm -Exe $regasm32

# 2. Word add-in kalitlarini yozish (LoadBehavior=3 -> ishga tushganda yuklanadi)
$friendly = "UzSpell - o'zbek imlo va grammatika"
$descr    = "O'zbek tili uchun oflayn imlo, grammatika va transliteratsiya (uz-hunspell asosida)"
foreach ($k in $addinKeys) {
    New-Item -Path $k -Force | Out-Null
    New-ItemProperty -Path $k -Name 'FriendlyName' -PropertyType String -Value $friendly -Force | Out-Null
    New-ItemProperty -Path $k -Name 'Description'  -PropertyType String -Value $descr    -Force | Out-Null
    New-ItemProperty -Path $k -Name 'LoadBehavior' -PropertyType DWord  -Value 3         -Force | Out-Null
}

Write-Host "UzSpell Word add-in roʻyxatga olindi (32/64-bit + HKLM/HKCU)."
exit 0
