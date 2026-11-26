# Thu phan doc phien + cho chua skill cua plugin Bot chat (12_BOT_CHAT\03_PHIEN_LICH_SU\PhienAi.cs, nguoi dung 18/11) KHONG can AutoCAD.
# Chay: powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\thu_bot.ps1  -> phai ra TAT CA DAT.
$ErrorActionPreference = 'Stop'
$GOC = Split-Path (Split-Path $PSScriptRoot)
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$T = Join-Path $env:TEMP ('tkpccc_bot_' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null
& "$FW\csc.exe" /nologo /target:exe /platform:x64 /codepage:65001 /out:"$T\ThuBot.exe" /r:System.Core.dll /r:"$FW\System.Web.Extensions.dll" `
  "$GOC\07_CAD\04_KIEM_THU\ThuBot.cs" "$GOC\07_CAD\12_BOT_CHAT\03_PHIEN_LICH_SU\PhienAi.cs"
if ($LASTEXITCODE) { throw 'Bien dich ThuBot loi' }
& "$T\ThuBot.exe" $T
$ma = $LASTEXITCODE
Remove-Item -Recurse -Force $T -ErrorAction SilentlyContinue
exit $ma
