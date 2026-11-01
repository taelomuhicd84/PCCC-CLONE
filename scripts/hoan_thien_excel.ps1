# Hoàn thiện file Excel thủy lực đã xuất: hiện dòng có số liệu, ẩn dòng trống hoặc ra #N/A (bảng nhánh 62–91, khối ống chính tới "Tổng lưu lượng: Q =").
# Chạy sau xuat_thuy_luc_excel.py (và chen_hang_cuoi.ps1 nếu có). Dùng: powershell -NoProfile -File scripts/hoan_thien_excel.ps1 <file.xlsx>
param([string]$f)
[Console]::OutputEncoding = [Text.Encoding]::UTF8; $ErrorActionPreference = 'Stop'
$x = New-Object -ComObject Excel.Application; $x.Visible = $false; $x.DisplayAlerts = $false
try {
  $w = $x.Workbooks.Open($f); $s = $w.Worksheets.Item('SP chung voi HR'); $x.CalculateFull()
  $an = 0; $hien = 0
  function trong($r) { "$($s.Range("F$r").Text)".Trim() -eq '' -or (('D', 'G', 'H', 'J', 'L') | ForEach-Object { $s.Range("$_$r").Text }) -join '' -match '#N/A' }
  function khoi($tu, $den, $giu) { # $giu: ẩn hay hiện; dòng hiện lại lấy chiều cao khối mẫu 65–67
    for ($i = $tu; $i -le $den; $i++) {
      if ($giu) { if ($s.Rows($i).Hidden -or $s.Rows($i).RowHeight -eq 0) { $s.Rows($i).RowHeight = $s.Rows(65 + (($i - $tu) % 3)).RowHeight; $script:hien++ } }
      elseif (-not $s.Rows($i).Hidden) { $s.Rows($i).Hidden = $true; $script:an++ }
    }
  }
  # Bảng nhánh: 5 đầu trái (62–76), 5 ô phải (77–91), mỗi đầu 3 dòng; dòng 63 (Z1) mẫu để ẩn, giữ nguyên
  foreach ($b in 65, 68, 71, 74, 77, 80, 83, 86, 89) { khoi $b ($b + 2) (-not (trong $b)) }
  if (trong 62) { khoi 62 64 $false }
  # Viền dưới nhánh: dòng hiện cuối của mỗi nhánh nhận viền (viền mẫu nằm ở dòng đã ẩn), cột A gộp lại tới dòng đó
  foreach ($nh in @(@(62, 76), @(77, 91))) {
    $vis = @($nh[0]..$nh[1] | Where-Object { -not $s.Rows($_).Hidden -and $s.Rows($_).RowHeight -gt 0 })
    if ($vis.Count -eq 0) { continue }
    $d = $nh[0]; $c = $vis[-1]; $o = $s.Range("A$d")
    if ($o.MergeArea.Rows.Count -gt 1) { $o.MergeArea.UnMerge() | Out-Null }
    $kh = $s.Range("A$($d):A$c"); $kh.Merge() | Out-Null; $kh.VerticalAlignment = -4108; $kh.HorizontalAlignment = -4108
    foreach ($b in 7, 8, 9, 10) { $kh.Borders($b).LineStyle = 1; $kh.Borders($b).Weight = 2 }
    $s.Range("B$($c):N$c").Borders(9).LineStyle = 1; $s.Range("B$($c):N$c").Borders(9).Weight = 2
  }
  # D98 quyết định (mẫu ghi ở P92, P99): "Đối xứng với nhánh phải" thì ẨN 92–96 (cân bằng bất đối xứng), HIỆN 99–100; "Không đối xứng với nhánh phải" thì ẨN 99–100, HIỆN 92–96. Dòng 97 (áp tại nút A) dùng chung nên luôn hiện.
  $dx = "$($s.Range('D98').Text)" -eq 'Đối xứng với nhánh phải'
  foreach ($i in 92..96) { if ($dx) { $s.Rows($i).Hidden = $true } else { $s.Rows($i).Hidden = $false; if ($s.Rows($i).RowHeight -lt 5) { $s.Rows($i).RowHeight = 23.5 } } }
  foreach ($i in 99..100) { if ($dx) { $s.Rows($i).Hidden = $false; if ($s.Rows($i).RowHeight -lt 5) { $s.Rows($i).RowHeight = 23.5 } } else { $s.Rows($i).Hidden = $true } }
  # Ống chính: khối từ dòng đoạn "X-Y" (B) tới trước đoạn sau; đoạn không có DN hoặc ra #N/A thì ẩn cả khối
  $cuoi = $s.UsedRange.Find('Tổng lưu lượng: Q =').Row
  $dau = @(101..($cuoi - 1) | Where-Object { "$($s.Range("B$_").Text)" -match '^[A-J]-[A-J]$' })
  for ($k = 0; $k -lt $dau.Count; $k++) {
    $den = $(if ($k + 1 -lt $dau.Count) { $dau[$k + 1] - 1 } else { $cuoi - 1 })
    $tr = trong $dau[$k]
    for ($i = $dau[$k]; $i -le $den; $i++) { if ($tr -and -not $s.Rows($i).Hidden) { $s.Rows($i).Hidden = $true; $an++ } elseif (-not $tr -and $s.Rows($i).Hidden) { $s.Rows($i).Hidden = $false; $hien++ } } # mẫu đã ẩn khối thì hiện lại khi có số liệu
  }
  # Khối nút chuẩn (đoạn 3 dòng + 5 dòng cân bằng): chiều cao và hình equation Q = √(Bp·P) như khối nút B (dòng 104–108)
  $mau = @($s.Shapes) | Where-Object { $_.TopLeftCell.Row -eq 105 -and $_.TopLeftCell.Column -eq 4 } | Select-Object -First 1
  for ($k = 1; $k -lt $dau.Count; $k++) {
    $d0 = $dau[$k]; $den = $(if ($k + 1 -lt $dau.Count) { $dau[$k + 1] - 1 } else { $cuoi - 1 })
    if ($s.Rows($d0).Hidden -or $den - $d0 -lt 7 -or $den - $d0 -gt 8) { continue } # khối có dòng chèn hàng cuối đã chỉnh riêng
    for ($i = 0; $i -le 4; $i++) { $s.Rows($d0 + 3 + $i).RowHeight = $s.Rows(104 + $i).RowHeight }
    if ($mau) { foreach ($sh in @($s.Shapes)) { $t = $sh.TopLeftCell.Row; if ($t -ge $d0 + 3 -and $t -le $d0 + 7 -and $sh.TopLeftCell.Column -eq 4) { $sh.Height = $mau.Height; $sh.Width = $mau.Width; $sh.Top = $s.Rows($d0 + 5).Top + ($mau.Top - $s.Rows(106).Top) } } }
  }
  $w.Save(); $w.Close($false)
  "Hoàn thiện: ẩn $an dòng trống/#N/A, hiện lại $hien dòng có số liệu"
} finally { $x.Quit() | Out-Null; [Runtime.InteropServices.Marshal]::ReleaseComObject($x) | Out-Null }
