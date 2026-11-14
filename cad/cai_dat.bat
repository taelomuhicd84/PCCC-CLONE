@echo off
rem Cai TKPCCC vao AutoCAD 2021-2024 de KHONG phai go NETLOAD. File nay chi dung chu ASCII.
rem   cai_dat.bat       tao lien ket (junction) bundle trong %APPDATA%\Autodesk\ApplicationPlugins -> AutoCAD tu nap moi lan mo;
rem                     dat bien moi truong TKPCCC_DLL cho module\lisp\TKPCCC.lsp.
rem   cai_dat.bat /go   go lien ket va bien moi truong (khong xoa gi trong repo).
rem Lien ket tro thang ve thu muc nay nen build.bat xong la AutoCAD dung ban moi (tat han AutoCAD roi mo lai).
setlocal
set "SRC=%~dp0TKPCCC.bundle"
set "PLG=%APPDATA%\Autodesk\ApplicationPlugins"
set "DST=%PLG%\TKPCCC.bundle"
if /i "%~1"=="/go" goto go

if not exist "%SRC%\Contents\TKPCCC.Cad.dll" (
  echo [TKPCCC] Chua co DLL: chay build.bat truoc.
  exit /b 1
)
if not exist "%PLG%" mkdir "%PLG%"
if exist "%DST%" (
  echo [TKPCCC] Da co "%DST%": giu nguyen.
) else (
  mklink /J "%DST%" "%SRC%" >nul || (echo [TKPCCC] Khong tao duoc lien ket. & exit /b 1)
  echo [TKPCCC] Da lien ket "%DST%" -^> "%SRC%"
)
setx TKPCCC_DLL "%SRC%\Contents\TKPCCC.Cad.dll" >nul
echo [TKPCCC] Xong. Tat han AutoCAD roi mo lai: plugin tu nap, go PCCC.
exit /b 0

:go
rem rmdir khong /s: chi go lien ket; neu la thu muc that co file thi bao loi va giu nguyen.
if exist "%DST%" rmdir "%DST%" && echo [TKPCCC] Da go lien ket "%DST%".
reg delete "HKCU\Environment" /v TKPCCC_DLL /f >nul 2>&1
echo [TKPCCC] Da go. Mo lai AutoCAD de het tu nap.
exit /b 0
