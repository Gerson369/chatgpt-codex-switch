@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist dist mkdir dist
if not exist build mkdir build
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:src\App.manifest /r:System.Security.dll /r:System.Web.Extensions.dll /r:System.Management.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Accessibility.dll /out:dist\ChatGPTCodexSwitch.exe src\AssemblyInfo.cs src\Core.cs src\App.cs src\Quota.cs src\Processes.cs src\DarkUi.cs src\QuotaDisplay.cs src\CompactList.cs src\Dialogs.cs src\AccountGrid.cs src\CardUi.cs
if errorlevel 1 exit /b 1
"%CSC%" /nologo /r:System.Security.dll /r:System.Web.Extensions.dll /out:build\Tests.exe src\Core.cs tests\Tests.cs src\Quota.cs src\Processes.cs src\QuotaDisplay.cs
if errorlevel 1 exit /b 1
build\Tests.exe
if errorlevel 1 exit /b 1
"%CSC%" /nologo /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Accessibility.dll /out:build\UiGridCheck.exe tests\UiGridCheck.cs
if errorlevel 1 exit /b 1
build\UiGridCheck.exe "%CD%\dist\ChatGPTCodexSwitch.exe" "%CD%\build\ui-check.png"
exit /b %ERRORLEVEL%
