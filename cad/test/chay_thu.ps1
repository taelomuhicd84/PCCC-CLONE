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
Copy-Item "$GOC\cad\data\ky_hieu\hinh_bc.json" $T  # hinh block mac dinh (lay tu PDF trang 11): PCCC_LIB tao thu vien moi theo hinh nay
# File danh muc "goc" cua repo: tro toi ban chep tam (nhu build.bat ghi nguon_vat_tu.txt) de PCCC_THEMBLOCK ghi ca hai file ma khong dung repo.
New-Item -ItemType Directory "$T\goc" | Out-Null
Copy-Item "$GOC\data\vat_tu.json" "$T\goc"
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
  'RECTANG', '50000,0', '66000,12000', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '12', '', '', 'D', 'L', '', '',
  'RECTANG', '100000,0', '114000,10000', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '', '1.5', '', 'D', 'L', '', '',
  'PLINE', '200000,0', '212000,0', '212000,8000', '203000,8000', '203000,5000', '200000,5000', 'C', 'PCCC_RAI', 'SPR_DAU_DOWN', '4', '', '', 'Co', 'D', 'L', '', '',
  'PLINE', '49000,-1000', '67000,-1000', '', '_.ZOOM', '_E', 'OSMODE', '0', 'PCCC_TUYEN', 'ONG_DN25', 'W', '50000,0', '66000,12000', '', 'L', '65000,2000',
  'CIRCLE', '80000,0', '150', '-BLOCK', 'VAN_KHACH', '80000,0', 'L', '', '-INSERT', 'VAN_KHACH', '80000,0', '1', '', '0',
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
  # dt doi luoi: S = 4 m khong co dt ra 4 x 3 (16 m2 moi dau); co dt 12 phai ra 4 x 4
  @('PCCC_RAI SPR_DAU_DOWN n=16 luoi=4x4 buoc=4000x3000 m2=12\b', 'PCCC_RAI: phong 16000x12000, S = 4 m, dt 12 m2 ra 4 x 4 (khong phai 4 x 3)'),
  # diem bam ngoai cac cot (x = 65000; OSMODE 0 de diem go khong bi bat vao dau phun x 64000): 11500 + 4 x 13000 = 63,5 m; bo qua diem bam (Enter, cot 56000) chi ra 59,5 m
  # le 1,5 m: phong 14000x10000, S 4 m: x n=4, (14-3)/3 = 3,667 lam tron 3,65 (cach tuong 1,525); y n=3, (10-3)/2 = 3,5. Khong le: 3500x3300
  @('PCCC_RAI SPR_DAU_DOWN n=12 luoi=4x3 buoc=3650x3500 ', 'PCCC_RAI go tay co le 1,5 m: phong 14000x10000 ra 4 x 3, buoc 3650x3500'),
  # theo_hang go tay (tra loi Co): phong chu L 12000x8000 khoet 3000x3000 goc tren trai ra 6 dau, HO 1,1 %; Khong / Enter ra 5 dau, HO 3,4 %
  @('PCCC_RAI SPR_DAU_DOWN n=6 luoi=3x2 buoc=4000x4000 m2=16 HO=1\.1%', 'PCCC_RAI go tay tra loi Co (theo_hang): phong chu L ra 6 dau HO 1,1 %'),
  @('PCCC_TUYEN ONG_DN25 doan=5 m=63\.5\b', 'PCCC_TUYEN: ong gop qua diem bam x = 65000 (ngoai cac cot) + 4 nhanh, dai 63,5 m'),
  @('PCCC_THEMBLOCK VAN_KHACH', 'PCCC_THEMBLOCK: block ten VAN_KHACH them duoi ma van_khach (ma trung ten block khong bi bao trung)'),
  @('VAN_KHACH\s+2 ', 'block moi them: PCCC_DAT dat duoc, PCCC_BOC dem 2 cai (ban chen tay + ban PCCC_DAT)'))
