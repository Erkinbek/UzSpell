<#
.SYNOPSIS
  UzSpell modulli oʻrnatuvchisini (Setup.exe) yigʻadi.

.DESCRIPTION
  Barcha modullarni "installer\staging\" papkasiga tayyorlaydi, soʻng Inno Setup
  (ISCC) bilan kompilyatsiya qilib, dist\UzSpell-Setup-<ver>-x64.exe hosil qiladi.

  Bosqichlar:
    1. Ish stoli dasturi (GUI)   -> staging\app   (self-contained win-x64)
    2. CLI                        -> staging\cli   (self-contained win-x64)
    3. Word qoʻshimchasi          -> staging\word  (net48)
    4. Brauzer kengaytmasi        -> staging\browser (chrome + firefox + yoʻriqnoma)
    5. VS Code kengaytmasi        -> staging\vscode\uzspell.vsix
    6. LibreOffice kengaytmasi    -> staging\libre\UzSpell.oxt
    7. Inno Setup taʼminlanadi va installer kompilyatsiya qilinadi.

.PARAMETER Version
  Oʻrnatuvchi versiyasi. Default: 1.5.2

.PARAMETER SkipBuild
  Modullarni qayta yigʻmasdan faqat ISCC ni ishga tushiradi (staging mavjud boʻlsa).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
