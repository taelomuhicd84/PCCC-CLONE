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
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  "%~dp0module\plugin\*.cs" "%~dp0module\chung\HangDoi.cs" || exit /b 1

copy /y "%~dp0..\data\vat_tu.json" "%OUT%\vat_tu.json" >nul
rem PCCC_THEMBLOCK ghi ma moi vao ca file goc cua repo (nguon duy nhat cua danh muc): de lai duong dan cho plugin.
echo %~dp0..\data\vat_tu.json>"%OUT%\nguon_vat_tu.txt"
rem Mac dinh ky hieu truc, kich thuoc, chu: nguoi dung sua o cad\data\ky_hieu, plugin doc ban chep canh DLL.
copy /y "%~dp0data\ky_hieu\ky_hieu.json" "%OUT%\ky_hieu.json" >nul
echo OK: %OUT%\TKPCCC.Cad.dll

rem Bang block (lenh PCCC_BLOCK): DLL giao dien rieng vi can acmgd.dll, accoreconsole khong nap duoc nen chi dung trong AutoCAD co giao dien.
del /q "%OUT%\TKPCCC.UI.dll.cu*" >nul 2>&1
if exist "%OUT%\TKPCCC.UI.dll" move /y "%OUT%\TKPCCC.UI.dll" "%OUT%\TKPCCC.UI.dll.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ /out:"%OUT%\TKPCCC.UI.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" /r:"%ACAD%\acmgd.dll" /r:"%ACAD%\AdWindows.dll" ^
  /r:"%FW%\WPF\PresentationCore.dll" /r:"%FW%\WPF\WindowsBase.dll" /r:"%FW%\System.Xaml.dll" ^
  /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:"%OUT%\TKPCCC.Cad.dll" ^
  "%~dp0module\ui\*.cs" || exit /b 1
echo OK: %OUT%\TKPCCC.UI.dll

rem May chu MCP (cad\bin\TKPCCC.Mcp.exe): AI goi cad_tt / cad_chay. Dang chay thi doi ten ban cu nhu DLL.
set BIN=%~dp0bin
if not exist "%BIN%" mkdir "%BIN%"
del /q "%BIN%\TKPCCC.Mcp.exe.cu*" >nul 2>&1
if exist "%BIN%\TKPCCC.Mcp.exe" move /y "%BIN%\TKPCCC.Mcp.exe" "%BIN%\TKPCCC.Mcp.exe.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:exe /codepage:65001 /optimize+ /out:"%BIN%\TKPCCC.Mcp.exe" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll ^
  "%~dp0module\mcp\Mcp.cs" "%~dp0module\chung\HangDoi.cs" || exit /b 1
copy /y "%~dp0skills\TU_VUNG.md" "%BIN%\TU_VUNG.md" >nul
echo OK: %BIN%\TKPCCC.Mcp.exe
echo Neu AutoCAD dang mo: tat han AutoCAD roi mo lai de nap ban moi.
