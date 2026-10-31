# Kiem thu plugin khong can mo AutoCAD: build ra thu muc tam, chay kich ban trong accoreconsole, kiem so dem.
# Chay: powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1   (file chi dung chu ASCII cho PowerShell 5.1)
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$T = Join-Path $env:TEMP ('tkpccc_thu_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null

& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\TKPCCC.Cad.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll "$GOC\cad\TKPCCC.Cad\Lenh.cs"
if ($LASTEXITCODE) { throw 'Bien dich loi' }
Copy-Item "$GOC\data\vat_tu.json" $T

# Ban ve mm: 3 dau bao khoi (1 cai copy de), 1 dau nhiet + MINSERT 2x3, ong DN50 15 m, khu "TANG 1" bao 2 dau khoi.
# Luu ban ve vao thu muc tam truoc khi boc: runner tat thi file _KL.json nam o day, khong ra Documents.
$kb = @('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\thu.dwg`"",
  'PCCC_LIB', 'Nap',
  'PCCC_DAT', 'BC_KHOI', '1000,1000', '2000,1000', '1000,1000', '',
  'PCCC_DAT', 'dau bao nhiet', '3000,1000', '',
  'MINSERT', 'BC_NHIET', '5000,5000', '1', '', '0', '2', '3', '1000', '1000',
  '-LAYER', 'M', 'PCCC_ONG_DN50', '', 'PLINE', '0,0', '10000,0', '10000,5000', '', '-LAYER', 'S', '0', '',
  'TEXT', '500,500', '300', '0', 'TANG 1', 'RECTANG', '0,0', '2500,2500',
  'PCCC_BOC', 'Co', 'L', '', '', 'ThuPlugin',
  'PCCC_VUNGBV', 'BC_KHOI', '3', 'PCCC_VUNGBV', 'BC_KHOI', '3',
  'PCCC_BANG', '0,-3000', '250',
  'PCCC_BOC', 'Khong', '', 'ThuPlugin', '_.QSAVE')
$kb -join "`r`n" | Set-Content -Encoding ascii "$T\kb.scr"
$p = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/s `"$T\kb.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb.out.txt"
if (-not $p.WaitForExit(180000)) { Stop-Process -Id $p.Id -Force; throw "accoreconsole qua 3 phut (kich ban sai dau nhac?): $T\kb.out.txt" }
$out = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb.out.txt")) -replace "`0", ''

$can = @(
  @('BC_KHOI\s+2 ', 'dem 2 dau khoi, bo 1 ban copy de'),
  @('BC_NHIET\s+7 ', 'dem 1 dau nhiet + MINSERT 2x3'),
  @('ONG_DN50\s+15 ', 'ong 15 m'),
  @('\[TANG 1\] BC_KHOI=2', 'tach theo khu'),
  @('R = 3 m .*thay 2 ', 'chay lai VUNGBV thay vong cu, khong ve trung'))
$loi = 0
foreach ($c in $can) { if ($out -match $c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
# Lan boc thu 2 (sau khi chen bang + vung bao ve) van phai ra 2 / 7: bang thong ke va vong tron khong bi dem.
$lan2 = $out.Substring($out.LastIndexOf('PCCC_BOC'))
if ($lan2 -match 'BC_KHOI\s+2 ' -and $lan2 -match 'BC_NHIET\s+7 ') { 'DAT   boc lai sau PCCC_BANG khong dem ky hieu trong bang' } else { 'LOI   boc lai sau PCCC_BANG'; $loi++ }
if ($out -match '\[TKPCCC\] L') { 'LOI   co lenh bao loi:'; ($out -split "`n" | Select-String '\[TKPCCC\] L'); $loi++ }
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
