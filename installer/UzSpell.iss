; UzSpell — modulli oʻrnatuvchi (Inno Setup 6)
; -----------------------------------------------------------------------------
; Foydalanuvchi oʻrnatish vaqtida kerakli modullarni tanlaydi:
;   - Ish stoli dasturi (GUI)
;   - Buyruq qatori vositasi (CLI)
;   - Microsoft Word qoʻshimchasi
;   - Visual Studio Code kengaytmasi
;   - LibreOffice kengaytmasi (.oxt)
;   - Brauzer kengaytmasi (Chrome / Firefox)
; Tanlangan modullar oʻrnatilgach darhol ishlashga moslab roʻyxatga olinadi.
;
; Bu skript installer/build-installer.ps1 tomonidan ISCC bilan kompilyatsiya
; qilinadi. AppVersion va StagingDir qiymatlari /D bilan uzatiladi.

#ifndef AppVersion
  #define AppVersion "1.5.2"
#endif
#ifndef StagingDir
  #define StagingDir "staging"
#endif

#define AppName "UzSpell"
#define AppPublisher "Erkin Pardayev"
#define AppURL "https://github.com/Erkinbek/UzSpell"
#define VSCodeExtId "pardayev.uzspell-vscode"

[Setup]
AppId={{B6A4E1C2-8F3D-4E7A-9C1B-2D3E4F5A6B7C}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=UzSpell-Setup-{#AppVersion}-x64
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupIconFile=..\src\UzSpell.App\uzspell.ico
UninstallDisplayIcon={app}\App\UzSpell.exe
LicenseFile={#StagingDir}\LICENSE.txt

[Languages]
Name: "uz"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full";   Description: "Barcha modullar"
Name: "custom"; Description: "Tanlab oʻrnatish"; Flags: iscustom

[Components]
Name: "gui";     Description: "Ish stoli dasturi (imlo, grammatika, transliteratsiya)";   Types: full custom
Name: "cli";     Description: "Buyruq qatori vositasi (uzspell)";                          Types: full custom
Name: "word";    Description: "Microsoft Word qoʻshimchasi (lentadagi UzSpell boʻlimi)";   Types: full custom
Name: "vscode";  Description: "Visual Studio Code kengaytmasi";                            Types: full custom
Name: "libre";   Description: "LibreOffice / OpenOffice kengaytmasi (.oxt)";               Types: full custom
Name: "browser"; Description: "Brauzer kengaytmasi (Chrome / Firefox) — qoʻlda yuklanadi"; Types: full custom

[Tasks]
Name: "desktopicon"; Description: "Ish stoliga yorliq qoʻshish";                       Components: gui
Name: "addpath";     Description: "uzspell ni PATH ga qoʻshish (istalgan joydan ishga tushirish uchun)"; Components: cli

[Files]
; --- Ish stoli dasturi (self-contained, .NET kerak emas) ---
Source: "{#StagingDir}\app\*";     DestDir: "{app}\App";         Components: gui;     Flags: recursesubdirs createallsubdirs ignoreversion
; --- CLI (self-contained) ---
Source: "{#StagingDir}\cli\*";     DestDir: "{app}\Cli";         Components: cli;     Flags: recursesubdirs createallsubdirs ignoreversion
; --- Word qoʻshimchasi (.NET Framework 4.x — Windows bilan birga keladi) ---
Source: "{#StagingDir}\word\*";    DestDir: "{app}\Word";        Components: word;    Flags: recursesubdirs createallsubdirs ignoreversion
; --- VS Code kengaytmasi (.vsix) ---
Source: "{#StagingDir}\vscode\*";  DestDir: "{app}\VSCode";      Components: vscode;  Flags: recursesubdirs createallsubdirs ignoreversion
; --- LibreOffice .oxt ---
Source: "{#StagingDir}\libre\*";   DestDir: "{app}\LibreOffice"; Components: libre;   Flags: recursesubdirs createallsubdirs ignoreversion
; --- Brauzer kengaytmasi (chrome/ + firefox/ + yoʻriqnoma) ---
Source: "{#StagingDir}\browser\*"; DestDir: "{app}\Browser";     Components: browser; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\UzSpell";            Filename: "{app}\App\UzSpell.exe"; Components: gui
Name: "{group}\UzSpell ni oʻchirish"; Filename: "{uninstallexe}"
Name: "{autodesktop}\UzSpell";      Filename: "{app}\App\UzSpell.exe"; Components: gui; Tasks: desktopicon

[Registry]
; --- CLI ni mashina PATH iga qoʻshish ---
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
    ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}\Cli"; \
    Check: NeedsAddPath('{app}\Cli'); Tasks: addpath; Components: cli

; (Word add-in registratsiyasi register-word-addin.ps1 orqali — pastdagi [Run] ga qarang.
;  COM sinfi 32- va 64-bit registr koʻrinishlariga, add-in kaliti HKLM+HKCU ga yoziladi.)

[Run]
; --- Word qoʻshimchasini roʻyxatga olish (32/64-bit COM + HKLM/HKCU add-in kalitlari) ---
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Word\register-word-addin.ps1"" -DllPath ""{app}\Word\UzSpell.WordAddin.dll"""; \
    StatusMsg: "Word qoʻshimchasi roʻyxatga olinmoqda..."; Flags: runhidden waituntilterminated; Components: word

; --- VS Code kengaytmasini oʻrnatish (code CLI topilsa) ---
Filename: "{cmd}"; Parameters: "/c ""code"" --install-extension ""{app}\VSCode\uzspell.vsix"" --force"; \
    StatusMsg: "VS Code kengaytmasi oʻrnatilmoqda..."; Flags: runhidden waituntilterminated; \
    Components: vscode; Check: VSCodeAvailable

; --- LibreOffice kengaytmasini oʻrnatish (unopkg topilsa) ---
Filename: "{code:GetUnopkgPath}"; Parameters: "add --suppress-license ""{app}\LibreOffice\UzSpell.oxt"""; \
    StatusMsg: "LibreOffice kengaytmasi oʻrnatilmoqda..."; Flags: runhidden waituntilterminated; \
    Components: libre; Check: UnopkgAvailable

; --- Oʻrnatish tugagach: dastur / brauzer yoʻriqnomasi ---
Filename: "{app}\App\UzSpell.exe"; Description: "UzSpell dasturini ishga tushirish"; \
    Flags: nowait postinstall skipifsilent; Components: gui
Filename: "{app}\Browser\YORIQNOMA.txt"; Description: "Brauzer kengaytmasini oʻrnatish yoʻriqnomasini ochish"; \
    Flags: nowait postinstall skipifsilent shellexec; Components: browser

[UninstallRun]
; --- Word COM sinfini bekor qilish (32/64-bit + HKLM/HKCU) ---
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Word\register-word-addin.ps1"" -DllPath ""{app}\Word\UzSpell.WordAddin.dll"" -Unregister"; \
    Flags: runhidden waituntilterminated; Components: word; RunOnceId: "UnregWordAddin"
; --- VS Code kengaytmasini olib tashlash ---
Filename: "{cmd}"; Parameters: "/c ""code"" --uninstall-extension {#VSCodeExtId}"; \
    Flags: runhidden waituntilterminated; Components: vscode; Check: VSCodeAvailable; RunOnceId: "UninstVSCode"
; --- LibreOffice kengaytmasini olib tashlash ---
Filename: "{code:GetUnopkgPath}"; Parameters: "remove uz.pardayev.uzspell.dictionaries"; \
    Flags: runhidden waituntilterminated; Components: libre; Check: UnopkgAvailable; RunOnceId: "UninstLibre"

[Code]
{ ----------------------------------------------------------------------------
  Yordamchi funksiyalar
  ---------------------------------------------------------------------------- }

{ PATH ga qoʻshish kerakmi (allaqachon bor boʻlsa qoʻshmaymiz) }
function NeedsAddPath(Param: string): Boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKLM,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath) then
  begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Uppercase(ExpandConstant(Param)) + ';',
    ';' + Uppercase(OrigPath) + ';') = 0;
