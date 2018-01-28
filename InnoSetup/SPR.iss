[Setup]
AppName=Shadow Play Renamer
AppVersion=1.0.0.0
AppCopyright=Copyright (c) 2018 Ádám Juhász
AppId={{31F2A279-1CA7-4373-8935-A955D954F234}
LicenseFile=gpl-3.0.rtf
DefaultDirName={pf}\SPR\
DisableProgramGroupPage=auto
VersionInfoVersion=1.0
VersionInfoCopyright=Copyright (c) 2018 Ádám Juhász
VersionInfoProductName=Shadow Play Renamer
VersionInfoProductVersion=1.0.0.0
OutputDir=bin
MinVersion=0,6.0

[Files]
Source: "InputFiles\COPYING"; DestDir: "{app}"; DestName: "COPYING"; Components: Common
Source: "InputFiles\SPRCore.dll"; DestDir: "{app}"; DestName: "SPRCore.dll"; Components: Common
Source: "InputFiles\SPRSVC.exe"; DestDir: "{app}"; DestName: "SPRSVC.exe"; Components: Service
Source: "InputFiles\SPRSVC.exe.config"; DestDir: "{app}"; DestName: "SPRSVC.exe.config"; Flags: onlyifdoesntexist uninsneveruninstall; Components: Service
Source: "InputFiles\SPRCLI.exe"; DestDir: "{app}"; DestName: "SPRCLI.exe"; Components: CLI
Source: "InputFiles\SPRCLI.exe.config"; DestDir: "{app}"; DestName: "SPRCLI.exe.config"; Components: CLI
Source: "InputFiles\SPRCore.pdb"; DestDir: "{app}"; DestName: "SPRCore.pdb"; Components: Debug
Source: "InputFiles\SPRSVC.pdb"; DestDir: "{app}"; DestName: "SPRSVC.pdb"; Components: Debug
Source: "InputFiles\SPRCLI.pdb"; DestDir: "{app}"; DestName: "SPRCLI.pdb"; Components: Debug

[Components]
Name: "Service"; Description: "This will only install the service on your disk."; Types: compact custom
Name: "Common"; Description: "Common part of the product to function"; Types: compact full custom; Flags: fixed
Name: "CLI"; Description: "Command-line interface"; Types: full custom
Name: "Debug"; Description: "Additional debug symbols to support debugging."; Types: custom

[Run]
Filename: "{app}\SPRSVC.exe"; Parameters: "install"; WorkingDir: "{app}"; Flags: postinstall waituntilterminated skipifdoesntexist runhidden; StatusMsg: "Installing service"; Components: Service
Filename: "{app}\SPRSVC.exe"; Parameters: "start"; WorkingDir: "{app}"; Flags: waituntilterminated skipifdoesntexist runhidden postinstall; StatusMsg: "Starting service"

[UninstallRun]
Filename: "{app}\SPRSVC.exe"; Parameters: "stop"; WorkingDir: "{app}"; Flags: waituntilterminated skipifdoesntexist runhidden; Components: Service
Filename: "{app}\SPRSVC.exe"; Parameters: "uninstall"; WorkingDir: "{app}"; Flags: waituntilterminated skipifdoesntexist runhidden; Components: Service
