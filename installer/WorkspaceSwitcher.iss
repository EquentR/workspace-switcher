; Workspace Switcher - Windows installer (Inno Setup 6)
;
; Build with:  pwsh -File installer/build-installer.ps1
; (the script publishes the WPF app and then invokes ISCC on this file)
;
; Values the build script can override on the ISCC command line:
;   /DAppVersion=1.2.3   product version, also used in the output file name
;   /DPublishDir=<path>  directory holding the publish output
;   /DOutputDir=<path>   directory the Setup.exe is written to

#define MyAppName "Workspace Switcher"
#define MyAppExeName "WorkspaceSwitcher.UI.exe"
#define MyAppPublisher "yMaxM15"
#define MyAppUrl "https://github.com/yMaxM15/workspace-switcher"
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

[Setup]
; Stable AppId: upgrades and uninstall always target this very installation.
AppId={{7D3B1D6C-2E4A-4B0E-9C51-7A5B9E0F4C21}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppVerName={#MyAppName} {#AppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}
AppUpdatesURL={#MyAppUrl}/releases
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} {#AppVersion} Setup
; {autopf} is "C:\Program Files" in the all-users install mode and
; "%LocalAppData%\Programs" in the per-user install mode. Either way the
; directory page is shown, so the user can install into any folder.
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=yes
; Per-user by default (no UAC prompt). The first wizard page and the
; /ALLUSERS command-line switch offer the all-users install mode.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
; The publish output is self-contained win-x64, so mirror that here.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.14393
OutputDir={#OutputDir}
OutputBaseFilename=WorkspaceSwitcher-{#AppVersion}-Setup
SetupIconFile=..\src\WorkspaceSwitcher.UI\app.ico
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Only ask for the wizard language when the system language is neither
; English nor Chinese.
ShowLanguageDialog=auto
; The app hides to the tray instead of exiting, which the Restart Manager
; cannot detect as "closed" - the [Code] section closes it explicitly.
CloseApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"

[CustomMessages]
; CreateDesktopIcon, AdditionalIcons and LaunchProgram come from the .isl files.
AppRunningInstall={#MyAppName} is still running.%n%nSetup must close it before the program files can be updated.%n%nClose it now?
chinesesimplified.AppRunningInstall={#MyAppName} 正在运行。%n%n安装程序需要先关闭它，才能更新程序文件。%n%n现在关闭吗？
AppRunningUninstall={#MyAppName} is still running.%n%nThe uninstaller must close it before the program files can be removed.%n%nClose it now?
chinesesimplified.AppRunningUninstall={#MyAppName} 正在运行。%n%n卸载程序需要先关闭它，才能删除程序文件。%n%n现在关闭吗？
AppRunningAbort=The installation was cancelled because {#MyAppName} is still running.%n%nExit the app from its tray icon, then run Setup again.
chinesesimplified.AppRunningAbort={#MyAppName} 仍在运行，安装已取消。%n%n请从托盘图标退出程序，然后重新运行安装程序。
AppCloseFailed=Setup could not close {#MyAppName}.%n%nExit the app from its tray icon, then run Setup again.
chinesesimplified.AppCloseFailed=安装程序无法关闭 {#MyAppName}。%n%n请从托盘图标退出程序，然后重新运行安装程序。
AppCloseFailedUninstall=The uninstaller could not close {#MyAppName}.%n%nExit the app from its tray icon, then uninstall again.
chinesesimplified.AppCloseFailedUninstall=卸载程序无法关闭 {#MyAppName}。%n%n请从托盘图标退出程序，然后重新卸载。

[Tasks]
; Tasks are checked by default in Inno Setup, so the desktop shortcut has to be
; opted into explicitly.
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Self-contained publish output of the WPF application.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; License text next to the program files.
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Checked by default on the final wizard page - the user can uncheck it.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; WorkingDir: "{app}"; Flags: postinstall nowait skipifsilent

[Code]
const
  AppExeName = '{#MyAppExeName}';
  GENERIC_READ_VALUE = $80000000;
  OPEN_EXISTING_VALUE = 3;
  FILE_ATTRIBUTE_NORMAL_VALUE = $80;
  INVALID_HANDLE_VALUE = $FFFFFFFF;
  ERROR_SUCCESS = 0;
  ERROR_PROCESS_NOT_FOUND = 128;

function CreateFileW(lpFileName: String; dwDesiredAccess, dwShareMode, lpSecurityAttributes,
  dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile: Cardinal): Cardinal;
  external 'CreateFileW@kernel32.dll stdcall';
function CloseHandle(hObject: Cardinal): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

{ True while the installed executable is held open by a running instance. Opens
  the file with no sharing allowed, which fails exactly while the image is
  mapped into a process. This works in both the installer and the uninstaller,
  unlike the Restart Manager (the app only hides to the tray on WM_CLOSE) and
  unlike tasklist (it refuses to run without a console). }
function IsAppInUse: Boolean;
var
  AppExePath: String;
  Handle: Cardinal;
begin
  Result := False;
  AppExePath := ExpandConstant('{app}\' + AppExeName);
  if not FileExists(AppExePath) then
    Exit;

  Handle := CreateFileW(AppExePath, GENERIC_READ_VALUE, 0, 0, OPEN_EXISTING_VALUE,
                        FILE_ATTRIBUTE_NORMAL_VALUE, 0);
  if Handle = INVALID_HANDLE_VALUE then
    Result := True
  else
    CloseHandle(Handle);
end;

function TerminateApp: Boolean;
var
  ResultCode: Integer;
begin
  { taskkill exits with 128 when no matching process exists, which still means
    the program files are usable. }
  Result := Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM ' + AppExeName + ' /F',
                 '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and
            ((ResultCode = ERROR_SUCCESS) or (ResultCode = ERROR_PROCESS_NOT_FOUND));
end;

{ Closes a running instance, asking first unless message boxes are suppressed
  (unattended runs must never block on UI). Returns False when the installation
  or uninstallation must not continue. }
function CloseAppIfRunning(const PromptMessage, FailedMessage: String): Boolean;
var
  InUse, Closed: Boolean;
begin
  Result := True;
  InUse := IsAppInUse;
  Log('WorkspaceSwitcher: program files in use = ' + IntToStr(Ord(InUse)));
  if not InUse then
    Exit;

  if SuppressibleMsgBox(PromptMessage, mbConfirmation, MB_YESNO, IDYES) = IDNO then
  begin
    Result := False;
    Exit;
  end;

  Closed := TerminateApp;
  Log('WorkspaceSwitcher: closed running instance = ' + IntToStr(Ord(Closed)));
  if not Closed then
  begin
    SuppressibleMsgBox(FailedMessage, mbCriticalError, MB_OK, IDOK);
    Result := False;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  if CloseAppIfRunning(CustomMessage('AppRunningInstall'),
                       CustomMessage('AppCloseFailed')) then
    Result := ''
  else
    Result := CustomMessage('AppRunningAbort');
end;

function InitializeUninstall: Boolean;
begin
  Result := CloseAppIfRunning(CustomMessage('AppRunningUninstall'),
                              CustomMessage('AppCloseFailedUninstall'));
end;