end;

{ RegAsm.exe yoʻlini topish (64- yoki 32-bit .NET Framework) }
function GetRegAsmPath(Param: string): string;
var
  Path64, Path32: string;
begin
  Path64 := ExpandConstant('{win}\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe');
  Path32 := ExpandConstant('{win}\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe');
  if FileExists(Path64) then
    Result := Path64
  else
    Result := Path32;
end;

{ unopkg.exe (LibreOffice) yoʻlini topish }
function FindUnopkg(): string;
var
  Candidates: array[0..3] of string;
  I: Integer;
begin
  Candidates[0] := ExpandConstant('{pf}\LibreOffice\program\unopkg.exe');
  Candidates[1] := ExpandConstant('{pf32}\LibreOffice\program\unopkg.exe');
  Candidates[2] := ExpandConstant('{pf}\OpenOffice 4\program\unopkg.exe');
  Candidates[3] := ExpandConstant('{pf32}\OpenOffice 4\program\unopkg.exe');
  Result := '';
  for I := 0 to 3 do
    if (Result = '') and FileExists(Candidates[I]) then
      Result := Candidates[I];
end;

function GetUnopkgPath(Param: string): string;
begin
  Result := FindUnopkg();
end;

function UnopkgAvailable(): Boolean;
begin
  Result := FindUnopkg() <> '';
