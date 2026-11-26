# Thu che do Brave cua plugin YouTube (nguoi dung 18/11) KHONG can AutoCAD: extension toan man hinh trong khung + gan chu cua so (GanBrave.cs).
# Dung ho so Brave tam trong %TEMP% (khong dung Brave cua nguoi dung); mot cua so Brave nho hien goc tren trai vai giay roi tu tat.
# Chay: powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\thu_brave.ps1  -> phai ra TAT CA DAT. Khong co Brave thi bo qua.
$ErrorActionPreference = 'Stop'
$GOC = Split-Path (Split-Path $PSScriptRoot)
$brave = @("$env:ProgramFiles", "${env:ProgramFiles(x86)}", "$env:LOCALAPPDATA") | ForEach-Object { Join-Path $_ 'BraveSoftware\Brave-Browser\Application\brave.exe' } |
  Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $brave) { 'BO QUA: may khong co Brave'; exit 0 }
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$T = Join-Path $env:TEMP ('tkpccc_brave_' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null
& "$FW\csc.exe" /nologo /target:exe /platform:x64 /codepage:65001 /out:"$T\ThuBrave.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll `
  "$GOC\07_CAD\04_KIEM_THU\ThuBrave.cs" "$GOC\07_CAD\13_YOUTUBE\GanBrave.cs"
if ($LASTEXITCODE) { throw 'Bien dich ThuBrave loi' }
& "$T\ThuBrave.exe" $brave "$GOC\07_CAD\13_YOUTUBE\brave_khung" $T
$ma = $LASTEXITCODE
Start-Sleep 2
Remove-Item -Recurse -Force $T -ErrorAction SilentlyContinue
exit $ma
