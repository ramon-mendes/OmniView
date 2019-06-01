@echo off

start "D:\Projetos-VSX\InstallKiller\InstallKiller\bin\Debug\InstallKiller.exe"

"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" "..\OmniView\OmniView2017.csproj" /t:Clean,Build /p:Configuration=Release

del OmniView.vsix
copy ..\OmniView\bin\Release\OmniView.vsix OmniView.vsix


::echo.
::echo ### Uninstalling VSIX
::"C:\Program Files (x86)\Microsoft Visual Studio\Installer\resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service\VSIXInstaller.exe" /quiet /logFile:vsixlog.txt /uninstall:OmniView.MISoftware.e3a6c7fb-0320-4655-9a97-afdf58c57fad
::"C:\Program Files (x86)\Microsoft Visual Studio\Installer\resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service\VSIXInstaller.exe" /quiet /logFile:vsixlog.txt /uninstall:OmniView.MISoftware.e3a6c7fb-0320-4655-9a97-afdf58c57fae
::type %TEMP%\vsixlog.txt

echo ### Installing VSIX
"C:\Program Files (x86)\Microsoft Visual Studio\Installer\resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service\VSIXInstaller.exe" /force /quiet /logFile:vsixlog.txt OmniView.vsix
type %TEMP%\vsixlog.txt

if %errorlevel% NEQ 0 Msg * "Install VSIX failed"

::echo ### Starting VS
::"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.exe" "D:\Projetos-VSX\OmniView\OmniView.sln" /log

pause