@echo off
rem Mot cu bam: cai thu vien neu chua co, bat runner (an) neu chua chay, mo trinh duyet tai http://127.0.0.1:8787
rem File nay chi dung chu ASCII (cmd doc sai UTF-8).
cd /d "%~dp0"
if not exist node_modules call npm install
netstat -ano | findstr "127.0.0.1:8787.*LISTENING" >nul || start "TKPCCC runner" /min cmd /c "npm start"
timeout /t 4 /nobreak >nul
start "" http://127.0.0.1:8787
