@echo off
rem Bien dich plugin bang csc.exe co san cua .NET Framework (khong can cai SDK). File nay chi dung chu ASCII (cmd).
rem Ket qua: 07_CAD\TKPCCC.bundle\Contents\TKPCCC.Cad.dll -> AutoCAD: NETLOAD file nay,
rem hoac chep ca thu muc TKPCCC.bundle vao %%APPDATA%%\Autodesk\ApplicationPlugins de tu nap khi mo AutoCAD.
rem Chi tham chieu acdbmgd + accoremgd (khong acmgd) nen chay duoc ca trong accoreconsole.
setlocal
set ACAD=%ACAD_DIR%
if "%ACAD%"=="" set ACAD=C:\Program Files\Autodesk\AutoCAD 2021
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set OUT=%~dp0TKPCCC.bundle\Contents
if not exist "%OUT%" mkdir "%OUT%"

rem Nguoi dung 26/11: moi thu muc so NN_TEN la mot hang muc tinh nang. Plugin PCCC gom tu moi NN_*\cad\*.cs (TKPCCC.Cad.dll,
rem chay duoc trong accoreconsole) va NN_*\ui\*.cs (TKPCCC.UI.dll, duoc tham chieu acmgd). Them tinh nang = them thu muc, khong sua file nay.
rem Nguoi dung 26/11 (chieu): gom hang muc theo nhom linh vuc (09_PCCC\NN_*): gom ca NN_*\NN_*\cad, NN_*\NN_*\ui.
set "CS_CAD="
set "CS_UI="
for /d %%g in ("%~dp0??_*") do for /d %%d in ("%%~g" "%%~g\??_*") do if exist "%%~d\cad\" call set CS_CAD=%%CS_CAD%% "%%~d\cad\*.cs"
for /d %%g in ("%~dp0??_*") do for /d %%d in ("%%~g" "%%~g\??_*") do if exist "%%~d\ui\" call set CS_UI=%%CS_UI%% "%%~d\ui\*.cs"
set CHUNG=%~dp005_NEN_TANG\chung

rem AutoCAD dang mo thi DLL bi khoa ghi nhung van doi ten duoc: doi ten ban cu, AutoCAD mo lai se nap ban moi.
rem "build.bat phat_hanh": ban phat hanh co chan ban quyen (/define:BAN_QUYEN) va nhung khoa cong khai vao DLL.
set DEF=
set RES=
set KCK=%~dp003_DU_LIEU_VE\ban_quyen\khoa_cong_khai.xml
if /i "%~1"=="phat_hanh" (
  if not exist "%KCK%" goto khong_khoa
  set DEF=/define:BAN_QUYEN
  set RES="/resource:%KCK%,khoa_cong_khai.xml"
)

del /q "%OUT%\TKPCCC.Cad.dll.cu*" >nul 2>&1
if exist "%OUT%\TKPCCC.Cad.dll" move /y "%OUT%\TKPCCC.Cad.dll" "%OUT%\TKPCCC.Cad.dll.cu%RANDOM%" >nul

"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ %DEF% %RES% /out:"%OUT%\TKPCCC.Cad.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  %CS_CAD% "%CHUNG%\HangDoi.cs" || exit /b 1