$loi = 0
foreach ($c in $can) { if ($out -match $c[0]) { "DAT   $($c[1])" } else { "LOI   $($c[1])"; $loi++ } }
# Lan boc thu 2 (sau khi chen bang + vung bao ve) van phai ra 2 / 7: bang thong ke va vong tron khong bi dem.
$lan2 = $out.Substring($out.LastIndexOf('Command: PCCC_BOC')) # 'Command: ': thong bao loi runner cung co chu PCCC_BOC
if ($lan2 -match 'BC_KHOI\s+2 ' -and $lan2 -match 'BC_NHIET\s+7 ') { 'DAT   boc lai sau PCCC_BANG khong dem ky hieu trong bang' } else { 'LOI   boc lai sau PCCC_BANG'; $loi++ }
if ($out -match '\[TKPCCC\] L') { 'LOI   co lenh bao loi:'; ($out -split "`n" | Select-String '\[TKPCCC\] L'); $loi++ }
# PCCC_THEMBLOCK ghi them 1 dong vao vat_tu.json canh DLL va vao file goc (nguon_vat_tu.txt tro toi ban chep tam $T\goc, repo khong bi dung toi).
$dmThu = Get-Content -Raw -Encoding UTF8 "$T\vat_tu.json" | ConvertFrom-Json
$dmGoc = Get-Content -Raw -Encoding UTF8 "$GOC\data\vat_tu.json" | ConvertFrom-Json
$dmGocThu = Get-Content -Raw -Encoding UTF8 "$T\goc\vat_tu.json" | ConvertFrom-Json
$moi = @($dmThu.vat_tu | Where-Object { $_.ma -eq 'VAN_KHACH' })
if ($moi.Count -eq 1 -and $moi[0].ten -eq 'Van khach them' -and $moi[0].he -eq 'SPR' -and $moi[0].loai -eq 'block' -and $moi[0].dvt -eq ('c' + [char]0xE1 + 'i') -and
  @($dmThu.vat_tu).Count -eq @($dmGoc.vat_tu).Count + 1 -and @($dmGocThu.vat_tu | Where-Object { $_.ma -eq 'VAN_KHACH' }).Count -eq 1 -and
  @($dmGocThu.vat_tu).Count -eq @($dmGoc.vat_tu).Count + 1) { 'DAT   vat_tu.json canh DLL va file goc (nguon_vat_tu.txt) them dung 1 dong cho ma moi' } else { 'LOI   vat_tu.json sau PCCC_THEMBLOCK'; $loi++ }
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
# Review 2026-11-12 (dau vao dung nhu review):
# CHAN: PDF bo doc underlay khong mo duoc (chi khoa sua, co mat khau mo, file hong) thi khong goi -PDFIMPORT (truoc day treo, nuot ca goi sau va QSAVE).
& python -I "$GOC\cad\test\tao_pdf_mau.py" $T
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
# so_do (G5, cad\skills\vi_du\so_do.json): 2 truc DN100, 3 vach tang (2 tang co dem) -> moi tang co dem 2 OS&Y + 2 FS, dau 2 truc 2 van xa khi.
# Chay SAU van.json (mat bang 2 OS&Y + 1 XK): block so do nam o layer PCCC_SODO nen dem OS&Y, XK sau so_do khong doi, khong co FS. Goi thu hai: truc ONG_DN25 khong co van OS&Y -> bao loi, khong ve gi; doc chu cao do.
Copy-Item "$GOC\cad\skills\vi_du\so_do.json" "$T\so_do.json"
'{"goc":[1200000,0],"ops":[{"op":"so_do","truc":["ONG_DN25"],"tang":[{"ten":"T1","cot":0,"dem":{"SPR_DAU_DOWN":4}}]},{"op":"doc","gi":"chu","l":"PCCC_SODO_TANG"}]}' |
  Set-Content -Encoding ascii "$T\so_do_loi.json"
