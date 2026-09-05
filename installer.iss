#define MyAppName "Multiple Pointers"
#ifndef MyAppVersion
#define MyAppVersion "0.8.3"
#endif
#ifndef MyAppFileVersion
#define MyAppFileVersion MyAppVersion + ".0"
#endif
#define MyAppPublisher "Multiple Pointers"
#define MyAppExeName "MultiplePointers.exe"

[Setup]
AppId={{6FA9AF7A-A3FD-4A7F-A0C7-1D0E6F2B97AA}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

; Per-user install: no administrator prompt is required.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DefaultDirName={localappdata}\Programs\Multiple Pointers
DefaultGroupName=Multiple Pointers
DisableProgramGroupPage=yes

OutputDir=release
OutputBaseFilename=MultiplePointers_Setup_v{#MyAppVersion}
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
CloseApplications=yes
RestartApplications=no
DisableWelcomePage=no
AllowNoIcons=yes

VersionInfoVersion={#MyAppFileVersion}
VersionInfoCompany=Multiple Pointers
VersionInfoDescription=Multiple Pointers Setup
VersionInfoProductName=Multiple Pointers
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Utwórz skrót na pulpicie"; GroupDescription: "Dodatkowe skróty:"; Flags: unchecked

[Files]
Source: "release\portable\MultiplePointers\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Multiple Pointers"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Multiple Pointers"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Uruchom Multiple Pointers"; Flags: nowait postinstall skipifsilent


[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
