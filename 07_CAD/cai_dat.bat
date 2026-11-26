@echo off
rem Cai TKPCCC vao AutoCAD 2021-2024 de KHONG phai go NETLOAD. File nay chi dung chu ASCII.
rem   cai_dat.bat       tao / TRO LAI lien ket (junction) 3 bundle (PCCC, Bot chat, YouTube) trong %APPDATA%\Autodesk\ApplicationPlugins
rem                     ve thu muc chua file nay -> AutoCAD tu nap moi lan mo; dat bien moi truong TKPCCC_DLL cho 05_NEN_TANG\lisp\TKPCCC.lsp.
rem   cai_dat.bat /lai  chi tro lai cac lien ket DA CO (build.bat goi): khong cai them bundle nguoi dung chua cai.
rem   cai_dat.bat /go   go lien ket va bien moi truong (khong xoa gi trong repo).
rem Nguoi dung 26/11: doi ten / doi cho thu muc du an thi lien ket cu tro ve ban cu (AutoCAD nap plugin cu). Chay lai file nay o cho moi
rem (hoac build.bat) la xong: lien ket cu (ke ca dang tro sang thu muc khac) bi go va tao lai ve day.
setlocal
set LOI=
set "GOC=%~dp0"
set "PLG=%APPDATA%\Autodesk\ApplicationPlugins"
if /i "%~1"=="/go" goto go

if not exist "%GOC%TKPCCC.bundle\Contents\TKPCCC.Cad.dll" (
  echo [TKPCCC] Chua co DLL: chay build.bat truoc.
  exit /b 1
)
if not exist "%PLG%" mkdir "%PLG%"
for %%b in (TKPCCC.bundle TKPCCC_BotChat.bundle TKPCCC_YouTube.bundle) do call :lien_ket %%b "%~1"
rem Plugin + Bot chat tim repo (danh muc vat tu, 07_CAD\.mcp.json) theo file nay: ghi lai theo cho moi.
for %%b in (TKPCCC.bundle TKPCCC_BotChat.bundle) do if exist "%GOC%%%b\Contents" echo %GOC%..\03_DU_LIEU\vat_tu.json>"%GOC%%%b\Contents\nguon_vat_tu.txt"
if /i not "%~1"=="/lai" setx TKPCCC_DLL "%GOC%TKPCCC.bundle\Contents\TKPCCC.Cad.dll" >nul
if defined LOI (echo [TKPCCC] CHUA XONG: chay lai file nay trong cmd / PowerShell thuong. & exit /b 1)
echo [TKPCCC] Xong. Tat han AutoCAD roi mo lai de nap ban o day.
exit /b 0

:lien_ket
set "SRC=%GOC%%~1"
set "DST=%PLG%\%~1"
if not exist "%SRC%\Contents" goto :eof
dir /AL /B "%PLG%" 2>nul | findstr /X /I /C:"%~1" >nul
if not errorlevel 1 (
  rmdir "%DST%" || (echo [TKPCCC] Khong go duoc lien ket cu "%DST%". & set LOI=1 & goto :eof)
) else if exist "%DST%" (
  echo [TKPCCC] "%DST%" la thu muc that, khong phai lien ket: giu nguyen. Xoa tay roi chay lai neu muon dung ban o day.
  goto :eof
) else if /i "%~2"=="/lai" (
  goto :eof
)
mklink /J "%DST%" "%SRC%" >nul || (echo [TKPCCC] Khong tao duoc lien ket "%DST%". & set LOI=1 & goto :eof)
echo [TKPCCC] Lien ket "%DST%" -^> "%SRC%"
goto :eof

:go
rem rmdir khong /s: chi go lien ket; neu la thu muc that co file thi bao loi va giu nguyen.
for %%b in (TKPCCC.bundle TKPCCC_BotChat.bundle TKPCCC_YouTube.bundle) do if exist "%PLG%\%%b" rmdir "%PLG%\%%b" && echo [TKPCCC] Da go lien ket "%PLG%\%%b".
reg delete "HKCU\Environment" /v TKPCCC_DLL /f >nul 2>&1
echo [TKPCCC] Da go. Mo lai AutoCAD de het tu nap.
exit /b 0
