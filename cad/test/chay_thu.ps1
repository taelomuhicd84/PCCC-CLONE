# Kiem thu plugin khong can mo AutoCAD: build ra thu muc tam, chay kich ban trong accoreconsole, kiem so dem.
# Chay: powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1   (file chi dung chu ASCII cho PowerShell 5.1)
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$T = Join-Path $env:TEMP ('tkpccc_thu_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null

& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\TKPCCC.Cad.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll `
  "$GOC\cad\module\plugin\*.cs" "$GOC\cad\module\chung\HangDoi.cs"
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
  'RECTANG', '50000,0', '56500,4500', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', 'D', 'L', '', '',
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
  @('R = 3 m .*thay 2 ', 'chay lai VUNGBV thay vong cu, khong ve trung'),
  @('PCCC_RAI SPR_DAU_DOWN n=4 luoi=2x2 buoc=3250x2250', 'PCCC_RAI: phong 6500x4500, S = 4 m ra 4 dau phun, buoc 3250 x 2250'))
$loi = 0
foreach ($c in $can) { if ($out -match $c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
# Lan boc thu 2 (sau khi chen bang + vung bao ve) van phai ra 2 / 7: bang thong ke va vong tron khong bi dem.
$lan2 = $out.Substring($out.LastIndexOf('Command: PCCC_BOC')) # 'Command: ': thong bao loi runner cung co chu PCCC_BOC
if ($lan2 -match 'BC_KHOI\s+2 ' -and $lan2 -match 'BC_NHIET\s+7 ') { 'DAT   boc lai sau PCCC_BANG khong dem ky hieu trong bang' } else { 'LOI   boc lai sau PCCC_BANG'; $loi++ }
if ($out -match '\[TKPCCC\] L') { 'LOI   co lenh bao loi:'; ($out -split "`n" | Select-String '\[TKPCCC\] L'); $loi++ }

# --- PCCC_SPEC: chay goi spec mau (cad\skills\vi_du\thu.json) trong ban ve moi, kiem ket qua JSON ---
# Goi mau co 1 thao tac sai co y (khong_co) de kiem duong bao loi: ok = false, loi dung vi tri, cac thao tac khac van chay.
Copy-Item "$GOC\cad\skills\vi_du\thu.json" "$T\spec.json"
Copy-Item "$GOC\cad\skills\vi_du\rai.json" "$T\rai.json"
'{"ban_ve":"khac.dwg","ops":[{"op":"tron","l":"KHOA","c":[0,0],"r":100}]}' | Set-Content -Encoding ascii "$T\khoa_sai.json"
'{"ban_ve":"SPEC.DWG","ops":[{"op":"tron","l":"KHOA","c":[0,0],"r":100}]}' | Set-Content -Encoding ascii "$T\khoa_dung.json"
# "anh" dung truoc "tron" trong goi nhung phai chay sau khi ve xong; rong 700 thi lay kho 800 px. Anh ghi vao hang doi tam, khong ra %LOCALAPPDATA%.
# Vung chup nam xa moi hinh khac (x = 900000) nen anh chi co net neu "tron" da commit truoc khi in.
'{"ops":[{"op":"anh","hop":[900000,0,908000,6000],"rong":700},{"op":"tron","l":"ANH","c":[904000,3000],"r":2000}]}' | Set-Content -Encoding ascii "$T\anh.json"
$env:TKPCCC_CAD_HANG_DOI = "$T\hd"
# Ban ky_hieu.json da doi ten layer truc: chung to plugin doc mac dinh tu file du lieu (cad\data\ky_hieu), khong dung so cung trong ma.
(Get-Content -Raw -Encoding UTF8 "$GOC\cad\data\ky_hieu\ky_hieu.json") -replace '"layer_truc":\s*"TRUC"', '"layer_truc": "TRUC_T"' |
  Set-Content -Encoding UTF8 "$T\ky_hieu.json"
@('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\spec.dwg`"", 'PCCC_SPEC', "$T\spec.json", 'PCCC_SPEC', "$T\rai.json",
  'PCCC_SPEC', "$T\khoa_sai.json", 'PCCC_SPEC', "$T\khoa_dung.json", 'PCCC_SPEC', "$T\anh.json", '_.QSAVE') -join "`r`n" |
  Set-Content -Encoding ascii "$T\kb2.scr"
$p2 = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/s `"$T\kb2.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb2.out.txt"
if (-not $p2.WaitForExit(180000)) { Stop-Process -Id $p2.Id -Force; throw "accoreconsole qua 3 phut (PCCC_SPEC): $T\kb2.out.txt" }
if (Test-Path "$T\spec.kq.json") {
  $kq = Get-Content -Raw -Encoding UTF8 "$T\spec.kq.json" | ConvertFrom-Json
  $ks = @(
    @(($kq.ve -eq 50), "spec ve 50 doi tuong (thay $($kq.ve))"),
    @(($kq.xoa -eq 1), 'spec xoa 1 doi tuong theo layer'),
    @(($kq.dem.BC_KHOI -eq 3), 'spec dat 3 dau khoi'),
    @(($kq.dem.ONG_DN50 -eq 15), 'spec ong 15 m'),
    @(($kq.ok -eq $false -and @($kq.loi).Count -eq 1 -and $kq.loi[0].i -eq 8), 'spec bao dung 1 thao tac sai, khong lam hong goi'),
    @((@($kq.doc[0].ds | Where-Object { $_.l -eq 'TRUC_T' -and $_.n -eq 7 }).Count -eq 1), 'spec doc layer + ky_hieu.json doi ten layer truc: TRUC_T co 7 truc'))
  foreach ($c in $ks) { if ($c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
} else { "LOI   PCCC_SPEC khong ghi ket qua: $T\kb2.out.txt"; $loi++ }
# --- rai + tuyen (cad\skills\vi_du\rai.json): so dem, buoc, cach tuong, chieu dai tuyen ---
if (Test-Path "$T\rai.kq.json") {
  $kr = Get-Content -Raw -Encoding UTF8 "$T\rai.kq.json" | ConvertFrom-Json
  $ks2 = @(
    @(($kr.ve -eq 29), "rai + tuyen ve 29 doi tuong (thay $($kr.ve))"),
    @(($kr.rai[0].n -eq 4 -and $kr.rai[0].bx -eq 3250 -and $kr.rai[0].by -eq 2250 -and $kr.rai[0].tuong[0] -eq 1625 -and $kr.rai[0].ho -eq 0), 'rai s = 4000: 4 thiet bi, buoc <= 4000, cach tuong <= 2000, khong ho'),
    @(($kr.tuyen[0].doan -eq 1 -and [math]::Abs($kr.tuyen[0].m - 11.05) -lt 0.011), "tuyen noi_tiep: 1 duong ran bo tu diem tu, dai 11,05 m (thay $($kr.tuyen[0].m))"),
    @(($kr.rai[1].n -eq 4 -and $kr.rai[1].tuong[0] -eq 1250 -and $kr.rai[1].tuong[1] -eq 750), 'rai buoc dung 4000 x 3000: cach tuong 1250 / 750 nhu ban mau'),
    @(($kr.tuyen[1].doan -eq 3 -and $kr.tuyen[1].m -eq 12.75), "tuyen xuong_ca: 1 ong gop + 2 nhanh, dai 12,75 m (thay $($kr.tuyen[1].m))"),
    @(($kr.rai[2].n -eq 2 -and $kr.rai[2].ny -eq 1 -and $kr.tuyen[2].doan -eq 2 -and $kr.tuyen[2].m -eq 1), 'hanh lang 1 hang + tuyen nhanh: 2 doan vuong goc 0,5 m'),
    @(($kr.rai[3].n -eq 3 -and $kr.rai[3].ho -eq 0), 'rai vung chu L tren layer: bo diem ngoai da giac, khong ho'),
    @(($kr.ok -eq $false -and @($kr.loi).Count -eq 1 -and $kr.loi[0].i -eq 8), 'rai thieu s va buoc thi bao loi, khong tu dat tri so'))
  foreach ($c in $ks2) { if ($c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
} else { "LOI   rai.json khong ghi ket qua: $T\kb2.out.txt"; $loi++ }
# --- Khoa ban_ve: spec ghi ten ban ve khac thi tu choi ca goi; dung ten (khong phan biet hoa thuong) thi ve ---
$ksai = if (Test-Path "$T\khoa_sai.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\khoa_sai.kq.json" | ConvertFrom-Json }
$kdung = if (Test-Path "$T\khoa_dung.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\khoa_dung.kq.json" | ConvertFrom-Json }
if ($ksai -and $ksai.ok -eq $false -and $ksai.ve -eq 0 -and $ksai.loi[0].i -eq -1) { 'DAT   ban_ve sai ten: tu choi ca goi, khong ve' } else { 'LOI   ban_ve sai ten van ve'; $loi++ }
if ($kdung -and $kdung.ok -eq $true -and $kdung.ve -eq 1) { 'DAT   ban_ve dung ten: ve binh thuong' } else { 'LOI   ban_ve dung ten ma khong ve'; $loi++ }
# --- anh: PNG rong 800 px, co net ve (anh trang tron chi co 1 gia tri byte: vung trong, hoac in truoc khi ve xong) ---
$ka = if (Test-Path "$T\anh.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\anh.kq.json" | ConvertFrom-Json }
$fa = if ($ka) { [string]@($ka.anh)[0] }
if ($fa -and (Test-Path $fa)) {
  Add-Type -AssemblyName System.Drawing
  $bmp = New-Object Drawing.Bitmap $fa
  $rongAnh = $bmp.Width
  $du = $bmp.LockBits((New-Object Drawing.Rectangle 0, 0, $bmp.Width, $bmp.Height), 'ReadOnly', 'Format32bppArgb')
  $byte = New-Object byte[] ($du.Stride * $du.Height)
  [Runtime.InteropServices.Marshal]::Copy($du.Scan0, $byte, 0, $byte.Length)
  $bmp.UnlockBits($du); $bmp.Dispose()
  $soMau = (New-Object 'Collections.Generic.HashSet[byte]' (, $byte)).Count
  if ($ka.ok -eq $true -and $rongAnh -eq 800 -and $soMau -gt 1) { 'DAT   anh: PNG 800 px co net ve, in sau khi ve xong' } else { "LOI   anh: rong $rongAnh, so gia tri byte $soMau"; $loi++ }
} else { "LOI   anh khong ra PNG: $T\kb2.out.txt"; $loi++ }
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