copy /y "%~dp0..\03_DU_LIEU\vat_tu.json" "%OUT%\vat_tu.json" >nul
rem Tri so rai thiet bi theo tieu chuan (rai / PCCC_RAI khong co "s"): nguon o 03_DU_LIEU\khoang_cach_rai.json goc repo.
copy /y "%~dp0..\03_DU_LIEU\khoang_cach_rai.json" "%OUT%\khoang_cach_rai.json" >nul
rem Bang DN ong nhanh theo so dau phun (PCCC_TUYEN TuDong): nguon duy nhat 03_DU_LIEU\ong_nhanh_dn.json goc repo.
copy /y "%~dp0..\03_DU_LIEU\ong_nhanh_dn.json" "%OUT%\ong_nhanh_dn.json" >nul
rem Bang chon loai dau bao theo cong nang (trang Bao chay doc bang "Loai theo khu vuc"): nguon duy nhat 01_TRI_THUC\07_BAO_CHAY\chon-loai-dau-bao.md.
copy /y "%~dp0..\01_TRI_THUC\07_BAO_CHAY\chon-loai-dau-bao.md" "%OUT%\chon-loai-dau-bao.md" >nul
rem Van thuyet minh mau (PCCC_THUYETMINH): nguon duy nhat 07_CAD\01_TRI_THUC_VE\ve\mau-thuyet-minh-pccc.md.
copy /y "%~dp001_TRI_THUC_VE\ve\mau-thuyet-minh-pccc.md" "%OUT%\mau-thuyet-minh-pccc.md" >nul
rem Bang khoang dam 5.9.1.1.6 (nut Tra cuu TCVN, tab Khoang dam trang Bao chay): nguon duy nhat 02_DIEU_KHOAN\07_BAO_CHAY\MOD_dau_bao_khoi_khoang_dam.md goc repo.
copy /y "%~dp0..\02_DIEU_KHOAN\07_BAO_CHAY\MOD_dau_bao_khoi_khoang_dam.md" "%OUT%\MOD_dau_bao_khoi_khoang_dam.md" >nul
rem PCCC_THEMBLOCK ghi ma moi vao ca file goc cua repo (nguon duy nhat cua danh muc): de lai duong dan cho plugin.
echo %~dp0..\03_DU_LIEU\vat_tu.json>"%OUT%\nguon_vat_tu.txt"
rem Mac dinh ky hieu truc, kich thuoc, chu: nguoi dung sua o 07_CAD\03_DU_LIEU_VE\ky_hieu, plugin doc ban chep canh DLL.
copy /y "%~dp003_DU_LIEU_VE\ky_hieu\ky_hieu.json" "%OUT%\ky_hieu.json" >nul
rem Hinh block mac dinh ky hieu bao chay (lay tu net vector PDF, scripts\hinh_tu_pdf.py): PCCC_LIB ve block moi theo hinh nay.
copy /y "%~dp003_DU_LIEU_VE\ky_hieu\hinh_bc.json" "%OUT%\hinh_bc.json" >nul
rem Dia chi web app Apps Script cap key, link Google Form (PCCC_BANQUYEN doc canh DLL).
copy /y "%~dp003_DU_LIEU_VE\ban_quyen\cau_hinh_ban_quyen.json" "%OUT%\cau_hinh_ban_quyen.json" >nul
echo OK: %OUT%\TKPCCC.Cad.dll

rem Bang block (lenh PCCC_BLOCK): DLL giao dien rieng vi can acmgd.dll, accoreconsole khong nap duoc nen chi dung trong AutoCAD co giao dien.
del /q "%OUT%\TKPCCC.UI.dll.cu*" >nul 2>&1
if exist "%OUT%\TKPCCC.UI.dll" move /y "%OUT%\TKPCCC.UI.dll" "%OUT%\TKPCCC.UI.dll.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ /out:"%OUT%\TKPCCC.UI.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" /r:"%ACAD%\acmgd.dll" /r:"%ACAD%\AdWindows.dll" ^
  /r:"%FW%\WPF\PresentationCore.dll" /r:"%FW%\WPF\WindowsBase.dll" /r:"%FW%\System.Xaml.dll" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:"%OUT%\TKPCCC.Cad.dll" ^
  %CS_UI% || exit /b 1
echo OK: %OUT%\TKPCCC.UI.dll

rem Nguoi dung 17/11: tach 3 plugin (PCCC, YouTube, Bot chat), moi plugin mot bundle rieng, nap / go doc lap (Hub TKPCCC).
rem Hai plugin rieng khong tham chieu DLL PCCC: bien dich kem 05_NEN_TANG\chung\GiaoDienNho.cs (mau, chu, cai_dat.json chung).
set OUTYT=%~dp0TKPCCC_YouTube.bundle\Contents
if not exist "%OUTYT%" mkdir "%OUTYT%"
del /q "%OUTYT%\TKPCCC.YouTube.dll.cu*" >nul 2>&1
if exist "%OUTYT%\TKPCCC.YouTube.dll" move /y "%OUTYT%\TKPCCC.YouTube.dll" "%OUTYT%\TKPCCC.YouTube.dll.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ /out:"%OUTYT%\TKPCCC.YouTube.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" /r:"%ACAD%\acmgd.dll" /r:"%ACAD%\AdWindows.dll" ^
  /r:"%FW%\WPF\PresentationCore.dll" /r:"%FW%\WPF\WindowsBase.dll" /r:"%FW%\System.Xaml.dll" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll ^
  /r:"%~dp013_YOUTUBE\webview2\Microsoft.Web.WebView2.Core.dll" /r:"%~dp013_YOUTUBE\webview2\Microsoft.Web.WebView2.WinForms.dll" ^
  "%~dp013_YOUTUBE\*.cs" "%CHUNG%\GiaoDienNho.cs" "%CHUNG%\HangDoi.cs" || exit /b 1
rem WebView2: 3 DLL cua Microsoft canh TKPCCC.YouTube.dll. Chi chep khi thieu: AutoCAD dang mo giu khoa file.
for %%f in (Microsoft.Web.WebView2.Core.dll Microsoft.Web.WebView2.WinForms.dll WebView2Loader.dll) do if not exist "%OUTYT%\%%f" copy /y "%~dp013_YOUTUBE\webview2\%%f" "%OUTYT%\%%f" >nul
rem Extension Brave "toan man hinh trong khung" (nguoi dung 18/11), nap bang --load-extension khi mo Brave ho so rieng.
if not exist "%OUTYT%\brave_khung" mkdir "%OUTYT%\brave_khung"
copy /y "%~dp013_YOUTUBE\brave_khung\*" "%OUTYT%\brave_khung\" >nul
echo OK: %OUTYT%\TKPCCC.YouTube.dll

