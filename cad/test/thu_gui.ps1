# Thu tren AutoCAD CO GIAO DIEN (khong nam trong bo bat buoc): script tu mo mot AutoCAD that khoang 1-2 phut roi tu tat.
# Nap ban build trong bundle (chay cad\build.bat truoc) tu mot thu muc tam, mo bang PCCC_BLOCK, roi goi may chu MCP that
# (cad\bin\TKPCCC.Mcp.exe) qua cau noi: cad_tt, cad_chay (pdf + rai + tuyen + anh), cad_anh. Doc bang block bang UI Automation, khong chup man hinh.
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
Copy-Item "$B\TKPCCC.Cad.dll", "$B\TKPCCC.UI.dll", "$B\vat_tu.json", "$B\ky_hieu.json" "$T\b"
$env:TKPCCC_CAD_HANG_DOI = "$T\hd"   # ca AutoCAD lan Mcp.exe mo tu day deu thua huong bien nay
# Phim tat PHX = PCCC_HELP (phim_tat.txt canh DLL): kich ban go PHX, bang lenh hien ra = phim tat da dang ky va PCCC_HELP mo bang.
'PHX=PCCC_HELP' | Set-Content -Encoding ascii "$T\b\phim_tat.txt"
@('NETLOAD', "`"$T\b\TKPCCC.Cad.dll`"", 'INSUNITS', '4', 'PCCC_BLOCK', 'PHX') -join "`r`n" | Set-Content -Encoding ascii "$T\gui.scr"
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

  # Hai bang (PCCC_BLOCK, PCCC_HELP): tim khung theo ten trong cua so AutoCAD (UI Automation). So dong doc tu chu cua cua so bang
  # (chu cua cua so "Block: n", "Lenh: n", doc bang GetWindowText) vi DataGridView trong AutoCAD chi hien la "Pane". Chup anh tung bang bang PrintWindow (ca khi bi che).
  Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes
  Add-Type -Namespace Win -Name U -MemberDefinition '[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint f); [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);'
  $AE = [Windows.Automation.AutomationElement]; $TS = [Windows.Automation.TreeScope]
  function TimBang($ten) {
    $dk = New-Object Windows.Automation.AndCondition(
      (New-Object Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $acad.Id)),
      (New-Object Windows.Automation.PropertyCondition($AE::NameProperty, $ten)))
    $k = $AE::RootElement.FindFirst($TS::Descendants, $dk)
    $kq = @{ khung = $k; dong = -1; nut = 0 }
    if ($k) {
      foreach ($e in $k.FindAll($TS::Descendants, [Windows.Automation.Condition]::TrueCondition)) {
        $sb = New-Object Text.StringBuilder 64
        [void][Win.U]::GetWindowText([IntPtr]$e.Current.NativeWindowHandle, $sb, 64)
        if ($sb.ToString() -match '^(Block|Lenh): (\d+)$') { $kq.dong = [int]$Matches[2] }
        if ($e.Current.ClassName -match 'BUTTON') { $kq.nut++ }
      }
    }
    $kq
  }
  function ChupBang($k, $file) {
    $h = [IntPtr]$k.Current.NativeWindowHandle; $r = $k.Current.BoundingRectangle
    if ($h -eq [IntPtr]::Zero -or $r.Width -lt 10) { return $false }
    $bmp = New-Object Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
    $g = [Drawing.Graphics]::FromImage($bmp); $dc = $g.GetHdc()
    $ok = [Win.U]::PrintWindow($h, $dc, 2)
    $g.ReleaseHdc($dc); $g.Dispose(); $bmp.Save($file); $bmp.Dispose()
    $ok
  }
  $soBlock = @((Get-Content -Raw -Encoding UTF8 "$GOC\data\vat_tu.json" | ConvertFrom-Json).vat_tu | Where-Object { $_.loai -eq 'block' }).Count
  $bb = TimBang 'TKPCCC - Block PCCC'
  # 5 nut bo goc + 1 o chon "chi block co trong ban ve" (CheckBox cung lop BUTTON)
  Kiem ($bb.khung -and -not $bb.khung.Current.IsOffscreen -and $bb.dong -eq $soBlock -and $bb.nut -eq 6) "bang PCCC_BLOCK hien, $($bb.dong) dong block (can $soBlock), $($bb.nut) nut (can 6)"
  $soLenh = [regex]::Matches((Get-Content -Raw -Encoding UTF8 "$GOC\cad\module\plugin\Lenh.cs"), 'new\[\] \{ "PCCC_').Count
  $bl = TimBang ('TKPCCC - L' + [char]0x1EC7 + 'nh PCCC')
  Kiem ($bl.khung -and -not $bl.khung.Current.IsOffscreen -and $bl.dong -eq $soLenh -and $bl.nut -eq 3) "phim tat PHX mo bang PCCC_HELP: $($bl.dong) lenh (can $soLenh), $($bl.nut) nut (can 3)"
  foreach ($c in @(@($bb, 'panel_block.png'), @($bl, 'panel_lenh.png'))) {
    if ($c[0].khung) { Kiem (ChupBang $c[0].khung "$T\$($c[1])") "chup anh $T\$($c[1])" }
  }
  $soAnh = @(Get-ChildItem "$T\b\anh_block\*.png" -ErrorAction SilentlyContinue).Count
  Kiem ($soAnh -eq $soBlock) "anh xem truoc: $soAnh file (can $soBlock)"
}
finally {
  if ($mcp) { try { $mcp.StandardInput.Close() } catch { }; if (-not $mcp.WaitForExit(5000)) { $mcp.Kill() } }
  # AutoCAD rieng cua lan thu: tat han, khong luu Drawing1.
  if (-not $acad.HasExited) { Stop-Process -Id $acad.Id -Force }
}
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
