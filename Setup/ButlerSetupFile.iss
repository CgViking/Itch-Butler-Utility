; Inno Setup script for Itch.io Butler Utility.
; Build the app first:
;   dotnet publish "ButlerUtility.App\ButlerUtility.App.csproj" -c Release -r win-x64 --self-contained true
; then compile this script from the Setup\ directory.

#define MyAppName "Itch.io Butler Utility"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "CgViking"
#define MyAppExeName "Itch.io Butler Utility.exe"
#define PublishDir "..\ButlerUtility.App\bin\Release\net8.0\win-x64\publish"

[Setup]
; Keep this AppId unchanged so new installers upgrade existing installs in place.
AppId={{8D7EB7C8-2639-45B7-8676-70F0BC01498B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequiredOverridesAllowed=dialog
; Asset name referenced by the update feeds (releases/latest/download/ButlerUtilitySetup.exe).
OutputBaseFilename=ButlerUtilitySetup
SetupIconFile=..\ButlerUtility.App\Assets\app.ico
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