end;

{ VS Code CLI (code.cmd) mavjudmi — keng tarqalgan joylar + PATH }
function VSCodeAvailable(): Boolean;
var
  ResultCode: Integer;
begin
  { Avval keng tarqalgan oʻrnatish joylari }
  if FileExists(ExpandConstant('{pf}\Microsoft VS Code\bin\code.cmd')) or
     FileExists(ExpandConstant('{localappdata}\Programs\Microsoft VS Code\bin\code.cmd')) then
  begin
    Result := True;
    exit;
  end;
  { Aks holda PATH dan sinab koʻramiz }
  Result := Exec(ExpandConstant('{cmd}'), '/c code --version', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

{ Oʻrnatishdan oldin: hech boʻlmasa bitta modul tanlanganini tekshirish
  (Inno Setup buni odatda taʼminlaydi, lekin aniqlik uchun) }
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpSelectComponents then
  begin
    if (not WizardIsComponentSelected('gui')) and
       (not WizardIsComponentSelected('cli')) and
       (not WizardIsComponentSelected('word')) and
       (not WizardIsComponentSelected('vscode')) and
       (not WizardIsComponentSelected('libre')) and
       (not WizardIsComponentSelected('browser')) then
    begin
      MsgBox('Iltimos, kamida bitta modulni tanlang.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

{ Oʻrnatishdan keyin foydalanuvchini ogohlantirish (modul bor, lekin host yoʻq) }
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsComponentSelected('libre') and (not UnopkgAvailable()) then
      MsgBox('LibreOffice topilmadi. Kengaytma fayli (.oxt) "' +
        ExpandConstant('{app}\LibreOffice') +
        '" papkasiga saqlandi. LibreOffice oʻrnatilgach uni qoʻlda oʻrnating: ' +
        'Tools → Extension Manager → Add.', mbInformation, MB_OK);
    if WizardIsComponentSelected('vscode') and (not VSCodeAvailable()) then
      MsgBox('VS Code topilmadi. Kengaytma fayli (.vsix) "' +
        ExpandConstant('{app}\VSCode') +
        '" papkasiga saqlandi. VS Code oʻrnatilgach: code --install-extension uzspell.vsix.',
        mbInformation, MB_OK);
  end;
end;

{ Oʻchirishda CLI ni PATH dan olib tashlash }
procedure RemoveFromPath(PathToRemove: string);
var
  OrigPath, NewPath, Expanded: string;
begin
  if not RegQueryStringValue(HKLM,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath) then
    exit;
  Expanded := Uppercase(ExpandConstant(PathToRemove));
  NewPath := OrigPath;
  StringChangeEx(NewPath, ';' + ExpandConstant(PathToRemove), '', True);
  StringChangeEx(NewPath, ExpandConstant(PathToRemove) + ';', '', True);
  StringChangeEx(NewPath, ExpandConstant(PathToRemove), '', True);
  if NewPath <> OrigPath then
    RegWriteExpandStringValue(HKLM,
      'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
      'Path', NewPath);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemoveFromPath(ExpandConstant('{app}\Cli'));
end;
