[Setup]
; App Details
AppName=NevPlayer
AppVersion=1.0.0
AppPublisher=NevPlayer

; Architecture configuration
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Installation Directory (Program Files)
DefaultDirName={autopf}\NevPlayer
DefaultGroupName=NevPlayer

; Icon configuration
SetupIconFile=src\NevPlayer.App\Assets\NevPlayer.ico
UninstallDisplayIcon={app}\NevPlayer.App.exe

; Output configuration
OutputDir=installer
OutputBaseFilename=NevPlayer_Setup_v1.0.0_x64
Compression=lzma2
SolidCompression=yes

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Package the self-contained output folder recursively
Source: "src\NevPlayer.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Package the Windows App SDK runtime installer
Source: "installer\WindowsAppRuntimeInstall-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion

[Icons]
; Start Menu shortcut
Name: "{group}\NevPlayer"; Filename: "{app}\NevPlayer.App.exe"
; Desktop shortcut
Name: "{autodesktop}\NevPlayer"; Filename: "{app}\NevPlayer.App.exe"; Tasks: desktopicon

[Run]
; Install Windows App SDK Runtime silently before launching the app
Filename: "{tmp}\WindowsAppRuntimeInstall-x64.exe"; Parameters: "--quiet"; StatusMsg: "Installing Windows App SDK prerequisites..."; Flags: waituntilterminated
; Option to launch the app after installation
Filename: "{app}\NevPlayer.App.exe"; Description: "{cm:LaunchProgram,NevPlayer}"; Flags: nowait postinstall skipifsilent