# so_do he bc (vi du so_do_bc.json = trang 11 ho so that: 12 zone + du phong, bang tinh nguon): them doc layer day + doc chu vung bang tinh.
# goi loi: zone 26 dau bao > toi_da 25, va thieu toi_da -> 2 loi, khong ve.
[IO.File]::WriteAllText("$T\so_do_bc.json", ([IO.File]::ReadAllText("$GOC\cad\skills\vi_du\so_do_bc.json") -replace '\{"op":"dem"\}\]\}\s*$',
  '{"op":"dem"},{"op":"doc","gi":"layer","l":"PCCC_SODO_DAY*"},{"op":"doc","gi":"chu","l":"PCCC_SODO_BC","hop":[-16600,8300,-700,15800]}]}'))
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
# Ma van sprinkler moi trong vat_tu.json (Buoc 5): dat 2 OS&Y + 1 XK, dem theo MA_VT ("mat bang" cho phep kiem chong dem trung cua so_do).
'{"goc":[1100000,0],"ops":[{"op":"dat","ma":"SPR_VAN_OSY_DN100","p":[[0,0],[0,1000]],"goc":90},{"op":"dat","ma":"SPR_VAN_XK","p":[[1000,0]]},{"op":"dem"}]}' |
  Set-Content -Encoding ascii "$T\van.json"
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
  'PCCC_SPEC', "$T\rai_moi.json", 'PCCC_SPEC', "$T\pdf_mk.json", 'PCCC_SPEC', "$T\rv_tuyen.json", 'PCCC_SPEC', "$T\hoa.json", 'PCCC_SPEC', "$T\hoa_doc.json",
  'PCCC_SPEC', "$T\rv_hang.json", 'PCCC_SPEC', "$T\rv2.json", 'PCCC_SPEC', "$T\van.json", 'PCCC_SPEC', "$T\so_do.json", 'PCCC_SPEC', "$T\so_do_loi.json", 'PCCC_SPEC', "$T\so_do_bc.json", 'PCCC_SPEC', "$T\so_do_bc_loi.json",
  'PCCC_SPEC', "$T\so_do_loc.json", 'PCCC_SPEC', "$T\so_do_bc2.json", 'INSUNITS', '6', 'PCCC_SPEC', "$T\so_do_m.json", 'INSUNITS', '4', '_.QSAVE',
  ('(progn (setq f (open "' + ($T -replace '\\', '/') + '/blk.txt" "w")) (setq e (cdr (assoc -2 (tblsearch "BLOCK" "BC_COI_DEN")))) ' +
   '(while e (write-line (cdr (assoc 0 (entget e))) f) (setq e (entnext e))) (setq l (tblsearch "LAYER" "PCCC_SODO_DAY_NGUON")) ' +
   '(write-line (strcat "LAYER " (itoa (cdr (assoc 62 l))) " " (cdr (assoc 6 l))) f) (close f))')) -join "`r`n" |
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
# PDF mau co 3 cap so "ban" (nhip ghi sai, so khong ke nhau cung hang) + chu "1 000 000": phai qua hai vong loc moi ra 200 +- 0,1 %.
if ([math]::Abs($tlTrang.tl - 200) -le 0.2 -and $tlTrang.cap -ge 9) { "DAT   pdf_ti_le.py: PDF mau ve 1:200 co cap so ban ra tl = $($tlTrang.tl) ($($tlTrang.cap) cap so, tin_cay $($tlTrang.tin_cay))" }
else { "LOI   pdf_ti_le.py: tl = $($tlTrang.tl), cap = $($tlTrang.cap), tin_cay = $($tlTrang.tin_cay)"; $loi++ }
& python -I "$GOC\cad\.claude\skills\tkpccc-ve-cad\scripts\pdf_ti_le.py" $pdfMau 0 | Out-Null
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
$ksb = DocKq 'so_do_bc'; $ksbl = DocKq 'so_do_bc_loi'
# Bang tinh nguon tinh tay (phu luc C TCVN 7568-14:2025, so lieu trang 11): IQ = 100 + (59 x 35 + 24 x 59) / 1000 = 103,481 mA;
# IA = 100 + 59 x 40 + 24 x 40 + 28 x 14,5 + 3 x 30 + 11 x 15 = 4081 mA; C20 = 1,25 x (0,103481 x 24 + 2 x 4,081 x 0,5) = 8,20568 Ah;
# IC = 1,25 x (0,103481 x 5 + 4,081 x 0,5) / 24 = 0,13322 A; IPSE = 0,103481 + 0,13322 = 0,2367 A (bang ghi 0.24).
$ng = if ($ksb) { $ksb.nguon }
Kiem ($ng -and [math]::Abs($ng.ia_ma - 4081) -lt 1e-6 -and [math]::Abs($ng.iq_ma - 103.481) -lt 1e-6 -and [math]::Abs($ng.c20_ah - 8.20568) -lt 1e-6 -and
  [math]::Abs($ng.ic_a - 0.133224) -lt 1e-6 -and [math]::Abs($ng.ipse_a - 0.236705) -lt 1e-6) `
  "so_do bc bang tinh nguon dung trang 11: IA 4081 mA, IQ 103,481 mA, C20 8,20568 Ah, IC 0,133 A, IPSE 0,24 A (thay $($ng | ConvertTo-Json -Compress))"
$chuBang = if ($ksb -and $ksb.doc -and $ksb.doc.Count -gt 1) { @($ksb.doc[1].ds | ForEach-Object { $_.t }) } else { @() }
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
Kiem ($lk.Count -eq 1 -and $lk[0].Count -eq 3 -and $lk[0][1] -match '^DI.N T.CH B.O V.$' -and $lk[0][2] -match '^ZONE 1 : 88\.11 M' -and
  $lh.Count -eq 1 -and $lh[0].Count -eq 2 -and $lh[0][1] -eq 'ZONE 2' -and $ld2.Count -eq 1 -and $ld2[0][1] -eq 'ZONE 2' -and
  $l120.Count -eq 1 -and @($l120[0].t -split "`r?`n").Count -eq 2) `
  "so_do bc: nhan duoi dien tro cuoi day nhu trang 11: khu vuc / DIEN TICH BAO VE / ZONE n : dt M2; ghep ten tang + kv; thieu dt, thieu kv bo dong ($($c1 | ForEach-Object { $_.t -replace "`r?`n", ' / ' }))"
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
$kv = DocKq 'van'
Kiem ($kv -and $kv.ok -eq $true -and $kv.ve -eq 3 -and $kv.dem.SPR_VAN_OSY_DN100 -eq 2 -and $kv.dem.SPR_VAN_XK -eq 1) `
  "dat ma van moi: 2 SPR_VAN_OSY_DN100 + 1 SPR_VAN_XK, dem theo MA_VT (thay ve $($kv.ve), dem $($kv.dem | ConvertTo-Json -Compress))"
# PCCC_THEMBLOCK that bai o danh muc: thu vien DWG khong doi, khong file danh muc nao co ma moi
$out2 = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes("$T\kb2.out.txt")) -replace "`0", ''
$dm2 = Get-Content -Raw -Encoding UTF8 "$T\vat_tu.json" | ConvertFrom-Json
$dmGoc2 = Get-Content -Raw -Encoding UTF8 $fGoc | ConvertFrom-Json
Kiem ((Get-FileHash "$T\PCCC_Blocks.dwg").Hash -eq $bamTv -and $out2 -match 'VAN_TRUNG \S+ c. trong' -and $out2 -match 'kh\S+ng ghi \S+c' -and
  @($dm2.vat_tu | Where-Object { $_.ma -match '^VAN_(TRUNG|RO)$' }).Count -eq 0 -and @($dmGoc2.vat_tu | Where-Object { $_.ma -eq 'VAN_TRUNG' }).Count -eq 1 -and
  @($dmGoc2.vat_tu | Where-Object { $_.ma -eq 'VAN_RO' }).Count -eq 0) 'PCCC_THEMBLOCK: ma trung trong file goc, file goc chi doc: bao loi, PCCC_Blocks.dwg va danh muc khong doi'
