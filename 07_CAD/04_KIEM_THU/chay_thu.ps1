# Kiem thu plugin khong can mo AutoCAD: build ra thu muc tam, chay kich ban trong accoreconsole, kiem so dem.
# Chay: powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\chay_thu.ps1   (file chi dung chu ASCII cho PowerShell 5.1)
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
# Moi thu muc so 07_CAD\NN_TEN la mot hang muc tinh nang (26/11): cad\*.cs -> TKPCCC.Cad.dll, ui\*.cs -> TKPCCC.UI.dll (nhu build.bat).
$CS_CAD = @(Get-ChildItem "$GOC\07_CAD\??_*\cad\*.cs" | ForEach-Object FullName); $CS_UI = @(Get-ChildItem "$GOC\07_CAD\??_*\ui\*.cs" | ForEach-Object FullName)
$T = Join-Path $env:TEMP ('tkpccc_thu_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null
# Cai dat nguoi dung (kieu dim, layer day...) doc tu file tam, khong dung %APPDATA% cua may: test lap lai duoc.
$env:TKPCCC_CAI_DAT = "$T\cai_dat.json"

& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\TKPCCC.Cad.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  $CS_CAD "$GOC\07_CAD\05_NEN_TANG\chung\HangDoi.cs"
if ($LASTEXITCODE) { throw 'Bien dich loi' }
# DLL giao dien (bang PCCC_BLOCK): accoreconsole khong nap duoc nen o day chi kiem bien dich; chay that phai thu trong AutoCAD.
& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\TKPCCC.UI.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$ACAD\acmgd.dll" /r:"$ACAD\AdWindows.dll" `
  /r:"$FW\WPF\PresentationCore.dll" /r:"$FW\WPF\WindowsBase.dll" /r:"$FW\System.Xaml.dll" `
  /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:"$T\TKPCCC.Cad.dll" $CS_UI
if ($LASTEXITCODE) { throw 'Bien dich TKPCCC.UI.dll loi' }
# DLL chi cho test: chen block dong tu ban ve mau Autodesk (cai cung AutoCAD) va dat trang thai Visibility (accoreconsole khong co setpropertyvalue).
& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\ThuBlockDong.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:System.Core.dll "$PSScriptRoot\ThuBlockDong.cs"
if ($LASTEXITCODE) { throw 'Bien dich ThuBlockDong.dll loi' }
# Block dong (16/11, hoc tu duy LHBBlockScheduler): "Pilot Light" tham so Color (khong ten Visibility -> nhanh Lookup), "Hex Socket Bolt" co ca
# Size (Lookup, dung truoc) va Visibility (phai lay Visibility). Gan ma: ca block = DEN_EXIT / BINH_BOT, rieng trang thai Amber = DEN_SUCO.
$mauDong = "$ACAD\Sample\en-us\Dynamic Blocks"
@("$mauDong\Electrical - Metric.dwg|Pilot Light - Metric|Color|Red|300000|0", "$mauDong\Electrical - Metric.dwg|Pilot Light - Metric|Color|Red|305000|0",
  "$mauDong\Electrical - Metric.dwg|Pilot Light - Metric|Color|Green|310000|0", "$mauDong\Electrical - Metric.dwg|Pilot Light - Metric|Color|Amber|315000|0",
  "$mauDong\Mechanical - Metric.dwg|Hex Socket Bolt (Side) - Metric|Visibility|M8|320000|0",
  "$mauDong\Mechanical - Metric.dwg|Hex Socket Bolt (Side) - Metric|Visibility|M10|325000|0",
  "$mauDong\Mechanical - Metric.dwg|Hex Socket Bolt (Side) - Metric|Visibility|M10|330000|0") | Set-Content -Encoding ascii "$T\thu_dong.txt"
$env:TKPCCC_THU_DONG = "$T\thu_dong.txt"
'{"Pilot Light - Metric":"DEN_EXIT","Pilot Light - Metric|Amber":"DEN_SUCO","Hex Socket Bolt (Side) - Metric":"BINH_BOT"}' | Set-Content -Encoding ascii "$T\block_alias.json"
Copy-Item "$GOC\03_DU_LIEU\vat_tu.json" $T
Copy-Item "$GOC\03_DU_LIEU\khoang_cach_rai.json" $T  # tri so rai theo tieu chuan (rai / PCCC_RAI khong co s)
Copy-Item "$GOC\03_DU_LIEU\ong_nhanh_dn.json" $T  # bang DN theo so dau phun (PCCC_TUYEN TuDong)
Copy-Item "$GOC\07_CAD\01_TRI_THUC_VE\ve\mau-thuyet-minh-pccc.md" $T  # van thuyet minh mau (PCCC_THUYETMINH)
Copy-Item "$GOC\07_CAD\03_DU_LIEU_VE\ky_hieu\hinh_bc.json" $T  # hinh block mac dinh (lay tu PDF trang 11): PCCC_LIB tao thu vien moi theo hinh nay
# File danh muc "goc" cua repo: tro toi ban chep tam (nhu build.bat ghi nguon_vat_tu.txt) de PCCC_THEMBLOCK ghi ca hai file ma khong dung repo.
New-Item -ItemType Directory "$T\goc" | Out-Null
Copy-Item "$GOC\03_DU_LIEU\vat_tu.json" "$T\goc"
"$T\goc\vat_tu.json" | Set-Content -Encoding ascii "$T\nguon_vat_tu.txt"

# Be rong anh va so gia tri byte khac nhau cua anh: anh trang tron chi co 1 gia tri.
Add-Type -AssemblyName System.Drawing
function DocAnh($f) {
  $bmp = New-Object Drawing.Bitmap $f
  $du = $bmp.LockBits((New-Object Drawing.Rectangle 0, 0, $bmp.Width, $bmp.Height), 'ReadOnly', 'Format32bppArgb')
  $byte = New-Object byte[] ($du.Stride * $du.Height)
  [Runtime.InteropServices.Marshal]::Copy($du.Scan0, $byte, 0, $byte.Length)
  $kq = @{ rong = $bmp.Width; mau = (New-Object 'Collections.Generic.HashSet[byte]' (, $byte)).Count }
  $bmp.UnlockBits($du); $bmp.Dispose()
  $kq
}

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
  'RECTANG', '50000,0', '66000,12000', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '12', '', '', 'L', '', '', '',
  'RECTANG', '100000,0', '114000,10000', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '', '1.5', '', 'L', '', '', '',
  'PLINE', '200000,0', '212000,0', '212000,8000', '203000,8000', '203000,5000', '200000,5000', 'C', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '', '', 'Co', 'L', '', '', '',
  'PLINE', '49000,-1000', '67000,-1000', '', '_.ZOOM', '_E', 'OSMODE', '0', 'PCCC_TUYEN', 'ONG_DN25', 'W', '50000,0', '66000,12000', '', 'L', '65000,2000',
  'PLINE', '49000,-1000', '67000,-1000', '', 'PCCC_TUYEN', 'TuDong', '', 'W', '50000,0', '66000,12000', '', 'L', '65000,2000',
  'CIRCLE', '80000,0', '150', '-BLOCK', 'VAN_KHACH', '80000,0', 'L', '', '-INSERT', 'VAN_KHACH', '80000,0', '1', '', '0',
  'PCCC_THEMBLOCK', 'L', 'van_khach', 'Van khach them', 'SPR', '', 'PCCC_DAT', 'VAN_KHACH', '81000,0', '',
  'PCCC_KYHIEU', '', '0,-20000', '', 'PCCC_QUYDINH', '0,-40000', '',
  'RECTANG', '0,-60000', '10000,-58000', 'PCCC_THOATNAN', 'L', '', 'PCCC_DAT', 'HVT_TU', '20000,-60000', '', 'PCCC_VUNGBV', 'HVT_TU', '26',
  'PCCC_THUYETMINH', '0,-80000', '',
  'NETLOAD', "`"$T\ThuBlockDong.dll`"", 'THU_BLOCKDONG',
  'PCCC_BOC', 'Khong', '', 'ThuPlugin', 'PCCC_BANG', '300000,-20000', '250', '_.QSAVE')
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
  # dt doi luoi: S = 4 m khong co dt ra 4 x 3 (16 m2 moi dau); co dt 12 phai ra 4 x 4
  @('PCCC_RAI SPR_DAU_DOWN n=16 luoi=4x4 buoc=4000x3000 m2=12\b', 'PCCC_RAI: phong 16000x12000, S = 4 m, dt 12 m2 ra 4 x 4 (khong phai 4 x 3)'),
  # diem bam ngoai cac cot (x = 65000; OSMODE 0 de diem go khong bi bat vao dau phun x 64000): 11500 + 4 x 13000 = 63,5 m; bo qua diem bam (Enter, cot 56000) chi ra 59,5 m
  # le 1,5 m: phong 14000x10000, S 4 m: x n=4, (14-3)/3 = 3,667 lam tron 3,65 (cach tuong 1,525); y n=3, (10-3)/2 = 3,5. Khong le: 3500x3300
  @('PCCC_RAI SPR_DAU_DOWN n=12 luoi=4x3 buoc=3650x3500 ', 'PCCC_RAI go tay co le 1,5 m: phong 14000x10000 ra 4 x 3, buoc 3650x3500'),
  # theo_hang go tay (tra loi Co): phong chu L 12000x8000 khoet 3000x3000 goc tren trai: luoi 6 dau con HO 1,1 % (viec 1, 13/11: phu kin them 1 dau) ra 7 dau, khong HO
  @('PCCC_RAI SPR_DAU_DOWN n=7 luoi=3x2 buoc=4000x4000 m2=16( kt=\d+)?(\r|\n)', 'PCCC_RAI go tay tra loi Co (theo_hang): phong chu L phu kin 7 dau, khong HO'),
  @('PCCC_TUYEN ONG_DN25 doan=5 m=63\.5\b', 'PCCC_TUYEN: ong gop qua diem bam x = 65000 (ngoai cac cot) + 4 nhanh, dai 63,5 m'),
  # TuDong (bang 03_DU_LIEU/ong_nhanh_dn.json, muc Den05): 4 hang x 4 dau; ong gop 16, 12 dau DN65 (DN70 cua bang), 8 DN50, 4 DN40; moi nhanh 4 DN40, 3 DN32, 2 + 1 DN25 (DN20 '-')
  @('PCCC_TUYEN TuDong Den05 doan=20 m=63\.5 DN25=8 DN32=4 DN40=5 DN50=1 DN65=2(\r|\n)', 'PCCC_TUYEN TuDong: cat tung dau phun, DN theo so dau phia sau (den 0,5 MPa), cung 63,5 m'),
  @('PCCC_THEMBLOCK VAN_KHACH', 'PCCC_THEMBLOCK: block ten VAN_KHACH them duoi ma van_khach (ma trung ten block khong bi bao trung)'),
  @('VAN_KHACH\s+2 ', 'block moi them: PCCC_DAT dat duoc, PCCC_BOC dem 2 cai (ban chen tay + ban PCCC_DAT)'),
  @('PCCC_KYHIEU dong=\d+ [^\r\n]*BC_KHOI[^\r\n]*BC_NHIET', 'PCCC_KYHIEU: bang ky hieu liet ke thiet bi co trong ban ve (dau khoi, dau nhiet)'),
  @('PCCC_QUYDINH dong=3\b', 'PCCC_QUYDINH: bang quy dinh lap dat 3 loai dau bao tu khoang_cach_rai.json'),
  @('PCCC_THOATNAN vung=1 m2=20\b', 'PCCC_THOATNAN: hatch ANSI31 duong thoat nan trong polyline 10 x 2 m'),
  @('PCCC_THUYETMINH he=BC,SPR,HVT muc=7\b', 'PCCC_THUYETMINH: ban ve co bao chay, sprinkler, vach tuong ra 3 cot, 7 muc van mau'),
  @('R = 26 m tr.n layer PCCC_VUNGBV_HVT_TU \(n.t ..t, in [^)]+\)', 'PCCC_VUNGBV tu vach tuong: vong net dut, layer in duoc'))