#>
param(
    [string]$Version = "1.5.2",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$InstallerDir = $PSScriptRoot
$RepoRoot     = Split-Path $InstallerDir -Parent
$Staging      = Join-Path $InstallerDir 'staging'

function Step($msg) { Write-Host "`n==== $msg ====" -ForegroundColor Cyan }
function Info($msg) { Write-Host "  $msg" -ForegroundColor Gray }

function Invoke-Native {
    param([string]$Exe, [string[]]$CmdArgs, [string]$Cwd = $RepoRoot)
    Push-Location $Cwd
    try {
        & $Exe @CmdArgs
        if ($LASTEXITCODE -ne 0) {
            throw "'$Exe $($CmdArgs -join ' ')' xatolik bilan tugadi (kod $LASTEXITCODE)."
        }
    } finally {
        Pop-Location
    }
}

# ---------------------------------------------------------------------------
if (-not $SkipBuild) {
    Step "Staging papkasi tozalanmoqda"
    if (Test-Path $Staging) { Remove-Item $Staging -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $Staging | Out-Null

    # --- Litsenziya (Inno Setup LicenseFile uchun) ---
    $licSrc = Join-Path $RepoRoot 'uz-hunspell\LICENSE'
    $licDst = Join-Path $Staging 'LICENSE.txt'
    if (Test-Path $licSrc) {
        Copy-Item $licSrc $licDst -Force
    } else {
        "UzSpell — GPL litsenziyali uz-hunspell lugʻatlari asosida." | Set-Content $licDst -Encoding UTF8
    }

    # === 1. Ish stoli dasturi (GUI) =======================================
    Step "1/6  Ish stoli dasturi yigʻilmoqda (self-contained win-x64)"
    Invoke-Native 'dotnet' @(
        'publish', 'src\UzSpell.App\UzSpell.App.csproj',
        '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
        '-p:PublishSingleFile=false', '-p:DebugType=none',
        '-o', (Join-Path $Staging 'app')
    )

    # === 2. CLI ============================================================
    Step "2/6  CLI yigʻilmoqda (self-contained win-x64)"
    Invoke-Native 'dotnet' @(
        'publish', 'src\UzSpell.Cli\UzSpell.Cli.csproj',
        '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
        '-p:PublishSingleFile=false', '-p:DebugType=none',
        '-o', (Join-Path $Staging 'cli')
    )

    # === 3. Word qoʻshimchasi (net48) ======================================
    Step "3/6  Word qoʻshimchasi yigʻilmoqda (net48)"
    Invoke-Native 'dotnet' @(
        'publish', 'src\UzSpell.WordAddin\UzSpell.WordAddin.csproj',
        '-c', 'Release', '-f', 'net48',
        '-o', (Join-Path $Staging 'word')
    )
    # Word registratsiya skripti (oʻrnatuvchi uni ishga tushiradi)
    Copy-Item (Join-Path $InstallerDir 'register-word-addin.ps1') `
        (Join-Path $Staging 'word\register-word-addin.ps1') -Force

    # === 4. Brauzer kengaytmasi ===========================================
    Step "4/6  Brauzer kengaytmasi yigʻilmoqda (chrome + firefox)"
    $extDir = Join-Path $RepoRoot 'extension'
    if (-not (Test-Path (Join-Path $extDir 'node_modules'))) {
        Info "npm install (extension)..."
        Invoke-Native 'npm' @('install', '--no-audit', '--no-fund') $extDir
    }
    Invoke-Native 'npm' @('run', 'build') $extDir
    $browserDst = Join-Path $Staging 'browser'
    New-Item -ItemType Directory -Force -Path $browserDst | Out-Null
    Copy-Item (Join-Path $extDir 'dist\chrome')  (Join-Path $browserDst 'chrome')  -Recurse -Force
    Copy-Item (Join-Path $extDir 'dist\firefox') (Join-Path $browserDst 'firefox') -Recurse -Force
    Copy-Item (Join-Path $InstallerDir 'browser-yoriqnoma.txt') (Join-Path $browserDst 'YORIQNOMA.txt') -Force

    # === 5. VS Code kengaytmasi (.vsix) ===================================
    Step "5/6  VS Code kengaytmasi (.vsix) yigʻilmoqda"
    $vsDir = Join-Path $RepoRoot 'vscode'
    if (-not (Test-Path (Join-Path $vsDir 'node_modules'))) {
        Info "npm install (vscode)..."
        Invoke-Native 'npm' @('install', '--no-audit', '--no-fund') $vsDir
    }
    Invoke-Native 'npm' @('run', 'build') $vsDir
    $vscodeDst = Join-Path $Staging 'vscode'
    New-Item -ItemType Directory -Force -Path $vscodeDst | Out-Null
    $vsix = Join-Path $vscodeDst 'uzspell.vsix'
    Info "vsce package..."
    Invoke-Native 'npx' @('--yes', '@vscode/vsce', 'package', '--allow-missing-repository', '-o', $vsix) $vsDir
    if (-not (Test-Path $vsix)) {
        throw "vsce .vsix faylini yarata olmadi: $vsix"
    }

    # === 6. LibreOffice .oxt ==============================================
    Step "6/6  LibreOffice kengaytmasi tayyorlanmoqda"
    $libreDst = Join-Path $Staging 'libre'
    New-Item -ItemType Directory -Force -Path $libreDst | Out-Null
    $oxt = Join-Path $RepoRoot 'dist\UzSpell-libreoffice.oxt'
    if (-not (Test-Path $oxt)) {
        Info "oxt topilmadi — libreoffice\pack.ps1 ishga tushirilmoqda..."
        Invoke-Native 'powershell' @('-ExecutionPolicy', 'Bypass', '-File',
            (Join-Path $RepoRoot 'libreoffice\pack.ps1')) $RepoRoot
    }
    Copy-Item $oxt (Join-Path $libreDst 'UzSpell.oxt') -Force
}

# ---------------------------------------------------------------------------
Step "Inno Setup (ISCC) taʼminlanmoqda"
function Find-ISCC {
    $cands = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $cands) { if (Test-Path $c) { return $c } }
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$iscc = Find-ISCC
if (-not $iscc) {
    Info "Inno Setup topilmadi — winget orqali oʻrnatilmoqda..."
    Invoke-Native 'winget' @('install', '--id', 'JRSoftware.InnoSetup',
        '--silent', '--accept-package-agreements', '--accept-source-agreements')
    $iscc = Find-ISCC
}
if (-not $iscc) {
    throw "Inno Setup (ISCC.exe) topilmadi. Qoʻlda oʻrnating: https://jrsoftware.org/isdl.php"
}
Info "ISCC: $iscc"

Step "Installer kompilyatsiya qilinmoqda"
Invoke-Native $iscc @(
    "/DAppVersion=$Version",
    '/DStagingDir=staging',
    (Join-Path $InstallerDir 'UzSpell.iss')
) $InstallerDir

$out = Join-Path $RepoRoot "dist\UzSpell-Setup-$Version-x64.exe"
if (Test-Path $out) {
    $sizeMB = [math]::Round((Get-Item $out).Length / 1MB, 1)
    Write-Host "`nTAYYOR: $out  ($sizeMB MB)" -ForegroundColor Green
} else {
    throw "Kompilyatsiya tugadi, lekin chiqish fayli topilmadi: $out"
}
