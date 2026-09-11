#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#ifndef SourceDir
  #define SourceDir "."
#endif

[Setup]
AppId={{AA7D17FD-44D1-4BE3-B020-B16866C10C49}
AppName=Ghost RDP
AppVersion={#AppVersion}
AppPublisher=Brendigo
AppPublisherURL=https://github.com/bren-wp/Ghost-RDP
AppSupportURL=https://github.com/bren-wp/Ghost-RDP/issues
AppUpdatesURL=https://github.com/bren-wp/Ghost-RDP/releases
DefaultDirName={localappdata}\Programs\Ghost RDP
DefaultGroupName=Ghost RDP
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=GhostRDP-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
Uninstallable=yes
UninstallDisplayName=Ghost RDP
UninstallDisplayIcon={app}\GhostRDP.exe
VersionInfoVersion={#AppVersion}
VersionInfoProductName=Ghost RDP
VersionInfoDescription=Ghost RDP Windows Setup
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\GhostRDP-Portable-x64.exe"; DestDir: "{app}"; DestName: "GhostRDP.exe"; Flags: ignoreversion
Source: "{#SourceDir}\GhostRDP-Host-x64.exe"; DestDir: "{app}"; DestName: "GhostRDP-Host.exe"; Flags: ignoreversion
Source: "{#SourceDir}\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Ghost RDP"; Filename: "{app}\GhostRDP.exe"; WorkingDir: "{app}"
Name: "{autoprograms}\Ghost RDP Host"; Filename: "{app}\GhostRDP-Host.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Ghost RDP"; Filename: "{app}\GhostRDP.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\GhostRDP.exe"; Description: "Launch Ghost RDP"; Flags: nowait postinstall skipifsilent
