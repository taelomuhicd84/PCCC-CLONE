# Chèn dòng tính hàng cuối thiếu đầu (tập huấn C07) ngay tại nút hàng cuối của file Excel thủy lực đã xuất, theo đúng khối hàng I của mẫu
# (bảng nhánh dòng 62–91 có ghi chú đặc tính nhánh ở M/N, phần cân bằng nút dòng 93–97).
# Dùng: powershell -NoProfile -File scripts/chen_hang_cuoi.ps1 <file.xlsx> <dòng Bp1 của nút hàng cuối, vd 113> <trái "32:2.43;..."> [<phải "25:3;25:0.57">]
# Đoạn ống ghi DN:L[:nút bản vẽ] từ đầu xa nút tới nút. Excel tự dời tham chiếu các dòng bên dưới khi chèn. File lưu UTF-8 có BOM (PowerShell 5 đọc tiếng Việt).
param([string]$f, [int]$j, [string]$trai, [string]$phai = '')
[Console]::OutputEncoding = [Text.Encoding]::UTF8; $ErrorActionPreference = 'Stop'
function doan($t) { $kq = New-Object System.Collections.ArrayList; if ($t) { foreach ($p in $t.Split(';')) { $a = $p.Split(':'); [void]$kq.Add(@([double]$a[0], [double]$a[1], $(if ($a.Count -gt 2) { $a[2] } else { '' }))) } }; return ,$kq }
$x = New-Object -ComObject Excel.Application; $x.Visible = $false; $x.DisplayAlerts = $false
try {
  $w = $x.Workbooks.Open($f); $s = $w.Worksheets.Item('SP chung voi HR')
  $chi = $(if ($j -eq 154) { 6 } else { ($j - 105) / 8 }); $nd = [string][char](66 + $chi) # nút hàng cuối: B, C, D…
  $la = @('I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII')[$chi + 1]
  $r = $j - 1; $dau0 = $r; $nut = @(); $tong = 0 # bảng nhánh chèn trước dòng "Cân bằng lưu lượng tại nút …"
  function chen($mau, $n) {
    $s.Rows($mau).Copy() | Out-Null; $s.Rows("$($script:r):$($script:r + $n - 1)").Insert(-4121) | Out-Null
    foreach ($c in $s.Range("A$($script:r):T$($script:r + $n - 1)").Cells) { $c.MergeArea.ClearContents() | Out-Null } # ô gộp: xóa cả vùng gộp
  }
  function rt($o, $ds) { # chữ có chỉ số: $ds = @(@('Q',''), @('C','d'), @('2','u'), ...) d = dưới, u = trên
    $c = $s.Range($o); $c.Value2 = ($ds | ForEach-Object { $_[0] }) -join ''; $p = 1
    foreach ($q in $ds) { $n = $q[0].Length; if ($q[1] -eq 'd') { $c.Characters($p, $n).Font.Subscript = $true } elseif ($q[1] -eq 'u') { $c.Characters($p, $n).Font.Superscript = $true }; $p += $n }
  }
  function nhan($o, $t, $tu, $dai) { $s.Range($o).ShrinkToFit = $true; rt $o @(@($t.Substring(0, $tu - 1), ''), @($t.Substring($tu - 1, $dai), 'd'), @($t.Substring($tu - 1 + $dai), '')) }
  foreach ($ben in @(@('trái', (doan $trai)), @('phải', (doan $phai)))) {
    $ten = $ben[0]; $ds = $ben[1]; if ($ds.Count -eq 0) { continue }
    $pa = 0; $dau = $r; $nt = (doan $trai).Count
    for ($k = 1; $k -le $ds.Count; $k++) {
      $cuoi = $k -eq $ds.Count
      chen $(if ($cuoi) { '71:73' } else { '65:67' }) 3; $a = $r; $tong++ # đoạn cuối nhánh lấy kiểu dòng 71–73 (có ô đặc tính nhánh M/N)
      for ($i = 0; $i -le 2; $i++) { $s.Rows($a + $i).RowHeight = $s.Rows(65 + $i).RowHeight } # dòng mẫu có thể đang ẩn: cao như khối 2-3
      # số đầu như mẫu: trái 1 (xa nhất) → n; phải nối tiếp, đánh từ đầu xa nhất (nt+np) về nút (nt+1)
      $h = $(if ($ten -eq 'trái') { $k } else { $nt + $ds.Count - $k + 1 }); $hs = $(if ($ten -eq 'trái') { $h + 1 } else { $h - 1 })
      $den = $(if (-not $cuoi) { [string]$hs } else { $nd }); $dd = $(if ($ds[$k - 1][2]) { $ds[$k - 1][2] } else { "$h-$den" }) # nhãn bản vẽ nếu có
      $s.Range("B$a").NumberFormat = '@'; $s.Range("B$a").Value2 = $dd # dạng chữ: "3-2" không bị đổi thành ngày
      nhan "C$a" "q$h = " 2 "$h".Length; $s.Range("D$a").Formula = "=`$K`$14*L$a^0.5"
      $s.Range("E$a").Value2 = [double]$k; $s.Range("F$a").Value2 = [double]$ds[$k - 1][0]; $s.Range("I$a").Value2 = [double]$ds[$k - 1][1]
      $s.Range("G$a").Formula = "=VLOOKUP(F$a,`$P`$6:`$T`$20,4,0)"; $s.Range("J$a").Formula = "=VLOOKUP(F$a,`$P`$6:`$T`$20,5,0)"
      $s.Range("H$a").Formula = "=4*D$($a + 2)/((1000*3.14*(G$a/1000)^2))"
      nhan "K$a" "P$h = " 2 "$h".Length
      $s.Range("L$a").Formula = $(if ($k -eq 1) { '=$H$15' } else { "=L$pa+L$($pa + 1)" })
      nhan "K$($a + 1)" "P$dd = " 2 $dd.Length; $s.Range("L$($a + 1)").Formula = "=D$($a + 2)^2*I$a/(100*J$a)"
      nhan "C$($a + 2)" "Q$dd = " 2 $dd.Length
      $s.Range("D$($a + 2)").Formula = $(if ($k -eq 1) { "=D$a" } else { "=D$a+D$($pa + 2)" })
      if ($k -eq 1 -and -not $cuoi -and $ten -eq 'trái') { $s.Range("M$a").Value2 = 'áp lực đầu phun 1'; $s.Range("M$($a + 2)").Value2 = "tổn hao trên đoạn $dd"; $s.Range("M$($a + 2)").Font.Bold = $false } # như dòng 62–64 mẫu
      $pa = $a; $r += 3
    }
    $pn = "$nd$la$ten"; nhan "K$($pa + 2)" "P$pn = " 2 $pn.Length # áp tại nút của nhánh, như PAItrái của mẫu
    $s.Range("L$($pa + 2)").Formula = "=L$pa+L$($pa + 1)"
    $s.Range("M$pa").Value2 = "Đặc tính thủy lực nhánh $la bên ${ten}:" # như M71–N73 mẫu
    rt "M$($pa + 1)" @(@('Bp', ''), @("$la$ten", 'd'), @(' = Q', ''), @("$dd", 'd'), @('2', 'u'), @('/P', ''), @($pn, 'd'))
    rt "M$($pa + 2)" @(@('Bp', ''), @("$la$ten", 'd'), @('=', ''))
    $s.Range("N$($pa + 2)").Formula = "=IFERROR(D$($pa + 2)^2/L$($pa + 2),`"`")"
    $kh = $s.Range("A$($dau):A$($r - 1)"); $kh.Merge() | Out-Null
    $kh.Cells(1, 1).Value2 = "$la$ten"; $kh.Cells(1, 1).Characters($la.Length + 1, $ten.Length).Font.Subscript = $true
    $kh.VerticalAlignment = -4108; $kh.HorizontalAlignment = -4108
    foreach ($b in 7, 8, 9, 10) { $kh.Borders($b).LineStyle = 1; $kh.Borders($b).Weight = 2 } # gộp ô làm mất viền: kẻ lại khung như A62
    $s.Range("A$($r - 1):N$($r - 1)").Borders(9).LineStyle = 1 # viền dưới nhánh
    $nut += , @("D$($pa + 2)", "L$($pa + 2)", "$nd$ten")
  }
  $r++ # dòng "Cân bằng lưu lượng tại nút …" của mẫu (đã dời xuống dưới bảng)
  chen '93:97' 5; $r1 = $r; $r2 = $r + 1; $r3 = $r + 2; $r4 = $r + 3; $rp = $r + 4 # câu so áp, Q' quy về PA, Q hàng, Bp, PA (như dòng 93–97)
  foreach ($sh in @($s.Shapes)) { $t = $sh.TopLeftCell.Row; if ($t -ge $dau0 -and $t -le $rp) { $sh.Delete() } } # hình equation của nút A bị chép theo: thay bằng chữ có chỉ số
  $s.Range("D$rp").Formula = '=MAX(' + (($nut | ForEach-Object { $_[1] }) -join ',') + ')'
  rt "B$rp" @(@("Áp tại nút $nd là P", ''), @('A', 'd'), @(' = ', ''))
  $tr = $nut[0]; $ph = $(if ($nut.Count -gt 1) { $nut[1] } else { $null })
  if ($ph) {
    $s.Range("B$r1").NumberFormat = 'General'; $s.Range("B$r1").Formula = "=IF($($tr[1])>=$($ph[1]),`"Áp suất tại nút $nd của nhánh trái lớn hơn nhánh phải: điều chỉnh lưu lượng nhánh phải về áp PA (B.2.11.3)`",`"Áp suất tại nút $nd của nhánh phải lớn hơn nhánh trái: điều chỉnh lưu lượng nhánh trái về áp PA (B.2.11.3)`")"
    $s.Range("F$r2").Formula = "=ROUND(IF($($tr[1])>=$($ph[1]),$($ph[0])*SQRT(D$rp/$($ph[1])),$($tr[0])*SQRT(D$rp/$($tr[1]))),3)"
    $s.Range("F$r3").Formula = "=ROUND(IF($($tr[1])>=$($ph[1]),$($tr[0]),$($ph[0]))+F$r2,3)"
    $x.Calculate()
    if ($s.Range($tr[1]).Value2 -ge $s.Range($ph[1]).Value2) { $cao = $tr; $thap = $ph } else { $cao = $ph; $thap = $tr }
    rt "B$r2" @(@("Q'", ''), @($thap[2], 'd'), @(' = Q', ''), @($thap[2], 'd'), @('·√(P', ''), @('A', 'd'), @('/P', ''), @($thap[2], 'd'), @(') =', ''))
    rt "E$r3" @(@('Q', ''), @($la, 'd'), @(' = Q', ''), @($cao[2], 'd'), @(" + Q'", ''), @($thap[2], 'd'), @(' =', ''))
  } else {
    $s.Range("B$r1").Value2 = "Chỉ có nhánh trái (1 nhánh): Q$la = Q nhánh trái"; $s.Rows($r2).RowHeight = 0 # không có Q' quy đổi
    $s.Range("F$r3").Formula = "=ROUND($($tr[0]),3)"
    rt "E$r3" @(@('Q', ''), @($la, 'd'), @(' = Q', ''), @($tr[2], 'd'), @(' =', ''))
  }
  $s.Range("G$r2").Value2 = 'l/s'; $s.Range("G$r3").Value2 = 'l/s'; $s.Range("E$rp").Value2 = 'Mpa' # đơn vị như G94, G95, E97 mẫu
  $s.Range("F$r2").NumberFormat = $s.Range('F94').NumberFormat
  $s.Range("F$r3").NumberFormat = $s.Range('F95').NumberFormat; $s.Range("E$r3").HorizontalAlignment = -4152 # căn phải, tràn sang D như hình equation D95 mẫu
  $s.Range("B$r3").Value2 = "Lưu lượng hàng ${la}:"
  $s.Range("B$r4").Value2 = "Đặc tính tổng quát hàng ${la}: "
  rt "E$r4" @(@('B', ''), @('p1', 'd'), @(' = Q', ''), @($la, 'd'), @('2', 'u'), @('/P', ''), @('A', 'd'), @(' =', ''))
  $s.Range("G$r4").Formula = "=F$r3^2/D$rp"
  $r = $rp + 1 # dòng Bp1 của nút hàng cuối (mẫu)
  $s.Range("B$r").Formula = ($s.Range("B$r").Formula -replace 'Cấu trúc của (hàng|nhánh) (\w+) giống với (hàng|nhánh) I', 'Hàng $2 tính riêng ở trên')
  $s.Range("J$r").Formula = "=G$r4"
  for ($i = 0; $i -le 3; $i++) { $s.Rows($r + $i).RowHeight = $s.Rows(105 + $i).RowHeight } # dòng Bp, Q, P, tổng của nút: cao như khối nút B mẫu
  $mau = @($s.Shapes) | Where-Object { $_.TopLeftCell.Row -eq 105 -and $_.TopLeftCell.Column -eq 4 } | Select-Object -First 1
  if ($mau) { foreach ($sh in @($s.Shapes)) { $t = $sh.TopLeftCell.Row; if (($t -eq $r -or $t -eq $r + 1) -and $sh.TopLeftCell.Column -eq 4) { $sh.Height = $mau.Height; $sh.Width = $mau.Width; $sh.Top = $s.Rows($r + 1).Top + ($mau.Top - $s.Rows(106).Top) } } } # hình công thức Q = √(Bp·P) của nút: cỡ như nút B
  $w.Save(); $w.Close($false)
  "Đã chèn $($r - $j) dòng tính hàng cuối ($tong đầu) trước dòng $j"
} finally { $x.Quit() | Out-Null; [Runtime.InteropServices.Marshal]::ReleaseComObject($x) | Out-Null }
