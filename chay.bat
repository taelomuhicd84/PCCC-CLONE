@echo off
rem Mot cu bam: cai thu vien neu chua co, bat runner (an) neu chua chay, cho runner len roi mo trinh duyet tai http://127.0.0.1:8787
rem File nay chi dung chu ASCII (cmd doc sai UTF-8).
cd /d "%~dp0"
if not exist node_modules call npm install
netstat -ano | findstr ":8787 .*LISTENING" >nul || start "TKPCCC runner" /min cmd /c "npm start"
set /a n=0
:cho
netstat -ano | findstr ":8787 .*LISTENING" >nul && goto len
set /a n+=1
if %n% GEQ 30 goto len
timeout /t 1 /nobreak >nul
goto cho
:len
start "" http://127.0.0.1:8787