Set-ItemProperty $fGoc IsReadOnly $false

# chay_spec.ps1: -Dwg tuong doi tinh theo thu muc hien tai cua PowerShell; thu muc co dau (chu a huyen) va dau cach.
$tmVN = "$T\Nh$([char]0xE0) A"
New-Item -ItemType Directory $tmVN | Out-Null
'{"ops":[{"op":"tron","l":"X","c":[0,0],"r":100}]}' | Set-Content -Encoding ascii "$tmVN\s1.json"
'{"ops":[{"op":"doc","gi":"layer","l":"X"}]}' | Set-Content -Encoding ascii "$tmVN\s2.json"
$env:TKPCCC_CAD_DLL = "$T\TKPCCC.Cad.dll"
Push-Location $tmVN
try {
  & "$GOC\cad\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1" -Spec 's1.json' -Dwg 'ban ve.dwg' | Out-Null
  & "$GOC\cad\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1" -Spec 's2.json' -Dwg 'ban ve.dwg' | Out-Null
} catch { "chay_spec.ps1 loi: $_" } finally { Pop-Location; Remove-Item Env:\TKPCCC_CAD_DLL }
$kc2 = if (Test-Path -LiteralPath "$tmVN\s2.kq.json") { Get-Content -Raw -Encoding UTF8 -LiteralPath "$tmVN\s2.kq.json" | ConvertFrom-Json }
Kiem ((Test-Path -LiteralPath "$tmVN\ban ve.dwg") -and $kc2 -and $kc2.ok -eq $true -and $kc2.doc[0].ds[0].l -eq 'X' -and $kc2.doc[0].ds[0].n -eq 1) `
  'chay_spec.ps1: DWG tuong doi theo $PWD, thu muc co dau: lan 1 tao ban ve, lan 2 mo lai dung ban ve do'
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
