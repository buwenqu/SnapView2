; SnapView Setup Script for Inno Setup
; Download Inno Setup: https://jrsoftware.org/isdl.php

[Setup]
AppName=SnapView
AppVersion=1.0
AppPublisher=SnapView
DefaultDirName={autopf}\SnapView
DefaultGroupName=SnapView
UninstallDisplayName=SnapView
UninstallDisplayIcon={app}\SnapView.exe
OutputDir=..\installer
OutputBaseFilename=SnapView_Setup
SetupIconFile=app.ico
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "publish\SnapView.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\SnapView"; Filename: "{app}\SnapView.exe"
Name: "{group}\卸载 SnapView"; Filename: "{uninstallexe}"
Name: "{autodesktop}\SnapView"; Filename: "{app}\SnapView.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式:"

; ---------- 文件关联（用户可选）----------
[Components]
Name: assoc_jpg; Description: "JPEG 图片 (.jpg)"; Types: full compact; Flags: checkablealone
Name: assoc_jpeg; Description: "JPEG 图片 (.jpeg)"; Types: full compact; Flags: checkablealone
Name: assoc_png; Description: "PNG 图片 (.png)"; Types: full compact; Flags: checkablealone
Name: assoc_bmp; Description: "BMP 图片 (.bmp)"; Types: full compact; Flags: checkablealone
Name: assoc_gif; Description: "GIF 图片 (.gif)"; Types: full compact; Flags: checkablealone
Name: assoc_webp; Description: "WebP 图片 (.webp)"; Types: full compact; Flags: checkablealone

; ---------- 注册表：Add/Remove Programs ----------
[Registry]
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; Flags: uninsdeletekey
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; ValueType: string; ValueName: "DisplayName"; ValueData: "SnapView"; Flags: uninsdeletevalue
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; ValueType: string; ValueName: "UninstallString"; ValueData: "{uninstallexe}"; Flags: uninsdeletevalue
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\SnapView.exe,0"; Flags: uninsdeletevalue
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; ValueType: string; ValueName: "Publisher"; ValueData: "SnapView"; Flags: uninsdeletevalue
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; ValueType: string; ValueName: "DisplayVersion"; ValueData: "1.0"; Flags: uninsdeletevalue
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; ValueType: dword; ValueName: "NoModify"; ValueData: "1"; Flags: uninsdeletevalue
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapView"; ValueType: dword; ValueName: "NoRepair"; ValueData: "1"; Flags: uninsdeletevalue

; ---------- 程序标识 ----------
Root: HKCU; Subkey: "Software\Classes\SnapView.AssocFile"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SnapView.AssocFile"; ValueType: string; ValueName: ""; ValueData: "SnapView Image"
Root: HKCU; Subkey: "Software\Classes\SnapView.AssocFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\SnapView.exe,0"
Root: HKCU; Subkey: "Software\Classes\SnapView.AssocFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\SnapView.exe"" ""%1"""

; ---------- 各扩展名关联 ----------
Root: HKCU; Subkey: "Software\Classes\.jpg\OpenWithProgids"; ValueType: string; ValueName: "SnapView.AssocFile"; ValueData: ""; Components: assoc_jpg
Root: HKCU; Subkey: "Software\Classes\.jpeg\OpenWithProgids"; ValueType: string; ValueName: "SnapView.AssocFile"; ValueData: ""; Components: assoc_jpeg
Root: HKCU; Subkey: "Software\Classes\.png\OpenWithProgids"; ValueType: string; ValueName: "SnapView.AssocFile"; ValueData: ""; Components: assoc_png
Root: HKCU; Subkey: "Software\Classes\.bmp\OpenWithProgids"; ValueType: string; ValueName: "SnapView.AssocFile"; ValueData: ""; Components: assoc_bmp
Root: HKCU; Subkey: "Software\Classes\.gif\OpenWithProgids"; ValueType: string; ValueName: "SnapView.AssocFile"; ValueData: ""; Components: assoc_gif
Root: HKCU; Subkey: "Software\Classes\.webp\OpenWithProgids"; ValueType: string; ValueName: "SnapView.AssocFile"; ValueData: ""; Components: assoc_webp

[Run]
Filename: "{app}\SnapView.exe"; Description: "启动 SnapView"; Flags: nowait postinstall skipifsilent unchecked
