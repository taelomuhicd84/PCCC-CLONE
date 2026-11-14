# Thu tren AutoCAD CO GIAO DIEN (khong nam trong bo bat buoc): script tu mo mot AutoCAD that khoang 1-2 phut roi tu tat.
# Nap ban build trong bundle (chay cad\build.bat truoc) tu mot thu muc tam, mo palette PCCC (lenh PCCC, PCCC_BLOCK, phim tat PHX = PCCC_HELP), roi goi may chu MCP that
# (cad\bin\TKPCCC.Mcp.exe) qua cau noi: cad_tt, cad_chay (pdf + rai + tuyen + anh), cad_anh; PCCC_DAT go ? (bang chon vat tu). Doc bang bang UI Automation.
# Chi chay khi KHONG co AutoCAD nao dang mo. Thu vien block, anh, hang doi deu nam trong thu muc tam: khong dung toi file cua nguoi dung.
# Chay: powershell -ExecutionPolicy Bypass -File cad\test\thu_gui.ps1     (file chi dung chu ASCII)
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$B = "$GOC\cad\TKPCCC.bundle\Contents"
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Dang co AutoCAD mo: dong lai roi chay (script nay tu mo va tu tat mot AutoCAD rieng).' }
foreach ($f in "$B\TKPCCC.Cad.dll", "$B\TKPCCC.UI.dll", "$B\vat_tu.json", "$GOC\cad\bin\TKPCCC.Mcp.exe") { if (-not (Test-Path $f)) { throw "Thieu $f - chay cad\build.bat truoc" } }
$T = Join-Path $env:TEMP ('tkpccc_gui_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory "$T\b" | Out-Null
Copy-Item "$B\TKPCCC.Cad.dll", "$B\TKPCCC.UI.dll", "$B\vat_tu.json", "$B\ky_hieu.json", "$B\khoang_cach_rai.json" "$T\b"
$env:TKPCCC_CAD_HANG_DOI = "$T\hd"   # ca AutoCAD lan Mcp.exe mo tu day deu thua huong bien nay
# Phim tat PHX = PCCC_HELP (phim_tat.txt canh DLL): kich ban go PHX, bang lenh hien ra = phim tat da dang ky va PCCC_HELP mo bang.
'PHX=PCCC_HELP' | Set-Content -Encoding ascii "$T\b\phim_tat.txt"
# PHX de cuoi: phim tat gui PCCC_HELP bat dong bo (SendStringToExecute), dat truoc thi lenh sau cua kich ban chen ngang.
@('NETLOAD', "`"$T\b\TKPCCC.Cad.dll`"", 'INSUNITS', '4', 'PCCC', 'PCCC_BLOCK', 'PCCC_BOTRI', 'PCCC_TIENICH',
  'PCCC_DAT', '?', '3000000,0', '3001000,0', '', 'PHX') -join "`r`n" | Set-Content -Encoding ascii "$T\gui.scr"
$loi = 0
function Kiem($dk, $ten) { if ($dk) { "DAT   $ten" } else { "LOI   $ten"; $script:loi++ } }
Add-Type -AssemblyName System.Drawing
function DocAnh($f) {
  if (-not $f -or -not (Test-Path $f)) { return @{ rong = 0; mau = 0 } }
  $bmp = New-Object Drawing.Bitmap $f
  $du = $bmp.LockBits((New-Object Drawing.Rectangle 0, 0, $bmp.Width, $bmp.Height), 'ReadOnly', 'Format32bppArgb')
  $byte = New-Object byte[] ($du.Stride * $du.Height)
  [Runtime.InteropServices.Marshal]::Copy($du.Scan0, $byte, 0, $byte.Length)
  $kq = @{ rong = $bmp.Width; mau = (New-Object 'Collections.Generic.HashSet[byte]' (, $byte)).Count }
  $bmp.UnlockBits($du); $bmp.Dispose()
  $kq
}

$acad = Start-Process "$ACAD\acad.exe" -ArgumentList "/nologo /b `"$T\gui.scr`"" -PassThru
$mcp = $null
try {
  # Cau noi ghi song.json khi plugin da nap va AutoCAD da co ban ve.
  for ($i = 0; $i -lt 180 -and -not (Test-Path "$T\hd\song.json"); $i++) { Start-Sleep -Seconds 1 }
  Kiem (Test-Path "$T\hd\song.json") "AutoCAD mo, NETLOAD plugin, cau noi chay (sau $i giay)"
  if (-not (Test-Path "$T\hd\song.json")) { throw 'AutoCAD khong nap duoc plugin trong 3 phut (co hop thoai dang cho?)' }
  Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes
  Add-Type -TypeDefinition @'
using System; using System.Collections.Generic; using System.Runtime.InteropServices; using System.Text;
public static class WinU {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint f);
  delegate bool CB(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, CB cb, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  public static List<string> ChuCon(IntPtr h) {
    List<string> kq = new List<string>();
    EnumChildWindows(h, delegate(IntPtr c, IntPtr l) { StringBuilder sb = new StringBuilder(64); GetWindowText(c, sb, 64); kq.Add(sb.ToString()); return true; }, IntPtr.Zero);
    return kq;
  }
  [DllImport("user32.dll")] static extern bool EnumWindows(CB cb, IntPtr l);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  /// Cua so cap cao dang hien cua tien trinh pid co tieu de bat dau bang dau (UIA RootElement.FindAll bo sot hop thoai modal mo tu kich ban).
  public static IntPtr TimCuaSo(uint pid, string dau) {
    IntPtr kq = IntPtr.Zero;
    EnumWindows(delegate(IntPtr h, IntPtr l) {
      uint p; GetWindowThreadProcessId(h, out p);
      if (p != pid || !IsWindowVisible(h)) return true;
      StringBuilder sb = new StringBuilder(128); GetWindowText(h, sb, 128);
      if (sb.ToString().StartsWith(dau)) { kq = h; return false; }
      return true; }, IntPtr.Zero);
    return kq;
  }
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, string l);
  [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, int m, IntPtr w, IntPtr l);
  /// Cua so con dau tien co ten lop chua lop va chu bat dau bang chu (UIA khong thay o con cua hop thoai nay).
  public static IntPtr Con(IntPtr h, string lop, string chu) {
    IntPtr kq = IntPtr.Zero;
    EnumChildWindows(h, delegate(IntPtr c, IntPtr l) {
      StringBuilder t = new StringBuilder(128), k = new StringBuilder(128); GetWindowText(c, t, 128); GetClassName(c, k, 128);
      if (k.ToString().Contains(lop) && t.ToString().StartsWith(chu)) { kq = c; return false; }
      return true; }, IntPtr.Zero);
    return kq;
  }
  public static void DatChu(IntPtr h, string s) { SendMessage(h, 0x000C, IntPtr.Zero, s); } // WM_SETTEXT: o tim nhan EN_CHANGE, bang loc lai
  public static void Bam(IntPtr h) { PostMessage(h, 0x00F5, IntPtr.Zero, IntPtr.Zero); } // BM_CLICK
}
'@
  $AE = [Windows.Automation.AutomationElement]; $TS = [Windows.Automation.TreeScope]
  $soBlock = @((Get-Content -Raw -Encoding UTF8 "$GOC\data\vat_tu.json" | ConvertFrom-Json).vat_tu | Where-Object { $_.loai -eq 'block' }).Count

  # Go ? o dau nhac ma (kich ban khoi dong: PCCC_DAT ?): mo bang chon ui/BangChonVatTu.cs thay cho in dong lenh. Kich ban chi nap o dau nhac dong lenh
  # nen dung cho toi khi bang dong; sau do tu dap 2 diem dat + Enter. Khong goi lenh qua COM: AutoCAD ban thi tu choi (RPC_E_CALL_REJECTED), ket qua chap chon.
  $hop = $null
  for ($i = 0; $i -lt 120 -and -not $hop; $i++) {
    Start-Sleep -Milliseconds 500
    $hw = [WinU]::TimCuaSo([uint32]$acad.Id, 'TKPCCC - M')
    if ($hw -ne [IntPtr]::Zero) { $hop = $AE::FromHandle($hw) }
  }
  function SoDong($e) { foreach ($c in [WinU]::ChuCon([IntPtr]$e.Current.NativeWindowHandle)) { if ($c -match '^V.t t.: (\d+)$') { return [int]$Matches[1] } }; -1 }
  Kiem $hop "PCCC_DAT go ? mo bang chon '$(if ($hop) { $hop.Current.Name })'"
  if ($hop) {
    $n0 = SoDong $hop
    Kiem ($n0 -eq $soBlock) "bang chon: $n0 dong (can $soBlock block)"
    $o = [WinU]::Con($hw, 'EDIT', '')
    Kiem ($o -ne [IntPtr]::Zero) 'bang chon co o tim'
    [WinU]::DatChu($o, 'dau bao khoi')
    Start-Sleep -Milliseconds 500
    $n1 = SoDong $hop
    Kiem ($n1 -ge 2 -and $n1 -lt $n0) "bang chon loc khong dau 'dau bao khoi': $n1 dong"
    $h = [IntPtr]$hop.Current.NativeWindowHandle; $r = $hop.Current.BoundingRectangle
    $bmp = New-Object Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
    $g = [Drawing.Graphics]::FromImage($bmp); $dc = $g.GetHdc(); [void][WinU]::PrintWindow($h, $dc, 2); $g.ReleaseHdc($dc); $g.Dispose()
    $bmp.Save("$T\bang_chon.png"); $bmp.Dispose()
    $nut = [WinU]::Con($hw, 'BUTTON', 'Ch')
    Kiem ($nut -ne [IntPtr]::Zero) 'bang chon co nut Chon'
    [WinU]::Bam($nut)
  }
  Start-Sleep -Seconds 8   # cho PCCC_BLOCK trong kich ban khoi dong chay xong

  $psi = New-Object Diagnostics.ProcessStartInfo "$GOC\cad\bin\TKPCCC.Mcp.exe"
  $psi.UseShellExecute = $false; $psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true
  $psi.StandardOutputEncoding = [Text.Encoding]::UTF8; $psi.CreateNoWindow = $true
  $mcp = [Diagnostics.Process]::Start($psi)
  function Gui($o) {
    $b = [Text.Encoding]::UTF8.GetBytes(($o | ConvertTo-Json -Depth 20 -Compress) + "`n")
    $mcp.StandardInput.BaseStream.Write($b, 0, $b.Length); $mcp.StandardInput.BaseStream.Flush()
  }
  function Nhan($ms) {
    $t = $mcp.StandardOutput.ReadLineAsync()
    if (-not $t.Wait($ms)) { throw 'May chu MCP khong tra loi' }
    $t.Result | ConvertFrom-Json
  }
  function GoiCongCu($id, $ten, $ts) {
    Gui @{ jsonrpc = '2.0'; id = $id; method = 'tools/call'; params = @{ name = $ten; arguments = $ts } }
    (Nhan 150000).result
  }
  Gui @{ jsonrpc = '2.0'; id = 1; method = 'initialize'; params = @{ protocolVersion = '2025-06-18'; capabilities = @{}; clientInfo = @{ name = 'thu_gui'; version = '0' } } }
  [void](Nhan 15000)

  $r = GoiCongCu 2 'cad_tt' @{}
  $tt = if ($r.isError) { $null } else { $r.content[0].text | ConvertFrom-Json }
  Kiem ($tt -and $tt.ok -eq $true -and $tt.ban_ve -match '\.dwg$') "cad_tt qua cau noi that: $(if ($tt) { $tt.ban_ve } else { $r.content[0].text })"

  $r = GoiCongCu 3 'cad_chay' @{ ops = @(
      @{ op = 'pdf'; file = "$GOC\cad\test\mau_kt.pdf"; trang = 1; tl = 200; p = @(200000, 0) },
      @{ op = 'rai'; ma = 'BC_KHOI'; hop = @(0, 0, 6500, 4500); s = 4000 },
      @{ op = 'tuyen'; ma = 'DAY_BC'; kieu = 'noi_tiep' },
      @{ op = 'anh'; hop = @(-1000, -1000, 7500, 5500); rong = 800 }) }
  $k = if ($r.isError) { $null } else { $r.content[0].text | ConvertFrom-Json }
  Kiem ($k -and $k.ok -eq $true -and $k.pdf[0].n -ge 20 -and $k.rai[0].n -eq 4 -and $k.tuyen[0].doan -eq 1) "cad_chay: pdf + rai 4 dau bao + tuyen: $($r.content[0].text)"
  $da = DocAnh ([string]@($k.anh)[0])
  Kiem ($da.rong -eq 800 -and $da.mau -gt 1) "anh trong goi cad_chay: PNG 800 px co net (-PLOT trong AutoCAD co giao dien), thay rong $($da.rong)"

  $r = GoiCongCu 4 'cad_anh' @{ rong = 640 }
  $k2 = if ($r.isError) { $null } else { $r.content[0].text | ConvertFrom-Json }
  $da2 = DocAnh ([string]@($k2.anh)[0])
  Kiem ($da2.rong -eq 640 -and $da2.mau -gt 1) "cad_anh toan ban ve: PNG 640 px co net: $($r.content[0].text)"

  # Palette chinh (lenh PCCC; PCCC_BLOCK, PCCC_HELP mo dung phan he): tim khung theo ten trong cua so AutoCAD (UI Automation), roi doc chu cua MOI cua so con
  # bang EnumChildWindows (ca trang dang an): "The: n" (Quy trinh ve), "Block: n" (Thu vien), "Lenh: n" (Lenh). DataGridView trong AutoCAD chi hien la "Pane".
  # Chup anh palette bang PrintWindow (ca khi bi che).
  $dk = New-Object Windows.Automation.AndCondition(
    (New-Object Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $acad.Id)),
    (New-Object Windows.Automation.PropertyCondition($AE::NameProperty, 'TKPCCC - PCCC')))
  $khung = $AE::RootElement.FindFirst($TS::Descendants, $dk)
  $so = @{}
  if ($khung) {
    foreach ($c in [WinU]::ChuCon([IntPtr]$khung.Current.NativeWindowHandle)) { if ($c -match '^(The|Block|Lenh|BoTri|TienIch): (\d+)$') { $so[$Matches[1]] = [int]$Matches[2] } }
  }
  Kiem ($khung -and -not $khung.Current.IsOffscreen) "lenh PCCC mo palette 'TKPCCC - PCCC'"
  Kiem ($so['The'] -eq 8) "Quy trinh ve: $($so['The']) the (can 8: 6 buoc + ADIM + CDIM)"
  Kiem ($so['Block'] -eq $soBlock) "PCCC_BLOCK mo phan he Thu vien: $($so['Block']) dong block (can $soBlock)"
  $soLenh = [regex]::Matches((Get-Content -Raw -Encoding UTF8 "$GOC\cad\module\plugin\Lenh.cs"), 'new\[\] \{ "PCCC').Count
  Kiem ($so['Lenh'] -eq $soLenh) "phim tat PHX mo PCCC_HELP (phan he Lenh): $($so['Lenh']) lenh (can $soLenh)"
  # PCCC_BOTRI (phan he Bo tri BC): moi dong co tri so trong khoang_cach_rai.json la mot lua chon thiet bi.
  $soTb = @((Get-Content -Raw -Encoding UTF8 "$GOC\data\khoang_cach_rai.json" | ConvertFrom-Json).thiet_bi).Count
  # PCCC_TIENICH: luoi the tien ich (BangTienIch.THE), nhom "Tat ca", o tim trong.
  $soTi = [regex]::Matches((Get-Content -Raw -Encoding UTF8 "$GOC\cad\module\ui\BangTienIch.cs"), 'new\[\] \{ "\d", "').Count
  Kiem ($so['TienIch'] -eq $soTi) "PCCC_TIENICH mo phan he Tien ich: $($so['TienIch']) the (can $soTi)"
  Kiem ($so['BoTri'] -eq $soTb) "PCCC_BOTRI mo (phan he Bo tri BC): $($so['BoTri']) thiet bi (can $soTb)"
  if ($khung) {
    $h = [IntPtr]$khung.Current.NativeWindowHandle; $r = $khung.Current.BoundingRectangle
    if ($h -ne [IntPtr]::Zero -and $r.Width -ge 10) {
      $bmp = New-Object Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
      $g = [Drawing.Graphics]::FromImage($bmp); $dc = $g.GetHdc()
      $ok = [WinU]::PrintWindow($h, $dc, 2)
      $g.ReleaseHdc($dc); $g.Dispose(); $bmp.Save("$T\palette_pccc.png"); $bmp.Dispose()
      Kiem $ok "chup anh $T\palette_pccc.png"
    }
  }
  $anh = @(Get-ChildItem "$T\b\anh_block\*.png" -ErrorAction SilentlyContinue)
  Kiem ($anh.Count -eq $soBlock) "anh xem truoc: $($anh.Count) file (can $soBlock)"
  if ($anh.Count) {
    $bmp = New-Object Drawing.Bitmap $anh[0].FullName; $p = $bmp.GetPixel(0, 0); $bmp.Dispose()
    Kiem ($p.R -eq 0 -and $p.G -eq 0 -and $p.B -eq 0) "anh xem truoc nen den nhu man hinh CAD ($($anh[0].Name) goc = $p)"
  }

  $r = GoiCongCu 5 'cad_chay' @{ ops = @(@{ op = 'dem' }) }
  $k3 = if ($r.isError) { $null } else { $r.content[0].text | ConvertFrom-Json }
  Kiem ($k3 -and $k3.dem.BC_KHOI -eq 6) "nut Chon nhan BC_KHOI (dong dau sau loc), PCCC_DAT dat 2 diem: BC_KHOI = $(if ($k3) { $k3.dem.BC_KHOI }) (can 4 rai MCP + 2)"
}
finally {
  if ($mcp) { try { $mcp.StandardInput.Close() } catch { }; if (-not $mcp.WaitForExit(5000)) { $mcp.Kill() } }
  # AutoCAD rieng cua lan thu: tat han, khong luu Drawing1.
  if (-not $acad.HasExited) { Stop-Process -Id $acad.Id -Force }
}
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
