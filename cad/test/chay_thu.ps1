# Kiem thu plugin khong can mo AutoCAD: build ra thu muc tam, chay kich ban trong accoreconsole, kiem so dem.
# Chay: powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1   (file chi dung chu ASCII cho PowerShell 5.1)
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$T = Join-Path $env:TEMP ('tkpccc_thu_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null

& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\TKPCCC.Cad.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  "$GOC\cad\module\plugin\*.cs" "$GOC\cad\module\chung\HangDoi.cs"
if ($LASTEXITCODE) { throw 'Bien dich loi' }
# DLL giao dien (bang PCCC_BLOCK): accoreconsole khong nap duoc nen o day chi kiem bien dich; chay that phai thu trong AutoCAD.
& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\TKPCCC.UI.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$ACAD\acmgd.dll" /r:"$ACAD\AdWindows.dll" `
  /r:"$FW\WPF\PresentationCore.dll" /r:"$FW\WPF\WindowsBase.dll" /r:"$FW\System.Xaml.dll" `
  /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:"$T\TKPCCC.Cad.dll" "$GOC\cad\module\ui\*.cs"
if ($LASTEXITCODE) { throw 'Bien dich TKPCCC.UI.dll loi' }
Copy-Item "$GOC\data\vat_tu.json" $T

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
  'RECTANG', '50000,0', '56500,4500', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '12', 'D', 'L', '', '',
  'PLINE', '49000,-1000', '58000,-1000', '', '_.ZOOM', '_E', 'PCCC_TUYEN', 'ONG_DN25', 'W', '50000,0', '56500,4500', '', 'L', '53000,2000',
  'CIRCLE', '80000,0', '150', '-BLOCK', 'KHACH_VAN', '80000,0', 'L', '', '-INSERT', 'KHACH_VAN', '80000,0', '1', '', '0',
  'PCCC_THEMBLOCK', 'L', 'van_khach', 'Van khach them', 'SPR', '', 'PCCC_DAT', 'VAN_KHACH', '81000,0', '',
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
  @('PCCC_RAI SPR_DAU_DOWN n=4 luoi=2x2 buoc=3250x2250 m2=7\.31', 'PCCC_RAI: phong 6500x4500, S = 4 m, dt 12 m2 ra 4 dau phun, buoc 3250 x 2250, 7,31 m2 moi dau'),
  @('PCCC_TUYEN ONG_DN25 doan=3 m=10\.8[78]', 'PCCC_TUYEN: ong gop qua diem bam x = 53000 + 2 nhanh, dai 10,88 m'),
  @('PCCC_THEMBLOCK VAN_KHACH', 'PCCC_THEMBLOCK: block cua nguoi dung vao thu vien duoi ma moi'),
  @('VAN_KHACH\s+1 ', 'block moi them: PCCC_DAT dat duoc, PCCC_BOC dem 1 cai'))
$loi = 0
foreach ($c in $can) { if ($out -match $c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
# Lan boc thu 2 (sau khi chen bang + vung bao ve) van phai ra 2 / 7: bang thong ke va vong tron khong bi dem.
$lan2 = $out.Substring($out.LastIndexOf('Command: PCCC_BOC')) # 'Command: ': thong bao loi runner cung co chu PCCC_BOC
if ($lan2 -match 'BC_KHOI\s+2 ' -and $lan2 -match 'BC_NHIET\s+7 ') { 'DAT   boc lai sau PCCC_BANG khong dem ky hieu trong bang' } else { 'LOI   boc lai sau PCCC_BANG'; $loi++ }
if ($out -match '\[TKPCCC\] L') { 'LOI   co lenh bao loi:'; ($out -split "`n" | Select-String '\[TKPCCC\] L'); $loi++ }
# PCCC_THEMBLOCK ghi them 1 dong vao vat_tu.json canh DLL (ban trong thu muc tam; file goc cua repo khong bi dung toi vi khong co nguon_vat_tu.txt).
$dmThu = Get-Content -Raw -Encoding UTF8 "$T\vat_tu.json" | ConvertFrom-Json
$dmGoc = Get-Content -Raw -Encoding UTF8 "$GOC\data\vat_tu.json" | ConvertFrom-Json
$moi = @($dmThu.vat_tu | Where-Object { $_.ma -eq 'VAN_KHACH' })
if ($moi.Count -eq 1 -and $moi[0].ten -eq 'Van khach them' -and $moi[0].he -eq 'SPR' -and $moi[0].loai -eq 'block' -and $moi[0].dvt -eq ('c' + [char]0xE1 + 'i') -and
  @($dmThu.vat_tu).Count -eq @($dmGoc.vat_tu).Count + 1) { 'DAT   vat_tu.json them dung 1 dong cho ma moi, cac dong cu giu nguyen' } else { 'LOI   vat_tu.json sau PCCC_THEMBLOCK'; $loi++ }
# Anh xem truoc cho bang PCCC_BLOCK: PCCC_LIB ve cho block mac dinh, PCCC_THEMBLOCK ve cho block moi; anh phai co net.
$anhLoi = @('BC_KHOI', 'VAN_KHACH' | Where-Object { -not (Test-Path "$T\anh_block\$_.png") -or (DocAnh "$T\anh_block\$_.png").mau -le 1 })
if ($anhLoi.Count -eq 0) { 'DAT   anh xem truoc block: BC_KHOI, VAN_KHACH co anh 64 px co net' } else { "LOI   anh xem truoc block thieu hoac trang: $($anhLoi -join ', ')"; $loi++ }

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
# pdf: pdf_ti_le.py tinh ti le cua PDF mau (cad\test\tao_pdf_mau.py ve 1:200) tu chuoi kich thuoc; thao tac "pdf" nhap voi ti le do,
# "doc" doc lai chu da nhap de do khoang cach truc 1-7. Can python co PyMuPDF.
$pdfMau = "$GOC\cad\test\mau_kt.pdf"
$tlTrang = (& python -I "$GOC\cad\.claude\skills\tkpccc-ve-cad\scripts\pdf_ti_le.py" $pdfMau 1 | ConvertFrom-Json)[0]
@{ ops = @(@{ op = 'pdf'; file = $pdfMau; trang = 1; tl = $tlTrang.tl; p = @(200000, 0) }, @{ op = 'doc'; gi = 'chu'; l = 'PDF_Text' }) } |
  ConvertTo-Json -Depth 5 -Compress | Set-Content -Encoding ascii "$T\pdf.json"
# Ban ky_hieu.json da doi ten layer truc: chung to plugin doc mac dinh tu file du lieu (cad\data\ky_hieu), khong dung so cung trong ma.
(Get-Content -Raw -Encoding UTF8 "$GOC\cad\data\ky_hieu\ky_hieu.json") -replace '"layer_truc":\s*"TRUC"', '"layer_truc": "TRUC_T"' |
  Set-Content -Encoding UTF8 "$T\ky_hieu.json"
@('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", '_.SAVEAS', '2018', "`"$T\spec.dwg`"", 'PCCC_SPEC', "$T\spec.json", 'PCCC_SPEC', "$T\rai.json",
  'PCCC_SPEC', "$T\khoa_sai.json", 'PCCC_SPEC', "$T\khoa_dung.json", 'PCCC_SPEC', "$T\anh.json", 'PCCC_SPEC', "$T\pdf.json", '_.QSAVE') -join "`r`n" |
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
    @(($kr.ve -eq 53), "rai + tuyen ve 53 doi tuong (thay $($kr.ve))"),
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
# --- anh: PNG rong 800 px, co net ve (anh trang tron chi co 1 gia tri byte: vung trong, hoac in truoc khi ve xong) ---
$ka = if (Test-Path "$T\anh.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\anh.kq.json" | ConvertFrom-Json }
$fa = if ($ka) { [string]@($ka.anh)[0] }
if ($fa -and (Test-Path $fa)) {
  $da = DocAnh $fa
  if ($ka.ok -eq $true -and $da.rong -eq 800 -and $da.mau -gt 1) { 'DAT   anh: PNG 800 px co net ve, in sau khi ve xong' } else { "LOI   anh: rong $($da.rong), so gia tri byte $($da.mau)"; $loi++ }
} else { "LOI   anh khong ra PNG: $T\kb2.out.txt"; $loi++ }
# --- pdf: ti le tinh tu chuoi kich thuoc; nhap vao phai ra kich thuoc that (truc 1-7 = 41000, khung 1090 pt = 76906 mm o 1:200) ---
if ([math]::Abs($tlTrang.tl - 200) -le 2 -and $tlTrang.cap -ge 8 -and $tlTrang.tin_cay -ge 0.8) { "DAT   pdf_ti_le.py: PDF mau ve 1:200 ra tl = $($tlTrang.tl) ($($tlTrang.cap) cap so)" }
else { "LOI   pdf_ti_le.py: tl = $($tlTrang.tl), cap = $($tlTrang.cap), tin_cay = $($tlTrang.tin_cay)"; $loi++ }
$kp = if (Test-Path "$T\pdf.kq.json") { Get-Content -Raw -Encoding UTF8 "$T\pdf.kq.json" | ConvertFrom-Json }
$chuPdf = if ($kp -and $kp.doc) { @($kp.doc[0].ds) } else { @() }
$t1 = @($chuPdf | Where-Object { $_.t -eq '1' })[0]; $t7 = @($chuPdf | Where-Object { $_.t -eq '7' })[0]
$truc17 = if ($t1 -and $t7) { [math]::Abs($t7.p[0] - $t1.p[0]) } else { 0 }
$rongKhung = if ($kp -and $kp.hop) { $kp.hop[2] - $kp.hop[0] } else { 0 }
if ($kp -and $kp.ok -eq $true -and $kp.pdf[0].n -ge 20 -and [math]::Abs($truc17 - 41000) -le 410 -and [math]::Abs($rongKhung - 76906) -le 769) {
  "DAT   pdf: nhap $($kp.pdf[0].n) doi tuong, truc 1-7 do duoc $truc17 (can 41000 +- 1 %), khung rong $rongKhung"
} else { "LOI   pdf: n = $($kp.pdf.n), truc 1-7 = $truc17, khung = $rongKhung, loi = $($kp.loi | ConvertTo-Json -Compress -Depth 4): $T\kb2.out.txt"; $loi++ }
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
