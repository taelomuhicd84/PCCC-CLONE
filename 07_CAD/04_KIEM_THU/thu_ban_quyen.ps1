# Kiem thu ban quyen khong can mo AutoCAD: tao cap khoa thu (thu muc tam), ky bang KeyGen va bang Node (gia lap Apps Script,
# nap Code.gs bang vm), BanQuyen.Kiem phai nhan key dung va tu choi sai email / sai may / het han / sua 1 byte. File chi dung chu ASCII.
# Chay: powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\thu_ban_quyen.ps1
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
# Moi thu muc so 07_CAD\NN_TEN la mot hang muc tinh nang (26/11): cad\*.cs -> TKPCCC.Cad.dll, ui\*.cs -> TKPCCC.UI.dll (nhu build.bat).
$CS_CAD = @(Get-ChildItem "$GOC\07_CAD\??_*\cad\*.cs" | ForEach-Object FullName); $CS_UI = @(Get-ChildItem "$GOC\07_CAD\??_*\ui\*.cs" | ForEach-Object FullName)
$T = Join-Path $env:TEMP ('tkpccc_bq_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null
$loi = $false
function Kiem($ten, $dat) { if ($dat) { "DAT  $ten" } else { "HONG $ten"; $script:loi = $true } }

# Ban /target:exe de doc duoc dong lenh (build.bat build ban winexe co cua so).
& "$FW\csc.exe" /nologo /target:exe /codepage:65001 /out:"$T\kg.exe" `
  /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  "$GOC\07_CAD\16_BAN_QUYEN\keygen\KeyGen.cs" "$GOC\07_CAD\16_BAN_QUYEN\cad\BanQuyen.cs"
if ($LASTEXITCODE) { throw 'Bien dich KeyGen loi' }
$kg = "$T\kg.exe"

& $kg tao "$T\cong" "$T\rieng" | Out-Null
Kiem 'tao cap khoa' ((Test-Path "$T\cong\khoa_cong_khai.xml") -and (Test-Path "$T\rieng\khoa_rieng.xml") -and (Test-Path "$T\rieng\khoa_rieng.pem"))
$ErrorActionPreference = 'Continue' # kg.exe ghi loi ra stderr: khong de PowerShell dung lai
& $kg tao "$T\cong" "$T\rieng" 2>$null | Out-Null
Kiem 'khong ghi de khoa cu' ($LASTEXITCODE -eq 2)
$cong = "$T\cong\khoa_cong_khai.xml"; $rieng = "$T\rieng\khoa_rieng.xml"; $pem = "$T\rieng\khoa_rieng.pem"

$may0 = (& $kg may | Out-String).Trim()
Kiem "ma may dang XXXX-XXXX-XXXX-XXXX ($may0)" ($may0 -cmatch '^[0-9A-F]{4}(-[0-9A-F]{4}){3}$')

$email = 'chu.nha@vidu.vn'; $may = 'ABCD-0123-4567-89EF'
$han = (Get-Date).AddDays(30).ToString('yyyy-MM-dd'); $cu = (Get-Date).AddDays(-1).ToString('yyyy-MM-dd')
function Nhan($key, $e, $m) { & $kg kiem $cong $key $e $m | Out-Null; return ($LASTEXITCODE -eq 0) }

$k1 = (& $kg ky $rieng $email $may $han | Out-String).Trim()
Kiem 'KeyGen ky -> BanQuyen nhan' (Nhan $k1 $email $may)
Kiem 'email viet hoa, co khoang trang van nhan' (Nhan $k1 '  Chu.Nha@ViDu.vn ' $may)
Kiem 'sai email bi tu choi' (-not (Nhan $k1 'khac@vidu.vn' $may))
Kiem 'sai may bi tu choi' (-not (Nhan $k1 $email 'ABCD-0123-4567-89EE'))
$kh = (& $kg ky $rieng $email $may $cu | Out-String).Trim()
Kiem 'het han bi tu choi' (-not (Nhan $kh $email $may))
$p = $k1.Split('.'); $c = $p[1].ToCharArray(); $c[10] = if ($c[10] -eq 'A') { 'B' } else { 'A' }
Kiem 'sua 1 byte chu ky bi tu choi' (-not (Nhan ($p[0] + '.' + (-join $c)) $email $may))
$c = $p[0].ToCharArray(); $c[3] = if ($c[3] -eq 'A') { 'B' } else { 'A' }
Kiem 'sua 1 byte noi dung bi tu choi' (-not (Nhan ((-join $c) + '.' + $p[1]) $email $may))

# Ma may moi theo seri o chua Windows (WMI), ma cu MachineGuid van nhan; keygen.py in cung ma may va ky trung tung byte.
$mayCu = (& $kg may_cu | Out-String).Trim()
Kiem "ma may cu dang XXXX-XXXX-XXXX-XXXX ($mayCu)" ($mayCu -cmatch '^[0-9A-F]{4}(-[0-9A-F]{4}){3}$')
Kiem "ma may moi theo seri o dia bat dau bang D ($may0), khac ma cu" ($may0 -cmatch '^D' -and $may0 -ne $mayCu)
function NhanMayNay($key) { & $kg kiem_may $cong $key $email | Out-Null; return ($LASTEXITCODE -eq 0) }
Kiem 'key cap theo ma may moi: plugin nhan' (NhanMayNay ((& $kg ky $rieng $email $may0 $han | Out-String).Trim()))
Kiem 'key cap theo ma may cu (MachineGuid, truoc 15/11): plugin van nhan' (NhanMayNay ((& $kg ky $rieng $email $mayCu $han | Out-String).Trim()))
Kiem 'key cap cho may khac: plugin tu choi' (-not (NhanMayNay $k1))
# Chong lui dong ho: moc ngay da thay (HMAC theo ma may) lon hon hom nay thi hop dong so han theo moc.
$bq = [Reflection.Assembly]::LoadFile($kg).GetType('TKPCCC.BanQuyen')
$d0 = (Get-Date).Date; $d40 = $d0.AddDays(40); $luu = $bq::DauNgay($d40, $may0)
Kiem 'moc ngay +40 hop le: ngay so han = moc' ($bq::NgayHieuLuc($d0, $luu, $may0) -eq $d40)
Kiem 'moc sua tay ngay (sai HMAC): bo qua' ($bq::NgayHieuLuc($d0, $luu.Replace($d40.ToString('yyyy-MM-dd'), $d0.AddDays(41).ToString('yyyy-MM-dd')), $may0) -eq $d0)
Kiem 'moc chep tu may khac: bo qua' ($bq::NgayHieuLuc($d0, $luu, $may) -eq $d0)
& $kg kiem $cong $k1 $email $may $d40.ToString('yyyy-MM-dd') | Out-Null
Kiem 'key han +30 ngay, ngay so han +40 (lui dong ho): tu choi' ($LASTEXITCODE -eq 1)
$py = "$GOC\07_CAD\16_BAN_QUYEN\keygen\keygen.py"
Kiem 'keygen.py ky (PEM) trung key KeyGen' ((& python $py ky $email $may $han --khoa $pem | Out-String).Trim() -eq $k1)
Kiem 'keygen.py ky (XML) trung key KeyGen' ((& python $py ky $email $may $han --khoa $rieng | Out-String).Trim() -eq $k1)
$mayPy = (& python $py may | Out-String).Trim()
Kiem "keygen.py may trung KeyGen may ($mayPy)" ($mayPy -eq $may0)

# Gia lap Apps Script: Utilities gia dung crypto cua Node, PEM PKCS#8 do KeyGen xuat. base64EncodeWebSafe giu dau "=" nhu Google.
$js = @'
const fs = require('fs'), vm = require('vm'), crypto = require('crypto');
const [gs, pem, email, may, han] = process.argv.slice(2);
const ctx = {}; vm.runInNewContext(fs.readFileSync(gs, 'utf8'), ctx);
const U = { Charset: { UTF_8: 'UTF-8' },
  computeRsaSha256Signature: (s, k) => Array.from(crypto.sign('sha256', Buffer.from(s, 'utf8'), k)).map(b => b > 127 ? b - 256 : b),
  base64EncodeWebSafe: d => Buffer.from(typeof d === 'string' ? Buffer.from(d, 'utf8') : d.map(b => b & 255)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_') };
const pemHong = fs.readFileSync(pem, 'utf8').replace(/\n/g, ' ');
const tu = new Date(2026, 0, 1);
const ok = ctx.chuanHan('', 30, tu) === '2026-01-31' && ctx.chuanHan('5/3/2027', 1, tu) === '2027-03-05' && ctx.chuanHan('2027-3-5', 1, tu) === '2027-03-05'
  && ctx.chuanHan('sai', 1, tu) === '' && ctx.timCot(['Dau thoi gian', 'Dia chi email', 'Ma may'], 'email') === 1
  && ctx.hopLeEmail('chu.nha+1@vi-du.com.vn') && !ctx.hopLeEmail('=1+1@x.vn') && !ctx.hopLeEmail('+a@x.vn')
  && !ctx.hopLeEmail('=importxml(concat("http://x.vn/",a2),"//a")&"@x.vn"') && !ctx.hopLeEmail('a@b')
  && ctx.hopLeMay('D1A2-0123-4567-89EF') && !ctx.hopLeMay('G1A2-0123-4567-89EF');
console.log((ok ? '' : 'HAM_THUAN_HONG ') + ctx.taoKey(email, may, han, ctx.chuanPem(pemHong), U));
'@
Set-Content "$T\gs.js" $js -Encoding ascii
$k2 = (& node "$T\gs.js" "$GOC\07_CAD\16_BAN_QUYEN\keygen\apps_script\Code.gs" $pem ' Chu.Nha@ViDu.vn' $may $han | Out-String).Trim()
Kiem 'ham thuan Code.gs (chuanHan, timCot, hopLeEmail chan chen cong thuc, hopLeMay)' (-not $k2.StartsWith('HAM_THUAN_HONG'))
$k2 = $k2.Replace('HAM_THUAN_HONG ', '')
Kiem 'key Apps Script (Node) co dau = van nhan' ($k2.Contains('=') -and (Nhan $k2 $email $may))
Kiem 'key Apps Script trung key KeyGen (PKCS#1 v1.5 tat dinh)' ($k2.Replace('=', '') -eq $k1)
$k3 = (& node "$T\gs.js" "$GOC\07_CAD\16_BAN_QUYEN\keygen\apps_script\Code.gs" $pem $email $may $cu | Out-String).Trim()
Kiem 'key Apps Script het han bi tu choi' (-not (Nhan $k3 $email $may))

Remove-Item -Recurse -Force "$T\rieng" # khoa rieng thu khong de lai tren may

# Ban phat hanh: bien dich duoc voi BAN_QUYEN va co khoa cong khai nhung trong DLL.
& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /define:BAN_QUYEN "/resource:$cong,khoa_cong_khai.xml" /out:"$T\TKPCCC.Cad.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  $CS_CAD "$GOC\07_CAD\05_NEN_TANG\chung\HangDoi.cs"
Kiem 'bien dich ban phat hanh' ($LASTEXITCODE -eq 0)
$a = [Reflection.Assembly]::LoadFile("$T\TKPCCC.Cad.dll")
Kiem 'khoa cong khai nhung trong DLL' ($a.GetManifestResourceNames() -contains 'khoa_cong_khai.xml')

Remove-Item -Recurse -Force $T -ErrorAction SilentlyContinue
if ($loi) { exit 1 } else { 'TAT CA DAT' }
