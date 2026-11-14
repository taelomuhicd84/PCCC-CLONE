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
rem "build.bat phat_hanh": ban phat hanh co chan ban quyen (/define:BAN_QUYEN) va nhung khoa cong khai vao DLL.
set DEF=
set RES=
set KCK=%~dp0data\ban_quyen\khoa_cong_khai.xml
if /i "%~1"=="phat_hanh" (
  if not exist "%KCK%" goto khong_khoa
  set DEF=/define:BAN_QUYEN
  set RES="/resource:%KCK%,khoa_cong_khai.xml"
)

del /q "%OUT%\TKPCCC.Cad.dll.cu*" >nul 2>&1
if exist "%OUT%\TKPCCC.Cad.dll" move /y "%OUT%\TKPCCC.Cad.dll" "%OUT%\TKPCCC.Cad.dll.cu%RANDOM%" >nul

"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ %DEF% %RES% /out:"%OUT%\TKPCCC.Cad.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  "%~dp0module\plugin\*.cs" "%~dp0module\chung\HangDoi.cs" || exit /b 1

copy /y "%~dp0..\data\vat_tu.json" "%OUT%\vat_tu.json" >nul
rem Tri so rai thiet bi theo tieu chuan (rai / PCCC_RAI khong co "s"): nguon o data\khoang_cach_rai.json goc repo.
copy /y "%~dp0..\data\khoang_cach_rai.json" "%OUT%\khoang_cach_rai.json" >nul
rem Bang chon loai dau bao theo cong nang (trang Bao chay doc bang "Loai theo khu vuc"): nguon duy nhat knowledge\chon-loai-dau-bao.md.
copy /y "%~dp0..\knowledge\chon-loai-dau-bao.md" "%OUT%\chon-loai-dau-bao.md" >nul
rem PCCC_THEMBLOCK ghi ma moi vao ca file goc cua repo (nguon duy nhat cua danh muc): de lai duong dan cho plugin.
echo %~dp0..\data\vat_tu.json>"%OUT%\nguon_vat_tu.txt"
rem Mac dinh ky hieu truc, kich thuoc, chu: nguoi dung sua o cad\data\ky_hieu, plugin doc ban chep canh DLL.
copy /y "%~dp0data\ky_hieu\ky_hieu.json" "%OUT%\ky_hieu.json" >nul
rem Hinh block mac dinh ky hieu bao chay (lay tu net vector PDF, scripts\hinh_tu_pdf.py): PCCC_LIB ve block moi theo hinh nay.
copy /y "%~dp0data\ky_hieu\hinh_bc.json" "%OUT%\hinh_bc.json" >nul
rem Dia chi web app Apps Script cap key, link Google Form (PCCC_BANQUYEN doc canh DLL).
copy /y "%~dp0data\ban_quyen\cau_hinh_ban_quyen.json" "%OUT%\cau_hinh_ban_quyen.json" >nul
echo OK: %OUT%\TKPCCC.Cad.dll

rem Bang block (lenh PCCC_BLOCK): DLL giao dien rieng vi can acmgd.dll, accoreconsole khong nap duoc nen chi dung trong AutoCAD co giao dien.
del /q "%OUT%\TKPCCC.UI.dll.cu*" >nul 2>&1
if exist "%OUT%\TKPCCC.UI.dll" move /y "%OUT%\TKPCCC.UI.dll" "%OUT%\TKPCCC.UI.dll.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ /out:"%OUT%\TKPCCC.UI.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" /r:"%ACAD%\acmgd.dll" /r:"%ACAD%\AdWindows.dll" ^
  /r:"%FW%\WPF\PresentationCore.dll" /r:"%FW%\WPF\WindowsBase.dll" /r:"%FW%\System.Xaml.dll" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:"%OUT%\TKPCCC.Cad.dll" ^
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

rem App tao key ban quyen cho chu plugin (khong phat hanh cho khach).
"%FW%\csc.exe" /nologo /target:winexe /codepage:65001 /optimize+ /out:"%BIN%\TKPCCC.KeyGen.exe" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  "%~dp0module\banquyen\KeyGen.cs" "%~dp0module\plugin\BanQuyen.cs" || exit /b 1
echo OK: %BIN%\TKPCCC.KeyGen.exe
echo Neu AutoCAD dang mo: tat han AutoCAD roi mo lai de nap ban moi.
exit /b 0

:khong_khoa
echo LOI: chua co %KCK% - chay cad\bin\TKPCCC.KeyGen.exe, bam "Tao cap khoa moi".
exit /b 1