set OUTBOT=%~dp0TKPCCC_BotChat.bundle\Contents
if not exist "%OUTBOT%" mkdir "%OUTBOT%"
del /q "%OUTBOT%\TKPCCC.BotChat.dll.cu*" >nul 2>&1
if exist "%OUTBOT%\TKPCCC.BotChat.dll" move /y "%OUTBOT%\TKPCCC.BotChat.dll" "%OUTBOT%\TKPCCC.BotChat.dll.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /optimize+ /out:"%OUTBOT%\TKPCCC.BotChat.dll" ^
  /r:"%ACAD%\acdbmgd.dll" /r:"%ACAD%\accoremgd.dll" /r:"%ACAD%\acmgd.dll" /r:"%ACAD%\AdWindows.dll" ^
  /r:"%FW%\WPF\PresentationCore.dll" /r:"%FW%\WPF\WindowsBase.dll" /r:"%FW%\System.Xaml.dll" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /recurse:"%~dp012_BOT_CHAT\*.cs" "%CHUNG%\GiaoDienNho.cs" || exit /b 1
rem Bot chat tim repo (07_CAD\.mcp.json) tu duong dan nay, nhu plugin PCCC.
echo %~dp0..\03_DU_LIEU\vat_tu.json>"%OUTBOT%\nguon_vat_tu.txt"
echo OK: %OUTBOT%\TKPCCC.BotChat.dll

rem May chu MCP (07_CAD\bin\TKPCCC.Mcp.exe): AI goi cad_tt / cad_chay. Dang chay thi doi ten ban cu nhu DLL.
set BIN=%~dp0bin
if not exist "%BIN%" mkdir "%BIN%"
del /q "%BIN%\TKPCCC.Mcp.exe.cu*" >nul 2>&1
if exist "%BIN%\TKPCCC.Mcp.exe" move /y "%BIN%\TKPCCC.Mcp.exe" "%BIN%\TKPCCC.Mcp.exe.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:exe /codepage:65001 /optimize+ /out:"%BIN%\TKPCCC.Mcp.exe" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll ^
  "%~dp006_VE_TU_DONG_AI\mcp\Mcp.cs" "%CHUNG%\HangDoi.cs" || exit /b 1
copy /y "%~dp001_TRI_THUC_VE\TU_VUNG.md" "%BIN%\TU_VUNG.md" >nul
echo OK: %BIN%\TKPCCC.Mcp.exe

rem Hub (07_CAD\bin\TKPCCC.Hub.exe): cai plugin tu nap + ghi MCP cad cho cac AI tren may, chan doan.
del /q "%BIN%\TKPCCC.Hub.exe.cu*" >nul 2>&1
if exist "%BIN%\TKPCCC.Hub.exe" move /y "%BIN%\TKPCCC.Hub.exe" "%BIN%\TKPCCC.Hub.exe.cu%RANDOM%" >nul
"%FW%\csc.exe" /nologo /target:winexe /codepage:65001 /optimize+ /out:"%BIN%\TKPCCC.Hub.exe" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  "%~dp014_HUB\Hub.cs" "%CHUNG%\HangDoi.cs" || exit /b 1
echo OK: %BIN%\TKPCCC.Hub.exe

rem App tao key ban quyen cho chu plugin (khong phat hanh cho khach).
"%FW%\csc.exe" /nologo /target:winexe /codepage:65001 /optimize+ /out:"%BIN%\TKPCCC.KeyGen.exe" ^
  /r:"%FW%\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  "%~dp011_BAN_QUYEN\keygen\KeyGen.cs" "%~dp011_BAN_QUYEN\cad\BanQuyen.cs" || exit /b 1
echo OK: %BIN%\TKPCCC.KeyGen.exe
rem Nguoi dung 26/11: doi cho / doi ten thu muc du an thi lien ket AutoCAD tu nap van tro ban cu. Build xong tro lai cac lien ket da co ve day,
rem tru ban build trong worktree / thu muc tam (agent build thu khong duoc chiem lien ket cua AutoCAD that).
echo %~dp0| findstr /I /C:"\.claude\worktrees\" /C:"\Temp\" >nul || call "%~dp0cai_dat.bat" /lai
echo Neu AutoCAD dang mo: tat han AutoCAD roi mo lai de nap ban moi.
exit /b 0

:khong_khoa
echo LOI: chua co %KCK% - chay 07_CAD\bin\TKPCCC.KeyGen.exe, bam "Tao cap khoa moi".
exit /b 1
