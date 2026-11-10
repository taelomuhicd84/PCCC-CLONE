@echo off
rem Bien dich plugin bang csc.exe co san cua .NET Framework (khong can cai SDK). File nay chi dung chu ASCII (cmd).
rem Ket qua: cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll -> AutoCAD: NETLOAD file nay,
rem hoac chep ca thu muc TKPCCC.bundle vao %%APPDATA%%\Autodesk\ApplicationPlugins de tu nap khi mo AutoCAD.
rem Chi tham chieu acdbmgd + accoremgd (khong acmgd) nen chay duoc ca trong accoreconsole.
setlocal
set ACAD=%ACAD_DIR%
if "%ACAD%"=="" set ACAD=C:\Program Files\Autodesk\AutoCAD 2021
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set OUT=%~dp0TKPCCC.bundle\Contents
if not exist "%OUT%" mkdir "%OUT%"

rem AutoCAD dang mo thi DLL bi khoa ghi nhung van doi ten duoc: doi ten ban cu, AutoCAD mo lai se nap ban moi.
del /q "%OUT%\TKPCCC.Cad.dll.cu*" >nul 2>&1
if exist "%OUT%\TKPCCC.Cad.dll" move /y "%OUT%\TKPCCC.Cad.dll" "%OUT%\TKPCCC.Cad.dll.cu%RANDOM%" >nul

"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ /out:"%OUT%\TKPCCC.Cad.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll ^
  "%~dp0src\*.cs" "%~dp0chung\HangDoi.cs" || exit /b 1

copy /y "%~dp0..\data\vat_tu.json" "%OUT%\vat_tu.json" >nul
echo OK: %OUT%\TKPCCC.Cad.dll

rem May chu MCP (cad\bin\TKPCCC.Mcp.exe): AI goi cad_tt / cad_chay. Dang chay thi doi ten ban cu nhu DLL.
set BIN=%~dp0bin
if not exist "%BIN%" mkdir "%BIN%"
del /q "%BIN%\TKPCCC.Mcp.exe.cu*" >nul 2>&1
if exist "%BIN%\TKPCCC.Mcp.exe" move /y "%BIN%\TKPCCC.Mcp.exe" "%BIN%\TKPCCC.Mcp.exe.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:exe /codepage:65001 /optimize+ /out:"%BIN%\TKPCCC.Mcp.exe" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll ^
  "%~dp0mcp\Mcp.cs" "%~dp0chung\HangDoi.cs" || exit /b 1
copy /y "%~dp0spec\TU_VUNG.md" "%BIN%\TU_VUNG.md" >nul
echo OK: %BIN%\TKPCCC.Mcp.exe
echo Neu AutoCAD dang mo: tat han AutoCAD roi mo lai de nap ban moi.