$loi = 0
foreach ($c in $can) { if ($out -match $c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
# Lan boc thu 2 (sau khi chen bang + vung bao ve) van phai ra 2 / 7: bang thong ke va vong tron khong bi dem.
$lan2 = $out.Substring($out.LastIndexOf('Command: PCCC_BOC')) # 'Command: ': thong bao loi runner cung co chu PCCC_BOC
if ($lan2 -match 'BC_KHOI\s+2 ' -and $lan2 -match 'BC_NHIET\s+7 ') { 'DAT   boc lai sau PCCC_BANG khong dem ky hieu trong bang' } else { 'LOI   boc lai sau PCCC_BANG'; $loi++ }
# Block dong tach theo chung loai (Visibility, Lookup): tong theo ma giu nguyen, dong '+ chung loai' duoi ma, gan rieng trang thai Amber sang ma khac.
if ($out -match 'THU_BLOCKDONG chen=7 sai=0') { 'DAT   chen 7 block dong mau Autodesk, dat duoc trang thai' } else { "LOI   THU_BLOCKDONG (thieu $mauDong ?)"; $loi++ }
$cl = '[^"\r\n]*"'
if ($lan2 -match "DEN_EXIT\s+3 [^\r\n]*[\r\n]+\s+\+ $cl`Green`": 1[\r\n]+\s+\+ $cl`Red`": 2[\r\n]") { 'DAT   block dong tach chung loai: DEN_EXIT 3 = Green 1 + Red 2 (tham so Color, nhanh Lookup)' } else { 'LOI   DEN_EXIT tach chung loai Green 1 / Red 2'; $loi++ }
if ($lan2 -match "DEN_SUCO\s+1 [^\r\n]*[\r\n]+\s+\+ $cl`Amber`": 1[\r\n]") { 'DAT   gan rieng trang thai (block_alias "ten|Amber") ra ma khac DEN_SUCO' } else { 'LOI   alias rieng trang thai Amber -> DEN_SUCO'; $loi++ }
if ($lan2 -match "BINH_BOT\s+3 [^\r\n]*[\r\n]+\s+\+ $cl`M10`": 2[\r\n]+\s+\+ $cl`M8`": 1[\r\n]") { 'DAT   block co ca Lookup Size va Visibility: tach theo Visibility (M10 2, M8 1)' } else { 'LOI   BINH_BOT tach theo Visibility M10 2 / M8 1'; $loi++ }
if ($lan2 -match 'PCCC_BANG dong=\d+ chung_loai=BINH_BOT:M10;BINH_BOT:M8;DEN_SUCO:Amber;DEN_EXIT:Green;DEN_EXIT:Red(\r|\n)') { 'DAT   PCCC_BANG co cot Chung loai, moi trang thai mot dong' } else { 'LOI   PCCC_BANG cot Chung loai'; $loi++ }
if ($out -match '\[TKPCCC\] L') { 'LOI   co lenh bao loi:'; ($out -split "`n" | Select-String '\[TKPCCC\] L'); $loi++ }
# PCCC_THEMBLOCK ghi them 1 dong vao vat_tu.json canh DLL va vao file goc (nguon_vat_tu.txt tro toi ban chep tam $T\goc, repo khong bi dung toi).
$dmThu = Get-Content -Raw -Encoding UTF8 "$T\vat_tu.json" | ConvertFrom-Json
$dmGoc = Get-Content -Raw -Encoding UTF8 "$GOC\03_DU_LIEU\vat_tu.json" | ConvertFrom-Json
$dmGocThu = Get-Content -Raw -Encoding UTF8 "$T\goc\vat_tu.json" | ConvertFrom-Json
$moi = @($dmThu.vat_tu | Where-Object { $_.ma -eq 'VAN_KHACH' })
if ($moi.Count -eq 1 -and $moi[0].ten -eq 'Van khach them' -and $moi[0].he -eq 'SPR' -and $moi[0].loai -eq 'block' -and $moi[0].dvt -eq ('c' + [char]0xE1 + 'i') -and
  @($dmThu.vat_tu).Count -eq @($dmGoc.vat_tu).Count + 1 -and @($dmGocThu.vat_tu | Where-Object { $_.ma -eq 'VAN_KHACH' }).Count -eq 1 -and
  @($dmGocThu.vat_tu).Count -eq @($dmGoc.vat_tu).Count + 1) { 'DAT   vat_tu.json canh DLL va file goc (nguon_vat_tu.txt) them dung 1 dong cho ma moi' } else { 'LOI   vat_tu.json sau PCCC_THEMBLOCK'; $loi++ }
# Anh xem truoc cho bang PCCC_BLOCK: PCCC_LIB ve cho block mac dinh, PCCC_THEMBLOCK ve cho block moi; anh phai co net.
$anhLoi = @('BC_KHOI', 'VAN_KHACH' | Where-Object { -not (Test-Path "$T\anh_block\$_.png") -or (DocAnh "$T\anh_block\$_.png").mau -le 1 })
if ($anhLoi.Count -eq 0) { 'DAT   anh xem truoc block: BC_KHOI, VAN_KHACH co anh co net' } else { "LOI   anh xem truoc block thieu hoac trang: $($anhLoi -join ', ')"; $loi++ }
function Kiem($dk, $ten) { if ($dk) { "DAT   $ten" } else { "LOI   $ten"; $script:loi++ } }
# Review 15/11 (R3b viec 5): accoreconsole phai chay DLL vua build, khong phai ban bundle tu nap (Initialize in duong dan DLL)
$nap = @([regex]::Matches($out, 'plugin PCCC \(([^)]+)\)') | ForEach-Object { $_.Groups[1].Value })
Kiem ($nap.Count -ge 1 -and @($nap | Where-Object { $_ -ine "$T\TKPCCC.Cad.dll" }).Count -eq 0) "DLL dang chay la ban vua build ($($nap -join ', ')); khac thi accoreconsole dang nap bundle tu nap: chay 07_CAD\cai_dat.bat /go roi thu lai"
# Mau anh = mau layer PCCC_<he> ma block duoc chen vao (net layer 0 ByLayer), khong phai mau layer 0 cua thu vien (trang); mien to (Solid) phai to dac.
function DemMau($f, $r0, $g0, $b0) {
  $bmp = New-Object Drawing.Bitmap $f; $n = 0; $trang = 0
  for ($y = 0; $y -lt $bmp.Height; $y++) { for ($x = 0; $x -lt $bmp.Width; $x++) {
    $c = $bmp.GetPixel($x, $y); if ($c.A -lt 128) { continue }
    if ([Math]::Abs($c.R - $r0) + [Math]::Abs($c.G - $g0) + [Math]::Abs($c.B - $b0) -lt 120) { $n++ }
    if ($c.R -gt 200 -and $c.G -gt 200 -and $c.B -gt 200) { $trang++ } } }
  $bmp.Dispose(); @{ n = $n; trang = $trang; rong = 0 + (New-Object Drawing.Bitmap $f).Width }
}
$mDo = if (Test-Path "$T\anh_block\BC_KHOI.png") { DemMau "$T\anh_block\BC_KHOI.png" 255 0 0 } else { @{ n = 0; trang = 0; rong = 0 } }
$mXanh = if (Test-Path "$T\anh_block\VAN_KHACH.png") { DemMau "$T\anh_block\VAN_KHACH.png" 0 0 255 } else { @{ n = 0; trang = 0 } }
$mTo = if (Test-Path "$T\anh_block\BC_DTCD.png") { DemMau "$T\anh_block\BC_DTCD.png" 255 0 0 } else { @{ n = 0 } }
Kiem ($mDo.n -gt 50 -and $mDo.trang -lt $mDo.n -and $mDo.rong -ge 128) "anh BC_KHOI do (layer PCCC_BC), 128 px: do=$($mDo.n) trang=$($mDo.trang) rong=$($mDo.rong)"
Kiem ($mXanh.n -gt 50 -and $mXanh.trang -lt $mXanh.n) "anh VAN_KHACH (he SPR, block nguoi dung tren layer 0) xanh: xanh=$($mXanh.n) trang=$($mXanh.trang)"
Kiem ($mTo.n -gt 1500) "anh BC_DTCD to dac mien Solid: diem do=$($mTo.n)"

# --- PCCC_SPEC: chay goi spec mau (07_CAD\01_TRI_THUC_VE\vi_du\thu.json) trong ban ve moi, kiem ket qua JSON ---
# Goi mau co 1 thao tac sai co y (khong_co) de kiem duong bao loi: ok = false, loi dung vi tri, cac thao tac khac van chay.
Copy-Item "$GOC\07_CAD\01_TRI_THUC_VE\vi_du\thu.json" "$T\spec.json"
Copy-Item "$GOC\07_CAD\01_TRI_THUC_VE\vi_du\rai.json" "$T\rai.json"
'{"ban_ve":"khac.dwg","ops":[{"op":"tron","l":"KHOA","c":[0,0],"r":100}]}' | Set-Content -Encoding ascii "$T\khoa_sai.json"
'{"ban_ve":"SPEC.DWG","ops":[{"op":"tron","l":"KHOA","c":[0,0],"r":100}]}' | Set-Content -Encoding ascii "$T\khoa_dung.json"
# "anh" dung truoc "tron" trong goi nhung phai chay sau khi ve xong; rong 700 thi lay kho 800 px. Anh ghi vao hang doi tam, khong ra %LOCALAPPDATA%.
# Vung chup nam xa moi hinh khac (x = 900000) nen anh chi co net neu "tron" da commit truoc khi in.
'{"ops":[{"op":"anh","hop":[900000,0,908000,6000],"rong":700},{"op":"tron","l":"ANH","c":[904000,3000],"r":2000}]}' | Set-Content -Encoding ascii "$T\anh.json"
$env:TKPCCC_CAD_HANG_DOI = "$T\hd"
# pdf: pdf_ti_le.py tinh ti le cua PDF mau (07_CAD\04_KIEM_THU\tao_pdf_mau.py ve 1:200) tu chuoi kich thuoc; thao tac "pdf" nhap voi ti le do,
# "doc" doc lai chu da nhap de do khoang cach truc 1-7. Can python co PyMuPDF.
$pdfMau = "$GOC\07_CAD\04_KIEM_THU\mau_kt.pdf"
$tlTrang = (& python -I "$GOC\07_CAD\.claude\skills\tkpccc-ve-cad\scripts\pdf_ti_le.py" $pdfMau 1 | ConvertFrom-Json)[0]
@{ ops = @(@{ op = 'pdf'; file = $pdfMau; trang = 1; tl = $tlTrang.tl; p = @(200000, 0) }, @{ op = 'doc'; gi = 'chu'; l = 'PDF_Text' }) } |
  ConvertTo-Json -Depth 5 -Compress | Set-Content -Encoding ascii "$T\pdf.json"
# Ban ky_hieu.json da doi ten layer truc: chung to plugin doc mac dinh tu file du lieu (07_CAD\03_DU_LIEU_VE\ky_hieu), khong dung so cung trong ma.
(Get-Content -Raw -Encoding UTF8 "$GOC\07_CAD\03_DU_LIEU_VE\ky_hieu\ky_hieu.json") -replace '"layer_truc":\s*"TRUC"', '"layer_truc": "TRUC_T"' |
  Set-Content -Encoding UTF8 "$T\ky_hieu.json"
# pdf sai so trang (review 2026-11-11: trang 99 cua PDF 1 trang lam -PDFIMPORT hoi lai mai, nuot cac lenh sau) + loi xep theo i:
# tron sai o i = 0, hai pdf sai o i = 1, 2 (pdf chay truoc moi thao tac khac nen truoc khi xep, loi cua tron dung cuoi).
@{ ops = @(@{ op = 'tron'; l = 'X'; c = @(0, 0); r = 0 }, @{ op = 'pdf'; file = $pdfMau; trang = 99; tl = 200 }, @{ op = 'pdf'; file = $pdfMau; trang = 0; tl = 200 }) } |
  ConvertTo-Json -Depth 5 -Compress | Set-Content -Encoding ascii "$T\pdf_sai.json"
# Nhap lan hai: AutoCAD dat nen moi o layer PDF2_*; xoa dung ten layer tra ve o pdf[].l, o goi RIENG, thi nen lan dau con nguyen.
@{ ops = @(@{ op = 'pdf'; file = $pdfMau; trang = 1; tl = $tlTrang.tl; p = @(200000, 100000) }) } | ConvertTo-Json -Depth 5 -Compress | Set-Content -Encoding ascii "$T\pdf2.json"
'{"ops":[{"op":"xoa","l":"PDF2_Geometry"},{"op":"xoa","l":"PDF2_Text"},{"op":"xoa","l":"PDF2_Solid Fills"},{"op":"doc","gi":"layer","l":"PDF*"}]}' | Set-Content -Encoding ascii "$T\xoa2.json"
# rai: vung sau loi (9000 x 6000, buoc 4000 x 3000 cho 15 m2 > dt 12) thi khong chen thiet bi cua vung truoc (8000 x 6000, dat).
'{"goc":[300000,0],"ops":[{"op":"net","l":"VUNG_P","p":[[0,0],[8000,0],[8000,6000],[0,6000]],"kin":true},{"op":"net","l":"VUNG_P","p":[[10000,0],[19000,0],[19000,6000],[10000,6000]],"kin":true},{"op":"rai","ma":"SPR_DAU_DOWN","l":"VUNG_P","buoc":[4000,3000],"dt":12}]}' |
  Set-Content -Encoding ascii "$T\rai_loi.json"
# tuyen xuong_ca: (0, 1) hai cot deu ngoai doan ong chinh, ve "truc" hai chieu: cung chon cot gan doan (x 5000) va noi them doan doc truc 2000 -> 12 m;
# (5) "qua" theo tung vung: moi diem dung cho vung chua no (vung 1 ong gop x 7000, vung 2 x 11000) -> 31 m.
'{"goc":[400000,0],"ops":[{"op":"tuyen","ma":"ONG_DN25","kieu":"xuong_ca","p":[[5000,2000],[8000,2000],[5000,4000],[8000,4000]],"truc":[[0,0],[3000,0]]},{"op":"tuyen","ma":"ONG_DN25","kieu":"xuong_ca","p":[[5000,2000],[8000,2000],[5000,4000],[8000,4000]],"truc":[[3000,0],[0,0]]},{"op":"net","l":"VUNG_Q","p":[[0,10000],[8000,10000],[8000,16000],[0,16000]],"kin":true},{"op":"net","l":"VUNG_Q","p":[[10000,10000],[18000,10000],[18000,16000],[10000,16000]],"kin":true},{"op":"rai","ma":"SPR_DAU_DOWN","l":"VUNG_Q","buoc":[4000,3000]},{"op":"tuyen","ma":"ONG_DN25","kieu":"xuong_ca","truc":[[-1000,9000],[20000,9000]],"qua":[[7000,13000],[11000,13000]]}]}' |
  Set-Content -Encoding ascii "$T\tuyen_thu.json"
# nhan: cho chu DN25 cua ong gop da co san chu DN25 thi khong ghi chong (3 doan, 2 chu nhanh) -> ve 1 + 3 + 2 = 6.
'{"goc":[500000,0],"ops":[{"op":"chu","l":"PCCC_NHAN","p":[-225,22500],"t":"DN25","cao":250,"can":"giua"},{"op":"tuyen","ma":"ONG_DN25","kieu":"xuong_ca","p":[[0,22000],[3000,22000],[0,25000],[3000,25000]],"truc":[[-1000,20000],[5000,20000]],"nhan":true}]}' |
  Set-Content -Encoding ascii "$T\nhan.json"
# Buoc 5 c, d, e (so tinh tay):
# (d) rai s 4000 + le [650, 1100], phong 6300 x 4500: x n 2, buoc min(4000, 6300 - 1300) = 4000, cach tuong 1150; y buoc 4500 - 2200 = 2300, cach 1100; m2 9,2 <= dt 12.
# (d) theo_hang, phong chu L (WC 1100 x 2000 goc tren trai): hang duoi rong 6300 -> x 1150 / 5150; hang tren rong 5200 -> buoc 3900, x 1750 / 5650.
#     noi_tiep qua 4 diem: 3900 + can(500^2 + 2300^2) + 4000 = 10,25 m (luoi chung cot thi 10,3 m).
# (c) tuyen cot: 3 cot x 0 / 3000 / 6000; ong chinh y 20000 tu x 1000 xuyen qua cot 3000 (diem y 18000 phia duoi):
#     4000 + 1000 (noi doc truc) + 6000 + 2500 = 13,5 m, 4 doan.
# (e) rai tim [0..1500..11500..(11500, 9000)], s 4000: diem bat buoc 1500, 11500; giua 10000 / 3 buoc; tuong dau 1500 (khong them);
#     doan cuoi 9000: them 2 diem, buoc 9000 / 2,5 = 3600, cach tuong 1800 -> 6 diem; nhanh xuong y -1000: 4 x 1000 + 4600 + 8200 = 16,8 m.
#     Cung tim + le 600: buoc cuoi min(4000, 8400 / 2) = 4000, cach tuong 1000. Tim kin 8000 x 6000: 4 goc + 4 giua canh = 8.
'{"goc":[600000,0],"ops":[{"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,0,6300,4500],"s":4000,"dt":12,"le":[650,1100]},{"op":"rai","ma":"SPR_DAU_DOWN","vung":[[0,10000],[6300,10000],[6300,14500],[1100,14500],[1100,12500],[0,12500]],"s":4000,"dt":12,"le":[650,1100],"theo_hang":true},{"op":"tuyen","ma":"ONG_DN25","kieu":"noi_tiep"},{"op":"tuyen","ma":"ONG_DN25","kieu":"cot","p":[[0,21000],[0,24000],[3000,21000],[3000,24000],[3000,18000],[6000,22500]],"truc":[[1000,20000],[8000,20000]]},{"op":"rai","ma":"SPR_DAU_DOWN","tim":[[0,30000],[1500,30000],[11500,30000],[11500,39000]],"s":4000},{"op":"tuyen","ma":"ONG_DN25","kieu":"nhanh","truc":[[-1000,29000],[20000,29000]]},{"op":"rai","ma":"SPR_DAU_DOWN","tim":[[0,50000],[1500,50000],[11500,50000],[11500,59000]],"s":4000,"le":600},{"op":"rai","ma":"SPR_DAU_DOWN","tim":[[0,70000],[8000,70000],[8000,76000],[0,76000],[0,70000]],"s":4000}]}' |
  Set-Content -Encoding ascii "$T\rai_moi.json"
# Nguoi dung 21/11: dau phun dung ke cach dim bao chay. rai kt:true theo trang Cai dat (mac dinh chuoi, khong cheo) nhu PCCC_RAI; kt:"cheo" them 4 dim cheo goc.
'{"goc":[9700000,0],"ops":[{"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,0,6500,4500],"buoc":[4000,3000],"kt":true}]}' | Set-Content -Encoding ascii "$T\rai_kt.json"
'{"goc":[9800000,0],"ops":[{"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,0,6500,4500],"buoc":[4000,3000],"kt":"cheo"}]}' | Set-Content -Encoding ascii "$T\rai_kt_cheo.json"
# Review 2026-11-12 (dau vao dung nhu review):
# CHAN: PDF bo doc underlay khong mo duoc (chi khoa sua, co mat khau mo, file hong) thi khong goi -PDFIMPORT (truoc day treo, nuot ca goi sau va QSAVE).
& python -I "$GOC\07_CAD\04_KIEM_THU\tao_pdf_mau.py" $T
if ($LASTEXITCODE) { throw 'tao_pdf_mau.py khong tao duoc PDF bao mat' }
@{ ops = @(@{ op = 'pdf'; file = "$T\khoa_sua.pdf"; trang = 1; tl = 200 }, @{ op = 'pdf'; file = "$T\mat_khau.pdf"; trang = 1; tl = 200 },
  @{ op = 'pdf'; file = "$T\hong.pdf"; trang = 1; tl = 200 }, @{ op = 'tron'; l = 'X'; c = @(0, -900000); r = 100 }) } |
  ConvertTo-Json -Depth 5 -Compress | Set-Content -Encoding ascii "$T\pdf_mk.json"
# (1) cot: 2 cot cung phia ngoai doan -> 1 doan noi tu chan xa nhat: 3 x 1 + 3 = 6 m, 4 doan (truoc 7 m, 5 doan).
# (2) truc co dinh thang hang: xuong_ca qua x 3500 chan nam tren doan ke tiep -> khong noi them: 3 + 1,5 + 1,5 = 6 m (truoc 6,5);
#     cot -> 2 doan 3 m (truoc 3 doan 5 m).
# (5) pha hoa: 2 cot doi xung x 1000 / 5000, khong qua, truc ve hai chieu -> ca hai lan ong gop o cot trai (X nho).
'{"goc":[700000,0],"ops":[{"op":"tuyen","ma":"ONG_DN25","kieu":"cot","p":[[-1000,1000],[-3000,1000],[2000,1000]],"truc":[[0,0],[5000,0]]},{"op":"tuyen","ma":"ONG_DN25","kieu":"cot","p":[[-1000,1000],[-3000,1000],[2000,1000]],"truc":[[5000,0],[0,0]]},{"op":"tuyen","ma":"ONG_DN25","kieu":"xuong_ca","p":[[2000,1000],[3500,1000],[2000,3000],[3500,3000]],"truc":[[0,0],[3000,0],[6000,0]],"qua":[3500,2000]},{"op":"tuyen","ma":"ONG_DN25","kieu":"cot","p":[[1000,30000],[1000,31000],[5000,30000]],"truc":[[0,29000],[3000,29000],[6000,29000]]}]}' |
  Set-Content -Encoding ascii "$T\rv_tuyen.json"
'{"goc":[800000,0],"ops":[{"op":"tuyen","ma":"ONG_DN25","kieu":"xuong_ca","p":[[1000,2000],[5000,2000],[1000,4000],[5000,4000]],"truc":[[-1000,0],[7000,0]]},{"op":"tuyen","ma":"ONG_DN25","kieu":"xuong_ca","p":[[1000,12000],[5000,12000],[1000,14000],[5000,14000]],"truc":[[7000,10000],[-1000,10000]]}]}' |
  Set-Content -Encoding ascii "$T\hoa.json"
'{"goc":[800000,0],"ops":[{"op":"doc","gi":"layer","l":"PCCC_ONG_DN25","hop":[900,-100,1100,4100]},{"op":"doc","gi":"layer","l":"PCCC_ONG_DN25","hop":[900,9900,1100,14100]}]}' |
  Set-Content -Encoding ascii "$T\hoa_doc.json"
# (3) theo_hang theo s, phong chu nhat 10000 x 6000: phai ra nhu luoi chung (6, khong phai 8).
# (4) theo_hang theo buoc: hang tren 9900 rong, 2 thiet bi cach tuong 2950 -> o 5,9 x 3 = 17,7 m2 > dt 12 phai bao loi; khong dt thi m2 17,7.
'{"goc":[900000,0],"ops":[{"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,0,10000,6000],"s":4000,"dt":12,"theo_hang":true},{"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,0,10000,6000],"s":4000,"theo_hang":true},{"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,0,10000,6000],"s":4000,"dt":12},{"op":"rai","ma":"SPR_DAU_DOWN","vung":[[0,10000],[10000,10000],[10000,16000],[100,16000],[100,13000],[0,13000]],"buoc":[4000,3000],"dt":12,"theo_hang":true},{"op":"rai","ma":"SPR_DAU_DOWN","vung":[[0,10000],[10000,10000],[10000,16000],[100,16000],[100,13000],[0,13000]],"buoc":[4000,3000],"theo_hang":true}]}' |
  Set-Content -Encoding ascii "$T\rv_hang.json"
# Review 2026-11-12b: (1) theo_hang + dt phong chu L 10 x 6 m khoet WC 3 x 3 m: 5 dau nhu khi khong dt (truoc 6).
# (2) cot + truc bac chu Z: chan x 9000 va 10000 cung noi ve dinh (8000,-3000) ngoai duong truc -> moi chan mot doan: 6 doan 12,77 m (truoc 5 doan 9,61 m);
#     truc chu L thuong van 5 doan 11 m.
# Review 2026-11-12c: (3) cot + truc bac 0,5 mm (ve tay lech): coi la tren truc, gop doan noi -> 5 doan 8 m (truoc 6 doan 9 m, ve chong).
#     (4) theo_hang hang rong bang vung (dong "w >= W" trong LuoiVung): phong 6 x 6 m, le 650, dt 12 ra dung luoi chung, x 1500 / 4500, m2 9 (bo dong do: x 1000 / 5000, m2 12).
'{"goc":[1000000,0],"ops":[{"op":"rai","ma":"SPR_DAU_DOWN","vung":[[0,0],[10000,0],[10000,6000],[3000,6000],[3000,3000],[0,3000]],"s":4000,"dt":12,"theo_hang":true},{"op":"tuyen","ma":"ONG_DN25","kieu":"cot","p":[[1000,11000],[3000,11000],[9000,11000],[10000,11000],[1000,12000],[3000,12000]],"truc":[[0,10000],[5000,10000],[5000,7000],[8000,7000]]},{"op":"tuyen","ma":"ONG_DN25","kieu":"cot","p":[[1000,21000],[3000,21000],[9000,21000],[10000,21000],[1000,22000],[3000,22000]],"truc":[[0,20000],[5000,20000],[5000,17000]]},{"op":"tuyen","ma":"ONG_DN25","kieu":"cot","p":[[1000,31000],[3000,31000],[9000,31000],[10000,31000],[1000,32000],[3000,32000]],"truc":[[0,30000],[5000,30000],[5000,29999.5],[8000,29999.5]]},{"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,40000,6000,46000],"s":4000,"dt":12,"le":650,"theo_hang":true}]}' |
  Set-Content -Encoding ascii "$T\rv2.json"
# so_do (G5, 07_CAD\01_TRI_THUC_VE\vi_du\so_do.json): 2 truc DN100, 3 vach tang (2 tang co dem) -> moi tang co dem 2 OS&Y + 2 FS, dau 2 truc 2 van xa khi.
# Chay SAU van.json (mat bang 2 OS&Y + 1 XK): block so do nam o layer PCCC_SODO nen dem OS&Y, XK sau so_do khong doi, khong co FS. Goi thu hai: truc ONG_DN25 khong co van OS&Y -> bao loi, khong ve gi; doc chu cao do.
Copy-Item "$GOC\07_CAD\01_TRI_THUC_VE\vi_du\so_do.json" "$T\so_do.json"
'{"goc":[1200000,0],"ops":[{"op":"so_do","truc":["ONG_DN25"],"tang":[{"ten":"T1","cot":0,"dem":{"SPR_DAU_DOWN":4}}]},{"op":"doc","gi":"chu","l":"PCCC_SODO_TANG"}]}' |
  Set-Content -Encoding ascii "$T\so_do_loi.json"
# so_do he bc (vi du so_do_bc.json = trang 11 ho so that: 12 zone + du phong, bang tinh nguon): them doc layer day + doc chu vung bang tinh.
# goi loi: zone 26 dau bao > toi_da 25, va thieu toi_da -> 2 loi, khong ve.
[IO.File]::WriteAllText("$T\so_do_bc.json", ([IO.File]::ReadAllText("$GOC\07_CAD\01_TRI_THUC_VE\vi_du\so_do_bc.json") -replace '\{"op":"dem"\}\]\}\s*$',
  '{"op":"dem"},{"op":"doc","gi":"layer","l":"PCCC_SODO_DAY*"},{"op":"doc","gi":"chu","l":"PCCC_SODO_BC","hop":[-16600,8300,-700,15800]},{"op":"doc","gi":"chu","l":"PCCC_SODO_BC"},{"op":"doc","gi":"chu","l":"PCCC_SODO_BC","hop":[-17500,15800,-8700,24500]}]}'))
# Review 13/11 NEN SUA 2, 3: moi op so_do vi du doi 1 gia tri (hs -1.25, hs 0, tq -24, sl -59, zone_tu 5 < 12 zone) -> 5 loi, khong ve.
$opBc = ([IO.File]::ReadAllText("$GOC\07_CAD\01_TRI_THUC_VE\vi_du\so_do_bc.json")); $opBc = $opBc.Substring($opBc.IndexOf('{"op":"so_do"')); $opBc = $opBc.Substring(0, $opBc.LastIndexOf(','))
[IO.File]::WriteAllText("$T\so_do_bc_ng.json", '{"goc":[1800000,0],"ops":[' + ((@(@('"hs":1.25', '"hs":-1.25'), @('"hs":1.25', '"hs":0'), @('"tq":24,', '"tq":-24,'),
  @('"sl":59', '"sl":-59'), @('"zone_tu":15', '"zone_tu":5')) | ForEach-Object { $opBc.Replace($_[0], $_[1]) }) -join ',') + ']}')
'{"goc":[1400000,0],"ops":[{"op":"so_do","he":"bc","toi_da":25,"tang":[{"cot":0,"zone":[{"dem":{"BC_KHOI":20,"BC_NHIET":6,"BC_NUTAN":1}}]}]},{"op":"so_do","he":"bc","tang":[{"cot":0,"dem":{"BC_KHOI":2}}]}]}' |
  Set-Content -Encoding ascii "$T\so_do_bc_loi.json"
# Review 12/11d NEN SUA 2: spr nhan dem chep tu ket qua dem (ca BC, ong, day) chi ghi ma SPR; ma la, so <= 0 bao loi.
$demDu = '"dem":{"BC_KHOI":10,"BC_NHIET":4,"DAY_BC":28.91,"ONG_DN25":183.32,"ONG_DN50":20,"SPR_DAU_DOWN":64,"SPR_VAN_OSY_DN100":2,"SPR_VAN_XK":1}'
('{"goc":[1500000,0],"ops":[{"op":"so_do","p":[0,0],"h":3000,"tang":[{"ten":"L1","cot":0,' + $demDu + '},{"ten":"L2","cot":4200,' + $demDu + '}]},' +
  '{"op":"so_do","p":[0,20000],"tang":[{"cot":0,"dem":{"SPR_DAU_DWN":4}}]},{"op":"so_do","p":[0,40000],"tang":[{"cot":0,"dem":{"SPR_DAU_DOWN":-3}}]},' +
  '{"op":"doc","gi":"chu","l":"PCCC_SODO"}]}') | Set-Content -Encoding ascii "$T\so_do_loc.json"
# NEN SUA 3: bc dem co DAY_BC khong bao loi. Bo cuc ho so that (12/11): zone truc tiep, khong vach tang/cao do, so luong DUOI ky hieu,
# dien tich bao ve cuoi hang, dong du phong, mui ten lien dong duoi tu.
# Nhan cuoi zone 3 dong (13/11): khu vuc (ten tang + kv) / DIEN TICH BAO VE: xx m2 / ZONE n; thieu dt bo dong dien tich; kv da co ten tang khong ghep lai.
('{"goc":[1700000,0],"ops":[{"op":"so_do","he":"bc","toi_da":25,"tang":[{"ten":"T1","cot":0,"zone":[{"kv":"KARAOKE","dt":88.11,"dem":{"BC_KHOI":10,"BC_NHIET":4,"DAY_BC":28.91,"BC_NUTAN":1}},' +
  '{"kv":"T1 KHO","dem":{"BC_KHOI":2}}]}]},' +
  '{"op":"so_do","he":"bc","toi_da":25,"p":[0,40000],"du_phong":"ZONE 4 DU PHONG","lien_dong":["LD THU"],"zone":[{"dt":120,"dem":{"BC_KHOI":21}},' +
  '{"kv":"KHU B","dem":{"BC_KHOI":22,"BC_NUTAN":1}},{"dem":{"SPR_VAN_OSY_DN100":2,"SPR_CTDC":2}}]},' +
  '{"op":"doc","gi":"chu","hop":[8500,4500,12000,9500]},{"op":"doc","gi":"chu","hop":[5500,45500,14000,50500]},' +
  '{"op":"doc","gi":"chu","hop":[-3000,45500,5200,50500]},{"op":"doc","gi":"chu","hop":[3500,43500,9000,44800]}]}') | Set-Content -Encoding ascii "$T\so_do_bc2.json"
# NEN SUA 1: ban ve don vi m (INSUNITS 6): co chu mac dinh nhan k, hop cua so do vai chuc m (truoc: 7 724 x 1 227 m).
'{"goc":[1600,0],"ops":[{"op":"so_do","he":"spr","p":[0,0],"tang":[{"ten":"T1","cot":0,"dem":{"SPR_DAU_DOWN":64}},{"ten":"T2","cot":4200,"dem":{"SPR_DAU_DOWN":64}}]}]}' |
  Set-Content -Encoding ascii "$T\so_do_m.json"
# Viec 1 (nguoi dung test 13/11, anh 1_rai_6771.png): da giac loi lom canh xien, s 10500, cach tuong toi da 5250; truoc day co dau bao cach canh xien
# duoi 6771 theo phuong dung. (1) moi diem cua vung phai co dau bao lech <= 5250 theo X va theo Y (kiem doc lap o duoi, toa do lay bang LISP);
# (2) BC_KHOI khong s: tri so TCVN 7568-14 tu 03_DU_LIEU/khoang_cach_rai.json, phong 30600 x 20400 ra 3 x 2, buoc 10200, cach tuong 5100, cheo goc 7212,5;
# (3) tran 16 m vuot h_max 15 cua dau bao khoi diem thi bao loi.
$XIEN = @(@(1100, 36000), @(13900, 40300), @(52500, 25100), @(16000, 0), @(0, 20600))
('{"goc":[2500000,0],"ops":[{"op":"rai","ma":"BC_KHOI","vung":' + (ConvertTo-Json -Compress $XIEN) + ',"s":10500,"kt":true},' +
  '{"op":"rai","ma":"BC_KHOI","hop":[0,60000,30600,80400],"kt":true},{"op":"rai","ma":"BC_KHOI","hop":[0,90000,10000,100000],"h":16}]}') |
  Set-Content -Encoding ascii "$T\rai_xien.json"
# Nguoi dung 19/11 (2 anh): dau bao lech ra ngoai / nam tren net vung. (1) dai hep canh phai xien, hep dan len tren (anh V5 XUONG): cot luoi theo
# hop bao nam sat canh xien (truoc sua: (1250, 19150) cach canh 1 mm); (2) hanh lang chu L nhanh doc rong 1500 (anh V3e HL): dau bao them de phu kin nam
# tren net trai; (3) nhu (2) co theo_hang. Moi dau bao phai trong vung, cach moi canh >= 500 (tuong_min BC_KHOI) va van phu kin (lech <= 5100 theo X, Y).
$HEP = @(@(@(0, 0), @(2500, 0), @(1000, 23000), @(0, 23000)),
  @(@(0, 30000), @(9000, 30000), @(9000, 34000), @(1500, 34000), @(1500, 42000), @(0, 42000)),
  @(@(0, 50000), @(9000, 50000), @(9000, 54000), @(1500, 54000), @(1500, 62000), @(0, 62000)))
('{"goc":[6000000,0],"ops":[{"op":"rai","ma":"BC_KHOI","vung":' + (ConvertTo-Json -Compress $HEP[0]) + '},' +
  '{"op":"rai","ma":"BC_KHOI","vung":' + (ConvertTo-Json -Compress $HEP[1]) + '},' +
  '{"op":"rai","ma":"BC_KHOI","vung":' + (ConvertTo-Json -Compress $HEP[2]) + ',"theo_hang":true},' +
  '{"op":"doc","gi":"dt","hop":[-3000,-3000,5000,26000]},{"op":"doc","gi":"dt","hop":[-3000,27000,12000,45000]},{"op":"doc","gi":"dt","hop":[-3000,47000,12000,65000]}]}') |
  Set-Content -Encoding ascii "$T\rai_hep.json"
'{"ops":[{"op":"doc","gi":"layer","l":"0"},{"op":"dem"}]}' | Set-Content -Encoding ascii "$T\dem_lon.json"
('{"goc":[9500000,200000],"ops":[{"op":"rai","ma":"SPR_DAU_DOWN","vung":' + (ConvertTo-Json -Compress @($XIEN | ForEach-Object { , @(($_[0] * 3), ($_[1] * 3)) })) + ',"s":3000}]}') |
  Set-Content -Encoding ascii "$T\rai_lon.json"
# Review 15/11: INSUNITS 1 (inch): buoc 10,2 m = 401,57 in (truoc coi la mm: 1 dau bao); INSUNITS 0 (Unitless) ban ve lon: doan mm, bao don_vi
'{"goc":[5000000,0],"ops":[{"op":"rai","ma":"BC_KHOI","hop":[0,0,1204.72,803.14]}]}' | Set-Content -Encoding ascii "$T\rai_inch.json"
'{"goc":[5100000,0],"ops":[{"op":"rai","ma":"BC_KHOI","hop":[0,0,30600,20400]}]}' | Set-Content -Encoding ascii "$T\rai_unitless.json"
# vung_tu_dong (viec 2 test 13/11): mat bang 2 phong tuong 2 net 200 (layer trung tinh KT_A), cua 1000 co ma tuong giua 2 phong, cot goc phong 2 la polyline,
# cot goc phong 1 CHI CO HATCH (ve bang -HATCH trong kb2), cot giua phong 1 (dao), chu + kich thuoc trong phong; truc TRUC_T (luoi_truc) va mot truc CENTER
# tren layer trung tinh NEN_A cat ngang ca hai phong (chi nhan ra truc nho kieu net). Phong 1 = 5,8 x 4,8 - goc 0,2 x 0,2 = 27,8 m2; phong 2 cung 27,8 m2.
# ho 500 < cua 1000: hai phong + o cua thong nhau = 55,8 m2 (layer VUNG_THU, xoa ngay). Diem ngoai cong trinh: bao loi. PCCC_RAI bam diem phong 2 o kb2.
'{"goc":[2600000,0],"ops":[{"op":"luoi_truc","x":[3000,6000],"y":[2500]},{"op":"net","l":"NEN_A","p":[[-1000,4000],[13000,4000]]},' +
 '{"op":"net","l":"KT_A","p":[[-100,-100],[12100,-100],[12100,5100],[-100,5100]],"kin":true},{"op":"net","l":"KT_A","p":[[5900,2000],[5900,100],[100,100],[100,4700]]},' +
 '{"op":"net","l":"KT_A","p":[[300,4900],[5900,4900],[5900,3000]]},{"op":"net","l":"KT_A","p":[[5900,2000],[6100,2000]]},{"op":"net","l":"KT_A","p":[[5900,3000],[6100,3000]]},' +
 '{"op":"net","l":"KT_A","p":[[6100,3000],[6100,4900],[11700,4900],[11700,4700],[11900,4700],[11900,100],[6100,100],[6100,2000]]},' +
 '{"op":"net","l":"KT_A","p":[[11700,4700],[12300,4700],[12300,5300],[11700,5300]],"kin":true},{"op":"net","l":"KT_A","p":[[3200,1500],[3600,1500],[3600,1900],[3200,1900]],"kin":true},' +
 '{"op":"chu","l":"KT_A","p":[1500,3000],"t":"PHONG 1","cao":300},{"op":"kt","l":"KT_A","p1":[6500,1000],"p2":[11000,1000],"cach":300}]}' | Set-Content -Encoding ascii "$T\vung_ve.json"
# PCCC_RAI quet chon (14/11): ban sao mat bang 2 phong o goc 2700000 (chua co PCCC_VUNG), khung chon C quanh no roi bam diem phong 2.
(Get-Content "$T\vung_ve.json") -replace '2600000', '2700000' | Set-Content -Encoding ascii "$T\vung_ve2.json"
# Nguoi dung 14/11 (anh 2_ca_ban_ve_mot_block): ca to la MOT block, vung ke bang net xanh la, do. Block KHOI_MB o goc 2900000: khung giay trang,
# vung A, B xanh (VUNG_XANH mau 3, chung canh x 25000), vung C do (VUNG_DO mau 1), o khung ten do tren layer KHAC (KHUNG_TEN mau 1). Bam mau xanh + do => dung 3 vung.
('{"goc":[2900000,0],"ops":[{"op":"net","l":"KT_A","p":[[0,0],[60000,0],[60000,40000],[0,40000]],"kin":true},' +
 '{"op":"net","l":"VUNG_XANH","mau":3,"p":[[5000,5000],[25000,5000],[25000,20000],[5000,20000]],"kin":true},' +
 '{"op":"net","l":"VUNG_XANH","mau":3,"p":[[25000,5000],[45000,5000],[45000,20000],[25000,20000]],"kin":true},' +
 '{"op":"net","l":"VUNG_DO","mau":1,"p":[[5000,25000],[20000,25000],[20000,35000],[5000,35000]],"kin":true},' +
 '{"op":"net","l":"KHUNG_TEN","mau":1,"p":[[50000,0],[60000,0],[60000,10000],[50000,10000]],"kin":true},{"op":"net","l":"KHUNG_TEN","mau":1,"p":[[50000,5000],[60000,5000]]}]}') |
  Set-Content -Encoding ascii "$T\vung_mau.json"
# Enter = moi phong, tu nhan tu tuong: khung giay chua 2 phong (10 x 8 m) bi bo, chi rai 2 phong.
('{"goc":[3100000,0],"ops":[{"op":"net","l":"KT_A","p":[[0,0],[60000,0],[60000,40000],[0,40000]],"kin":true},' +
 '{"op":"net","l":"KT_A","p":[[5000,5000],[15000,5000],[15000,13000],[5000,13000]],"kin":true},' +
 '{"op":"net","l":"KT_A","p":[[20000,5000],[30000,5000],[30000,13000],[20000,13000]],"kin":true}]}') | Set-Content -Encoding ascii "$T\vung_khung.json"
'{"goc":[2600000,0],"ops":[{"op":"vung_tu_dong","p":[[1500,1200]],"ho":500,"l":"VUNG_THU"},{"op":"xoa","l":"VUNG_THU"},' +
 '{"op":"vung_tu_dong","p":[[1500,1200],[10500,3500]]},{"op":"rai","ma":"SPR_DAU_DOWN","l":"PCCC_VUNG","hop":[-500,-500,12500,5500],"s":4000},' +
 '{"op":"vung_tu_dong","p":[[30000,30000]]}]}' | Set-Content -Encoding ascii "$T\vung_do.json"
# Ma van sprinkler moi trong vat_tu.json (Buoc 5): dat 2 OS&Y + 1 XK, dem theo MA_VT ("mat bang" cho phep kiem chong dem trung cua so_do).
'{"goc":[1100000,0],"ops":[{"op":"dat","ma":"SPR_VAN_OSY_DN100","p":[[0,0],[0,1000]],"goc":90},{"op":"dat","ma":"SPR_VAN_XK","p":[[1000,0]]},{"op":"dem"}]}' |
  Set-Content -Encoding ascii "$T\van.json"
# Bot chat sua vung quet chon (18/11): sua theo l / hop (doi + l_moi + mau, doi chu), doc gi dt, h sai thi sua bao loi, xoa h sai khong xoa gi.
('{"goc":[5300000,0],"ops":[{"op":"net","l":"SUA_THU","p":[[0,0],[1000,0]]},{"op":"chu","l":"SUA_THU","p":[0,500],"t":"CU","cao":100},' +
 '{"op":"sua","l":"SUA_THU","doi":[500,200],"l_moi":"SUA_MOI","mau":1},{"op":"sua","l":"SUA_MOI","hop":[-1000,-1000,3000,3000],"t":"MOI"},' +
 '{"op":"doc","gi":"dt","l":"SUA_MOI"},{"op":"sua","h":["ZZZ0"],"doi":[1,1]},{"op":"xoa","h":["ZZZ0"]}]}') | Set-Content -Encoding ascii "$T\sua.json"
# Bang tren mat bang (hoc ban ve mau 14/11): quy dinh lap dat + ky hieu cac van trong vung van.json
'{"ops":[{"op":"bang","loai":"quy_dinh","p":[5200000,0]},{"op":"bang","loai":"ky_hieu","p":[5200000,-8000],"hop":[1099000,-1000,1102000,2000]}]}' |
  Set-Content -Encoding ascii "$T\bang_mb.json"
# PCCC_THEMBLOCK khong ghi gi khi danh muc khong ghi duoc: file goc da co VAN_TRUNG (them tay) va la file chi doc.
# van_trung: trung ma trong file goc; van_ro: file goc chi doc. Ca hai phai dung TRUOC khi luu PCCC_Blocks.dwg.
$fGoc = "$T\goc\vat_tu.json"
[IO.File]::WriteAllText($fGoc, ([IO.File]::ReadAllText($fGoc) -replace '"vat_tu":\s*\[', ("`"vat_tu`": [`r`n    { `"ma`": `"VAN_TRUNG`", `"ten`": `"Van trung`", `"dvt`": `"cai`", `"he`": `"SPR`", `"loai`": `"block`" },")))
Set-ItemProperty $fGoc IsReadOnly $true
$bamTv = (Get-FileHash "$T\PCCC_Blocks.dwg").Hash
@('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\spec.dwg`"",
  'CIRCLE', '-500000,0', '150', '-BLOCK', 'VAN_TRUNG', '-500000,0', 'L', '', '-INSERT', 'VAN_TRUNG', '-500000,0', '1', '', '0',
  'PCCC_THEMBLOCK', 'L', 'van_trung', 'Van trung', 'SPR', '', 'PCCC_THEMBLOCK', 'L', 'van_ro', 'Van ro', 'SPR', '',
  'CIRCLE', '-600000,0', '77', '-BLOCK', 'BC_COI_DEN', '-600000,0', 'L', '',
  '-LAYER', 'M', 'PCCC_SODO_DAY_NGUON', 'C', '3', '', 'L', 'CONTINUOUS', '', 'S', '0', '',
  'PCCC_SPEC', "$T\spec.json", 'PCCC_SPEC', "$T\rai.json",
  'PCCC_SPEC', "$T\khoa_sai.json", 'PCCC_SPEC', "$T\khoa_dung.json", 'PCCC_SPEC', "$T\anh.json", 'PCCC_SPEC', "$T\pdf_sai.json", 'PCCC_SPEC', "$T\pdf.json",
  'PCCC_SPEC', "$T\pdf2.json", 'PCCC_SPEC', "$T\xoa2.json", 'PCCC_SPEC', "$T\rai_loi.json", 'PCCC_SPEC', "$T\tuyen_thu.json", 'PCCC_SPEC', "$T\nhan.json",
  'PCCC_SPEC', "$T\rai_moi.json", 'PCCC_SPEC', "$T\rai_kt.json", 'PCCC_SPEC', "$T\rai_kt_cheo.json", 'PCCC_SPEC', "$T\pdf_mk.json", 'PCCC_SPEC', "$T\rv_tuyen.json", 'PCCC_SPEC', "$T\hoa.json", 'PCCC_SPEC', "$T\hoa_doc.json",
  'PCCC_SPEC', "$T\rv_hang.json", 'PCCC_SPEC', "$T\rv2.json", 'PCCC_SPEC', "$T\van.json", 'PCCC_SPEC', "$T\sua.json", 'PCCC_SPEC', "$T\bang_mb.json", 'PCCC_SPEC', "$T\so_do.json", 'PCCC_SPEC', "$T\so_do_loi.json", 'PCCC_SPEC', "$T\so_do_bc.json", 'PCCC_SPEC', "$T\so_do_bc_loi.json", 'PCCC_SPEC', "$T\so_do_bc_ng.json",
  'PCCC_SPEC', "$T\so_do_loc.json", 'PCCC_SPEC', "$T\so_do_bc2.json", 'INSUNITS', '6', 'PCCC_SPEC', "$T\so_do_m.json", 'INSUNITS', '1', 'PCCC_SPEC', "$T\rai_inch.json", 'INSUNITS', '0', 'PCCC_SPEC', "$T\rai_unitless.json", 'INSUNITS', '4', 'PCCC_SPEC', "$T\rai_xien.json", 'PCCC_SPEC', "$T\rai_hep.json",
  'RECTANG', '3000000,0', '3030600,20400', 'PCCC_RAI', 'BC_KHOI', '', '', 'L', '', '', '',
  'RECTANG', '3200000,0', '3230600,20400', 'PCCC_RAI', 'BC_KHOI', 'HeSo', '50', '', '', 'L', '', '', '',
  'RECTANG', '3300000,0', '3330000,20000', 'PCCC_RAI', 'BC_BEAM', '', '', 'L', '', '', '',
  'RECTANG', '3400000,0', '3430600,20400', 'PCCC_RAI', 'BC_KHOI', 'XemTruoc', '', '', 'L', '', '', '',
  'RECTANG', '3500000,0', '3514400,7200', 'PCCC_RAI', 'BC_NHIET', 'Dam', '', '', 'L', '', '', '',
  '_.ZOOM', 'W', '3199000,-1000', '3231000,21000', 'PCCC_DAY', 'DAY_BC', 'C', '3199000,-1000', '3231000,21000', '', '3200000,-500', 'Vong', 'ZONE 1',
  # Hoc ban ve mau 14/11: day ho (khong vong) cua dau bao co dien tro cuoi day o mut; khong ghi zone
  'PCCC_DAY', 'DAY_BC', 'C', '3199000,-1000', '3231000,21000', '', '', '', '',
  # Tien ich (14/11 toi): 4 dau khoi, 2 cai trung diem, 1 cai doi sang layer 0; roi kiem trung, sua layer, danh so, xoay, ty le, doi block, can dim, doi DN
  '_.ZOOM', 'W', '3599000,-1000', '3604000,10000', 'PCCC_DAT', 'BC_KHOI', '3600000,0', '3600000,0', '3603000,0', '3603000,3000', '',
  '_.CHPROP', 'C', '3602900,2900', '3603100,3100', '', '_LA', '0', '',
  'PCCC_TRUNG', 'C', '3599000,-1000', '3604000,4000', '', 'PCCC_LOPCHUAN', 'C', '3599000,-1000', '3604000,4000', '', 'Co',
  'PCCC_DANHSO', 'C', '3599000,-1000', '3604000,4000', '', 'S', '1', '',
  'PCCC_XOAY', 'C', '3599000,-1000', '3604000,4000', '', '90', 'PCCC_TYLE', 'C', '3599000,-1000', '3604000,4000', '', '2',
  'PCCC_DOIBLOCK', 'C', '3599000,-1000', '3604000,4000', '', 'BC_NHIET',
  '_.DIMLINEAR', '3600000,5000', '3601000,5000', '3600500,5500', '_.DIMLINEAR', '3601000,5000', '3602000,5000', '3601500,6000',
  'PCCC_CANDIM', 'C', '3599000,4900', '3603000,6100', '', '3600000,7000',
  '_.PLINE', '3600000,9000', '3603000,9000', '', 'PCCC_DOIDN', 'L', '', 'ONG_DN65',
  # Dot 3: lo thang PCCC_LOAITRU trum dau bao giua hang duoi (x 15300, y 5100) -> bo 1; PCCC_KT phong da rai k = 50 % (phu kin) va phong 20 x 20 m 1 dau bao (mu 59,5 %)
  '_.ZOOM', 'W', '3899000,-1000', '3931000,21000', '-LAYER', 'M', 'PCCC_LOAITRU', '', '_.RECTANG', '3913300,3100', '3917300,7100', '-LAYER', 'S', '0', '',
  '_.RECTANG', '3900000,0', '3930600,20400', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '3899000,-1000', '3931000,21000', '', '', '',
  '_.ZOOM', 'W', '3199000,-1000', '3231000,21000', 'PCCC_KT', 'C', '3199000,-1000', '3231000,21000', '',
  '_.ZOOM', 'W', '3799000,-1000', '3821000,21000', '-LAYER', 'M', 'PCCC_VUNG', '', '_.RECTANG', '3800000,0', '3820000,20000', '-LAYER', 'S', '0', '',
  'PCCC_DAT', 'BC_KHOI', '3810000,10000', '', 'PCCC_KT', 'C', '3799000,-1000', '3821000,21000', '',
  # Review 15/11: WC 2 x 2 m PCCC_VUNG khong dau bao, sat phong 10 x 10 m co dau bao cach WC 2-4 m: WC phai mu 100 % (truoc tinh phu xuyen tuong)
  '_.ZOOM', 'W', '3829000,-1000', '3843000,11000', '-LAYER', 'M', 'PCCC_VUNG', '', '_.RECTANG', '3830000,0', '3840000,10000', '_.RECTANG', '3840000,4000', '3842000,6000', '-LAYER', 'S', '0', '',
  'PCCC_DAT', 'BC_KHOI', '3838000,5000', '', 'PCCC_KT', 'C', '3829000,-1000', '3843000,11000', '',
  # Vung tron PCCC_VUNG (Bo tri nhanh "Tron"), tach vung TZ, dat ten MZ
  '_.ZOOM', 'W', '3989000,-11000', '4121000,11000', '-LAYER', 'M', 'PCCC_VUNG', '', '_.CIRCLE', '4000000,0', '10000',
  '_.RECTANG', '4100000,0', '4120000,10000', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '3989000,-11000', '4011000,11000', '', '', 'PCCC_KT', 'C', '3989000,-11000', '4011000,11000', '',
  'PCCC_TACHVUNG', 'C', '4119000,4000', '4121000,6000', '4108000,-1000', '4108000,11000',
  'PCCC_KHUVUC', '4102000,5000', 'Kho A', '4115000,5000', 'Van phong 1', '',
  # Ong gio 2 x 2 m trum dau bao (4215300, 5100) cua luoi 3 x 2: dau do dich ra ngoai bien ong (1 m + 0,1 m), van 6 dau
  '_.ZOOM', 'W', '4199000,-1000', '4231000,21000', '-LAYER', 'M', 'ONG_GIO', '', '_.RECTANG', '4214300,4100', '4216300,6100', '-LAYER', 'S', '0', '',
  '_.RECTANG', '4200000,0', '4230600,20400', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '4199000,-1000', '4231000,21000', '', '', '',
  # Luat 14/11 (ban ve xuong bao bi): A | o X | B; C tuong 2 net ho 5 m; D ho 4 m co cung cua; E ho 4 m khong cua (ho);
  # F hatch ANSI31 (hien huu); G cua o khe chu T 2,5 m; H goc hut 5 mm. Phong cach nhau 15 m (> 12 m) de khe thang hang khong noi cheo.
  '_.ZOOM', 'W', '4299000,-3000', '4478000,11000',
  '_.RECTANG', '4300000,0', '4310000,10000', '_.RECTANG', '4310000,0', '4312000,10000', '_.RECTANG', '4312000,0', '4322000,10000',
  '_.LINE', '4310000,0', '4312000,10000', '', '_.LINE', '4310000,10000', '4312000,0', '',
  '_.PLINE', '4342000,0', '4337000,0', '4337000,10000', '4352000,10000', '4352000,0', '4347000,0', '',
  '_.PLINE', '4342000,200', '4337200,200', '4337200,9800', '4351800,9800', '4351800,200', '4347000,200', '',
  '_.PLINE', '4369000,0', '4367000,0', '4367000,10000', '4377000,10000', '4377000,0', '4373000,0', '',
  '_.PLINE', '4394000,0', '4392000,0', '4392000,10000', '4402000,10000', '4402000,0', '4398000,0', '',
  '_.RECTANG', '4417000,0', '4427000,10000', '-HATCH', 'P', 'ANSI31', '100', '0', 'S', 'L', '', '',
  '_.LINE', '4442000,-2000', '4442000,10000', '', '_.PLINE', '4442000,10000', '4452000,10000', '4452000,0', '4444500,0', '',
  '_.PLINE', '4467000,0', '4467000,10000', '4477000,10000', '4477000,0', '', '_.LINE', '4467000,0', '4476995,0', '',
  '-LAYER', 'M', 'CUA', '', '_.ARC', 'C', '4369000,0', '4370000,0', '4369000,1000', '_.ARC', 'C', '4444500,0', '4444500,1000', '4443500,0', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '4299000,-3000', '4478000,11000', '', '', '',
  # Khoang dam (5.9.1.1.6, tran 8 m; nguoi dung bo sung 15/11: xet do sau dam d tu ky hieu b x h). Dam layer DAM, bam 1 net dam mau.
  # A 20 x 10 m, 3 dam B1 (300x600) sau -> 4 khoang 50 m2; B 12 x 6 m luoi dam B2 (200x300) d = 0,3 -> bo, 1 phong phang; C 12 x 6 m luoi dam ghi nguoc h x b "DS 700X300" (so lon = h, d = 0,7) -> 18 khoang 4 m2 gom chung 2 dau
  '_.ZOOM', 'W', '4499000,-1000', '4575000,11000', '_.RECTANG', '4500000,0', '4520000,10000', '_.RECTANG', '4535000,0', '4547000,6000', '_.RECTANG', '4562000,0', '4574000,6000',
  '-LAYER', 'M', 'DAM', '', '_.LINE', '4505000,0', '4505000,10000', '', '_.LINE', '4510000,0', '4510000,10000', '', '_.LINE', '4515000,0', '4515000,10000', '',
  '_.LINE', '4537000,0', '4537000,6000', '', '_.LINE', '4539000,0', '4539000,6000', '', '_.LINE', '4541000,0', '4541000,6000', '', '_.LINE', '4543000,0', '4543000,6000', '', '_.LINE', '4545000,0', '4545000,6000', '', '_.LINE', '4535000,2000', '4547000,2000', '', '_.LINE', '4535000,4000', '4547000,4000', '',
  '_.LINE', '4564000,0', '4564000,6000', '', '_.LINE', '4566000,0', '4566000,6000', '', '_.LINE', '4568000,0', '4568000,6000', '', '_.LINE', '4570000,0', '4570000,6000', '', '_.LINE', '4572000,0', '4572000,6000', '', '_.LINE', '4562000,2000', '4574000,2000', '', '_.LINE', '4562000,4000', '4574000,4000', '',
  '_.TEXT', '4505300,8000', '250', '0', 'B1 (300x600)', '_.TEXT', '4537300,5000', '250', '0', 'B2 (200x300)', '_.TEXT', '4564300,5000', '250', '0', 'DS 700X300', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '8', '', 'C', '4499000,-1000', '4575000,11000', '', '', '4505000,2000', '', '',
  # D 10 x 10 m, 1 dam layer DAMD khong ky hieu: hoi co chung, tra loi 250x300 (d = 0,3) -> bo dam, 1 phong phang
  '_.ZOOM', 'W', '4589000,-1000', '4601000,11000', '_.RECTANG', '4590000,0', '4600000,10000', '-LAYER', 'M', 'DAMD', '', '_.LINE', '4595000,0', '4595000,10000', '', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '8', '', 'C', '4589000,-1000', '4601000,11000', '', '', '4595000,2000', '', '250x300', '',
  # E 10 x 10 m, khong net dam mau: TungDam chon line doc x = 5 m nhap 400x600 (sau, chia 2 khoang), line ngang nua trai 200x250 (nong, bo)
  '_.ZOOM', 'W', '4609000,-1000', '4621000,11000', '_.RECTANG', '4610000,0', '4620000,10000', '_.LINE', '4615000,0', '4615000,10000', '', '_.LINE', '4610000,5000', '4615000,5000', '',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '8', '', 'C', '4609000,-1000', '4621000,11000', '', '', '', 'TungDam', '4615000,2000', '', '400x600', '4612000,5000', '', '200x250', '', '',
  # Review 15/11: F, G 2,5 x 2,5 m (< 9 m2, tran 8 m) cach nhau 30 m: khoang nho gom THEO PHONG, moi phong 1 dau (truoc: 1 hop bao chung)
  '_.ZOOM', 'W', '4629000,-1000', '4664000,4000', '_.RECTANG', '4630000,0', '4632500,2500', '_.RECTANG', '4660000,0', '4662500,2500',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '8', '', 'C', '4629000,-1000', '4664000,4000', '', '', '', '', '',
  # H 3 x 2 m, Dam, Enter khong nhap tran: nguong 4 m2 (an toan) -> khoang 6 m2 la khoang lon, rai rieng (truoc: nguong 9)
  '_.ZOOM', 'W', '4679000,-1000', '4684000,3000', '_.RECTANG', '4680000,0', '4683000,2000',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '', '', 'C', '4679000,-1000', '4684000,3000', '', '', '', '', '',
  '-LAYER', 'M', 'NEN_A', 'L', 'CENTER', '', 'M', 'KT_A', '', 'PCCC_SPEC', "$T\vung_ve.json",
  '-HATCH', 'P', 'SOLID', 'W', 'N', '2599800,4700', '2600300,4700', '2600300,5200', '2599800,5200', 'C', '', '', '-LAYER', 'S', '0', '',
  'PCCC_SPEC', "$T\vung_do.json", 'PCCC_SPEC', "$T\vung_ve2.json", 'OSMODE', '0', '_.ZOOM', 'W', '2699000,-1000', '2714000,6000', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '', '', '', 'C', '2699000,-1000', '2714000,6000', '', '', '2710500,1200', '',
  'PCCC_SPEC', "$T\vung_mau.json", '_.ZOOM', 'W', '2899000,-1000', '2961000,41000', '-BLOCK', 'KHOI_MB', '2900000,0', 'C', '2899000,-1000', '2961000,41000', '',
  '-INSERT', 'KHOI_MB', '2900000,0', '1', '', '0', 'PCCC_RAI', 'BC_KHOI', '', '', 'L', '', '2905000,12000', '2905000,30000', '', '',
  'PCCC_SPEC', "$T\vung_khung.json", '_.ZOOM', 'W', '3099000,-1000', '3161000,41000', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '3099000,-1000', '3161000,41000', '', '', '', '_.QSAVE',
  ('(progn (setq f (open "' + ($T -replace '\\', '/') + '/blk.txt" "w")) (setq e (cdr (assoc -2 (tblsearch "BLOCK" "BC_COI_DEN")))) ' +
   '(while e (write-line (cdr (assoc 0 (entget e))) f) (setq e (entnext e))) (setq l (tblsearch "LAYER" "PCCC_SODO_DAY_NGUON")) ' +
   '(write-line (strcat "LAYER " (itoa (cdr (assoc 62 l))) " " (cdr (assoc 6 l))) f) ' +
   '(setq ss (ssget "_X" (list (cons 0 "MTEXT,TEXT") (cons 8 "PCCC_SODO*"))) i 0 sai 0) ' +
   '(repeat (sslength ss) (if (/= (strcase (cdr (assoc 7 (entget (ssname ss i))))) "PCCC_ARIAL") (setq sai (1+ sai))) (setq i (1+ i))) ' +
   '(write-line (strcat "KIEU " (itoa (sslength ss)) " " (itoa sai)) f) (close f))'),
  # toa do moi dau bao khoi cua rai_xien (goc x 2500000, vung da giac y < 50000)
  ('(progn (setq f (open "' + ($T -replace '\\', '/') + '/xien.txt" "w") ss (ssget "_X" (list (cons 0 "INSERT") (cons 2 "BC_KHOI"))) i 0) ' +
   '(repeat (sslength ss) (setq p (cdr (assoc 10 (entget (ssname ss i))))) (if (and (> (car p) 2490000) (< (car p) 2600000) (< (cadr p) 50000)) ' +
   '(write-line (strcat (rtos (car p) 2 3) "," (rtos (cadr p) 2 3)) f)) (setq i (1+ i))) (close f))'),
  # 14/11 toi: XemTruoc khong chen block; BC_BEAM 2 tia = 2 block + 2 net tia + 2 vach dau thu tren PCCC_BC_TIA
  ('(progn (defun dk (ten x0 x1 / ss i n p) (setq ss (ssget "_X" (list (cons 0 "INSERT") (cons 2 ten))) i 0 n 0) ' +
   '(if ss (repeat (sslength ss) (setq p (cdr (assoc 10 (entget (ssname ss i))))) (if (and (> (car p) x0) (< (car p) x1)) (setq n (1+ n))) (setq i (1+ i)))) n) ' +
   '(setq f (open "' + ($T -replace '\\', '/') + '/moi.txt" "w")) (write-line (strcat "XEM " (itoa (dk "BC_KHOI" 3399000 3431000))) f) ' +
   '(write-line (strcat "BEAM " (itoa (dk "BC_BEAM" 3299000 3331000))) f) (setq ss (ssget "_X" (list (cons 8 "PCCC_BC_TIA")))) ' +
   '(write-line (strcat "TIA " (itoa (if ss (sslength ss) 0))) f) (close f))'),
  # Do thoi gian (review 15/11, ve muot): block NOI_THAT 50 net x 10 000 ban chen (noi that cua ban ve kien truc), roi dem ca ban ve va rai vung lon
  ('(progn (entmake (list (cons 0 "BLOCK") (cons 2 "NOI_THAT") (cons 70 0) (list 10 0.0 0.0 0.0))) (setq i 0) ' +
   '(repeat 50 (entmake (list (cons 0 "LINE") (list 10 (* i 10.0) 0.0 0.0) (list 11 (* i 10.0) 500.0 0.0))) (setq i (1+ i))) (entmake (list (cons 0 "ENDBLK"))) ' +
   '(setq i 0) (repeat 10000 (entmake (list (cons 0 "INSERT") (cons 2 "NOI_THAT") (list 10 (+ 9000000.0 (* (rem i 100) 1000.0)) (* (/ i 100) 1000.0) 0.0))) (setq i (1+ i))) (princ))'),
  'PCCC_SPEC', "$T\dem_lon.json", 'PCCC_SPEC', "$T\rai_lon.json") -join "`r`n" |
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
# --- rai + tuyen (07_CAD\01_TRI_THUC_VE\vi_du\rai.json): so dem, buoc, cach tuong, chieu dai tuyen ---
if (Test-Path "$T\rai.kq.json") {
  $kr = Get-Content -Raw -Encoding UTF8 "$T\rai.kq.json" | ConvertFrom-Json
  $ks2 = @(
    @(($kr.ve -eq 57), "rai + tuyen ve 57 doi tuong, ke ca 4 kich thuoc cheo goc cua rai kt (thay $($kr.ve))"),
    @(($kr.rai[0].n -eq 4 -and $kr.rai[0].bx -eq 3250 -and $kr.rai[0].by -eq 2250 -and $kr.rai[0].tuong[0] -eq 1625 -and $kr.rai[0].ho -eq 0), 'rai s = 4000: 4 thiet bi, buoc <= 4000, cach tuong <= 2000, khong ho'),
    @(($kr.tuyen[0].doan -eq 1 -and [math]::Abs($kr.tuyen[0].m - 11.05) -lt 0.011), "tuyen noi_tiep: 1 duong ran bo tu diem tu, dai 11,05 m (thay $($kr.tuyen[0].m))"),
    @(($kr.rai[1].n -eq 4 -and $kr.rai[1].tuong[0] -eq 1250 -and $kr.rai[1].tuong[1] -eq 750), 'rai buoc dung 4000 x 3000: cach tuong 1250 / 750 nhu ban mau'),
    @(($kr.tuyen[1].doan -eq 3 -and $kr.tuyen[1].m -eq 12.75), "tuyen xuong_ca: 1 ong gop + 2 nhanh, dai 12,75 m (thay $($kr.tuyen[1].m))"),
    @(($kr.rai[2].n -eq 2 -and $kr.rai[2].ny -eq 1 -and $kr.tuyen[2].doan -eq 2 -and $kr.tuyen[2].m -eq 1), 'hanh lang 1 hang + tuyen nhanh: 2 doan vuong goc 0,5 m'),
    @(($kr.rai[3].n -eq 3 -and $kr.rai[3].ho -eq 0), 'rai vung chu L tren layer: bo diem ngoai da giac, khong ho'),
    @(($kr.rai[4].n -eq 16 -and $kr.rai[4].bx -eq 4000 -and $kr.rai[4].by -eq 3000 -and $kr.rai[4].m2 -eq 12), "rai s = 4000 + dt = 12: phong 16 x 12 m ra 4 x 4, buoc 4000 x 3000 (thay $($kr.rai[4].bx) x $($kr.rai[4].by), $($kr.rai[4].m2) m2)"),
    @(($kr.tuyen[3].kieu -eq 'tia' -and $kr.tuyen[3].doan -eq 4 -and [math]::Abs($kr.tuyen[3].m - 17.87) -lt 0.011), "tuyen tia: 4 dau bao, 4 net ra den bao phong, dai 17,87 m (thay $($kr.tuyen[3].m))"),
    @(($kr.ok -eq $false -and @($kr.loi).Count -eq 2 -and $kr.loi[0].i -eq 11 -and $kr.loi[0].ly_do -match 'dt' -and $kr.loi[1].i -eq 12),
      'rai buoc 4000 x 4000 vuot dt = 12 thi bao loi; thieu s va buoc thi bao loi, khong tu dat tri so'))
  foreach ($c in $ks2) { if ($c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
} else { "LOI   rai.json khong ghi ket qua: $T\kb2.out.txt"; $loi++ }
# --- Khoa ban_ve: spec ghi ten ban ve khac thi tu choi ca goi; dung ten (khong phan biet hoa thuong) thi ve ---
$ksai = if (Test-Path "$T\khoa_sai.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\khoa_sai.kq.json" | ConvertFrom-Json }
$kdung = if (Test-Path "$T\khoa_dung.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\khoa_dung.kq.json" | ConvertFrom-Json }
if ($ksai -and $ksai.ok -eq $false -and $ksai.ve -eq 0 -and $ksai.loi[0].i -eq -1) { 'DAT   ban_ve sai ten: tu choi ca goi, khong ve' } else { 'LOI   ban_ve sai ten van ve'; $loi++ }
if ($kdung -and $kdung.ok -eq $true -and $kdung.ve -eq 1) { 'DAT   ban_ve dung ten: ve binh thuong' } else { 'LOI   ban_ve dung ten ma khong ve'; $loi++ }
$kbm = if (Test-Path "$T\bang_mb.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\bang_mb.kq.json" | ConvertFrom-Json }
if ($kbm -and $kbm.ok -eq $true -and $kbm.ve -eq 2 -and -not $kbm.loi) { 'DAT   spec bang: quy_dinh + ky_hieu (van trong hop) ve 2 bang' } else { 'LOI   spec bang: ' + ($kbm | ConvertTo-Json -Compress); $loi++ }
# --- anh: PNG rong 800 px, co net ve (anh trang tron chi co 1 gia tri byte: vung trong, hoac in truoc khi ve xong) ---
$ka = if (Test-Path "$T\anh.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\anh.kq.json" | ConvertFrom-Json }
$fa = if ($ka) { [string]@($ka.anh)[0] }
if ($fa -and (Test-Path $fa)) {
  $da = DocAnh $fa
  if ($ka.ok -eq $true -and $da.rong -eq 800 -and $da.mau -gt 1) { 'DAT   anh: PNG 800 px co net ve, in sau khi ve xong' } else { "LOI   anh: rong $($da.rong), so gia tri byte $($da.mau)"; $loi++ }
} else { "LOI   anh khong ra PNG: $T\kb2.out.txt"; $loi++ }
# --- pdf: ti le tinh tu chuoi kich thuoc; nhap vao phai ra kich thuoc that (truc 1-7 = 41000, khung 1090 pt = 76906 mm o 1:200) ---
# PDF mau co 3 cap so "ban" (nhip ghi sai, so khong ke nhau cung hang) + chu "1 000 000": phai qua hai vong loc moi ra 200 +- 0,1 %.
if ([math]::Abs($tlTrang.tl - 200) -le 0.2 -and $tlTrang.cap -ge 9) { "DAT   pdf_ti_le.py: PDF mau ve 1:200 co cap so ban ra tl = $($tlTrang.tl) ($($tlTrang.cap) cap so, tin_cay $($tlTrang.tin_cay))" }
else { "LOI   pdf_ti_le.py: tl = $($tlTrang.tl), cap = $($tlTrang.cap), tin_cay = $($tlTrang.tin_cay)"; $loi++ }
& python -I "$GOC\07_CAD\.claude\skills\tkpccc-ve-cad\scripts\pdf_ti_le.py" $pdfMau 0 | Out-Null
if ($LASTEXITCODE -eq 2) { 'DAT   pdf_ti_le.py: trang 0 bao loi, khong lang le tra trang cuoi' } else { "LOI   pdf_ti_le.py trang 0: ma thoat $LASTEXITCODE"; $loi++ }
$kp = if (Test-Path "$T\pdf.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\pdf.kq.json" | ConvertFrom-Json }
$chuPdf = if ($kp -and $kp.doc) { @($kp.doc[0].ds) } else { @() }
$t1 = @($chuPdf | Where-Object { $_.t -eq '1' })[0]; $t7 = @($chuPdf | Where-Object { $_.t -eq '7' })[0]
$truc17 = if ($t1 -and $t7) { [math]::Abs($t7.p[0] - $t1.p[0]) } else { 0 }
$rongKhung = if ($kp -and $kp.hop) { $kp.hop[2] - $kp.hop[0] } else { 0 }
if ($kp -and $kp.ok -eq $true -and $kp.pdf[0].n -ge 20 -and [math]::Abs($truc17 - 41000) -le 410 -and [math]::Abs($rongKhung - 76906) -le 769) {
  "DAT   pdf: nhap $($kp.pdf[0].n) doi tuong, truc 1-7 do duoc $truc17 (can 41000 +- 1 %), khung rong $rongKhung"
} else { "LOI   pdf: n = $($kp.pdf.n), truc 1-7 = $truc17, khung = $rongKhung, loi = $($kp.loi | ConvertTo-Json -Compress -Depth 4): $T\kb2.out.txt"; $loi++ }

function DocKq($ten) { if (Test-Path "$T\$ten.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\$ten.kq.json" | ConvertFrom-Json } }
function Kiem($dk, $ten) { if ($dk) { "DAT   $ten" } else { "LOI   $ten"; $script:loi++ } }
$ks = DocKq 'pdf_sai'
Kiem ($ks -and $ks.ok -eq $false -and $ks.ve -eq 0 -and @($ks.loi).Count -eq 3 -and $ks.loi[0].i -eq 0 -and $ks.loi[1].i -eq 1 -and $ks.loi[2].i -eq 2 -and
  $ks.loi[1].ly_do -match 'trang' -and $ks.loi[2].ly_do -match 'trang') 'pdf trang 99 va trang 0: bao loi, khong goi -PDFIMPORT; loi xep theo i'
$kp2 = DocKq 'pdf2'; $kx2 = DocKq 'xoa2'
Kiem ($kp -and $kp2 -and $kx2 -and @($kp2.pdf[0].l | Where-Object { $_ -notmatch '^PDF2_' }).Count -eq 0 -and $kx2.xoa -eq $kp2.pdf[0].n -and
  @($kx2.doc[0].ds | Where-Object { $_.l -match '^PDF2_' }).Count -eq 0 -and ($kx2.doc[0].ds | Measure-Object n -Sum).Sum -eq $kp.pdf[0].n) `
  "pdf nhap lan hai vao layer PDF2_* ($($kp2.pdf[0].l -join ', ')); xoa dung ten do o goi rieng: xoa $($kx2.xoa), nen lan dau con nguyen"
$krl = DocKq 'rai_loi'
Kiem ($krl -and $krl.ok -eq $false -and @($krl.loi).Count -eq 1 -and $krl.loi[0].i -eq 2 -and $krl.ve -eq 2 -and -not $krl.rai) 'rai: vung thu hai vuot dt thi khong chen thiet bi nao (ve chi gom 2 polyline)'
$ktt = DocKq 'tuyen_thu'
Kiem ($ktt -and $ktt.ok -eq $true -and $ktt.tuyen[0].doan -eq 4 -and $ktt.tuyen[0].m -eq 12 -and $ktt.tuyen[1].doan -eq 4 -and $ktt.tuyen[1].m -eq 12) `
  "tuyen xuong_ca: chan ong gop ngoai doan ong chinh thi noi them doc truc; dao chieu truc van cung cot ($($ktt.tuyen[0].m) / $($ktt.tuyen[1].m) m, can 12)"
Kiem ($ktt -and $ktt.tuyen[2].doan -eq 6 -and $ktt.tuyen[2].m -eq 31) "tuyen xuong_ca: qua theo tung vung, 2 vung 31 m (thay $($ktt.tuyen[2].m))"
$knh = DocKq 'nhan'
Kiem ($knh -and $knh.ok -eq $true -and $knh.ve -eq 6) "tuyen nhan: khong ghi chong chu DN25 len chu DN25 da co (ve $($knh.ve), can 6)"
$kkt = DocKq 'rai_kt'; $kkc = DocKq 'rai_kt_cheo'
Kiem ($kkt -and $kkc -and $kkt.ok -and $kkc.ok -and ($kkc.ve - $kkt.ve) -eq 4) "rai kt:true dim nhu PCCC_RAI theo Cai dat (khong cheo), kt:cheo them 4 dim cheo goc (ve $($kkt.ve) / $($kkc.ve))"
$krm = DocKq 'rai_moi'
$r0 = if ($krm) { $krm.rai[0] }
Kiem ($krm -and $krm.ok -eq $true -and $r0.n -eq 4 -and $r0.bx -eq 4000 -and $r0.by -eq 2300 -and $r0.tuong[0] -eq 1150 -and $r0.tuong[1] -eq 1100 -and $r0.m2 -eq 9.2) `
  "rai s + le [650,1100]: buoc 4000 x 2300, cach tuong 1150 / 1100, m2 9,2 ($($r0 | ConvertTo-Json -Compress))"
Kiem ($krm -and $krm.rai[1].n -eq 4 -and $krm.tuyen[0].m -eq 10.25) "rai theo_hang phong chu L: hang tren buoc 3900 theo be rong trong, noi_tiep 10,25 m (thay $($krm.tuyen[0].m))"
Kiem ($krm -and $krm.tuyen[1].kieu -eq 'cot' -and $krm.tuyen[1].doan -eq 4 -and $krm.tuyen[1].m -eq 13.5) "tuyen cot: moi cot 1 nhanh vuong goc, ong chinh xuyen phong, 13,5 m (thay $($krm.tuyen[1].m))"
Kiem ($krm -and $krm.rai[2].n -eq 6 -and $krm.rai[2].bx -eq 3600 -and $krm.rai[2].tuong[0] -eq 1500 -and $krm.rai[2].tuong[1] -eq 1800 -and $krm.tuyen[2].doan -eq 6 -and $krm.tuyen[2].m -eq 16.8) `
  "rai tim: 2 diem bat buoc + 2 giua + 2 ra tuong, buoc lon nhat 3600, nhanh 16,8 m (thay $($krm.rai[2] | ConvertTo-Json -Compress), $($krm.tuyen[2].m) m)"
Kiem ($krm -and $krm.rai[3].n -eq 6 -and $krm.rai[3].bx -eq 4000 -and $krm.rai[3].tuong[1] -eq 1000 -and $krm.rai[4].n -eq 8 -and $krm.rai[4].bx -eq 4000) `
  "rai tim + le 600: doan ra tuong buoc 4000, cach tuong 1000; tim kin 8000 x 6000 ra 8 dau ($($krm.rai[3].tuong -join '/'), $($krm.rai[4].n))"
# Review 2026-11-12
$kmk = DocKq 'pdf_mk'
Kiem ($kmk -and $kmk.ok -eq $false -and $kmk.ve -eq 1 -and @($kmk.loi).Count -eq 3 -and @($kmk.loi | Where-Object { $_.ly_do -match 'PDF' }).Count -eq 3 -and (DocKq 'rv_hang')) `
  "pdf chi khoa sua / co mat khau / hong: bao loi, khong goi -PDFIMPORT, goi sau van chay ($($kmk.loi.ly_do -join ' | '))"
$krt = DocKq 'rv_tuyen'
$tt = if ($krt) { @($krt.tuyen) } else { @() }
Kiem ($krt -and $krt.ok -eq $true -and $tt[0].doan -eq 4 -and $tt[0].m -eq 6 -and $tt[1].doan -eq 4 -and $tt[1].m -eq 6) "tuyen cot: 2 cot cung phia ngoai doan chi 1 doan noi, 6 m ca hai chieu truc ($($tt[0].m) / $($tt[1].m), $($tt[0].doan) doan)"
Kiem ($krt -and $tt[2].doan -eq 3 -and $tt[2].m -eq 6) "tuyen xuong_ca: truc dinh thang hang, chan tren doan ke khong noi de ong chinh (6 m, thay $($tt[2].m), $($tt[2].doan) doan)"
Kiem ($krt -and $tt[3].doan -eq 2 -and $tt[3].m -eq 3) "tuyen cot: truc dinh thang hang, 2 doan 3 m (thay $($tt[3].doan) doan $($tt[3].m) m)"
$khd = DocKq 'hoa_doc'
Kiem ((DocKq 'hoa') -and $khd -and @($khd.doc).Count -eq 2 -and $khd.doc[0].ds[0].n -eq 1 -and $khd.doc[1].ds[0].n -eq 1) `
  "tuyen xuong_ca pha hoa: 2 cot doi xung, truc ve hai chieu deu chon cot trai ($($khd.doc | ConvertTo-Json -Compress -Depth 4))"
$krh = DocKq 'rv_hang'
$rh = if ($krh) { @($krh.rai) } else { @() }
Kiem ($krh -and $rh[0].n -eq 6 -and $rh[1].n -eq 6 -and $rh[2].n -eq 6) "rai theo_hang theo s phong chu nhat: nhu luoi chung 6 thiet bi ($($rh[0].n) / $($rh[1].n) / $($rh[2].n))"
Kiem ($krh -and $krh.ok -eq $false -and @($krh.loi).Count -eq 1 -and $krh.loi[0].i -eq 3 -and $krh.loi[0].ly_do -match 'dt' -and $rh.Count -eq 4 -and $rh[3].m2 -eq 17.7) `
  "rai theo_hang theo buoc: o hang 17,7 m2 vuot dt 12 thi bao loi, khong dt thi m2 17,7 (thay $($rh[3].m2), loi $($krh.loi | ConvertTo-Json -Compress -Depth 3))"
$kr2 = DocKq 'rv2'
$r2t = if ($kr2) { @($kr2.tuyen) } else { @() }
Kiem ($kr2 -and $kr2.ok -eq $true -and $kr2.rai[0].n -eq 5) "rai theo_hang + dt 12 phong chu L: 5 dau nhu khong dt (thay $($kr2.rai[0].n))"
Kiem ($kr2 -and $r2t[0].doan -eq 6 -and $r2t[0].m -eq 12.77 -and $r2t[1].doan -eq 5 -and $r2t[1].m -eq 11) `
  "tuyen cot truc bac chu Z: moi chan mot doan noi 6 doan 12,77 m; truc chu L 5 doan 11 m (thay $($r2t[0].doan)/$($r2t[0].m), $($r2t[1].doan)/$($r2t[1].m))"
Kiem ($kr2 -and $r2t[2].doan -eq 5 -and $r2t[2].m -eq 8) "tuyen cot truc bac 0,5 mm: gop doan noi 5 doan 8 m, khong ve chong (thay $($r2t[2].doan)/$($r2t[2].m))"
Kiem ($kr2 -and $kr2.rai[1].n -eq 4 -and $kr2.rai[1].m2 -eq 9) "rai theo_hang hang rong bang vung: dung luoi chung 4 dau, m2 9 (thay $($kr2.rai[1].n) / $($kr2.rai[1].m2))"
# Viec 1: phu kin da giac canh xien. Kiem doc lap voi plugin: diem mau cach 250 mm trong vung + 100 mm doc canh (ca dinh).
function TrongDg($dg, $x, $y) {
  $tr = $false; $n = $dg.Count
  for ($i = 0; $i -lt $n; $i++) { $a = $dg[$i]; $b = $dg[($i + $n - 1) % $n]
    if ((($a[1] -gt $y) -ne ($b[1] -gt $y)) -and ($x -lt ($b[0] - $a[0]) * ($y - $a[1]) / ($b[1] - $a[1]) + $a[0])) { $tr = -not $tr } }
  $tr
}
# Khoang cach tu (x, y) toi duong bao da giac.
function CachCanh($dg, $x, $y) {
  $kq = [double]::MaxValue; $n = $dg.Count
  for ($i = 0; $i -lt $n; $i++) { $a = $dg[$i]; $b = $dg[($i + 1) % $n]; $dx = $b[0] - $a[0]; $dy = $b[1] - $a[1]
    $t = [math]::Max(0.0, [math]::Min(1.0, (($x - $a[0]) * $dx + ($y - $a[1]) * $dy) / ($dx * $dx + $dy * $dy)))
    $kq = [math]::Min($kq, [math]::Sqrt([math]::Pow($x - $a[0] - $t * $dx, 2) + [math]::Pow($y - $a[1] - $t * $dy, 2))) }
  $kq
}
$kxi = DocKq 'rai_xien'
$db = if (Test-Path "$T\xien.txt") { @(Get-Content "$T\xien.txt" | ForEach-Object { $v = $_ -split ','; , @(([double]$v[0] - 2500000), [double]$v[1]) }) } else { @() }
function TrongXien($x, $y) {
  $tr = $false; $n = $XIEN.Count
  for ($i = 0; $i -lt $n; $i++) { $a = $XIEN[$i]; $b = $XIEN[($i + $n - 1) % $n]
    if ((($a[1] -gt $y) -ne ($b[1] -gt $y)) -and ($x -lt ($b[0] - $a[0]) * ($y - $a[1]) / ($b[1] - $a[1]) + $a[0])) { $tr = -not $tr } }
  $tr
}
$mauX = New-Object Collections.Generic.List[object]
for ($y = 125; $y -lt 40300; $y += 250) { for ($x = 125; $x -lt 52500; $x += 250) { if (TrongXien $x $y) { $mauX.Add(@($x, $y)) } } }
for ($i = 0; $i -lt $XIEN.Count; $i++) { $a = $XIEN[$i]; $b = $XIEN[($i + 1) % $XIEN.Count]
  $k = [math]::Ceiling([math]::Sqrt([math]::Pow($b[0] - $a[0], 2) + [math]::Pow($b[1] - $a[1], 2)) / 100)
  for ($j = 0; $j -lt $k; $j++) { $mauX.Add(@(($a[0] + ($b[0] - $a[0]) * $j / $k), ($a[1] + ($b[1] - $a[1]) * $j / $k))) } }
$hoX = @($mauX | Where-Object { $m = $_; -not ($db | Where-Object { [math]::Abs($_[0] - $m[0]) -le 5251 -and [math]::Abs($_[1] - $m[1]) -le 5251 } | Select-Object -First 1) })
$ngoaiX = @($db | Where-Object { -not (TrongXien $_[0] $_[1]) })
$satX = @($db | Where-Object { (CachCanh $XIEN $_[0] $_[1]) -lt 499.9 })
Kiem ($kxi -and $db.Count -ge 13 -and $db.Count -eq $kxi.rai[0].n -and $hoX.Count -eq 0 -and $ngoaiX.Count -eq 0 -and $satX.Count -eq 0 -and $kxi.rai[0].ho -eq 0) `
  "rai da giac canh xien (anh 1_rai_6771): $($db.Count) dau bao, moi diem vung lech <= 5250 theo X va Y ($($mauX.Count) diem mau, hoX $($hoX.Count), ngoai vung $($ngoaiX.Count), cach canh < 500 $($satX.Count), vd hoX $($hoX | Select-Object -First 3 | ForEach-Object { $_ -join '/' }))"
# Viec 19/11: rai_hep. Kiem doc lap: trong vung, khoang cach toi moi canh, phu kin (diem mau 250 mm trong vung + 100 mm doc canh).
$khp = DocKq 'rai_hep'
$hepMoTa = @(); $hepDat = [bool]$khp -and $khp.ok -eq $true
for ($v = 0; $v -lt 3; $v++) {
  $dg = $HEP[$v]
  $ds = if ($khp) { @($khp.doc[$v].ds | Where-Object { $_.ten -eq 'BC_KHOI' } | ForEach-Object { , @(([double]$_.p[0] - 6000000), [double]$_.p[1]) }) } else { @() }
  $xx = $dg | ForEach-Object { $_[0] } | Measure-Object -Minimum -Maximum; $yy = $dg | ForEach-Object { $_[1] } | Measure-Object -Minimum -Maximum
  $mau = New-Object Collections.Generic.List[object]
  for ($y = $yy.Minimum + 125; $y -lt $yy.Maximum; $y += 250) { for ($x = $xx.Minimum + 125; $x -lt $xx.Maximum; $x += 250) { if (TrongDg $dg $x $y) { $mau.Add(@($x, $y)) } } }
  for ($i = 0; $i -lt $dg.Count; $i++) { $a = $dg[$i]; $b = $dg[($i + 1) % $dg.Count]
    $k = [math]::Ceiling([math]::Sqrt([math]::Pow($b[0] - $a[0], 2) + [math]::Pow($b[1] - $a[1], 2)) / 100)
    for ($j = 0; $j -lt $k; $j++) { $mau.Add(@(($a[0] + ($b[0] - $a[0]) * $j / $k), ($a[1] + ($b[1] - $a[1]) * $j / $k))) } }
  $ho = @($mau | Where-Object { $m = $_; -not ($ds | Where-Object { [math]::Abs($_[0] - $m[0]) -le 5101 -and [math]::Abs($_[1] - $m[1]) -le 5101 } | Select-Object -First 1) })
  $ngoai = @($ds | Where-Object { -not (TrongDg $dg $_[0] $_[1]) })
  $cach = if ($ds.Count) { ($ds | ForEach-Object { CachCanh $dg $_[0] $_[1] } | Measure-Object -Minimum).Minimum } else { 0 }
  $n = if ($khp) { $khp.rai[$v].n } else { -1 }
  if (-not ($ds.Count -gt 0 -and $ds.Count -eq $n -and $ngoai.Count -eq 0 -and $cach -ge 499.9 -and $ho.Count -eq 0 -and $khp.rai[$v].ho -eq 0)) { $hepDat = $false }
  $hepMoTa += "vung $($v + 1): $($ds.Count)/$n dau, ngoai $($ngoai.Count), cach canh min $([math]::Round($cach, 1)), ho $($ho.Count)/$($mau.Count) [$(($ds | ForEach-Object { $_ -join '/' }) -join ' ')]"
}
Kiem $hepDat "rai vung hep (nguoi dung 19/11): dai canh xien + hanh lang chu L (+ theo_hang): moi dau bao trong vung, cach canh >= 500, phu kin ($($hepMoTa -join '; '))"
$rx1 = if ($kxi) { $kxi.rai[1] }
Kiem ($kxi -and $rx1.n -eq 6 -and $rx1.bx -eq 10200 -and $rx1.by -eq 10200 -and $rx1.tuong[0] -eq 5100 -and $rx1.tuong[1] -eq 5100 -and $rx1.cheo -eq 7212.5) `
  "rai BC_KHOI khong s: TCVN 7568-14 tu khoang_cach_rai.json, 3 x 2 buoc 10200 cach tuong 5100, cheo goc 7212,5 ($($rx1 | ConvertTo-Json -Compress))"
Kiem ($kxi -and $kxi.ok -eq $false -and @($kxi.loi).Count -eq 1 -and $kxi.loi[0].i -eq 2 -and $kxi.loi[0].ly_do -match 'cao 16') `
  "rai BC_KHOI tran 16 m vuot h_max 15 thi bao loi ($($kxi.loi | ConvertTo-Json -Compress -Depth 3))"
# so_do: ve 48 = 3 vach tang x 4 + 2 tang x (2 nhanh x 5 + ong xa + no van + chu dem) + 2 truc x 3 + chu tram bom + truc xa x 3
$ksd = DocKq 'so_do'; $kv0 = DocKq 'van'
Kiem ($ksd -and $kv0 -and $ksd.ok -eq $true -and $ksd.ve -eq 48 -and $ksd.dem.SPR_VAN_OSY_DN100 -eq 2 -and $kv0.dem.SPR_VAN_OSY_DN100 -eq 2 -and -not $ksd.dem.SPR_CTDC -and
  $ksd.dem.SPR_VAN_XK -eq $kv0.dem.SPR_VAN_XK -and $ksd.dem.ONG_DN50 -eq $kv0.dem.ONG_DN50 -and $ksd.dem.ONG_DN50 -gt 0) `
  "so_do: mat bang 2 OS&Y + so do 2 tang x 2 truc: dem OS&Y van 2, khong FS, XK khong doi, ong so do khong cong, ve 48 (thay ve $($ksd.ve), dem $($ksd.dem | ConvertTo-Json -Compress))"
$ksl = DocKq 'so_do_loi'
$chuSd = if ($ksl -and $ksl.doc) { @($ksl.doc[0].ds | ForEach-Object { $_.t }) } else { @() }
Kiem ($ksl -and $ksl.ve -eq 0 -and @($ksl.loi).Count -eq 1 -and $ksl.loi[0].ly_do -match 'OS&Y' -and
  ($chuSd -contains ([string][char]0xB1 + '0.000')) -and ($chuSd -contains '+4.200') -and ($chuSd -contains '+8.400') -and $chuSd.Count -eq 6) `
  "so_do: chu cao do +-0.000 / +4.200 / +8.400 + 3 ten tang; truc DN25 khong co van OS&Y thi bao loi, khong ve ($($chuSd -join ' | '))"
$ksb = DocKq 'so_do_bc'; $ksbl = DocKq 'so_do_bc_loi'; $ksng = DocKq 'so_do_bc_ng'
$lng = @($ksng.loi | ForEach-Object { $_.ly_do })
Kiem ($ksng -and $ksng.ve -eq 0 -and $lng.Count -eq 5 -and $lng[0] -match 'nguon\.hs.+> 0' -and $lng[1] -match 'nguon\.hs.+> 0' -and $lng[2] -match 'nguon\.tq".+> 0' -and
  $lng[3] -match 'BC_NHIET.+sl.+>= 0' -and $lng[4] -match 'zone_tu.+\(5\).+\(12\)') `
  "so_do bc (review 13/11 NEN SUA 2, 3): nguon hs -1,25 / hs 0 / tq -24 / sl -59 va zone_tu 5 < 12 zone bao loi, khong ve ($($lng -join ' | '))"
# NEN SUA 1: so duoi thiet bi chi thi HL zone 4, 9 (chu "20" phia tren day) cach day zone >= 3 pt (1 pt = 33 mm). Day = goc nhan zone + 6 pt; chu cao 6 pt.
$chuSd = if ($ksb -and $ksb.doc -and $ksb.doc.Count -gt 2) { @($ksb.doc[2].ds) } else { @() }
$hlCach = foreach ($n in 4, 9) {
  $nh = @($chuSd | Where-Object { $_.t -match "ZONE $n\s*$" -and $_.t -match 'M' })
  if ($nh.Count -ne 1) { -99; continue }
  $yd = $nh[0].p[1] + 6 * 33
  $so = @($chuSd | Where-Object { $_.t -eq '20' -and $_.p[1] -gt $yd -and $_.p[1] -lt $yd + 40 * 33 })
  if ($so.Count -ne 1) { -98 } else { [math]::Round(($so[0].p[1] - 6 * 33 - $yd) / 33, 1) }
}
Kiem (@($hlCach).Count -eq 2 -and @($hlCach | Where-Object { $_ -lt 3 }).Count -eq 0) "so_do bc thiet bi chi thi HL lech (-25, +25,6) pt nhu trang 11: day so cach day zone 4, 9 >= 3 pt (pt: $($hlCach -join ', '))"
# Gian hang theo noi dung (viec 2, 13/11): ky hieu chi thi HL zone 4, 9 (dinh = so "20" + 3 + 17,2 pt ky hieu kem chu HL) khong cham day so cua hang tren (zone 5, 10).
# Chi zone 4: doc chu tra toi da 40 chu nen khong toi nhan "ZONE 10"; zone 9 cung cach tinh.
$hlTren = foreach ($n in @(4)) {
  # nhan ten zone canh bo day: chu "ZONE n" dat tai day + 1,5 pt (zone giam sat van khong co nhan 3 dong)
  $n5 = @($chuSd | Where-Object { $_.t -eq "ZONE $($n + 1)" }); $s20 = @($chuSd | Where-Object { $_.t -eq '20' })
  if ($n5.Count -ne 1 -or $s20.Count -lt 2) { -99; continue }
  $yd5 = $n5[0].p[1] - 1.5 * 33
  $soTren = @($chuSd | Where-Object { $_.t -match '^\d{2}$' -and $_.p[1] -lt $yd5 -and $_.p[1] -gt $yd5 - 15 * 33 })
  $p20 = @($s20 | Where-Object { $_.p[1] -lt $yd5 } | Sort-Object { $_.p[1] } -Descending)[0].p[1]
  if ($soTren.Count -eq 0) { -98 } else { [math]::Round((($soTren | ForEach-Object { $_.p[1] } | Measure-Object -Minimum).Minimum - 6 * 33 - $p20) / 33 - 20.2, 1) }
}
Kiem (@($hlTren).Count -eq 1 -and @($hlTren | Where-Object { $_ -lt 2 }).Count -eq 0) "so_do bc gian hang theo noi dung: ky hieu chi thi HL zone 4 cach day so hang tren >= 2 pt (pt: $($hlTren -join ', '))"
# Bang tinh nguon tinh tay (phu luc C TCVN 7568-14:2025, so lieu trang 11): IQ = 100 + (59 x 35 + 24 x 59) / 1000 = 103,481 mA;
# IA = 100 + 59 x 40 + 24 x 40 + 28 x 14,5 + 3 x 30 + 11 x 15 = 4081 mA; C20 = 1,25 x (0,103481 x 24 + 2 x 4,081 x 0,5) = 8,20568 Ah;
# IC = 1,25 x (0,103481 x 5 + 4,081 x 0,5) / 24 = 0,13322 A; IPSE = 0,103481 + 0,13322 = 0,2367 A (bang ghi 0.24).
$ng = if ($ksb) { $ksb.nguon }
Kiem ($ng -and [math]::Abs($ng.ia_ma - 4081) -lt 1e-6 -and [math]::Abs($ng.iq_ma - 103.481) -lt 1e-6 -and [math]::Abs($ng.c20_ah - 8.20568) -lt 1e-6 -and
  [math]::Abs($ng.ic_a - 0.133224) -lt 1e-6 -and [math]::Abs($ng.ipse_a - 0.236705) -lt 1e-6) `
  "so_do bc bang tinh nguon dung trang 11: IA 4081 mA, IQ 103,481 mA, C20 8,20568 Ah, IC 0,133 A, IPSE 0,24 A (thay $($ng | ConvertTo-Json -Compress))"
$chuBang = if ($ksb -and $ksb.doc -and $ksb.doc.Count -gt 1) { @($ksb.doc[1].ds | ForEach-Object { $_.t }) } else { @() }
# Viec ke (1) 13/11: zone 4, 9 trang 11 dung dau bao khoi tren tran (S khung vuong, ma BC_KHOI_TRAN) -> bang ky hieu cot trai co dong rieng.
$chuKh = if ($ksb -and $ksb.doc -and $ksb.doc.Count -gt 3) { @($ksb.doc[3].ds | ForEach-Object { $_.t }) } else { @() }
Kiem (@($chuKh | Where-Object { $_ -like '*KH*I KI*U*TR*N TR*N*' }).Count -eq 1 -and $ksb.dem.BC_KHOI_TRAN -eq $null) `
  "so_do bc bang ky hieu co dong DAU BAO KHOI KIEU DIEM TREN TRAN (BC_KHOI_TRAN, zone 4, 9) ($($chuKh.Count) chu cot trai)"
Kiem ((@('4081', '8.20568', '103.481', '0.133', '0.24', '2360', '406', '(Kh*ng *ng k*)') | Where-Object { $p = $_; -not ($chuBang | Where-Object { $_ -like $p }) }).Count -eq 0) `
  "so_do bc bang tinh in so cham thap phan nhu trang 11: 4081, 8.20568, 103.481, 0.133, 0.24, 2360, 406, (Khong dang ke) ($($chuBang.Count) o chu)"
Kiem ($ksb -and $ksb.ok -eq $true -and $ksb.ve -gt 300 -and $ksb.dem.BC_KHOI -eq $ksd.dem.BC_KHOI -and $ksb.dem.BC_NHIET -eq $ksd.dem.BC_NHIET -and -not $ksb.dem.BC_DTCD -and
  -not $ksb.dem.BC_COI_DEN -and -not $ksb.dem.BC_CHI_THI -and -not $ksb.dem.SPR_CTDC) `
  "so_do bc trang 11 (12 zone, du phong, 5 lien dong, bang ky hieu, bang tinh): ve $($ksb.ve); block so do khong cong vao dem ($($ksb.dem | ConvertTo-Json -Compress))"
$lay = if ($ksb -and $ksb.doc) { @($ksb.doc[0].ds) } else { @() }
$nLay = { param($t) $x = @($lay | Where-Object { $_.l -eq $t }); if ($x.Count) { $x[0].n } else { 0 } }
Kiem ((& $nLay 'PCCC_SODO_DAY_TH') -ge 10 -and (& $nLay 'PCCC_SODO_DAY_TH_FR') -ge 16 -and (& $nLay 'PCCC_SODO_DAY_NGUON') -ge 8 -and (& $nLay 'PCCC_SODO_DAY_NGUON_FR') -eq 3) `
  "so_do bc moi loai day mot layer theo layer_so_do cua ma: zone 1-9 DAY_TH, bo day + zone van DAY_TH_FR, coi den DAY_NGUON, 2 nguon + mau DAY_NGUON_FR ($($lay | ForEach-Object { $_.l + '=' + $_.n }))"
$blk = if (Test-Path "$T\blk.txt") { @(Get-Content "$T\blk.txt") } else { @() }
$kieu = @($blk | Where-Object { $_ -match '^KIEU ' }); $blk = @($blk | Where-Object { $_ -notmatch '^KIEU ' })
$kn = if ($kieu.Count -eq 1) { $kieu[0].Split(' ') } else { @('', '0', '1') }
Kiem ([int]$kn[1] -gt 100 -and $kn[2] -eq '0') "so_do: moi MTEXT/TEXT tren layer PCCC_SODO* dung kieu chu PCCC_ARIAL co dau tieng Viet ($kieu)"
Kiem ($blk.Count -eq 2 -and $blk[0] -eq 'CIRCLE' -and $blk[1] -match '^LAYER 3 continuous$') `
  "so_do bc dung ky hieu cua ban ve: block BC_COI_DEN co san (1 CIRCLE) khong bi thu vien ghi de; layer day nguon co san giu mau 3, net Continuous ($($blk -join ' | '))"
Kiem ($ksbl -and $ksbl.ve -eq 0 -and @($ksbl.loi).Count -eq 2 -and $ksbl.loi[0].ly_do -match '26 .+toi_da 25' -and $ksbl.loi[1].ly_do -match 'toi_da') `
  "so_do bc: zone 26 dau bao > toi_da 25 bao loi; thieu toi_da bao loi; khong ve ($($ksbl.loi | ConvertTo-Json -Compress -Depth 3))"
$kbc2 = DocKq 'so_do_bc2'
$dc = { param($i) if ($kbc2 -and $kbc2.doc -and $kbc2.doc.Count -gt $i) { @($kbc2.doc[$i].ds) } else { @() } }
$c1 = @(& $dc 0); $c2 = @(& $dc 1); $c3 = @(& $dc 2); $c4 = @(& $dc 3)
$dong = { param($ds, $dau) @($ds | Where-Object { ($_.t -split "`r?`n")[0] -eq $dau } | ForEach-Object { , @($_.t -split "`r?`n") }) }
$lk = @(& $dong $c1 'T1 KARAOKE'); $lh = @(& $dong $c1 'T1 KHO'); $ld2 = @(& $dong $c2 'KHU B'); $l120 = @($c2 | Where-Object { $_.t -match ': 120 M' })
Kiem ($kbc2 -and $kbc2.ok -eq $true) "so_do bc: dem co DAY_BC (chep tu ket qua dem) bo qua day, khong bao loi ($($kbc2.loi | ConvertTo-Json -Compress -Depth 3))"
Kiem ($lk.Count -eq 1 -and $lk[0].Count -eq 3 -and $lk[0][1] -match '^DI.N T.CH B.O V.: 88\.11 M' -and $lk[0][2] -eq 'ZONE 1' -and
  $lh.Count -eq 1 -and $lh[0].Count -eq 2 -and $lh[0][1] -eq 'ZONE 2' -and $ld2.Count -eq 1 -and $ld2[0][1] -eq 'ZONE 2' -and
  $l120.Count -eq 1 -and @($l120[0].t -split "`r?`n").Count -eq 2) `
  "so_do bc: nhan duoi dien tro cuoi day nhu trang 11: khu vuc / DIEN TICH BAO VE: dt M2 / ZONE n; ghep ten tang + kv; thieu dt, thieu kv bo dong ($($c1 | ForEach-Object { $_.t -replace "`r?`n", ' / ' }))"
$yZ = { param($re) @($c3 | Where-Object { $_.t -match $re } | ForEach-Object { $_.p[1] }) }
$z1 = & $yZ '^ZONE 1$'; $z2 = & $yZ '^ZONE 2$'; $z3 = & $yZ '^ZONE 3$'; $dp = @($c3 | Where-Object { $_.t -eq 'ZONE 4 DU PHONG' })
Kiem ($z1.Count -eq 1 -and $z2.Count -eq 1 -and $z3.Count -eq 1 -and $z1[0] -lt $z2[0] -and $z2[0] -lt $z3[0] -and $dp.Count -eq 1 -and $dp[0].p[1] -gt $z1[0] -and $dp[0].p[1] -lt $z2[0] -and
  $dp[0].p[0] -lt 1700000 + 1000) "so_do bc: zone 1 duoi cung, xep len tren; dong du phong ben trai bo day giua zone 1 va 2 nhu trang 11 ($z1 / $z2 / $z3 / $($dp.p -join ','))"
$n21 = @($c2 | Where-Object { $_.t -eq '21' } | ForEach-Object { $_.p[1] }); $gs = @($c2 | Where-Object { $_.t -match '^VAN C.NG OS&Y' })
Kiem ($n21.Count -eq 1 -and $n21[0] -lt $z1[0] - 300 -and @($c2 + $c3 | Where-Object { $_.t -match '^x\d' }).Count -eq 0 -and $gs.Count -eq 1 -and $gs[0].t -match 'C.NG T.C D.NG CH.Y$') `
  "so_do bc: so luong ghi DUOI ky hieu; zone giam sat van ghi ten van OS&Y + cong tac dong chay ben phai ($n21 < $z1; $($gs.t -replace "`r?`n", ' / '))"
$ld = @($c4 | Where-Object { $_.t -eq 'LD THU' })
Kiem ($ld.Count -eq 1 -and $ld[0].p[0] -gt 1703900 -and $ld[0].p[1] -gt 43800 -and @($c1 + $c2 + $c3 + $c4 | Where-Object { $_.t -match '^[+-]\d\.\d{3}$|0\.000' }).Count -eq 0) `
  "so_do bc: lien dong di ngang sang phai tu canh tu (chu tren duong), khong cao do ($($ld.p -join ','))"
$ksm = DocKq 'so_do_m'
Kiem ($ksm -and $ksm.ok -eq $true -and $ksm.hop -and ($ksm.hop[2] - $ksm.hop[0]) -lt 50 -and ($ksm.hop[3] - $ksm.hop[1]) -lt 30) `
  "so_do ban ve don vi m: co chu nhan k, hop vai chuc m (thay $($ksm.hop -join ', '))"
$ksu = DocKq 'sua'
$dl = if ($ksu) { @($ksu.doc[0].ds) } else { @() }
$dLn = $dl | Where-Object { $_.loai -eq 'LWPOLYLINE' }; $dCh = $dl | Where-Object { $_.loai -eq 'MTEXT' }
Kiem ($ksu -and $ksu.sua -eq 4 -and $ksu.xoa -eq 0 -and @($ksu.loi).Count -eq 1 -and $ksu.loi[0].i -eq 5 -and $ksu.doc[0].n -eq 2 -and
  $dLn.mau -eq 1 -and $dLn.l -eq 'SUA_MOI' -and ($dLn.p[0] -join ',') -eq '5300500,200' -and ($dLn.p[1] -join ',') -eq '5301500,200' -and $dCh.t -eq 'MOI' -and $dLn.h -match '^[0-9A-F]+$') `
  "sua (Bot chat): doi 500,200 + l_moi + mau 1, doi chu MOI, doc gi dt tra handle; h sai bao loi ($($ksu | ConvertTo-Json -Compress -Depth 5))"
$kv = DocKq 'van'
Kiem ($kv -and $kv.ok -eq $true -and $kv.ve -eq 3 -and $kv.dem.SPR_VAN_OSY_DN100 -eq 2 -and $kv.dem.SPR_VAN_XK -eq 1) `
  "dat ma van moi: 2 SPR_VAN_OSY_DN100 + 1 SPR_VAN_XK, dem theo MA_VT (thay ve $($kv.ve), dem $($kv.dem | ConvertTo-Json -Compress))"
# PCCC_THEMBLOCK that bai o danh muc: thu vien DWG khong doi, khong file danh muc nao co ma moi
$out2 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb2.out.txt")) -replace "`0", ''
$kvd = DocKq 'vung_do'
$vd = if ($kvd) { @($kvd.vung_tu_dong) } else { @() }
# Lam gon vung (nguoi dung 16/11): cot goc 0,2 x 0,2 m lap vao vung (khac < 1 m): moi phong 5,8 x 4,8 = 27,84 m2, 4 dinh; thong nhau 55,88 m2.
Kiem ($kvd -and $vd.Count -eq 3 -and $vd[0].m2 -eq 55.88 -and $vd[1].m2 -eq 27.84 -and $vd[1].dinh -eq 4 -and $vd[2].m2 -eq 27.84 -and $vd[2].dinh -eq 4 -and
  ($vd[1].hop -join ',') -eq '100,100,5900,4900' -and ($vd[2].hop -join ',') -eq '6100,100,11900,4900') `
  "vung_tu_dong: bo truc (TRUC_T, CENTER tren NEN_A), cot hatch + cot polyline, noi o cua: 2 phong 27,84 m2 (khac cot lap); ho 500 thi 2 phong thong nhau 55,88 m2 ($($vd | ConvertTo-Json -Compress -Depth 3))"
Kiem ($kvd -and @($kvd.rai).Count -eq 2 -and @($kvd.loi).Count -eq 1 -and $kvd.loi[0].i -eq 4 -and $kvd.loi[0].ly_do -match '\[30000,30000\]: ') `
  "vung_tu_dong + rai l PCCC_VUNG: rai 2 phong; diem ngoai cong trinh bao loi ($($kvd.rai.n -join '/'), $($kvd.loi | ConvertTo-Json -Compress -Depth 3))"
Kiem ($out2 -match 'm2=27\.84 \(4 [\s\S]{0,400}PCCC_RAI SPR_DAU_DOWN n=\d+') 'PCCC_RAI bam diem trong phong khong co polyline: tu dung vung 27,84 m2 (4 dinh, khac cot lap) roi rai'
$vm = [regex]::Match($out2, 'layer VUNG_XANH[\s\S]*?trong (\d+) ph')
$vmXanh = ([regex]::Matches($vm.Value, 'm2=300 \(4 ')).Count; $vmDo = ([regex]::Matches($vm.Value, 'm2=150 \(4 ')).Count
Kiem ($vm.Success -and $vm.Groups[1].Value -eq '3' -and $vmXanh -eq 2 -and $vmDo -eq 1 -and $vm.Value -match 'layer VUNG_DO') "PCCC_RAI ban ve la 1 block, bam net mau xanh + do: dung dung 3 vung (2 x 300 + 150 m2), bo khung giay, o khung ten do layer khac ($($vm.Groups[1].Value) vung, xanh $vmXanh, do $vmDo)"
$vk = [regex]::Match($out2, 'File spec JSON: [^\r\n]*vung_khung\.json[\s\S]*?trong (\d+) ph')
Kiem ($vk.Success -and $vk.Groups[1].Value -eq '2' -and ([regex]::Matches($vk.Value, 'm2=80 \(4 ')).Count -eq 2) "PCCC_RAI Enter = moi phong (tu nhan tu tuong): khung giay chua 2 phong bi bo, rai dung 2 phong 80 m2 ($($vk.Groups[1].Value) vung)"
$dm2 = Get-Content -Raw -Encoding UTF8 "$T\vat_tu.json" | ConvertFrom-Json
$dmGoc2 = Get-Content -Raw -Encoding UTF8 $fGoc | ConvertFrom-Json
Kiem ((Get-FileHash "$T\PCCC_Blocks.dwg").Hash -eq $bamTv -and $out2 -match 'VAN_TRUNG \S+ c. trong' -and $out2 -match 'kh\S+ng ghi \S+c' -and
  @($dm2.vat_tu | Where-Object { $_.ma -match '^VAN_(TRUNG|RO)$' }).Count -eq 0 -and @($dmGoc2.vat_tu | Where-Object { $_.ma -eq 'VAN_TRUNG' }).Count -eq 1 -and
  @($dmGoc2.vat_tu | Where-Object { $_.ma -eq 'VAN_RO' }).Count -eq 0) 'PCCC_THEMBLOCK: ma trung trong file goc, file goc chi doc: bao loi, PCCC_Blocks.dwg va danh muc khong doi'
Set-ItemProperty $fGoc IsReadOnly $false
# Viec 1: PCCC_RAI BC_KHOI khong hoi S, m2, cach tuong (chi hoi chieu cao tran, Enter), lay TCVN 7568-14 tu khoang_cach_rai.json: 30600 x 20400 ra 3 x 2 buoc 10200.
# Nguoi dung 16/11: tu dim sau khi rai, trung hang trung cot dim 1 lan: 3 cot + 2 tuong = 4 dim X, 2 hang + 2 tuong = 3 dim Y.
Kiem ($out2 -match 'PCCC_RAI BC_KHOI n=6 luoi=3x2 buoc=10200x10200 m2=104\.04 kt=7(\r|\n)') 'PCCC_RAI BC_KHOI: tri so TCVN 7568-14 tu file, khong hoi S, ra 3 x 2 buoc 10200; tu dim 1 chuoi X (4) + 1 chuoi Y (3)'
Kiem ($out2 -match 'PCCC_RAI BC_KHOI n=24 luoi=6x4 buoc=5100x5100 ') 'PCCC_RAI HeSo 50 (muc toi uu k, PCCC_BOTRI gui): cung phong ra 6 x 4 buoc 5100, gap 4 lan k = 100 %'
Kiem ($out2 -match 'PCCC_RAI BC_BEAM tia=2 buoc=10000 tuong=5000 kt=3') 'PCCC_RAI BC_BEAM (TCVN 7568-14 5.9.1.3.2): phong 30 x 20 m ra 2 tia song song canh dai, cach nhau 10 m, cach tuong 5 m; chuoi dim 5000/10000/5000'
Kiem ($out2 -match 'PCCC_RAI BC_KHOI n=6 luoi=3x2 buoc=10200x10200 m2=104\.04 \(xem') 'PCCC_RAI XemTruoc: dem 6 dau bao, bao (xem truoc, chua ve)'
Kiem ($out2 -match 'PCCC_RAI BC_NHIET n=6 luoi=3x2 buoc=4800x3600 ') 'PCCC_RAI Dam (5.9.2.1.5): dau nhiet buoc 7,2 x 0,7 = 5,04 m, phong 14,4 x 7,2 ra 3 x 2 (khong dam: 2 x 1)'
Kiem ($out2 -match 'PCCC_DAY DAY_BC thiet_bi=24 vong m=[\d.]+ zone=ZONE 1 m2=[1-9]') 'PCCC_DAY: day loop noi 24 dau bao cua phong HeSo 50, vong ve diem dau, nhan ZONE 1 + dien tich vung PCCC_VUNG'
Kiem ($out2 -match 'PCCC_DAY DAY_BC thiet_bi=24 m=[\d.]+ dtcd=1(\r|\n)') 'PCCC_DAY day ho: 1 dien tro cuoi day o mut, khong ghi zone'
Kiem ($out2 -match 'PCCC_TRUNG trung=1 thua=1') 'PCCC_TRUNG (CKC): 2 dau khoi trung diem ra 1 cum, 1 ban thua'
Kiem ($out2 -match 'PCCC_LOPCHUAN sai=1 sua=1') 'PCCC_LOPCHUAN (CKL, APL): dau khoi o layer 0 ve lai PCCC_BC'
Kiem ($out2 -match 'PCCC_DANHSO n=4\b') 'PCCC_DANHSO (AN): danh so 4 thiet bi'
Kiem ($out2 -match 'PCCC_XOAY n=4 goc=90' -and $out2 -match 'PCCC_TYLE n=4 he_so=2') 'PCCC_XOAY, PCCC_TYLE (MR, SCB): 4 block quanh diem chen'
Kiem ($out2 -match 'PCCC_DOIBLOCK n=4 BC_NHIET') 'PCCC_DOIBLOCK (SB): 4 dau khoi thanh dau nhiet'
Kiem ($out2 -match 'PCCC_CANDIM n=2') 'PCCC_CANDIM (AD): 2 dim dong ve mot moc'
Kiem ($out2 -match 'PCCC_DOIDN n=1 ONG_DN65') 'PCCC_DOIDN (DN): doan ong sang layer PCCC_ONG_DN65'
Kiem ($out2 -match 'xem_truoc=12' -and $out2 -match 'xoa_xem_truoc=12') 'XemTruoc ve tam 6 diem + 6 vong R tren PCCC_XEM_TRUOC, luot rai that sau xoa het 12'
Kiem ($out2 -match 'bo_loai_tru=1' -and $out2 -match 'PCCC_RAI BC_KHOI n=5 luoi=3x2') 'PCCC_LOAITRU: lo thang trum 1 diem luoi 3 x 2, con 5 dau bao'
Kiem ($out2 -match 'PCCC_KT phong=1 mu=0% diem_mu=0') 'PCCC_KT: phong rai k = 50 % phu kin'
Kiem ($out2 -match 'PCCC_KT ph\S+ng \d+: m2=4 [^\r\n]*=0 [^\r\n]*=100%') 'PCCC_KT: WC 2 x 2 m khong dau bao sat phong co dau bao: mu 100 % (khong tinh phu xuyen tuong)'
Kiem ($out2 -match 'khoang_dam nguong=9 lon=0 nho=2[\s\S]{0,1500}khoang_nho_chung n=1\b[\s\S]{0,600}khoang_nho_chung n=1\b') 'Khoang dam nho 2 phong cach 30 m: gom theo tung phong, moi phong 1 dau'
Kiem ($out2 -match 'khoang_dam nguong=4 lon=1 nho=0') 'Dam khong nhap tran: nguong 4 m2, khoang 6 m2 rai rieng'
$kin = DocKq 'rai_inch'; $kun = DocKq 'rai_unitless'
$kdl = DocKq 'dem_lon'; $krl = DocKq 'rai_lon'
Kiem ($kdl -and $kdl.doc[0].ds[0].n -ge 10000 -and $kdl.ms -ge 0) "DO: dem ca ban ve co 10 000 block noi that x 50 net: $($kdl.ms) ms"
Kiem ($krl -and $krl.rai[0].n -gt 1000 -and $krl.rai[0].ho -eq 0 -and $krl.ms -ge 0) "DO: rai vung da giac xien ~12 600 m2 s 3 m: n=$($krl.rai[0].n), $($krl.ms) ms"
Kiem ($kin -and $kin.rai[0].n -eq 6 -and [math]::Abs($kin.rai[0].bx - 401.57) -lt 0.1 -and $kin.don_vi -eq 'in') "INSUNITS 1 (inch): rai 30,6 x 20,4 m ra 6 dau, buoc 401,57 in ($($kin | ConvertTo-Json -Compress -Depth 4))"
Kiem ($kun -and $kun.rai[0].n -eq 6 -and $kun.don_vi -match '^mm') "INSUNITS 0: ban ve lon doan mm, bao don_vi ($($kun.don_vi))"
Kiem ($out2 -match 'PCCC_KT phong=1 mu=59\.5% diem_mu=952') 'PCCC_KT: phong 20 x 20 m, 1 dau khoi o tam, R 7,2 m: mu 59,5 % (952 / 1600 diem mau 0,5 m)'
Kiem ($out2 -match 'PCCC_TACHVUNG vung=2 m2=80\+120') 'PCCC_TACHVUNG (TZ): vung 20 x 10 m cat tai x = 8 m ra 80 + 120 m2'
Kiem ($out2 -match 'PCCC_KHUVUC vung=\d+ dat_ten=2 ') 'PCCC_KHUVUC (MZ): dat ten 2 vung'
Kiem ($out2 -match 'dich_ong_gio=1(\r|\n)[\s\S]{0,200}PCCC_RAI BC_KHOI n=6 luoi=3x2') 'Ong gio (layer ONG_GIO) trum 1 dau bao: dich ra ngoai bien, van 6 dau'
Kiem ($out2 -match 'o_x=1 [\s\S]{0,300}gach_cheo=1 [\s\S]{0,300}bo_phong=2 [\s\S]{0,2500}trong 6 ph') `
  'O X (lo thong tang) + hatch ANSI31 bo; cua 2 net 5 m, cung cua 4 m, cua khe chu T, goc hut 5 mm khep: 6 phong (khe 4 m khong cua van ho)'
Kiem ($out2 -match 'dam ky_hieu=3(\r|\n)[\s\S]{0,300}dam_sau=10 dam_nong=7[\s\S]{0,600}khoang_dam nguong=9 lon=5 nho=18[\s\S]{0,4000}khoang_nho_chung n=2\b') `
  'Khoang dam theo ky hieu b x h: 10 net dam sau giu, 7 net B2 (d = 0,3) bo; 4 khoang 50 m2 + phong B phang rai tung khoang, 18 khoang 4 m2 gom 2 dau'
Kiem ($out2 -match 'dam ky_hieu=0 d_chung=0.3[\s\S]{0,300}dam_sau=0 dam_nong=1[\s\S]{0,600}khoang_dam nguong=9 lon=1 nho=0') `
  'Dam khong ky hieu: hoi co chung 250x300 (d = 0,3) -> bo dam, phong 10 x 10 m rai nhu tran phang'
Kiem ($out2 -match 'tung_dam d=0.6 m: 1 [\s\S]{0,800}tung_dam d=0.25 m: 1 [\s\S]{0,300}dam ky_hieu=0 tung_dam=2[\s\S]{0,300}dam_sau=2 dam_nong=0[\s\S]{0,600}khoang_dam nguong=9 lon=2 nho=0') `
  'TungDam: line tim dam 400x600 doi ra 2 mep (+-200, mep dam = tuong) chia 2 khoang, line 200x250 bo (khong lam tuong, khong chia khoang)'
# Y tu video VPTTOOL 16/11: ghi m2 giua tung khoang, dau X o khoang duoi nguong (layer PCCC_KHOANG_M2); tat bang kd_ghi_m2 = Khong trong cai_dat.json.
Kiem ($out2 -match 'khoang_dam nguong=9 lon=5 nho=18[\s\S]{0,8000}ghi_khoang=23 dau_x=18 bo_nho_1m2=0' -and $out2 -match 'khoang_dam nguong=9 lon=0 nho=2[\s\S]{0,3000}ghi_khoang=2 dau_x=2 ') `
  'Ghi m2 tung khoang: 5 khoang lon + 18 khoang 4 m2 ghi 23 chu, 18 dau X; 2 khoang nho 2 phong: 2 chu, 2 dau X'
Kiem ($out2 -match 'PCCC_RAI BC_KHOI n=4 luoi=2x2 buoc=10000x10000 m2=100[\s\S]{0,600}PCCC_KT phong=1 mu=0% diem_mu=0[\s\S]{0,80}Command: PCCC_TACHVUNG') `
  'Vung tron PCCC_VUNG (CIRCLE r 10 m): PCCC_RAI 2 x 2 buoc 10 m trong vong tron, PCCC_KT phu kin'
$moi = if (Test-Path "$T\moi.txt") { Get-Content -Raw "$T\moi.txt" } else { '' }
Kiem ($moi -match 'XEM 0' -and $moi -match 'BEAM 2' -and $moi -match 'TIA 4') "XemTruoc khong chen block, BC_BEAM 2 block + 4 net PCCC_BC_TIA: $($moi -replace '\s+', ' ')"

# --- Kien truc (goi A 14/11): tuong, vach, cua, kt_chuoi, vung_tu_dong tren phong ve bang tuong; lenh PCCC_TRUC, PCCC_TUONG, PCCC_CUA, PCCC_ADIM, PCCC_CDIM ---
# Phong 6000 x 4000 (tim), tuong 200 kin + vach 100 o x 4000 (2 dau cham tim tuong ngoai), cua 900 tuong duoi, cua 800 tren vach.
# Mat trong: phong trai 3850 x 3800 = 14,63 m2, phong phai 1850 x 3800 = 7,03 m2; ho 500 < cua 900 thi phong trai ho ra ngoai: bao loi.
# kt_chuoi luoi 4 cot x 3 hang (1 diem lech 5 mm): phuong tu nhan X (qua o tren) ra 3 dim; phuong y ra 2 dim.
$ktG = @(1000, 4000, 7000, 10000) | ForEach-Object { $x = $_; @(10000, 12000, 14000) | ForEach-Object { , @($x, $_) } }
$ktG[3] = @(4005, 10000)
$ktP = ConvertTo-Json -Compress $ktG
('{"ops":[{"op":"tuong","p":[[0,0],[6000,0],[6000,4000],[0,4000]],"kin":true},{"op":"tuong","vach":true,"p":[[4000,0],[4000,4000]]},' +
 '{"op":"cua","p":[3000,0],"w":900,"ve":[3000,2000]},{"op":"cua","p":[4000,2000],"w":800,"ve":[5000,2000],"mo":"phai"},' +
 '{"op":"kt_chuoi","p":[[0,0],[4000,0],[6000,0],[6000,4000]],"qua":[0,-1000]},{"op":"kt_chuoi","p":[[0,0],[0,4000]],"phuong":"y","qua":[-1000,0]},' +
 '{"op":"vung_tu_dong","p":[[2000,2000]],"ho":500,"l":"VUNG_X"},{"op":"vung_tu_dong","p":[[2000,2000],[5000,2000]]},' +
 '{"op":"kt_chuoi","l":"DIM_C","p":' + $ktP + ',"qua":[0,16000]},{"op":"kt_chuoi","l":"DIM_D","p":' + $ktP + ',"phuong":"y"},' +
 '{"op":"doc","gi":"layer","l":"DIM*"},{"op":"doc","gi":"layer","l":"CUA"},{"op":"anh","hop":[-2500,-2500,8000,5500],"rong":800}]}') | Set-Content -Encoding ascii "$T\kt.json"
# ADIM: luoi BC_KHOI 3 cot x 4 hang (1 dau lech 3 mm), 2 moc tuong x 19500 / 28500 -> 5 moc, 4 dim; CDIM them 1 (diem trung moc cuoi bo qua);
# ADIM lan 2 khong moc tuong, phuong Y -> 3 dim. Tat ca tren layer hien hanh ADIM_L.
$adG = @(21000, 24000, 27000) | ForEach-Object { $x = $_; @(11000, 13000, 15000, 17000) | ForEach-Object { , @($x, $_) } }
$adG[5] = @(24003, 13000)
('{"ops":[{"op":"dat","ma":"BC_KHOI","p":' + (ConvertTo-Json -Compress $adG) + '}]}') | Set-Content -Encoding ascii "$T\kt_dat.json"
'{"ops":[{"op":"doc","gi":"layer","l":"ADIM_L"},{"op":"vung_tu_dong","p":[[43000,2000]]}]}' | Set-Content -Encoding ascii "$T\kt_tay.json"
# Lam gon vung (nguoi dung 16/11, test bao chay.dwg): phong 20 x 15 m co hoc tran 400 sau, buoc tuong 150, khac cot 400 x 400 goc, go cung cua 40,
# loi thang 3 x 3 m goc duoi trai. Vung ra 6 dinh: lap khac nho (< 1 m) ra duong ngoai, giu loi thang: 20 x 15 - 3,15 x 3 = 290,55 m2.
('{"goc":[200000,0],"ops":[{"op":"net","l":"KT_A","kin":true,"p":[[3150,0],[10000,0],[10050,40],[10150,40],[10200,0],[19600,0],[19600,400],[20000,400],' +
 '[20000,15000],[12000,15000],[12000,14600],[6000,14600],[6000,15000],[0,15000],[0,8000],[150,8000],[150,3000],[3150,3000]]},' +
 '{"op":"vung_tu_dong","p":[[10000,8000]]},' +
 # Vung PCCC_VUNG ve tay ve dung diem dau nhung khong khep (Closed = 0, nguoi dung 16/11): van la vung kin.
 '{"op":"net","l":"PCCC_VUNG","p":[[30000,0],[50000,0],[50000,15000],[30000,15000],[30000,0]]},{"op":"doc","gi":"vung","hop":[29000,-1000,51000,16000]}]}') |
  Set-Content -Encoding ascii "$T\gon.json"
@('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\kt.dwg`"",
  'PCCC_SPEC', "$T\kt.json", 'PCCC_SPEC', "$T\kt_dat.json", '-LAYER', 'M', 'ADIM_L', '', '_.ZOOM', '_E', 'OSMODE', '0',
  'PCCC_ADIM', '', 'W', '19000,9000', '30000,20000', '', '19500,10000', '28500,10000', '', '20000,19000',
  'PCCC_CDIM', '30000,19000', '30000,25000', '',
  'PCCC_ADIM', 'Block', 'W', '19000,9000', '30000,20000', '', '', 'Y', '18000,12000', '-LAYER', 'S', '0', '',
  'PCCC_ADIMMOC', '1', '19500,10000', 'PCCC_ADIMMOC', '2', '28500,10000', 'PCCC_ADIM', 'Block', 'W', '19000,9000', '30000,20000', '', 'X', '20000,21000',
  '_.MLINE', '60000,0', '63000,0', '64000,0', '', '_.ZOOM', '_E', 'PCCC_ADIM', 'Mline', 'W', '59000,-1000', '65000,1000', '', '', 'X', '60000,-2000',
  'PCCC_TRUC', '6000,6000,4500', '5000', '-30000,0',
  'PCCC_TUONG', '', '', '40000,0', '46000,0', '46000,4000', '40000,4000', 'D',
  'PCCC_CUA', '43000,0', '', '43000,2000', '',
  'PCCC_SPEC', "$T\kt_tay.json", 'PCCC_SPEC', "$T\gon.json",
  # PCCC_DAYBC (nguoi dung 16/11): he thuong luoi 3 x 3 + nut nhan + 2 chuong, tu bam (8000,-3000); he dia chi 6 dau thuong + 2 dau dia chi + ZM + CM + nut nhan
  # + 2 chuong + tu BC_TT trong vung chon; he dia chi chon nhom he thuong (khong ZM) thi bao loi.
  'PCCC_DAT', 'BC_KHOI', '302000,2000', '308000,2000', '314000,2000', '302000,8000', '308000,8000', '314000,8000', '302000,14000', '308000,14000', '314000,14000', '',
  'PCCC_DAT', 'BC_NUTAN', '316500,500', '', 'PCCC_DAT', 'BC_CHUONG', '316500,1500', '300000,16000', '',
  'PCCC_DAT', 'BC_KHOI', '332000,2000', '338000,2000', '344000,2000', '332000,8000', '338000,8000', '344000,8000', '',
  'PCCC_DAT', 'BC_KHOI_DC', '332000,14000', '338000,14000', '', 'PCCC_DAT', 'BC_ZM', '346500,500', '', 'PCCC_DAT', 'BC_CM', '347500,500', '',
  'PCCC_DAT', 'BC_NUTAN', '346500,9000', '', 'PCCC_DAT', 'BC_CHUONG', '346500,10000', '330000,15000', '', 'PCCC_DAT', 'BC_TT', '338000,-3000', '', '_.ZOOM', '_E',
  'PCCC_DAYBC', 'Thuong', 'W', '299000,-1000', '317000,17000', '', '308000,-3000',
  'PCCC_DAYBC', 'DiaChi', 'W', '329000,-4000', '348000,17000', '',
  'PCCC_DAYBC', 'DiaChi', 'W', '299000,-1000', '317000,17000', '', '308000,-3000',
  # Nguoi dung 16/11 (anh rai linh tinh): ban ve mm de INSUNITS 1 (inch) -> PCCC_RAI tu doi ve mm truoc khi rai (kho 378 000 don vi = 9,6 km neu la inch).
  '_.ZOOM', '_E', 'INSUNITS', '1', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '199000,-1000', '221000,16000', '', '',
  '_.QSAVE') -join "`r`n" | Set-Content -Encoding ascii "$T\kb3.scr"
$p3 = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/s `"$T\kb3.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb3.out.txt"
if (-not $p3.WaitForExit(180000)) { Stop-Process -Id $p3.Id -Force; throw "accoreconsole qua 3 phut (kien truc): $T\kb3.out.txt" }
$out3 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb3.out.txt")) -replace "`0", ''
$kk = DocKq 'kt'
$vk = if ($kk) { @($kk.vung_tu_dong) } else { @() }
$nDim = @{}; if ($kk -and $kk.doc) { foreach ($d in @($kk.doc[0].ds)) { $nDim[$d.l] = $d.n } }
Kiem ($kk -and $vk.Count -eq 2 -and $vk[0].m2 -eq 14.63 -and $vk[0].dinh -eq 4 -and $vk[1].m2 -eq 7.03 -and $vk[1].dinh -eq 4) `
  "tuong + vach + cua: vung_tu_dong dung 2 phong 14,63 / 7,03 m2, 4 dinh ($($vk | ConvertTo-Json -Compress -Depth 3))"
Kiem ($kk -and $kk.loi -and @($kk.loi).Count -eq 1 -and $kk.loi[0].i -eq 6) "cua 900 > ho 500: phong ho, vung_tu_dong bao loi ($($kk.loi | ConvertTo-Json -Compress -Depth 3))"
Kiem ($kk -and $kk.xoa -eq 3 -and $kk.doc[1].ds[0].n -eq 4) "cua: thay 3 polyline tuong (2 vong tuong ngoai + vach), 2 canh + 2 cung tren CUA (xoa $($kk.xoa))"
Kiem ($nDim['DIM'] -eq 3 -and $nDim['DIM_C'] -eq 3 -and $nDim['DIM_D'] -eq 2) "kt_chuoi: luoi 4 x 3 lech 5 mm phuong X ra 3 dim, Y ra 2 dim, khong dim 0 ($($nDim | ConvertTo-Json -Compress))"
$fk = if ($kk) { [string]@($kk.anh)[0] }
Kiem ($fk -and (Test-Path $fk) -and (DocAnh $fk).mau -gt 1) "anh phong tuong + cua + chuoi dim: $fk"
$kt2 = DocKq 'kt_tay'
Kiem ($out3 -match 'PCCC_ADIM n=4 phuong=X moc=5' -and $out3 -match 'PCCC_CDIM n=1' -and $out3 -match 'PCCC_ADIM n=3 phuong=Y' -and $kt2 -and $kt2.doc[0].ds[0].n -eq 8) `
  'PCCC_ADIM quet luoi 3 x 4 + 2 moc tuong ra 4 dim (1 cot 1 moc), CDIM noi 1, ADIM phuong Y ra 3 dim'
Kiem ($out3 -match 'PCCC_ADIM n=2 phuong=X moc=3') 'PCCC_ADIM loc Mline (spec ADIM buoc 3): mline 3 dinh ra 2 dim'
Kiem ($out3 -match 'PCCC_ADIMMOC 2 X=28500 Y=10000' -and ([regex]::Matches($out3, 'PCCC_ADIM n=4 phuong=X moc=5 \(\d+ diem, 2 moc tuong\)')).Count -eq 2) `
  'PCCC_ADIMMOC: 2 moc tuong bam truoc, PCCC_ADIM khong hoi lai, ra cung 4 dim nhu khi go moc trong lenh'
Kiem ($out3 -match 'PCCC_TRUC 4x2 truc ve=' -and $out3 -match 'PCCC_TUONG tuong d=200 ve=2' -and $out3 -match 'PCCC_CUA ve=\d+ thay=2' -and
  $kt2 -and @($kt2.vung_tu_dong).Count -eq 1 -and $kt2.vung_tu_dong[0].m2 -eq 22.04) `
  "PCCC_TRUC, PCCC_TUONG (kin), PCCC_CUA: phong 5800 x 3800 = 22,04 m2 dung duoc vung ($($kt2.vung_tu_dong | ConvertTo-Json -Compress -Depth 3))"
$kgon = DocKq 'gon'
Kiem ($kgon -and @($kgon.vung_tu_dong).Count -eq 1 -and $kgon.vung_tu_dong[0].dinh -eq 6 -and $kgon.vung_tu_dong[0].m2 -eq 290.55) `
  "Lam gon vung (nguoi dung 16/11): hoc tran, buoc tuong, khac cot, go cua lap ra duong ngoai, giu loi thang: 6 dinh 290,55 m2 ($($kgon.vung_tu_dong | ConvertTo-Json -Compress -Depth 3))"
$vgKin = @(if ($kgon -and $kgon.doc) { $kgon.doc[0].ds })  # @() ngoai: if tra 1 phan tu thi PowerShell 5 bo mang
Kiem ($vgKin.Count -eq 1 -and $vgKin[0].m2 -eq 300) "PCCC_VUNG ve ve dung diem dau ma Closed = 0 van la vung kin 300 m2 ($($vgKin | ConvertTo-Json -Compress -Depth 3))"
Kiem ($out3 -match 'PCCC_DAYBC he=thuong tin_hieu=10 m=[\d.]+ dtcd=1 chuong=2 m_chuong=[\d.]+ xeo=0') `
  'PCCC_DAYBC he thuong: 1 duong qua 9 dau bao + nut nhan, 1 dien tro cuoi day, chuong 1 duong rieng, khong doan xeo'
Kiem ($out3 -match 'PCCC_DAYBC he=dia_chi tin_hieu=9 m=[\d.]+ dtcd=1 chuong=2 m_chuong=[\d.]+ vong=4 m_vong=[\d.]+ xeo=0 \(t') `
  'PCCC_DAYBC he dia chi: mach vong tu BC_TT qua 2 dau dia chi + ZM + CM; 7 thiet bi thuong ve ZM (1 dien tro), 2 chuong ve CM, khong doan xeo'
$mInch = [regex]::Match($out3, 'don_vi_doi=1->4[\s\S]{0,1500}?PCCC_RAI BC_KHOI n=(\d+) ')
Kiem ($mInch.Success -and [int]$mInch.Groups[1].Value -ge 2 -and [int]$mInch.Groups[1].Value -le 8) `
  "INSUNITS 1 tren ban ve mm: PCCC_RAI tu dat INSUNITS 4 roi rai phong 20 x 15 m ra vai dau (thay $($mInch.Groups[1].Value); truoc sua: hang tram dau)"
Kiem ($out3 -match 'L\S+i: 10 [^\r\n]{0,40}module ZM') 'PCCC_DAYBC he dia chi khong co ZM ma co dau bao thuong: bao loi, khong ve'

# --- Nguoi dung 16/11 (rai "do" sau khi chep net PCCC_VUNG huong dan, block ca to co hinh chi tiet): 2 phong 15 x 20 m (tuong layer TUONG),
# 1 phong chi tiet 3 x 3 m cach xa 70 m, phong trai da co polyline PCCC_VUNG ve san (khong chon). Enter moi phong: bo cum chi tiet,
# phong trai rai theo vung ve san (khong ve chong), phong phai tu dung; 2 x 4 dau. Luoi 2 x 2 khong lech: moi phong 1 chuoi X (3) + 1 chuoi Y (3).
('{"ops":[{"op":"net","l":"TUONG","p":[[0,0],[30000,0],[30000,20000],[0,20000]],"kin":true},{"op":"net","l":"TUONG","p":[[15000,0],[15000,20000]]},' +
 '{"op":"net","l":"TUONG","p":[[100000,0],[103000,0],[103000,3000],[100000,3000]],"kin":true},' +
 '{"op":"net","l":"PCCC_VUNG","p":[[0,0],[15000,0],[15000,20000],[0,20000]],"kin":true}]}') | Set-Content -Encoding ascii "$T\cum.json"
(@('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\cum.dwg`"", 'PCCC_SPEC', "$T\cum.json", '_.ZOOM', '_E',
  '(setq ss (ssget "X" (list (cons 8 "TUONG"))))', 'PCCC_RAI', 'BC_KHOI', '', '', '!ss', '', '', '', '_.QSAVE') -join "`r`n") | Set-Content -Encoding ascii "$T\kb4.scr"
$p4 = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/s `"$T\kb4.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb4.out.txt"
if (-not $p4.WaitForExit(180000)) { Stop-Process -Id $p4.Id -Force; throw "accoreconsole qua 3 phut (cum phong): $T\kb4.out.txt" }
$out4 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb4.out.txt")) -replace "`0", ''
$n4 = 0; foreach ($mm in [regex]::Matches($out4, 'PCCC_RAI BC_KHOI n=(\d+) luoi=2x2 [^\r\n]* kt=(\d+)')) { if ($mm.Groups[2].Value -eq '6') { $n4 += [int]$mm.Groups[1].Value } }
Kiem ($out4 -match 'bo_cum=1 phong=1 ' -and $out4 -match 'theo_vung_co_san=1 ' -and ([regex]::Matches($out4, 'V\S+ng t\S+ d\S+ng m2=300 ')).Count -eq 1 -and $n4 -eq 8) `
  "Enter moi phong: bo cum chi tiet xa, phong co PCCC_VUNG ve san rai theo vung do khong ve chong, 2 phong x 4 dau, moi phong 6 dim (n=$n4)"

# --- Sua 6 loi lo ra o tang 8 KOCHAM 16/11 (07_CAD/00_QUAN_LY_DU_AN/TIEN_DO.md): moi loi mot ca, cung mot ban ve ---
'{"ops":[{"op":"vung_tu_dong","p":[[12000,5000]],"l":"TAM"}]}' | Set-Content -Encoding ascii "$T\vtd6.json"
(@('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\sua6.dwg`"", 'OSMODE', '0',
  # 1: phong 20 x 15 m tuong layer TUONG, loi thang 5 x 5 m goc tren trai: tuong TUONG nam trong block CORE1 chen tren layer HANA_FURNITURE
  #    (truoc sua: block layer noi that bo ca net tuong ben trong, PCCC_RAI gop loi vao phong = 300 m2; vung_tu_dong cung vay)
  '-LAYER', 'M', 'TUONG', '', '_.RECTANG', '0,0', '20000,15000', 'PLINE', '0,10000', '5000,10000', '5000,15000', '0,15000', 'C',
  '-BLOCK', 'CORE1', '0,0', 'L', '', '-LAYER', 'M', 'HANA_FURNITURE', '', '-INSERT', 'CORE1', '0,0', '1', '', '0', '-LAYER', 'S', '0', '',
  '_.ZOOM', 'W', '-1000,-1000', '21000,16000', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '-500,-500', '20500,15500', '', '', '12000,5000', '',
  'PCCC_SPEC', "$T\vtd6.json",
  # 2: PCCC_ONG tu hoi diem (truoc sua: SendStringToExecute PLINE, trong .scr toa do bi doc thanh lenh "Unknown command")
  'PCCC_ONG', 'ONG_DN50', '100000,0', '105000,0', '105000,3000', '',
  # 3: vung A 10 x 6 m (4 dau UP), vung B cach 2 m (2 dau UP), vung C sat A 0,2 m (1 dau DOWN, nhu WC); khung W lay ca 7 dau -> giu 4 dau cua A
  '-LAYER', 'M', 'PCCC_VUNG', '', '_.RECTANG', '200000,0', '210000,6000', '_.RECTANG', '200000,8000', '210000,11000', '_.RECTANG', '210200,0', '213000,6000',
  '-LAYER', 'S', '0', '', 'PCCC_DAT', 'SPR_DAU_UP', '202500,1500', '207500,1500', '202500,4500', '207500,4500', '',
  'PCCC_DAT', 'SPR_DAU_UP', '202500,9500', '207500,9500', '', 'PCCC_DAT', 'SPR_DAU_DOWN', '211600,3000', '',
  'PCCC_ONG', 'ONG_DN50', '199000,-1000', '214000,-1000', '', '_.ZOOM', 'W', '198000,-2000', '215000,12000',
  'PCCC_TUYEN', 'ONG_DN25', 'W', '199000,-500', '214000,12000', '', 'L', '205000,3000',
  # 4 + 6: vung ve san 12 x 6 m, dam B1 (300x600) doc x = 4, 8 m, ngang y = 3 m -> 6 khoang 4 x 3 m, moi khoang 1 dau; dim chung tren vung
  #    (truoc sua: co vung ve san thi khong chia khoang, 1 vung 2 dau; dim tung khoang 6 x 4 = 24)
  '-LAYER', 'M', 'PCCC_VUNG', '', '_.RECTANG', '300000,0', '312000,6000', '-LAYER', 'M', 'DAM', '',
  '_.LINE', '304000,0', '304000,6000', '', '_.LINE', '308000,0', '308000,6000', '', '_.LINE', '300000,3000', '312000,3000', '',
  '_.TEXT', '304300,4500', '250', '0', 'B1 (300x600)', '-LAYER', 'S', '0', '', '_.ZOOM', 'W', '299000,-1000', '313000,7000',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '3.5', '', 'W', '299500,-500', '312500,6500', '', '', '304000,1000', '',
  # 5: sanh 4 x 3 m, van phong 9,7 x 14 m (cach sanh 0,3 m), dai tren loi 4 x 6 m; loi 4 x 5 m giua sanh va dai khong co vung.
  #    Tu o sanh, dau bao o dai va van phong: duong gay goc tu -> dai cat loi, phai vong qua van phong
  '-LAYER', 'M', 'PCCC_VUNG', '', '_.RECTANG', '400000,0', '404000,3000', '_.RECTANG', '404300,0', '414000,14000', '_.RECTANG', '400000,8000', '404000,14000',
  '-LAYER', 'S', '0', '', 'PCCC_DAT', 'BC_KHOI', '402000,11000', '409000,7000', '', '_.ZOOM', 'W', '399000,-1000', '415000,15000',
  'PCCC_DAYBC', 'Thuong', 'C', '399500,-500', '414500,14500', '', '401000,1500',
  # 7 (nguoi dung 16/11: loi thang, sanh thang may, buong thang, WC khong dat dau bao): 4 phong 15 / 5 / 5 / 5 x 10 m; chu "SANH THANG MAY",
  #    block BON_CAU, chu "THANG BO". Enter moi phong: chi rai phong 150 m2, bo 3 phong 50 m2
  '-LAYER', 'M', 'TUONG', '', '_.RECTANG', '500000,0', '530000,10000', '_.LINE', '520000,0', '520000,10000', '', '_.LINE', '525000,0', '525000,10000', '',
  '_.LINE', '515000,0', '515000,10000', '', '-LAYER', 'S', '0', '', '_.TEXT', '516000,5000', '250', '0', 'SANH THANG MAY', '-LAYER', 'S', 'TUONG', '',
  '-LAYER', 'S', '0', '', '_.CIRCLE', '522500,5000', '300', '-BLOCK', 'BON_CAU', '522500,5000', 'L', '', '-INSERT', 'BON_CAU', '522500,5000', '1', '', '0',
  '_.TEXT', '526000,5000', '250', '0', 'THANG BO', '_.ZOOM', 'W', '499000,-1000', '531000,11000',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '499500,-500', '530500,10500', '', '', '',
  # 8 (nguoi dung 16/11: d = h dam - day san): vung 12 x 6 m, luoi dam B2 (250x400), san 120 -> d = 0,28 <= 0,3: bo dam, khong chia khoang
  '-LAYER', 'M', 'PCCC_VUNG', '', '_.RECTANG', '600000,0', '612000,6000', '-LAYER', 'M', 'DAM', '',
  '_.LINE', '604000,0', '604000,6000', '', '_.LINE', '608000,0', '608000,6000', '', '_.LINE', '600000,3000', '612000,3000', '',
  '_.TEXT', '604300,4500', '250', '0', 'B2 (250x400)', '-LAYER', 'S', '0', '', '_.ZOOM', 'W', '599000,-1000', '613000,7000',
  'PCCC_RAI', 'BC_KHOI', 'Dam', 'DaySan', '120', '4.5', '', 'W', '599500,-500', '612500,6500', '', '', '604000,1000', '',
  # 9 (nguoi dung 17/11: nhan KOCHAM "DS 1400X250" that la rong 1400 cao 700): vung 12 x 6 m, luoi dam nhan "DS 700X300" (so lon = h: 0,7, sau).
  #    SuaDam: net x = 704 m nhap 300x250 (nong, bo du nhan ghi sau), net x = 707 m nhap 1400x700 (doc dung b x h: d = 0,7, khong phai 1,4) -> 4 khoang
  #    7 x 3 / 5 x 3 m (khong 4 x 3 m de khoi lan voi dem cua loi 4)
  '-LAYER', 'M', 'PCCC_VUNG', '', '_.RECTANG', '700000,0', '712000,6000', '-LAYER', 'M', 'DAM', '',
  '_.LINE', '704000,0', '704000,6000', '', '_.LINE', '707000,0', '707000,6000', '', '_.LINE', '700000,3000', '712000,3000', '',
  '_.TEXT', '704300,4500', '250', '0', 'DS 700X300', '-LAYER', 'S', '0', '', '_.ZOOM', 'W', '699000,-1000', '713000,7000',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '4.5', '', 'W', '699500,-500', '712500,6500', '', '', '704000,1000', 'SuaDam', '704000,1000', '300x250', '707000,1000', '1400x700', '', '',
  # 10 (nguoi dung 17/11, tang 8 KOCHAM: sanh thang may khong ghi chu "SANH"): van phong 10 x 10 m, dai trai 5 m chia 3 phong: duoi 20 m2,
  #    giua 15 m2 (sanh), tren 15 m2 la gieng co block THANG_MAY. Enter moi phong: bo gieng + sanh (sat gieng 0,3 m), rai van phong 100 + phong 20 m2
  '-LAYER', 'M', 'TUONG', '', '_.RECTANG', '800000,0', '815000,10000', '_.LINE', '805000,0', '805000,10000', '',
  '_.LINE', '800000,4000', '805000,4000', '', '_.LINE', '800000,7000', '805000,7000', '', '-LAYER', 'S', '0', '', '_.ZOOM', 'W', '799000,-1000', '816000,11000',
  '_.RECTANG', '800300,7300', '804700,9700', '_.LINE', '800300,7300', '804700,9700', '', '-BLOCK', 'THANG_MAY', '800000,7000', 'W', '800200,7200', '804800,9800', '',
  '-INSERT', 'THANG_MAY', '800000,7000', '1', '', '0', '_.ZOOM', 'W', '799000,-1000', '816000,11000',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '799500,-500', '815500,10500', '', '', '',
  # 11 (nguoi dung 17/11: vung la PHONG, dam chi chia khoang ben trong): phong tuong 12 x 6 m, dam B1 (300x600) doc x = 904, 908 m, Enter moi phong
  #    -> 1 vung 72 m2 tren PCCC_VUNG (truoc sua: 3 vung 24 m2), 3 khoang tren PCCC_KHOANG_M2
  '-LAYER', 'M', 'TUONG', '', '_.RECTANG', '900000,0', '912000,6000', '-LAYER', 'M', 'DAM', '',
  '_.LINE', '904000,0', '904000,6000', '', '_.LINE', '908000,0', '908000,6000', '',
  '_.TEXT', '904300,4500', '250', '0', 'B1 (300x600)', '-LAYER', 'S', '0', '', '_.ZOOM', 'W', '899000,-1000', '913000,7000',
  'PCCC_RAI', 'BC_KHOI', 'Dam', '3.5', '', 'C', '899500,-500', '912500,6500', '', '', '904000,1000', '', '',
  # 12 (nguoi dung 17/11, file KOCHAM: truc ve bang net lien, khong co layer tuong; "ngoai duong truc thi khong phan vung"): phong 19,8 x 9,8 m
  #    layer KT, truc net lien layer 0 (y 0 / 5 / 10 m, x 0 / 10 / 20 m) co block KI_HIEU_TRUC (vong + chu) o dau; phong nho 6 x 1,4 m duoi truc y = 0.
  #    Truoc sua: truc cat phong thanh 6 vung, phong ngoai truc cung rai. Nay: 1 vung 194,04 m2, bo phong ngoai truc
  '-LAYER', 'S', '0', '', '_.ZOOM', 'W', '998000,-2000', '1002000,2000', '_.CIRCLE', '1000000,0', '500', '_.TEXT', 'J', 'MC', '1000000,0', '250', '0', 'A',
  '-BLOCK', 'KI_HIEU_TRUC', '1000000,0', 'W', '999000,-1000', '1001000,1000', '', '_.ZOOM', 'W', '1096000,-4000', '1124000,13000',
  '_.LINE', '1098000,0', '1122000,0', '', '_.LINE', '1098000,5000', '1122000,5000', '', '_.LINE', '1098000,10000', '1122000,10000', '',
  '_.LINE', '1100000,-2000', '1100000,12000', '', '_.LINE', '1110000,-2000', '1110000,12000', '', '_.LINE', '1120000,-2000', '1120000,12000', '',
  '-INSERT', 'KI_HIEU_TRUC', '1097500,0', '1', '', '0', '-INSERT', 'KI_HIEU_TRUC', '1097500,5000', '1', '', '0', '-INSERT', 'KI_HIEU_TRUC', '1097500,10000', '1', '', '0',
  '-INSERT', 'KI_HIEU_TRUC', '1100000,-2500', '1', '', '0', '-INSERT', 'KI_HIEU_TRUC', '1110000,-2500', '1', '', '0', '-INSERT', 'KI_HIEU_TRUC', '1120000,-2500', '1', '', '0',
  '-LAYER', 'M', 'KT', '', '_.RECTANG', '1100100,100', '1119900,9900', '_.RECTANG', '1102000,-2000', '1108000,-600', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1096500,-3500', '1123000,12500', '', '', '',
  # 13 (nguoi dung 17/11: dau bao lap o mat duoi dam thi dat dung tim be rong dam; LoaiTran KhongTran): phong 12,6 x 8,4 m, dam 300 rong (2 mep)
  #    cach 2,1 m -> o 1,8 x 1,8 m < 4 m2 (h 3,5): truoc sua bo loc rong < 1 m bo het o, khong chia khoang; nay 24 khoang, 2 dau tren tim dam y = 4,2 m
  '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1199000,-1000', '1214000,9400', '_.RECTANG', '1200000,0', '1212600,8400', '-LAYER', 'M', 'DAM', '',
  '_.RECTANG', '1202050,0', '1202350,8400', '_.RECTANG', '1204150,0', '1204450,8400', '_.RECTANG', '1206250,0', '1206550,8400',
  '_.RECTANG', '1208350,0', '1208650,8400', '_.RECTANG', '1210450,0', '1210750,8400',
  '_.RECTANG', '1200000,1950', '1212600,2250', '_.RECTANG', '1200000,4050', '1212600,4350', '_.RECTANG', '1200000,6150', '1212600,6450',
  '_.TEXT', '1203000,7000', '150', '0', 'D1 (300x700)', '_.TEXT', '1209000,7000', '150', '0', 'D1 (300x700)', '_.TEXT', '1209000,1000', '150', '0', 'D1 (300x700)',
  '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'KhongTran', 'DaySan', '120', '3.5', '', 'W', '1199500,-500', '1213100,8900', '', '', '1202050,1000', '', '',
  # 14 (20/11, xuong Quyet Thang, LoaiTran MaiKeo): xuong 20 x 48 m, cot 400 x 400 hai tuong doc buoc 6 m; dai mai che 3,4 m phia nam co chu, khong tuong ngan;
  #    phan loi dau hoi 8 x 2 m phia bac; nha van phong 8 x 6 m cach xuong 4 m, hien 2,4 m co 3 cot 300 x 300 o mep ngoai (y = 7,8 m, trong 6 m quanh xuong).
  #    kho 4 x 3 m sat dai mai che (cach 0,1 m, khong chu PHONG).
  #    Truoc sua: cot hien thanh hang vi keo thua, dai loi va dai mai che thanh khoang rieng, hien rai dau bao. Nay: 8 khoang, bo khoang mai che, bo hien, bo kho sat mai che (L5b).
  '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1286000,-8000', '1322000,52000',
  '_.PLINE', '1300000,-3400', '1320000,-3400', '1320000,48000', '1316000,48000', '1316000,50000', '1308000,50000', '1308000,48000', '1300000,48000', 'C',
  '_.RECTANG', '1288000,10000', '1296000,16000', '_.RECTANG', '1288200,10200', '1295800,15800', '_.RECTANG', '1302000,-6400', '1306000,-3500', '_.PLINE', '1288000,10000', '1288000,7600', '1296000,7600', '1296000,10000', '',
  '_.TEXT', '1306000,-2000', '500', '0', 'KHU VUC MAI CHE', '_.TEXT', '1289000,13000', '400', '0', 'PHONG LAM VIEC', '-LAYER', 'M', 'COT', '',
  '_.RECTANG', '1300000,-200', '1300400,200', '_.RECTANG', '1319600,-200', '1320000,200', '_.RECTANG', '1300000,5800', '1300400,6200', '_.RECTANG', '1319600,5800', '1320000,6200', '_.RECTANG', '1300000,11800', '1300400,12200',
  '_.RECTANG', '1319600,11800', '1320000,12200', '_.RECTANG', '1300000,17800', '1300400,18200', '_.RECTANG', '1319600,17800', '1320000,18200', '_.RECTANG', '1300000,23800', '1300400,24200', '_.RECTANG', '1319600,23800', '1320000,24200',
  '_.RECTANG', '1300000,29800', '1300400,30200', '_.RECTANG', '1319600,29800', '1320000,30200', '_.RECTANG', '1300000,35800', '1300400,36200', '_.RECTANG', '1319600,35800', '1320000,36200', '_.RECTANG', '1300000,41800', '1300400,42200',
  '_.RECTANG', '1319600,41800', '1320000,42200', '_.RECTANG', '1300000,47800', '1300400,48200', '_.RECTANG', '1319600,47800', '1320000,48200', '_.RECTANG', '1288050,7650', '1288350,7950', '_.RECTANG', '1291850,7650', '1292150,7950',
  '_.RECTANG', '1295650,7650', '1295950,7950',
  # L7: thang bo trong phong lam viec, block ten vo nghia "XQZ" (9 bac 1 m cach 250, 2 limon), cach tuong phai 0,1 m -> khoet 1,1 x 2 m = 2,2 m2
  '-LAYER', 'S', '0', '', '_.LINE', '1294900,12000', '1294900,14000', '', '_.LINE', '1295900,12000', '1295900,14000', '',
  '_.LINE', '1294900,12000', '1295900,12000', '', '_.LINE', '1294900,12250', '1295900,12250', '', '_.LINE', '1294900,12500', '1295900,12500', '',
  '_.LINE', '1294900,12750', '1295900,12750', '', '_.LINE', '1294900,13000', '1295900,13000', '', '_.LINE', '1294900,13250', '1295900,13250', '',
  '_.LINE', '1294900,13500', '1295900,13500', '', '_.LINE', '1294900,13750', '1295900,13750', '', '_.LINE', '1294900,14000', '1295900,14000', '',
  '-BLOCK', 'XQZ', '1294900,12000', 'W', '1294850,11950', '1295950,14050', '', '-INSERT', 'XQZ', '1294900,12000', '1', '', '0',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '9', 'Khong', 'C', '1286500,-7500', '1321500,51500', '', '', '',
  # Ca 15 (Karaoke ABC 20/11): layer ten tuong chi co canh cua, tuong ve tren layer 0 -> Enter moi phong van dung phong 6,5 x 4 m (truoc sua: 0 vung)
  '-LAYER', 'M', 'tuong', '', '_.RECTANG', '1600500,-99900', '1601400,-99850', '-LAYER', 'S', '0', '', '_.RECTANG', '1600000,-100000', '1606500,-96000',
  '_.ZOOM', 'W', '1599000,-101000', '1607500,-95000', 'PCCC_RAI', 'BC_NHIET', '', 'Khong', 'C', '1599500,-100500', '1607000,-95500', '', '', '',
  '_.QSAVE') -join "`r`n") | Set-Content -Encoding ascii "$T\kb6.scr"
$p6 = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/s `"$T\kb6.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb6.out.txt"
if (-not $p6.WaitForExit(180000)) { Stop-Process -Id $p6.Id -Force; throw "accoreconsole qua 3 phut (sua 6 loi): $T\kb6.out.txt" }
$out6 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb6.out.txt")) -replace "`0", ''
$v6 = DocKq 'vtd6'
Kiem ($out6 -match 'V\S+ng t\S+ d\S+ng m2=275 ' -and $v6 -and $v6.vung_tu_dong[0].m2 -eq 275) `
  "Loi 1: tuong TUONG trong block layer noi that van la tuong: PCCC_RAI bam diem va vung_tu_dong cung ra 275 m2, khong gop loi thang (vtd=$($v6.vung_tu_dong[0].m2))"
Kiem ($out6 -match 'PCCC_ONG ONG_DN50 doan=2 m=8(\r|\n)' -and $out6 -notmatch 'Unknown command "?100000') 'Loi 2: PCCC_ONG tu hoi diem, chay duoc trong .scr: 2 doan 8 m'
Kiem ($out6 -match 'tuyen_loc giu=4 bo=3 nhom=3 ' -and $out6 -match 'PCCC_TUYEN ONG_DN25 doan=\d+ m=') 'Loi 3: PCCC_TUYEN khung W lay 7 dau: bo 2 dau vung tach roi + 1 dau khac loai, noi 4 dau'
Kiem ($out6 -match 'chia_khoang=6 ' -and $out6 -match 'khoang_dam nguong=4 lon=6 nho=0' -and [regex]::Matches($out6, 'PCCC_RAI BC_KHOI n=1 luoi=1x1 buoc=4000x3000 m2=12(\r|\n)').Count -eq 6) `
  'Loi 4: Dam + vung PCCC_VUNG ve san: chia 6 khoang 12 m2, moi khoang 1 dau bao'
$m6 = [regex]::Match($out6, 'kt_gom n=6 kt=(\d+) ')
# 17/11: chuoi chung them moc mep dam (nguoi dung: dau bao trong khoang thi mep dam la tuong, dim tu mep dam): 7 -> 10 dim
Kiem ($m6.Success -and [int]$m6.Groups[1].Value -le 10) "Loi 6: 6 khoang cua mot vung ve san dim chung: 1 chuoi X + 1 chuoi Y, moc mep dam ($($m6.Groups[1].Value) dim; truoc sua 24)"
$m5 = [regex]::Match($out6, 'PCCC_DAYBC he=thuong tin_hieu=2 m=([\d.]+) [^\r\n]* ne_loi=(\d+)')
Kiem ($m5.Success -and [int]$m5.Groups[2].Value -ge 1 -and [double]$m5.Groups[1].Value -gt 25) `
  "Loi 5: PCCC_DAYBC: doan tu -> dai cat loi thang (ngoai vung) di vong qua van phong (m=$($m5.Groups[1].Value), ne_loi=$($m5.Groups[2].Value); duong thang 22 m)"
# Nguoi dung 16/11: day vong khong bam mep vung. Truoc: doan dung o x 404500 (cach mep van phong 0,2 m), 27 m; nay x 404750 (0,45 m), 27,5 m
Kiem ($m5.Success -and [double]$m5.Groups[1].Value -ge 27.4) "Day di vong cach mep vung >= 0,4 m, khong trung net tuong (m=$($m5.Groups[1].Value); bam mep 27)"
Kiem ($out6 -match 'bo_khu_khong_bao=3 m2=50,50,50 ' -and ([regex]::Matches($out6, 'V\S+ng t\S+ d\S+ng m2=150 ')).Count -eq 1) `
  'Enter moi phong: bo phong chu SANH THANG MAY, phong block BON_CAU, phong chu THANG BO (3 x 50 m2), chi rai phong 150 m2'
Kiem ($out6 -match 'dam ky_hieu=1 day_san=120[\s\S]{0,600}dam_sau=0 dam_nong=3 ') 'Dam + DaySan 120: dam 250x400 con d = 0,28 m <= 0,3: bo ca 3 net dam, khong chia khoang'
Kiem ($out6 -match 'sua_dam d=0.25 m doan=1 [\s\S]{0,400}sua_dam d=0.7 m doan=1 [\s\S]{0,400}dam ky_hieu=1 sua_dam=2[\s\S]{0,600}chia_khoang=4 [\s\S]{0,400}dam_sau=2 dam_nong=1 ') `
  'SuaDam: co nhap tay thang nhan (300x250 bo dam nhan 700X300), 1400x700 doc dung b x h (d 0,7): 2 dam sau, 1 nong, 4 khoang'
$c10 = [regex]::Match($out6, 'bo_sanh_thang=1 m2=15 [\s\S]{0,3000}?PCCC_RAI BC_KHOI n=')
$c10b = $out6.Substring([Math]::Max(0, $out6.IndexOf('bo_sanh_thang=')))
Kiem ($c10.Success -and $c10b -match '^[\s\S]{0,3000}?m2=100 ' -and $c10b -match '^[\s\S]{0,3000}?m2=20 ' -and $c10b -notmatch '^[\s\S]{0,1500}?V\S+ng t\S+ d\S+ng m2=15 ') `
  'Sanh thang may khong ghi chu: phong 15 m2 sat block THANG_MAY bo (bo_sanh_thang), gieng bo, rai van phong 100 m2 + phong 20 m2'
Kiem ($out6 -match 'chia_khoang=3 \(ph\S+ng t\S+ d\S+ng[\s\S]{0,600}V\S+ng t\S+ d\S+ng m2=72 ' -and ([regex]::Matches($out6, 'V\S+ng t\S+ d\S+ng m2=24 ')).Count -eq 0) `
  'Dam + tu dung phong: vung la phong 72 m2 (mot vung), dam sau chia 3 khoang 24 m2 ve tren PCCC_KHOANG_M2'
$c12 = $out6.Substring([Math]::Max(0, $out6.LastIndexOf('khung_truc')))
if ($c12.IndexOf('[CoTran/KhongTran/HonHop/MaiKeo]') -gt 0) { $c12 = $c12.Substring(0, $c12.IndexOf('[CoTran/KhongTran/HonHop/MaiKeo]')) } # bo phan ca 13
Kiem ($c12 -match '^khung_truc x=1100000\.\.1120000 y=0\.\.10000[\s\S]{0,300}bo_net_truc=\d+[\s\S]{0,600}bo_ngoai_truc=1 ' -and $c12 -match 'V\S+ng t\S+ d\S+ng m2=194\.04 ' `
  -and ([regex]::Matches($c12, 'V\S+ng t\S+ d\S+ng m2=')).Count -eq 1) `
  'Truc net lien co ky hieu truc: khong cat phong (1 vung 194,04 m2), phong ngoai truc ngoai cung khong phan vung (bo_ngoai_truc=1)'
$i14 = $out6.LastIndexOf('[CoTran/KhongTran/HonHop/MaiKeo]')
$i13 = [Math]::Max(0, $out6.LastIndexOf('[CoTran/KhongTran/HonHop/MaiKeo]', [Math]::Max(0, $i14 - 1)))
$c13 = $out6.Substring($i13, [Math]::Max(0, $i14 - $i13)) # ca 13, bo phan ca 14
$c14 = $out6.Substring([Math]::Max(0, $i14))
Kiem ($c13 -match 'tim_dam=8[\s\S]{0,600}chia_khoang=24 [\s\S]{0,400}nho=24 [\s\S]{0,400}khoang_nho_chung n=2 len_dam=2 ') `
  'LoaiTran KhongTran, o 1,8 x 1,8 m < 4 m2: chia 24 khoang (o hep < 1 m van la khoang), 2 dau bao dat tren tim dam (mat duoi dam, 5.9.1.1.6 c)'
Kiem ($c14 -match 'bo_hien=1 ' -and $c14 -match 'mai_keo bo_khoang_khu_ten=1 ' -and $c14 -match 'mai_keo bo_ke_mai_che=1 m2=11.6' -and $c14 -match 'mai_keo cot=21 phong_chia=1 khoang=8 ') `
  'LoaiTran MaiKeo: 8 khoang theo hang cot (cot hien nha ben khong thanh hang, phan loi dau hoi gop khoang dau), khoang mai che co chu bo, kho sat mai che bo, hien co cot bo'
Kiem ($c14 -match 'khoet_thang=1 m2=1.8 ' -and $c14 -match 'V\S+ng t\S+ d\S+ng m2=40.76 ') `
  'L7: thang bo block ten vo nghia (hinh 9 bac) trong phong lam viec 7,6 x 5,6 m (tuong 2 net): khoet hop 0,9 x 2 m (nan sat mat tuong 0,1 m), vung con 40,76 m2'
Kiem ($c14 -match 'V\S+ng t\S+ d\S+ng m2=26 [\s\S]{0,300}PCCC_RAI BC_NHIET n=1 ') `
  'Ca 15: layer "tuong" chi co canh cua (mat vun), tuong tren layer 0: Enter moi phong lui ve moi net lien, ra phong 26 m2 + 1 dau bao nhiet'

# Review 20/11 (moc L1-L8, cad-review Opus max: 4 CHAN). Ban ve rieng kb7, moi ca mot dau moc (princ "==Ex==") de cat dong lenh theo ca.
function NhaE2($x) { # van phong 20 x 8 m tuong 1 net, hanh lang 2 m phia nam, 3 phong 7 / 7 / 6 x 6 m, cot 300 x 300 tren tuong nam + bac buoc 5 m
  $r = @('-LAYER', 'M', 'TUONG', '', '_.RECTANG', "$x,0", "$($x + 20000),8000", '_.LINE', "$x,2000", "$($x + 20000),2000", '',
    '_.LINE', "$($x + 7000),2000", "$($x + 7000),8000", '', '_.LINE', "$($x + 14000),2000", "$($x + 14000),8000", '', '-LAYER', 'M', 'COT', '')
  foreach ($c in 0..4) { $cx = $x + 5000 * $c; $r += @('_.RECTANG', "$($cx - 150),-150", "$($cx + 150),150", '_.RECTANG', "$($cx - 150),7850", "$($cx + 150),8150") }
  $r + @('_.ZOOM', 'W', "$($x - 1000),-1000", "$($x + 21000),9000", '-LAYER', 'S', '0', '')
}
function Xuong($x, $y0, $y1) { # xuong 20 m tuong 1 net y0..y1, cot 400 x 400 sat trong hai tuong doc, tam y = 0, 6, 12 ... <= y1 m
  $r = @('-LAYER', 'M', 'TUONG', '', '_.RECTANG', "$x,$y0", "$($x + 20000),$y1", '-LAYER', 'M', 'COT', '')
  for ($y = 0; $y -le $y1; $y += 6000) { $r += @('_.RECTANG', "$x,$($y - 200)", "$($x + 400),$($y + 200)", '_.RECTANG', "$($x + 19600),$($y - 200)", "$($x + 20000),$($y + 200)") }
  $r + @('-LAYER', 'S', '0', '')
}
$e3 = @('-LAYER', 'M', 'TUONG', '', '_.RECTANG', '200000,0', '224000,48000', '-LAYER', 'M', 'COT', '')
foreach ($y in (0..8 | ForEach-Object { 6000 * $_ })) {
  $e3 += @('_.RECTANG', "200000,$($y - 200)", "200400,$($y + 200)", '_.RECTANG', "219800,$($y - 200)", "220200,$($y + 200)", '_.RECTANG', "223600,$($y - 200)", "224000,$($y + 200)")
}
(@('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\rv.dwg`"", 'OSMODE', '0') + @(
  # E5 (CHAN 4): hai phong 2,8 x 3 m chung tuong 1 net, MaiKeo h 9. Truoc sua: gom nhu khoang dam nho, 1 dau bao tren tuong chung; nay moi phong 1 dau
  '(princ "\n==E5==")', '-LAYER', 'M', 'TUONG', '', '_.RECTANG', '0,0', '5600,3000', '_.LINE', '2800,0', '2800,3000', '', '_.ZOOM', 'W', '-1000,-1000', '6600,4000',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '9', 'Khong', 'C', '-500,-500', '6100,3500', '', '', '',
  # E2 (CHAN 2): van phong tuong 1 net co cot tren tuong ngoai, MaiKeo h 3,5. Truoc sua: bo_hien=2 (phong 6 x 6 + hanh lang); nay hien_nghi, du 4 phong
  '(princ "\n==E2M==")') + (NhaE2 100000) + @('PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '3.5', 'Khong', 'C', '99500,-500', '120500,8500', '', '', '',
  # E2 CoTran (L1 giu dung): cot giua tuong 1 net khong bac cau thanh tuong ao: hanh lang 40 m2, phong 42 m2
  '(princ "\n==E2C==")') + (NhaE2 500000) + @('PCCC_RAI', 'BC_KHOI', '', '', 'C', '499500,-500', '520500,8500', '', '', '',
  # E3 (CHAN 3): xuong 24 x 48 m, cot x = 0 / 20 / 24 m buoc 6 m, dai mai che x 20-24 m khong tuong ngan, chu MAI CHE o khoang giua (y 18-24 m).
  #    Truoc sua: bo_khoang_khu_ten m2=144 (120 m2 la xuong); nay giu khoang, chi bao
  '(princ "\n==E3==")') + $e3 + @('-LAYER', 'S', '0', '', '_.TEXT', '220800,21000', '500', '0', 'MAI CHE', '_.ZOOM', 'W', '199000,-1000', '225000,49000',
  'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '9', 'Khong', 'C', '199500,-500', '224500,48500', '', '', '',
  # E1 (CHAN 1): xuong 24 x 18 m tuong 2 net 200, 3 cot 400 x 400 tuong nam (mat ngoai cot = mat ngoai tuong), cua cuon 5,2 m de trong giua 2 cot.
  #    Truoc sua: khe canh net cot khong khep, vung=0; nay 1 vung > 400 m2
  '(princ "\n==E1==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '299000,-1000', '325000,19000',
  '_.LINE', '300000,0', '306000,0', '', '_.LINE', '312000,0', '318000,0', '', '_.LINE', '318400,0', '324000,0', '',
  '_.LINE', '300200,200', '306000,200', '', '_.LINE', '312000,200', '318000,200', '', '_.LINE', '318400,200', '323800,200', '',
  '_.PLINE', '324000,0', '324000,18000', '300000,18000', '300000,0', '', '_.PLINE', '323800,200', '323800,17800', '300200,17800', '300200,200', '',
  '-LAYER', 'M', 'COT', '', '_.RECTANG', '306000,0', '306400,400', '_.RECTANG', '311600,0', '312000,400', '_.RECTANG', '318000,0', '318400,400',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '299500,-500', '324500,18500', '', '', '',
  # E8 (CHAN 1): tuong nam la 5 tru polyline kin 1000 x 220 (de hatch), cua so 1,5 m de trong; 3 tuong con lai polyline ho 2 net.
  #    Truoc sua: tru thanh cot, khe 1,5 m khong khep, vung=0; nay 1 vung
  '(princ "\n==E8==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '399000,-1000', '412000,7000',
  '_.RECTANG', '400000,0', '401000,220', '_.RECTANG', '402500,0', '403500,220', '_.RECTANG', '405000,0', '406000,220', '_.RECTANG', '407500,0', '408500,220',
  '_.RECTANG', '410000,0', '411000,220', '_.PLINE', '411000,0', '411000,6000', '400000,6000', '400000,0', '',
  '_.PLINE', '410780,220', '410780,5780', '400220,5780', '400220,220', '', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '399500,-500', '411500,6500', '', '', '',
  # Review 21/11 (NEN SUA). E4 (L8): mat bang 2 phong 10 x 15 m + hinh chi tiet cach 20 m: phong 4 x 3 m chu PHONG KY THUAT. Truoc sua: 3 vung; nay bo cum chi tiet
  '(princ "\n==E4==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '599000,-1000', '645000,16000', '_.RECTANG', '600000,0', '620000,15000', '_.LINE', '610000,0', '610000,15000', '',
  '_.RECTANG', '640000,0', '644000,3000', '-LAYER', 'S', '0', '', '_.TEXT', '640300,1300', '200', '0', 'PHONG KY THUAT',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '599500,-500', '644500,15500', '', '', '',
  # E9 (DuongViKeo): xuong 20 x 30 m cot 2 tuong doc buoc 6 m; cach 4 m phong 15 x 15 m chu PHONG HOP (dong) va phong 15 x 15 m khong chu (tay), khong cot.
  #    Truoc sua: muon hang cot xuong, chia ca 2 phong (phong_chia=3); nay chi chia xuong
  '(princ "\n==E9==")') + (Xuong 700000 0 30000) + @('-LAYER', 'M', 'TUONG', '', '_.RECTANG', '724000,0', '739000,15000', '_.RECTANG', '681000,0', '696000,15000',
  '-LAYER', 'S', '0', '', '_.TEXT', '725000,7000', '400', '0', 'PHONG HOP', '_.ZOOM', 'W', '680000,-1000', '740000,31000',
  'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '9', 'Khong', 'C', '679500,-500', '739500,30500', '', '', '',
  # E9B (luot muon dung): xuong co cot + xuong ke ben chung tuong khong ve cot: xuong ke ben muon hang cot, 2 phong chia, 10 khoang
  '(princ "\n==E9B==")') + (Xuong 800000 0 30000) + @('-LAYER', 'M', 'TUONG', '', '_.RECTANG', '820000,0', '840000,30000', '-LAYER', 'S', '0', '',
  '_.ZOOM', 'W', '799000,-1000', '841000,31000', 'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '9', 'Khong', 'C', '799500,-500', '840500,30500', '', '', '',
  # E10 (L5 phong bom): nha 20 x 10 m chia 16 / 4 m, chu PHONG BOM trong phong 40 m2 (trong nha: van rai); nha bom 4 x 3 m dung rieng cach 6 m (bo)
  '(princ "\n==E10==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '899000,-1000', '931000,11000', '_.RECTANG', '900000,0', '920000,10000', '_.LINE', '916000,0', '916000,10000', '',
  '_.RECTANG', '926000,0', '930000,3000', '-LAYER', 'S', '0', '', '_.TEXT', '916500,5000', '200', '0', 'PHONG BOM', '_.TEXT', '926300,1300', '200', '0', 'PHONG BOM',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '899500,-500', '930500,10500', '', '', '',
  # E11 (L5b): xuong 20 x 51,4 m dai mai che 3,4 m phia nam co chu; KHO 1 6 x 5,9 m cach dai 0,1 m, KHO 2 chi sat KHO 1. Truoc sua: bo_ke_mai_che=2; nay giu 2 kho
  '(princ "\n==E11==")') + (Xuong 1000000 (-3400) 48000) + @('-LAYER', 'M', 'TUONG', '', '_.RECTANG', '1002000,-9400', '1008000,-3500', '_.RECTANG', '1002000,-15300', '1008000,-9400',
  '-LAYER', 'S', '0', '', '_.TEXT', '1006000,-2000', '500', '0', 'KHU VUC MAI CHE', '_.TEXT', '1003000,-7000', '400', '0', 'KHO 1', '_.TEXT', '1003000,-13000', '400', '0', 'KHO 2',
  '_.ZOOM', 'W', '999000,-16000', '1021000,49000', 'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '9', 'Khong', 'C', '999500,-15800', '1020500,48500', '', '', '',
  # E7 (L7): 3 phong tuong 1 net, block THANG_BO: 10 x 7 m thang 2 x 3 m sat goc (khoet), 8 x 8 m thang 6 x 6 m (buong thang, bo), 12 x 8 m thang giua phong (khong khoet)
  '(princ "\n==E7==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1099000,-1000', '1135000,9000',
  '_.RECTANG', '1100000,0', '1110000,7000', '_.RECTANG', '1112000,0', '1120000,8000', '_.RECTANG', '1122000,0', '1134000,8000', '-LAYER', 'S', '0', '',
  '_.RECTANG', '1100100,100', '1102100,3100', '-BLOCK', 'THANG_BO1', '1100100,100', 'W', '1100050,50', '1102150,3150', '', '-INSERT', 'THANG_BO1', '1100100,100', '1', '', '0',
  '_.RECTANG', '1113000,1000', '1119000,7000', '-BLOCK', 'THANG_BO2', '1113000,1000', 'W', '1112950,950', '1119050,7050', '', '-INSERT', 'THANG_BO2', '1113000,1000', '1', '', '0',
  '_.RECTANG', '1127000,2500', '1129000,5500', '-BLOCK', 'THANG_BO3', '1127000,2500', 'W', '1126950,2450', '1129050,5550', '', '-INSERT', 'THANG_BO3', '1127000,2500', '1', '', '0',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1099500,-500', '1134500,8500', '', '', '',
  # E12 (Karaoke ABC 21/11): hanh lang 20 x 2 m giua 2 phong bac + 2 phong nam, tuong ngan 200 hai ben thang hang (mat tuong cung x). Bac: nap dau
  #    tuong sat mat hanh lang (chu T tai dau khe). Nam: tuong 2 net, mat hanh lang y 4000 lien, mat trong phong y 3800 (khe 2,2 m cat ngang mat hanh lang).
  #    Truoc sua: khe ngang hanh lang khep nhu o cua, hanh lang vo 2 vung 19,8 m2; nay 1 vung 40 m2
  '(princ "\n==E12==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1199000,-1000', '1221000,11000',
  '_.RECTANG', '1200000,0', '1209900,3800', '_.RECTANG', '1210100,0', '1220000,3800', '_.RECTANG', '1200000,6000', '1209900,10000', '_.RECTANG', '1210100,6000', '1220000,10000',
  '_.LINE', '1200000,4000', '1220000,4000', '', '_.LINE', '1209900,6000', '1210100,6000', '', '_.LINE', '1200000,4000', '1200000,6000', '', '_.LINE', '1220000,4000', '1220000,6000', '',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1199500,-500', '1220500,10500', '', '', '',
  # E13: nhu E12 phia nam; phia bac 1 phong 20 x 3,8 m, tuong 200 co cua 0,9 m (canh mo vao phong ve bang cung ELIP ti le 0,9 nhu block cua Karaoke)
  #    ma cua x 9900 thang hang mat tuong ngan nam; tru loi 200 x 100 tren mat hanh lang bac x 15000-15200 thang hang tuong ngan nam thu hai.
  #    Truoc sua: khe tu nap ma cua / mat tru loi toi goc phong nam khep, hanh lang vo 3 vung; nay 1 vung ~40 m2
  '(princ "\n==E13==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1299000,-1000', '1321000,11000',
  '_.RECTANG', '1300000,0', '1309900,3800', '_.RECTANG', '1310100,0', '1315000,3800', '_.RECTANG', '1315200,0', '1320000,3800',
  '_.PLINE', '1315000,6000', '1315000,5900', '1315200,5900', '1315200,6000', '',
  '_.PLINE', '1309900,6200', '1300000,6200', '1300000,10000', '1320000,10000', '1320000,6200', '1310800,6200', '',
  '_.LINE', '1300000,4000', '1320000,4000', '', '_.LINE', '1300000,6000', '1309900,6000', '1309900,6200', '', '_.LINE', '1310800,6200', '1310800,6000', '1320000,6000', '',
  '_.LINE', '1300000,4000', '1300000,6000', '', '_.LINE', '1320000,4000', '1320000,6000', '',
  '_.ELLIPSE', 'A', 'C', '1309900,6200', '1310800,6200', '810', '0', '90', '_.LINE', '1309900,6200', '1309900,7100', '',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1299500,-500', '1320500,10500', '', '', '',
  # E14 (Karaoke ABC 21/11, nguoi dung khoanh do): loi 1,5 x 8 m giua 2 phong 4 x 8 m tuong 1 net; 2 block cua (cung + canh) tren layer TUONG o hai ben loi,
  #    canh mo vao phong, canh hai cua thang hang y 1000. Truoc sua: khe 1,5 m giua 2 canh cua khep ngang loi, loi mat dai 1 m cuoi; nay loi du 12 m2
  '(princ "\n==E14==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1399000,-1000', '1410500,9000',
  '_.ARC', 'C', '1404000,1000', '1404000,1900', '1403100,1000', '_.LINE', '1404000,1000', '1403100,1000', '',
  '-BLOCK', 'CUA_X', '1404000,1000', 'W', '1403050,950', '1404050,1950', '',
  '-INSERT', 'CUA_X', '1404000,1000', '1', '', '0', '-INSERT', 'CUA_X', '1405500,1000', '-1', '1', '0',
  '_.PLINE', '1404000,1000', '1404000,0', '1400000,0', '1400000,8000', '1404000,8000', '1404000,1900', '',
  '_.PLINE', '1405500,1000', '1405500,0', '1409500,0', '1409500,8000', '1405500,8000', '1405500,1900', '',
  '_.LINE', '1404000,0', '1405500,0', '', '_.LINE', '1404000,8000', '1405500,8000', '',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1399500,-500', '1410000,8500', '', '', '',
  # E15 (can ho 441, 21/11): phong 12 x 8 m, 2 tuong cut thang hang y 4000 (khe 6 m, khong cua); block xref gom moi cua cua tang (layer CUA, hop 12,2 x 8,3 m:
  #    cung canh + 2 diem goc) phu ca phong. Truoc sua: khe 6 m "co cua trong khe", khep doi phong 48 + 48 m2; nay 1 vung 96 m2
  '(princ "\n==E15==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1499000,-1000', '1533000,9000',
  '_.PLINE', '1500000,0', '1512000,0', '1512000,8000', '1500000,8000', 'C', '_.LINE', '1500000,4000', '1503000,4000', '', '_.LINE', '1509000,4000', '1512000,4000', '',
  '-LAYER', 'M', 'CUA', '', '_.ARC', 'C', '1521000,0', '1521900,0', '1521000,900', '_.POINT', '1519900,-100', '_.POINT', '1532100,8100',
  '-BLOCK', 'CUA_TANG', '1520000,0', 'W', '1519800,-200', '1532200,8200', '', '-INSERT', 'CUA_TANG', '1500000,0', '1', '', '0',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1499500,-500', '1512500,8500', '', '', '',
  # E16 (nguoi dung 21/11, can ho 441: sanh thang may chi bo khi la buong dem / trong buong thang): nhu ca 10 (dai trai 5 m: duoi 20 m2, sanh 15 m2,
  #    gieng THANG_MAY 15 m2) nhung sanh co 3 cung canh cua quanh bien = hanh lang chung: van rai sanh 15 m2 (ca 10 khong cua van bo).
  #    Moi cung quet 90 do (ARC C tam, dau, cuoi di nguoc chieu kim dong ho; cung 270 do khong phai canh cua -> chi 2 cua = buong dem, bi bo)
  '(princ "\n==E16==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1599000,-1000', '1616000,11000',
  '_.RECTANG', '1600000,0', '1615000,10000', '_.LINE', '1605000,0', '1605000,10000', '', '_.LINE', '1600000,4000', '1605000,4000', '', '_.LINE', '1600000,7000', '1605000,7000', '',
  '_.ARC', 'C', '1605000,5000', '1605900,5000', '1605000,5900', '_.ARC', 'C', '1601000,4000', '1601000,3100', '1601900,4000', '_.ARC', 'C', '1600000,5500', '1600000,6400', '1599100,5500',
  '-LAYER', 'S', '0', '', '_.RECTANG', '1600300,7300', '1604700,9700', '_.LINE', '1600300,7300', '1604700,9700', '', '-BLOCK', 'THANG_MAY2', '1600000,7000', 'W', '1600200,7200', '1604800,9800', '',
  '-INSERT', 'THANG_MAY2', '1600000,7000', '1', '', '0', '_.ZOOM', 'W', '1599000,-1000', '1616000,11000',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1599500,-500', '1615500,10500', '', '', '',
  # E17 (Karaoke 123 khung 4, 21/11): phong 5,8 x 4,8 m tuong 2 net 200; hatch gach cheo tuong DA NO thanh net 45 do cach 90 mm (x - y = k.127,28)
  #    trong ca 4 dai tuong, layer "TUONG HOAN THIEN" (la layer tuong). Truoc sua: mau cung duong gach o 2 tuong ke nhau khep ngang goc phong,
  #    vung canh cheo; nay 1 vung 27,84 m2
  '(princ "\n==E17==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1699000,-1000', '1707200,6200',
  '_.RECTANG', '1700000,0', '1706200,5200', '_.RECTANG', '1700200,200', '1706000,5000',
  '(defun hn (x0 y0 x1 y1 / c xa xb) (setq c (* 127.28 (1- (fix (/ (- x0 y1) 127.28))))) (while (< c (- x1 y0)) (setq xa (max x0 (+ y0 c)) xb (min x1 (+ y1 c))) (if (< (+ xa 1) xb) (entmake (list (cons 0 "LINE") (cons 8 "TUONG HOAN THIEN") (list 10 xa (- xa c) 0.0) (list 11 xb (- xb c) 0.0)))) (setq c (+ c 127.28))))',
  '(hn 1700000 0 1706200 200)', '(hn 1700000 5000 1706200 5200)', '(hn 1700000 200 1700200 5000)', '(hn 1706000 200 1706200 5000)',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1699500,-500', '1706700,5700', '', '', '',
  # E18 (Karaoke 123 khung 4, 21/11, mau co dau bao): o giao hanh lang 1,8 m voi loi doc 2 m = 2,0 x 1,8 m (khep boi tuong ngan + mau tuong treo).
  #    Truoc sua: PhongThat bo (2a/p = 0,95 < 1 nhu dai tuong); sau E18 co vung 3,6 m2; tu E22 (nguoi dung 21/11 toi) o giao gop vao hanh lang: 18 m2
  '(princ "\n==E18==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1796000,0', '1811000,8500',
  '_.RECTANG', '1797000,600', '1810000,7800', '_.LINE', '1800000,600', '1800000,7800', '', '_.LINE', '1797000,6000', '1800000,6000', '',
  '_.LINE', '1802000,600', '1802000,6000', '', '_.LINE', '1802000,6000', '1810000,6000', '', '_.LINE', '1802000,7800', '1802000,7600', '',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1796500,100', '1810500,8300', '', '', '',
  # E19 (Karaoke 123 tang 2, 21/11, mau co dau bao): hanh lang 12 x 1,8 m; giua phia duoi la o thang ho (khong tuong day, khe 6,5 m thong ra ngoai),
  #    mep san / lan can 2 net (layer THAY_1, cach 60 mm) noi 2 tuong. Truoc sua: luot chi-tuong bo net THAY, hanh lang khong kin; nay vung 21,6 m2
  '(princ "\n==E19==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1899000,-7000', '1913000,3000',
  '_.LINE', '1900000,-6000', '1900000,1800', '1912000,1800', '1912000,-6000', '', '_.LINE', '1900000,-6000', '1902750,-6000', '', '_.LINE', '1909250,-6000', '1912000,-6000', '',
  '_.LINE', '1902750,-6000', '1902750,100', '', '_.LINE', '1909250,-6000', '1909250,100', '', '_.LINE', '1900000,0', '1902750,0', '', '_.LINE', '1909250,0', '1912000,0', '',
  '-LAYER', 'M', 'THAY_1', '', '_.LINE', '1902750,0', '1909250,0', '', '_.LINE', '1902750,60', '1909250,60', '',
  '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1899500,-6500', '1912500,2300', '', '', '',
  # E20 (nguoi dung 21/11: sanh thang may khong rai; sanh, sanh tiep tan thi rai): nhu ca 10 (sanh 15 m2 khong cua sat gieng THANG_MAY = buong dem, bi bo)
  #    nhung trong sanh co chu "SANH TIEP TAN": van rai 15 m2
  '(princ "\n==E20==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '1999000,-1000', '2016000,11000',
  '_.RECTANG', '2000000,0', '2015000,10000', '_.LINE', '2005000,0', '2005000,10000', '', '_.LINE', '2000000,4000', '2005000,4000', '', '_.LINE', '2000000,7000', '2005000,7000', '',
  '-LAYER', 'S', '0', '', '_.TEXT', '2000500,5300', '300', '0', 'SANH TIEP TAN',
  '_.RECTANG', '2000300,7300', '2004700,9700', '_.LINE', '2000300,7300', '2004700,9700', '', '-BLOCK', 'THANG_MAY3', '2000000,7000', 'W', '2000200,7200', '2004800,9800', '',
  '-INSERT', 'THANG_MAY3', '2000000,7000', '1', '', '0', '_.ZOOM', 'W', '1999000,-1000', '2016000,11000',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '1999500,-500', '2015500,10500', '', '', '',
  # E21 (truong hoc khung 6, 21/11): phong 10 x 8 m co chu "PHONG BO MON" (bo dau, bo cach = PHONGBOMON chua PHONGBOM): truoc sua bo nhu phong bom
  '(princ "\n==E21==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2099000,-1000', '2111000,9000',
  '_.RECTANG', '2100000,0', '2110000,8000', '-LAYER', 'S', '0', '', '_.TEXT', '2101000,4000', '300', '0', 'PHONG BO MON',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2099500,-500', '2110500,8500', '', '', '',
  # E22 (nguoi dung 21/11 Karaoke 123 tang 2: vung phong bao luon sanh nho truoc cua WC, chi chua buong xi): phong 4,8 x 5 m tuong 1 net; cot WC 1,2 m ben phai:
  #    kho duoi (0-1,5 m, canh trai lan tuong that), sanh giua (1,5-3 m, canh trai mo tron = cau), buong xi tren (co bon cau polyline kin 0,4 x 0,6 m).
  #    Truoc sua: phong 24 m2, sanh 1,8 m2 bi PhongThat bo; nay gop sanh: 25,8 m2, kho + buong xi khong gop
  '(princ "\n==E22==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2199000,-1000', '2207000,6000', '_.RECTANG', '2200000,0', '2206000,5000',
  '_.LINE', '2204800,0', '2204800,1400', '', '_.LINE', '2204800,3100', '2204800,3800', '', '_.LINE', '2204800,4500', '2204800,5000', '',
  '_.LINE', '2204800,1500', '2206000,1500', '', '_.LINE', '2204800,3000', '2206000,3000', '', '-LAYER', 'S', '0', '', '_.RECTANG', '2205200,3900', '2205600,4500',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2199500,-500', '2206500,5500', '', '', '',
  # E22B: nhu E22 nhung trong sanh co hinh kin co bon cau (lavabo / bon cau no roi): khong gop, phong 24 m2
  '(princ "\n==E22B==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2209000,-1000', '2217000,6000', '_.RECTANG', '2210000,0', '2216000,5000',
  '_.LINE', '2214800,0', '2214800,1400', '', '_.LINE', '2214800,3100', '2214800,3800', '', '_.LINE', '2214800,4500', '2214800,5000', '',
  '_.LINE', '2214800,1500', '2216000,1500', '', '_.LINE', '2214800,3000', '2216000,3000', '', '-LAYER', 'S', '0', '', '_.RECTANG', '2215200,3900', '2215600,4500',
  '_.RECTANG', '2215200,2000', '2215600,2600',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2209500,-500', '2216500,5500', '', '', '',
  # E23 (Karaoke 123 tang 2): hanh lang 14 x 1,8 m tuong 2 net, cot 200 x 200 trong dai tuong buoc 3,5 m; phia duoi tu cot giua la lan can 2 net (THAY_1)
  #    Khi plugin chi nhan cot mot phia (chay rieng sau E22: cot=3) cau giua 2 mat cot cat hanh lang thanh 12,6 + 6,3 + 6,65 m2, luat gop hanh lang gop lai 26,6 m2;
#    trong kb7 nhan du 6 cot nen khong co cau (27,16 m2). shortcut: chua tai hien on dinh cau cat hanh lang; luat gop hanh lang da kiem tren ban ve that Karaoke 123 tang 2.
  '(princ "\n==E23==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2299000,-2000', '2316000,4000',
  '_.RECTANG', '2300000,1800', '2303500,2000', '_.RECTANG', '2303700,1800', '2307000,2000', '_.RECTANG', '2307200,1800', '2310500,2000', '_.RECTANG', '2310700,1800', '2314000,2000',
  '_.RECTANG', '2300000,-200', '2303500,0', '_.RECTANG', '2303700,-200', '2307000,0', '_.LINE', '2300000,0', '2300000,1800', '', '_.LINE', '2314000,-200', '2314000,1800', '',
  '-LAYER', 'M', 'COT', '', '_.RECTANG', '2303500,1800', '2303700,2000', '_.RECTANG', '2303500,-200', '2303700,0', '_.RECTANG', '2307000,1800', '2307200,2000',
  '_.RECTANG', '2307000,-200', '2307200,0', '_.RECTANG', '2310500,1800', '2310700,2000', '_.RECTANG', '2310500,-200', '2310700,0',
  '-LAYER', 'M', 'THAY_1', '', '_.LINE', '2307200,-140', '2310500,-140', '', '_.LINE', '2307200,-200', '2310500,-200', '', '_.LINE', '2310700,-140', '2314000,-140', '',
  '_.LINE', '2310700,-200', '2314000,-200', '', '-LAYER', 'S', '0', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2299500,-500', '2314500,2500', '', '', '',
  # E24 (nguoi dung 23/11 Karaoke 123 tang 2): sanh thong tang 10 x 8 m tuong 1 net, ho 3,5 m o canh duoi (khong khep: khong thanh mat kin),
  #    dau X tren layer TRUC (bi bo nhu truc); thang ho goc tren phai 2 x 1,2 m (bac + vien layer 0). Truoc sua: khong vung nao.
  #    Nay: vung rieng tu khung X, khac theo mep thang 80 - 2,4 = 77,6 m2. Gieng 2,5 x 2,5 m kin co dau X layer tuong: van khong rai (o_x=1;
#    dau X co gieng tren layer bo / layer thuong khong nhan: truong hoc khung 6 co 40 block ong thoat nuoc co dau X).
  '(princ "\n==E24==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2399000,-1000', '2416000,9000',
  '_.PLINE', '2405000,0', '2400000,0', '2400000,8000', '2410000,8000', '2410000,0', '2408500,0', '', '_.RECTANG', '2412000,0', '2414500,2500',
  '_.LINE', '2412000,0', '2414500,2500', '', '_.LINE', '2412000,2500', '2414500,0', '',
  '-LAYER', 'M', 'TRUC', '', '_.LINE', '2400000,0', '2410000,8000', '', '_.LINE', '2400000,8000', '2410000,0', '', '-LAYER', 'S', '0', '',
  '_.LINE', '2408000,6800', '2408000,8000', '',
  '_.LINE', '2408000,7700', '2410000,7700', '', '_.LINE', '2408000,7400', '2410000,7400', '', '_.LINE', '2408000,7100', '2410000,7100', '',
  '_.LINE', '2408000,6800', '2410000,6800', '', 'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2399500,-500', '2415000,8500', '', '', '',
  # E25 (nguoi dung 23/11): hanh lang 20 x 1,8 m, cot 300 x 300 tren hai tuong tai x 5,4 / 10 / 14,6 m. 1 hang 2 dau bao (giua be rong y 0,9 m):
  #    chuoi doc hanh lang lech khoi hang 1/4 be rong (chieu 23/11; truoc di tren hang y 900), chuoi ngang (tuong - dau bao - tuong) 1 lan, lech sang phia khong cat cot (x 4,5 m, khong phai 5,5 m)
  '(princ "\n==E25==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2419000,-1000', '2441000,3000', '_.RECTANG', '2420000,0', '2440000,1800',
  '-LAYER', 'M', 'COT', '', '_.RECTANG', '2425250,-150', '2425550,150', '_.RECTANG', '2425250,1650', '2425550,1950', '_.RECTANG', '2429850,-150', '2430150,150',
  '_.RECTANG', '2429850,1650', '2430150,1950', '_.RECTANG', '2434450,-150', '2434750,150', '_.RECTANG', '2434450,1650', '2434750,1950', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2419500,-500', '2440500,2500', '', '', '',
  '(setq ss (ssget "_X" (list (cons 0 "DIMENSION"))) i 0)(while (< i (sslength ss)) (setq d (entget (ssname ss i)) p (cdr (assoc 10 d)) a (cdr (assoc 13 d)) b (cdr (assoc 14 d))) (if (< 2419000 (car a) 2441000) (princ (strcat "\nKTD " (rtos (car p) 2 0) "," (rtos (cadr p) 2 0) " " (rtos (car a) 2 0) "," (rtos (cadr a) 2 0) " " (rtos (car b) 2 0) "," (rtos (cadr b) 2 0)))) (setq i (1+ i)))(princ)',
  # E26 (nguoi dung 23/11 "rectang con ho 1 khuc"): hanh lang tren y 5,2-7,2 m; phong trai 7 x 5 m, o phai 2,8 x 5 m, vach 2 net x 7,0-7,2 m;
  #    tuong 2 net y 5,0-5,2 m chi tu x 0 toi 7 m, phia phai chi co cot 0,2 x 0,2 m o x 9,8 m: dai 0,2 m giua o va hanh lang chi la 2 cau khep khe.
  #    Truoc sua: dai bi bo, o ho voi hanh lang; nay dai gop vao o (phong ke NHO nhat): o 14,56 m2. Net vach x 7,2 m ho dau o y 5 m (cau y 5 m khong khep
  #    vi dau cot cham net ngoai x 10 m thanh chu T xuyen): CacMat 1d bit dau vach (truoc: nhanh cut bi bo, o 15,6 m2 trum vach).
  '(princ "\n==E26==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2449000,-1000', '2461000,8000', '_.RECTANG', '2450000,0', '2460000,7200',
  '_.PLINE', '2450000,5000', '2457000,5000', '2457000,5200', '2450000,5200', '', '_.LINE', '2457000,0', '2457000,5000', '', '_.LINE', '2457200,0', '2457200,5000', '',
  '-LAYER', 'M', 'COT', '', '_.RECTANG', '2459800,5000', '2460000,5200', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2449500,-500', '2460500,7700', '', '', '',
  # E27 (nguoi dung 23/11 Bot chat "hoi lo, duong line dau tien cua lan can hay tuong chu"): hanh lang 10 x 1,8 m, dau phai 2 cot 0,2 x 0,2 m, giua 2 cot
  #    lan can polyline KIN 60 x 1800 (layer THAY_1) sang sanh 6 x 2,2 m. Truoc sua: polyline kin bi bo -> hanh lang dinh sanh mot vung 31,2 m2;
  #    nay vung dung o net trong lan can x 9,94 m: 17,89 m2; sanh 13,2 m2.
  '(princ "\n==E27==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2469000,-1000', '2487000,3000',
  '_.LINE', '2479800,0', '2470000,0', '2470000,1800', '2479800,1800', '', '_.LINE', '2480000,2000', '2486000,2000', '2486000,-200', '2480000,-200', '',
  '_.RECTANG', '2479800,-200', '2480000,0', '_.RECTANG', '2479800,1800', '2480000,2000',
  '-LAYER', 'M', 'THAY_1', '', '_.PLINE', '2479940,0', '2480000,0', '2480000,1800', '2479940,1800', 'C', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2469500,-700', '2486500,2500', '', '', '',
  # E28 (nguoi dung 23/11 truong hoc khung 6 "hanh lang ben lam gi rai dau bao"): day 3 phong 6 x 6 m, hanh lang 2,3 m mo ra san: mep ngoai
  #    4 cot 0,2 x 0,2 + lan can 2 net (layer THAY_1) giua cac cot, ben kia khong co phong. Hanh lang bo (QCVN 10 1.5.11), 3 phong 36 m2 van rai.
  '(princ "\n==E28==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2499000,-1500', '2520000,9500',
  '_.LINE', '2500000,-200', '2500000,8300', '2518000,8300', '2518000,-200', '', '_.LINE', '2500000,2300', '2518000,2300', '',
  '_.LINE', '2506000,2300', '2506000,8300', '', '_.LINE', '2512000,2300', '2512000,8300', '',
  '-LAYER', 'M', 'COT', '', '_.RECTANG', '2500000,-200', '2500200,0', '_.RECTANG', '2505900,-200', '2506100,0',
  '_.RECTANG', '2511900,-200', '2512100,0', '_.RECTANG', '2517800,-200', '2518000,0',
  '-LAYER', 'M', 'THAY_1', '', '_.LINE', '2500200,-50', '2505900,-50', '', '_.LINE', '2500200,-150', '2505900,-150', '',
  '_.LINE', '2506100,-50', '2511900,-50', '', '_.LINE', '2506100,-150', '2511900,-150', '',
  '_.LINE', '2512100,-50', '2517800,-50', '', '_.LINE', '2512100,-150', '2517800,-150', '', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2499500,-1000', '2518500,9000', '', '', '',
  # E29 (cung ca): hanh lang TRONG nha 18 x 2,3 m (phong ca hai ben) van rai; phong 12 x 6 m chu "VE SINH NAM" 72 m2 (> 60) bo theo chu.
  '(princ "\n==E29==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2539000,-7000', '2560000,9500',
  '_.RECTANG', '2540000,-6000', '2558000,8300', '_.LINE', '2540000,0', '2558000,0', '', '_.LINE', '2540000,2300', '2558000,2300', '',
  '_.LINE', '2546000,2300', '2546000,8300', '', '_.LINE', '2552000,2300', '2552000,8300', '', '_.LINE', '2552000,-6000', '2552000,0', '',
  '-LAYER', 'S', '0', '', '_.TEXT', '2543000,-3000', '300', '0', 'VE SINH NAM',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2539500,-6500', '2558500,8800', '', '', '',
  # E30 (cung ca): thang ve NET ROI (khong block): 8 bac 1,8 m cach 0,3 m (layer 0, sat tuong trai) trong phong 3 x 4 m -> khoet ve thang 3,8 m2.
  '(princ "\n==E30==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2579000,-1000', '2584000,5000',
  '_.RECTANG', '2580000,0', '2583000,4000', '-LAYER', 'S', '0', '',
  '_.LINE', '2580000,500', '2581800,500', '', '_.LINE', '2580000,800', '2581800,800', '', '_.LINE', '2580000,1100', '2581800,1100', '',
  '_.LINE', '2580000,1400', '2581800,1400', '', '_.LINE', '2580000,1700', '2581800,1700', '', '_.LINE', '2580000,2000', '2581800,2000', '',
  '_.LINE', '2580000,2300', '2581800,2300', '', '_.LINE', '2580000,2600', '2581800,2600', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2579500,-500', '2583500,4500', '', '', '',
  # E31 (nguoi dung 25/11 Karaoke 123 quet tang 1 + 2 mot lan): 2 mat bang 24 x 10 m dat canh nhau (tang 2 doi 30 m), 3 phong 3 / 4 / 5 x 10 m giong nhau.
  #    Tang 1: x 12-24 la sanh mo 120 m2. Tang 2: tuong x 18 chia phong 60 m2 + o thong tang 6 x 10 m co dau X (layer TRUC). Ten mat bang: TEXT
  #    "MAT BANG TANG 1", block thuoc tinh DONG2 "MAT BANG ... TANG 2". Ky vong: tang 1 sanh tach 60 (nhu o X tang 2) + 60.
  '(princ "\n==E31==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2599000,-5000', '2656000,11000',
  '_.RECTANG', '2600000,0', '2624000,10000', '_.LINE', '2603000,0', '2603000,10000', '', '_.LINE', '2607000,0', '2607000,10000', '', '_.LINE', '2612000,0', '2612000,10000', '',
  '_.RECTANG', '2630000,0', '2654000,10000', '_.LINE', '2633000,0', '2633000,10000', '', '_.LINE', '2637000,0', '2637000,10000', '', '_.LINE', '2642000,0', '2642000,10000', '',
  '_.LINE', '2648000,0', '2648000,10000', '', '-LAYER', 'M', 'TRUC', '', '_.LINE', '2648000,0', '2654000,10000', '', '_.LINE', '2648000,10000', '2654000,0', '',
  '-LAYER', 'S', '0', '', '_.TEXT', '2605000,-3000', '500', '0', 'MAT BANG TANG 1',
  "(entmake '((0 . `"BLOCK`") (2 . `"TENMB31`") (70 . 2) (10 0.0 0.0 0.0)))(entmake '((0 . `"LINE`") (8 . `"0`") (10 -3000.0 -400.0 0.0) (11 3000.0 -400.0 0.0)))(entmake '((0 . `"ATTDEF`") (8 . `"0`") (10 0.0 0.0 0.0) (40 . 500.0) (1 . `"`") (3 . `"TEN`") (2 . `"DONG2`") (70 . 0)))(entmake '((0 . `"ENDBLK`")))(princ)",
  "(entmake '((0 . `"INSERT`") (2 . `"TENMB31`") (66 . 1) (10 2638000.0 -3000.0 0.0)))(entmake '((0 . `"ATTRIB`") (8 . `"0`") (10 2638000.0 -3000.0 0.0) (40 . 500.0) (1 . `"MAT BANG BAO CHAY DUOI TRAN TANG 2`") (2 . `"DONG2`") (70 . 0)))(entmake '((0 . `"SEQEND`")))(princ)",
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2599000,-5000', '2656000,11000', '', '', '',
  # E32 (nguoi dung 25/11 "dim nhay tum lum", Karaoke 123 tang 1): vung chu L = hanh lang 20 x 1,8 m + canh 2,7 x 5,4 m (dac 35 % hop bao).
  #    Truoc sua: mot luoi theo hop 20 x 7,2 m, 4 dau bao lech hang, dim cat ngang. Nay cat 2 manh: hanh lang 2 dau bao 5000 | 10000 | 5000, canh 1.
  '(princ "\n==E32==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2659000,-1000', '2681000,8200',
  '_.PLINE', '2660000,0', '2680000,0', '2680000,7200', '2677300,7200', '2677300,1800', '2660000,1800', 'C', '-LAYER', 'S', '0', '',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2659500,-500', '2680500,7700', '', '', '',
  '(setq ss (ssget "_X" (list (cons 0 "DIMENSION"))) i 0)(while (< i (sslength ss)) (setq d (entget (ssname ss i)) a (cdr (assoc 13 d)) b (cdr (assoc 14 d))) (if (< 2659000 (car a) 2681000) (princ (strcat "\nKTL " (rtos (car a) 2 0) "," (rtos (cadr a) 2 0) " " (rtos (car b) 2 0) "," (rtos (cadr b) 2 0)))) (setq i (1+ i)))(princ)',
  # E33 (nguoi dung 25/11 "chua bao quanh tuong WC trong phong"): nhu E22 nhung canh cua WC ve polyline kin 40 x 900 mm tren layer TUONG dung tu tuong sanh:
  #    khac vao sanh 1,2 x 1,5 m, 2A/P 0,497 < 0,5 -> truoc sua sanh khong gop (phong 24 m2); nay be rong theo hop bao: gop, 25,8 m2.
  '(princ "\n==E33==")', '-LAYER', 'M', 'TUONG', '', '_.ZOOM', 'W', '2689000,-1000', '2697000,6000', '_.RECTANG', '2690000,0', '2696000,5000',
  '_.LINE', '2694800,0', '2694800,1400', '', '_.LINE', '2694800,3100', '2694800,3800', '', '_.LINE', '2694800,4500', '2694800,5000', '',
  '_.LINE', '2694800,1500', '2696000,1500', '', '_.LINE', '2694800,3000', '2696000,3000', '', '_.PLINE', '2695800,1500', '2695840,1500', '2695840,2400', '2695800,2400', 'C',
  '-LAYER', 'S', '0', '', '_.RECTANG', '2695200,3900', '2695600,4500',
  'PCCC_RAI', 'BC_KHOI', '', '', 'C', '2689500,-500', '2696500,5500', '', '', '',
  '(princ "\n==EHET==")', '_.QSAVE')) -join "`r`n" | Set-Content -Encoding ascii "$T\kb7.scr"
$p7 = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/s `"$T\kb7.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb7.out.txt"
if (-not $p7.WaitForExit(180000)) { Stop-Process -Id $p7.Id -Force; throw "accoreconsole qua 3 phut (review 20/11): $T\kb7.out.txt" }
$out7 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb7.out.txt")) -replace "`0", ''
function CaRv($ten) { $i = $out7.LastIndexOf("==$ten=="); if ($i -lt 0) { return '' }; $j = $out7.IndexOf('==E', $i + 6); if ($j -lt 0) { $j = $out7.Length }; $out7.Substring($i, $j - $i) }
function DtVung($c) { @([regex]::Matches($c, 'V\S+ng t\S+ d\S+ng m2=([\d.]+) ') | ForEach-Object { [double]$_.Groups[1].Value }) }
$e5 = CaRv 'E5'
Kiem ($e5 -match 'nho=0 ' -and ([regex]::Matches($e5, 'PCCC_RAI BC_KHOI n=1 ')).Count -eq 2) `
  'Review CHAN 4: MaiKeo khong gom phong nho nhu khoang dam: 2 phong 2,8 x 3 m chung tuong, moi phong 1 dau bao (truoc: 1 dau tren tuong chung)'
$e2m = CaRv 'E2M'
Kiem ($e2m -match 'hien_nghi=' -and $e2m -notmatch 'bo_hien=' -and (DtVung $e2m).Count -eq 4) `
  "Review CHAN 2: hanh lang / phong hep co cot trong tuong ngoai (tuong 1 net) khong bi coi la hien: hien_nghi, du 4 phong (vung=$((DtVung $e2m) -join ','))"
$e2c = CaRv 'E2C'
Kiem ((DtVung $e2c) -contains 40 -and (DtVung $e2c) -contains 42) "L1 giu dung (CoTran): cot giua tuong 1 net khong bac cau thanh tuong ao: hanh lang 40 m2, phong 42 m2 (vung=$((DtVung $e2c) -join ','))"
$e3c = CaRv 'E3'
Kiem ($e3c -match 'chu_khu_khoang_giua=1 ' -and $e3c -notmatch 'bo_khoang_khu_ten' -and $e3c -match 'mai_keo cot=27 phong_chia=1 khoang=8 ') `
  'Review CHAN 3: chu MAI CHE o khoang giua xuong (mai che doc canh dai, khong tuong ngan): giu khoang, chi bao; van 8 khoang'
$e1 = CaRv 'E1'
Kiem ((DtVung $e1).Count -eq 1 -and (DtVung $e1)[0] -gt 400) "Review CHAN 1: cua cuon 5,2 m de trong giua 2 cot trong tuong 2 net van khep: 1 vung > 400 m2 (vung=$((DtVung $e1) -join ','))"
$e8 = CaRv 'E8'
Kiem ((DtVung $e8).Count -eq 1 -and (DtVung $e8)[0] -gt 50) "Review CHAN 1: tru tuong polyline kin 1000 x 220 khong phai cot, cua so 1,5 m van khep: 1 vung (vung=$((DtVung $e8) -join ','))"
$e4 = CaRv 'E4'
Kiem ($e4 -match 'bo_cum=1 phong=1 ' -and (DtVung $e4).Count -eq 2) "Review NEN SUA 1 (L8): phong chu PHONG trong hinh chi tiet cach mat bang 20 m van bo theo cum, rai 2 phong (vung=$((DtVung $e4) -join ','))"
$e9 = CaRv 'E9'
Kiem ($e9 -match 'mai_keo cot=12 phong_chia=1 khoang=5 ') 'Review NEN SUA 2: phong hop co ten va phong khong ten khong cot canh xuong khong chia theo hang cot muon; chi xuong 5 khoang'
$e9b = CaRv 'E9B'
Kiem ($e9b -match 'mai_keo cot=12 phong_chia=2 khoang=10 ') 'DuongViKeo luot muon: xuong ke ben chung tuong khong ve cot van chia 5 khoang theo hang cot xuong ben'
$e10 = CaRv 'E10'
Kiem ($e10 -match 'phong_bom_trong_nha m2=40 ' -and $e10 -match 'bo_khu_ten=1 ' -and (DtVung $e10) -contains 40 -and (DtVung $e10) -contains 160) `
  "Review NEN SUA 3 (L5): phong bom trong nha van rai (40 m2), nha bom dung rieng bo (vung=$((DtVung $e10) -join ','))"
$e11 = CaRv 'E11'
Kiem ($e11 -match 'mai_keo bo_khoang_khu_ten=1 ' -and $e11 -notmatch 'bo_ke_mai_che' -and @(DtVung $e11 | Where-Object { $_ -eq 35.4 }).Count -eq 2) `
  "Review NEN SUA 4 (L5b): KHO 1, KHO 2 co chu ten sat dai mai che khong bo theo (vung=$((DtVung $e11) -join ','))"
$e7 = CaRv 'E7'
Kiem ($e7 -match 'khoet_thang=1 ' -and $e7 -match 'bo_buong_thang=1 m2=64 ' -and (DtVung $e7) -contains 96) `
  "Review NEN SUA 5 (L7): thang sat goc khoet, thang 6 x 6 m trong phong 8 x 8 m = buong thang bo, thang giua phong 12 x 8 m khong khoet (vung=$((DtVung $e7) -join ','))"
$e12 = CaRv 'E12'
Kiem ((DtVung $e12) -contains 40 -and (DtVung $e12) -notcontains 19.8) `
  "Karaoke ABC 21/11: tuong ngan hai ben hanh lang thang hang (chu T hai dau khe) khong khep khe ngang hanh lang: 1 vung 40 m2 (vung=$((DtVung $e12) -join ','))"
$e13 = CaRv 'E13'
Kiem (@(DtVung $e13 | Where-Object { $_ -gt 39.9 -and $_ -lt 40.1 }).Count -eq 1 -and @(DtVung $e13 | Where-Object { $_ -lt 15 }).Count -eq 0) `
  "Karaoke ABC 21/11: ma cua phong bac (canh elip) va tru loi thang hang tuong ngan nam khong khep khe ngang hanh lang: 1 vung ~40 m2 (vung=$((DtVung $e13) -join ','))"
$e14 = CaRv 'E14'
Kiem ((DtVung $e14) -contains 12 -and (DtVung $e14) -contains 32) `
  "Karaoke ABC 21/11: net trong block cua (canh, khuon) tren layer tuong khong lam tuong: loi 1,5 x 8 m du 12 m2, phong 32 m2 (vung=$((DtVung $e14) -join ','))"
$e15 = CaRv 'E15'
Kiem ((DtVung $e15) -contains 96 -and (DtVung $e15) -notcontains 48) `
  "Can ho 441 21/11: block gom moi cua cua tang (hop rong ca 2 chieu > 3,5 m) khong phai 1 cua: khe 6 m khong khep, 1 vung 96 m2 (vung=$((DtVung $e15) -join ','))"
$e16 = CaRv 'E16'
Kiem ($e16 -notmatch 'bo_sanh_thang' -and (DtVung $e16) -contains 15) `
  "Nguoi dung 21/11: sanh sat gieng thang co 3 cua quanh bien (hanh lang chung, khong phai buong dem) van rai 15 m2 (vung=$((DtVung $e16) -join ','))"
$e17 = CaRv 'E17'
Kiem ((DtVung $e17).Count -eq 1 -and (DtVung $e17) -contains 27.84) `
  "Karaoke 123 21/11: hatch tuong da no (net 45 do ngan trong dai tuong) khong khep ngang goc phong: 1 vung 27,84 m2 (vung=$((DtVung $e17) -join ','))"
$e18 = CaRv 'E18'
Kiem ((DtVung $e18) -contains 18 -and (DtVung $e18) -contains 10.8 -and (DtVung $e18) -notcontains 3.6) `
  "Karaoke 123 21/11: o giao hanh lang 2,0 x 1,8 m khong bi bo nhu dai tuong, gop vao hanh lang 14,4 m2 (nguoi dung 21/11 toi): 18 m2 (vung=$((DtVung $e18) -join ','))"
$e19 = CaRv 'E19'
Kiem ((DtVung $e19) -contains 21.6 -and (DtVung $e19) -contains 16.5) `
  "Karaoke 123 21/11: lan can / mep san 2 net (layer thuong) noi 2 tuong khep hanh lang tren o thang ho: vung 21,6 m2 (vung=$((DtVung $e19) -join ','))"
$e20 = CaRv 'E20'
Kiem ($e20 -notmatch 'bo_sanh_thang' -and (DtVung $e20) -contains 15) `
  "Nguoi dung 21/11: phong sat gieng thang co chu SANH TIEP TAN la sanh chung, khong bo nhu sanh thang may: rai 15 m2 (vung=$((DtVung $e20) -join ','))"
$e21 = CaRv 'E21'
Kiem ($e21 -notmatch 'bo_khu_ten' -and (DtVung $e21) -contains 80) `
  "Truong hoc 21/11: chu PHONG BO MON (PHONGBOMON) khong phai phong bom: rai 80 m2 (vung=$((DtVung $e21) -join ','))"
$e22 = CaRv 'E22'
Kiem ($e22 -match 'gop_sanh=1 ' -and (DtVung $e22).Count -eq 1 -and (DtVung $e22) -contains 25.8) `
  "Nguoi dung 21/11: sanh nho truoc WC (canh mo tron = cau) gop vao phong; kho canh lan tuong, buong xi khong gop: 24 + 1,8 = 25,8 m2 (vung=$((DtVung $e22) -join ','))"
$e22b = CaRv 'E22B'
Kiem ($e22b -notmatch 'gop_sanh' -and (DtVung $e22b) -contains 24) `
  "Sanh co hinh kin co bon cau / lavabo (no roi) la buong xi: khong gop, phong 24 m2 (vung=$((DtVung $e22b) -join ','))"
$e23 = CaRv 'E23'
Kiem ((DtVung $e23).Count -eq 1 -and (DtVung $e23)[0] -ge 26.1 -and (DtVung $e23)[0] -le 26.2) `
  "Karaoke 123 21/11: hanh lang qua cot + lan can khong vo doan: 1 vung; 23/11 nguoi dung: bam mat tuong, chi doan lan can 6,8 m khac ra 0,14 m (25,2 + 0,95 = 26,15 m2; truoc 27,16 lan tuong) (vung=$((DtVung $e23) -join ','))"
$e24 = CaRv 'E24'
Kiem ($e24 -match 'o_x=1 ' -and $e24 -match 'thong_tang=1 ' -and (DtVung $e24).Count -eq 1 -and (DtVung $e24) -contains 77.6) `
  "Nguoi dung 23/11: sanh thong tang (dau X layer truc, tuong ho) thanh vung rieng khac theo mep thang 77,6 m2; gieng 2,5 x 2,5 m van khong rai (vung=$((DtVung $e24) -join ','))"
$e26 = CaRv 'E26'
Kiem ((DtVung $e26) -contains 14.56 -and (DtVung $e26) -contains 20 -and (DtVung $e26) -contains 35) `
  "Nguoi dung 23/11: dai 0,2 m giua o va hanh lang chi co cau khep khe gop vao o (khong ho): o 14,56 m2, vach 2 net 0,2 m ho dau ben trai khong vao o (vung=$((DtVung $e26) -join ','))"
$e27 = CaRv 'E27'
Kiem ((DtVung $e27) -contains 17.89 -and (DtVung $e27) -contains 13.2) `
  "Nguoi dung 23/11: lan can polyline kin 60 x 1800 giua 2 cot: vung hanh lang dung o net dau tien cua lan can 17,89 m2 (truoc dinh sanh 31,2), sanh 13,2 m2 (vung=$((DtVung $e27) -join ','))"
$e28 = CaRv 'E28'
Kiem ($e28 -match 'bo_hanh_lang_ben=1 ' -and (DtVung $e28).Count -eq 3 -and ((DtVung $e28) | Where-Object { $_ -eq 36 }).Count -eq 3) `
  "Nguoi dung 23/11: hanh lang 2,3 m mo ra san (cot + lan can, ben kia khong phong) = hanh lang ben, khong rai; 3 phong 36 m2 van rai (vung=$((DtVung $e28) -join ','))"
$e29 = CaRv 'E29'
Kiem ($e29 -notmatch 'bo_hanh_lang_ben' -and $e29 -match 'bo_khu_khong_bao=1 m2=72 ' -and (DtVung $e29) -contains 41.4) `
  "Nguoi dung 23/11: hanh lang trong nha (phong 2 ben) van rai 41,4 m2; phong chu VE SINH 72 m2 (> 60) bo theo chu (vung=$((DtVung $e29) -join ','))"
$e30 = CaRv 'E30'
Kiem ($e30 -match 'khoet_thang=1 m2=3.8 ') "Nguoi dung 23/11: thang ve net roi 8 bac 1,8 m cach 0,3 m: nhan ve thang 1,8 x 2,1 m, khoet 3,8 m2 ($([regex]::Match($e30, 'khoet_thang=\S+ m2=\S+').Value))"
$e31 = CaRv 'E31'
Kiem ($e31 -match 'mat_bang_tang=1,2 ' -and $e31 -match 'chieu_thong_tang=1 tang 1 duoi 2 m2=120->60\+60 ' -and (DtVung $e31).Count -eq 10 -and @(DtVung $e31 | Where-Object { $_ -eq 60 }).Count -eq 4) `
  "Nguoi dung 25/11: quet 2 tang, tang 2 co o X thong tang 6 x 10 m, tang 1 khong co: sanh tang 1 120 m2 tach 60 (nhu o X tang 2) + 60; ten tang tu TEXT + thuoc tinh block ($([regex]::Match($e31, 'chieu_thong_tang=\S+ \S+ \S+ \S+ \S+ \S+').Value); vung=$((DtVung $e31) -join ','))"
$e32 = CaRv 'E32'
$ktl = @([regex]::Matches($e32, 'KTL (-?\d+),(-?\d+) (-?\d+),(-?\d+)') | ForEach-Object { , @($_.Groups[1..4] | ForEach-Object { [double]$_.Value }) })
$ktlN = @($ktl | Where-Object { $_[1] -eq $_[3] -and $_[1] -lt 1800 } | ForEach-Object { [math]::Abs($_[2] - $_[0]) } | Sort-Object)
Kiem ($e32 -match 'PCCC_RAI BC_KHOI n=3 manh=2 ' -and ($ktlN -join ',') -eq '5000,5000,10000') `
  "Nguoi dung 25/11: vung chu L (hanh lang 20 x 1,8 + canh 2,7 x 5,4 m) rai + dim theo 2 manh: hanh lang 5000 | 10000 | 5000, canh 1 dau bao ($([regex]::Match($e32, 'PCCC_RAI BC_KHOI n=\S+ \S+').Value); dim ngang hanh lang $($ktlN -join ','))"
$e33 = CaRv 'E33'
Kiem ($e33 -match 'gop_sanh=1 ' -and (DtVung $e33) -contains 25.8) `
  "Nguoi dung 25/11: canh cua WC (polyline 40 x 900 layer tuong) khac vao sanh truoc WC van gop sanh vao phong: 25,8 m2 (vung=$((DtVung $e33) -join ','))"
$e25 = CaRv 'E25'
$ktd = @([regex]::Matches($e25, 'KTD (-?\d+),(-?\d+) (-?\d+),(-?\d+) (-?\d+),(-?\d+)') | ForEach-Object { , @($_.Groups[1..6] | ForEach-Object { [double]$_.Value }) })
$ktN = @($ktd | Where-Object { $_[3] -eq $_[5] }); $ktD = @($ktd | Where-Object { $_[2] -eq $_[4] })
Kiem ($ktN.Count -eq 3 -and @($ktN | Where-Object { $_[1] -ne $ktN[0][1] }).Count -eq 0 -and [math]::Abs($ktN[0][1] - 900) -ge 300 -and [math]::Abs($ktN[0][1] - 900) -le 500 -and $ktD.Count -eq 2 -and @($ktD | Where-Object { $_[0] -ne 2424500 }).Count -eq 0) `
  "Nguoi dung 23/11: hanh lang hep 1 hang: chuoi doc lech khoi hang dau bao (y 900) 1/4 be rong 0,45 m, khong ngang bang dau bao, khong sat tuong; chuoi ngang 1 lan o x 4,5 m khong cat cot ($(($ktd | ForEach-Object { $_ -join ',' }) -join ' | '))"

# Thoi gian dung phong tren ban ve that cua nguoi dung (gitignore, chi chay khi co): truoc sua 78 s (23 trieu diem cat), chay rieng 9,7-9,9 s (21/11), ca bo test may ban 11-13 s: moc 15 s.
$bvThat = Get-ChildItem "$GOC\07_CAD\file_cad_test" -Filter 'test b*o ch*y.dwg' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($bvThat) {
  Copy-Item $bvThat.FullName "$T\that.dwg"
  (@('NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '(setq ss (ssget "X" (list (cons 0 "INSERT") (cons 2 "Factory 1A4- 1st"))))',
    'PCCC_RAI', 'BC_KHOI', '', '', '!ss', '', '', '', '_.QUIT', '_Y') -join "`r`n") | Set-Content -Encoding ascii "$T\kb5.scr"
  $p5 = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/i `"$T\that.dwg`" /s `"$T\kb5.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb5.out.txt"
  if (-not $p5.WaitForExit(300000)) { Stop-Process -Id $p5.Id -Force; throw "accoreconsole qua 5 phut (ban ve that): $T\kb5.out.txt" }
  $out5 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb5.out.txt")) -replace "`0", ''
  $mT = [regex]::Match($out5, 'thoi_gian dung_phong=(\d+)ms')
  Kiem ($mT.Success -and [int]$mT.Groups[1].Value -lt 15000 -and $out5 -match 'thoi_gian rai=\d+ms vung=[1-9]') `
    "Ban ve that test bao chay.dwg: dung phong $($mT.Groups[1].Value) ms (< 15 s; truoc sua 78 s), rai duoc"
} else { 'BO    ban ve that 07_CAD\file_cad_test\test bao chay.dwg khong co: bo ca thoi gian' }

# Ban chep ban ve mau (12_KET_QUA/, khong commit; chi chay khi may co): moc so vung / so dau bao de bat hoi quy.
# Quyet Thang 23/11: luat khe xuyen (1bd93ea) tinh vach dau tuong 0,2 m la chu T, o cua 5,6 m tuong xuong 2 khong khep -> 25 vung / 57 DB con 12 / 20.
$qt = "$GOC\12_KET_QUA\QUYET_THANG\cad\thu_maikeo\qt_maikeo.dwg"
if (Test-Path $qt) {
  Copy-Item $qt "$T\qt.dwg"
  (@('NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '(if (setq ss (ssget "_X" (list (cons 8 "PCCC_*")))) (command "_.ERASE" ss "") (princ))',
    'PCCC_RAI', 'BC_KHOI', 'LoaiTran', 'MaiKeo', '9', 'Khong', 'C', '1305000,-1655000', '1397000,-1575000', '', '', '', '_.QUIT', '_Y') -join "`r`n") | Set-Content -Encoding ascii "$T\kbqt.scr"
  $pq = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/i `"$T\qt.dwg`" /s `"$T\kbqt.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kbqt.out.txt"
  if (-not $pq.WaitForExit(300000)) { Stop-Process -Id $pq.Id -Force; throw "accoreconsole qua 5 phut (Quyet Thang): $T\kbqt.out.txt" }
  $oq = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kbqt.out.txt")) -replace "`0", ''
  Kiem ($oq -match 'thoi_gian rai=\d+ms vung=25\b' -and $oq -match '\b57 \S+ b\S+o kh\S+i trong') `
    "Ban chep Quyet Thang (MaiKeo 9 m): 25 vung / 57 dau bao nhu dap an ($([regex]::Match($oq, 'vung=\d+').Value) $([regex]::Match($oq, '\d+ \S+ b\S+o kh').Value))"
} else { 'BO    ban chep Quyet Thang 12_KET_QUA\QUYET_THANG\cad\thu_maikeo\qt_maikeo.dwg khong co' }
# Karaoke 123 tang 2 (hoc_bao_chay.dwg, ban chep y -50 m): nguoi dung ve tay 15 vung (sanh WC gop vao phong, o giao gop hanh lang; 23/11).
# Tu 23/11 toi: + 1 vung sanh thong tang (dau X tren layer truc, nguoi dung: vung rai rieng) = 16.
$hb = "$GOC\12_KET_QUA\HOC_BAO_CHAY\cad\hoc_bao_chay.dwg"
if (Test-Path $hb) {
  Copy-Item $hb "$T\hb.dwg"
  (@('NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.ZOOM', 'W', '334000,-62500', '379000,-41500',
    "(if (setq ss (ssget `"_W`" '(334000 -62500) '(379000 -41500) '((8 . `"PCCC_*`")))) (command `"_.ERASE`" ss `"`") (princ))",
    'PCCC_RAI', 'BC_KHOI', '', 'Khong', 'C', '334000,-62500', '379000,-41500', '', '', '', '_.QUIT', '_Y') -join "`r`n") | Set-Content -Encoding ascii "$T\kbhb.scr"
  $ph = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/i `"$T\hb.dwg`" /s `"$T\kbhb.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kbhb.out.txt"
  if (-not $ph.WaitForExit(300000)) { Stop-Process -Id $ph.Id -Force; throw "accoreconsole qua 5 phut (Karaoke 123): $T\kbhb.out.txt" }
  $oh = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kbhb.out.txt")) -replace "`0", ''
  Kiem ($oh -match 'thoi_gian rai=\d+ms vung=16\b' -and $oh -match 'gop_sanh=\d+ gop_o_nho=\d+' -and $oh -match 'thong_tang=1 m2=64\.3' -and $oh -match 'm2=35\.53 ') `
    "Ban chep Karaoke 123 tang 2: 15 vung nhu nguoi dung ve tay + vung sanh thong tang 6,3 x 10,2 m (23/11 toi); hanh lang duoi dung o net trong lan can kin 60 mm: 35,53 m2 (truoc 35,64 toi net ngoai) ($([regex]::Match($oh, 'vung=\d+').Value) $([regex]::Match($oh, 'gop_sanh=\d+ gop_o_nho=\d+').Value))"
  # Nguoi dung 25/11: quet tang 1 (y -100 m) + tang 2 mot lan. Chep tang 2 (y -50 m) xuong canh tang 1 (dy -49390,8 nhu ban nguoi dung), them ten mat bang.
  # Truoc sua: khung truc chi nhan o tang 1 -> 16 phong tang 2 bi bo_ngoai_truc; sanh tang 1 duoi o X tang 2 gop chung hanh lang 122,22 m2.
  # Nay: tang 1 122,22 -> 61,77 (nhu vung o X tang 2) + 49,88 hanh lang bam tuong thang (nhu nguoi dung sua tay 25/11), bo ve thang 10,6 m2 (thang net roi
  # khong nhan, duoi lo thang tang 2); 31 vung. Hanh lang chu L rai 2 manh: 3 dau bao.
  Copy-Item $hb "$T\hb12.dwg"
  (@('NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.ZOOM', 'W', '330000,-120000', '380000,-40000',
    "(setq s0 (ssget `"_W`" '(334000 -62500) '(379000 -41500)))(command `"_.COPY`" s0 `"`" `"0,0`" `"0,-49390.8`")",
    '_.ZOOM', 'W', '274000,-118000', '378500,-87000', '_.TEXT', '290000,-116500', '500', '0', 'MAT BANG BAO CHAY DUOI TRAN TANG 1',
    '_.TEXT', '346000,-115500', '500', '0', 'MAT BANG BAO CHAY DUOI TRAN TANG 2',
    "(setq ss (ssget `"_W`" '(274000 -118000) '(378500 -87000) '((8 . `"PCCC_*`"))))(if ss (command `"_.ERASE`" ss `"`"))",
    'PCCC_RAI', 'BC_KHOI', '', 'Khong', 'C', '274000,-118000', '378500,-87000', '', '', '', '_.QUIT', '_Y') -join "`r`n") | Set-Content -Encoding ascii "$T\kbhb12.scr"
  $p12 = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/i `"$T\hb12.dwg`" /s `"$T\kbhb12.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kbhb12.out.txt"
  if (-not $p12.WaitForExit(300000)) { Stop-Process -Id $p12.Id -Force; throw "accoreconsole qua 5 phut (Karaoke 123 tang 1 + 2): $T\kbhb12.out.txt" }
  $o12 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kbhb12.out.txt")) -replace "`0", ''
  Kiem ($o12 -match 'mat_bang_tang=1,2 ' -and $o12 -notmatch 'bo_ngoai_truc' -and $o12 -match 'chieu_thong_tang=1 tang 1 duoi 2 m2=122\.22->61\.77\+49\.88 bo_ve_thang=10\.6 ' -and $o12 -match 'thoi_gian rai=\d+ms vung=31\b' -and $o12 -match 'PCCC_RAI BC_KHOI n=3 manh=2 ') `
    "Ban chep Karaoke 123 tang 1 + 2 quet mot lan: tang 2 giu (khong bo ngoai truc), sanh tang 1 = vung o X tang 2 61,77 m2 + hanh lang 49,88 bam tuong, rai 2 manh ($([regex]::Match($o12, 'chieu_thong_tang=\S+ \S+ \S+ \S+ \S+ \S+ \S+').Value) $([regex]::Match($o12, 'vung=\d+').Value))"
} else { 'BO    ban chep Karaoke 123 12_KET_QUA\HOC_BAO_CHAY\cad\hoc_bao_chay.dwg khong co' }

# chay_spec.ps1: -Dwg tuong doi tinh theo thu muc hien tai cua PowerShell; thu muc co dau (chu a huyen) va dau cach.
$tmVN = "$T\Nh$([char]0xE0) A"
New-Item -ItemType Directory $tmVN | Out-Null
'{"ops":[{"op":"tron","l":"X","c":[0,0],"r":100}]}' | Set-Content -Encoding ascii "$tmVN\s1.json"
'{"ops":[{"op":"doc","gi":"layer","l":"X"}]}' | Set-Content -Encoding ascii "$tmVN\s2.json"
$env:TKPCCC_CAD_DLL = "$T\TKPCCC.Cad.dll"
Push-Location $tmVN
try {
  & "$GOC\07_CAD\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1" -Spec 's1.json' -Dwg 'ban ve.dwg' | Out-Null
  & "$GOC\07_CAD\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1" -Spec 's2.json' -Dwg 'ban ve.dwg' | Out-Null
} catch { "chay_spec.ps1 loi: $_" } finally { Pop-Location; Remove-Item Env:\TKPCCC_CAD_DLL }
$kc2 = if (Test-Path -LiteralPath "$tmVN\s2.kq.json") { Get-Content -Raw -Encoding UTF8 -LiteralPath "$tmVN\s2.kq.json" | ConvertFrom-Json }
Kiem ((Test-Path -LiteralPath "$tmVN\ban ve.dwg") -and $kc2 -and $kc2.ok -eq $true -and $kc2.doc[0].ds[0].l -eq 'X' -and $kc2.doc[0].ds[0].n -eq 1) `
  'chay_spec.ps1: DWG tuong doi theo $PWD, thu muc co dau: lan 1 tao ban ve, lan 2 mo lai dung ban ve do'
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
