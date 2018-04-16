; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Apsim-HPC"
#define MyAppVersion "1.5"
#define MyAppPublisher "Apsim "
#define MyAppURL "http://www.apsim.info/"
#define MyAppExeName "Apsim-HPC.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{8D04F19F-A302-436F-BE57-3C56B793DFB0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\..\Release
OutputBaseFilename=HPCApsim-Setup
Compression=lzma
SolidCompression=yes

[Code]

function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1'          .NET Framework 1.1
//    'v2.0'          .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//    'v4.5.1'        .NET Framework 4.5.1
//    'v4.5.2'        .NET Framework 4.5.2
//    'v4.6'          .NET Framework 4.6
//    'v4.6.1'        .NET Framework 4.6.1
//    'v4.6.2'        .NET Framework 4.6.2
//    'v4.7'          .NET Framework 4.7
//    'v4.7.1'        .NET Framework 4.7.1
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key, versionKey: string;
    install, release, serviceCount, versionRelease: cardinal;
    success: boolean;
begin
    versionKey := version;
    versionRelease := 0;

    // .NET 1.1 and 2.0 embed release number in version key
    if version = 'v1.1' then begin
        versionKey := 'v1.1.4322';
    end else if version = 'v2.0' then begin
        versionKey := 'v2.0.50727';
    end

    // .NET 4.5 and newer install as update to .NET 4.0 Full
    else if Pos('v4.', version) = 1 then begin
        versionKey := 'v4\Full';
        case version of
          'v4.5':   versionRelease := 378389;
          'v4.5.1': versionRelease := 378675; // 378758 on Windows 8 and older
          'v4.5.2': versionRelease := 379893;
          'v4.6':   versionRelease := 393295; // 393297 on Windows 8.1 and older
          'v4.6.1': versionRelease := 394254; // 394271 before Win10 November Update
          'v4.6.2': versionRelease := 394802; // 394806 before Win10 Anniversary Update
          'v4.7':   versionRelease := 460798; // 460805 before Win10 Creators Update
          'v4.7.1': versionRelease := 461308; // 461310 before Win10 Fall Creators Update
        end;
    end;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0 and newer use value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 and newer use additional value Release
    if versionRelease > 0 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= versionRelease);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;

// this is the main function that detects the required version
function IsRequiredDotNetDetected(): Boolean;  
begin
    result := IsDotNetDetected('v4.5', 0);
end;

function InitializeSetup(): Boolean;
var
  answer: integer;
  ErrorCode: Integer;
begin
    //check for the .net runtime. If it is not found then show a message.
    if not IsRequiredDotNetDetected() then 
    begin
        answer := MsgBox('The Microsoft .NET Framework 4.5 is required.' + #13#10 + #13#10 +
        'Click OK to go to the web site or Cancel to quit', mbInformation, MB_OKCANCEL);        
        result := false;
        if (answer = MROK) then
        begin
          ShellExecAsOriginalUser('open', 'http://www.microsoft.com/en-au/download/details.aspx?id=40773', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
        end;
    end
    else
      result := true;
end; 

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ..\Apsim-HPC.exe; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\ApsimFile.dll; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\CSGeneral.dll; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\CMPServices.dll; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\ICSharpCode.SharpZipLib.dll; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\Apsim.Shared.dll; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\System.Data.SQLite.dll; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\Renci.SshNet.dll; DestDir: {app}\Model; Flags: ignoreversion; 
Source: ..\DeploymentSupport\Windows\Bin\*.dll; DestDir: {app}\Model; Flags: ignoreversion; Check: not Is64BitInstallMode
Source: ..\DeploymentSupport\Windows\lib\gtk-2.0\2.10.0\engines\*.dll; DestDir: {app}\lib\gtk-2.0\2.10.0\engines; Flags: ignoreversion; Check: not Is64BitInstallMode
Source: ..\DeploymentSupport\Windows\Bin64\*.dll; DestDir: {app}\Model; Flags: ignoreversion; Check: Is64BitInstallMode
Source: ..\DeploymentSupport\Windows\Bin64\lib\gtk-2.0\2.10.0\engines\*.dll; DestDir: {app}\lib\gtk-2.0\2.10.0\engines; Flags: ignoreversion; Check: Is64BitInstallMode

;Source: .\Apsim-HPC\bin\Debug\atk-sharp.dll; DestDir: {app}\bin; Flags: ignoreversion;  
;Source: .\Apsim-HPC\bin\Debug\gdk-sharp.dll; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: .\Apsim-HPC\bin\Debug\glade-sharp.dll; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: .\Apsim-HPC\bin\Debug\glib-sharp.dll; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: .\Apsim-HPC\bin\Debug\gtk-dotnet.dll; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: .\Apsim-HPC\bin\Debug\gtk-sharp.dll; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: "C:\Program Files (x86)\GtkSharp\2.12\lib\Mono.Posix\Mono.Posix.dll"; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: .\Apsim-HPC\bin\Debug\pango-sharp.dll; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: .\Apsim-HPC\bin\Debug\System.Data.SQLite.dll; DestDir: {app}\bin; Flags: ignoreversion; 
;Source: "C:\Program Files (x86)\GtkSharp\2.12\*"; DestDir: {app}; Flags: recursesubdirs; 

Source: ..\..\UserInterface\Images\earth_connection24.png; DestDir: {app}\UserInterface\Images; Flags: ignoreversion;
Source: ..\..\Apsim.xml; DestDir: {app}\; Flags: ignoreversion;

[Icons]
Name: "{commonprograms}\Model\{#MyAppName}"; Filename: "{app}\Model\{#MyAppExeName}"
Name: "{commondesktop}\Model\{#MyAppName}"; Filename: "{app}\Model\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\Model\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

