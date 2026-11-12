# Chay mot goi spec bang accoreconsole, KHONG can mo AutoCAD: vao ban ve co san (-Dwg) hoac tao ban ve moi, luu lai, in ket qua.
# Dung ban build trong bundle (chay cad\build.bat truoc). Anh cua thao tac "anh" duoc chep ve <thu muc spec>\anh\.
# Dung: powershell -ExecutionPolicy Bypass -File chay_spec.ps1 -Spec <spec.json> [-Dwg <ban_ve.dwg>]
# Bo -Dwg thi ban ve la <ten spec>.dwg canh file spec. File chi dung chu ASCII.
param([Parameter(Mandatory = $true)][string]$Spec, [string]$Dwg)
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$GOC = (Resolve-Path "$PSScriptRoot\..\..\..\..\..").Path
$DLL = if ($env:TKPCCC_CAD_DLL) { $env:TKPCCC_CAD_DLL } else { "$GOC\cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll" }  # bien moi truong: cho test dung ban build tam
if (-not (Test-Path $DLL)) { throw "Thieu $DLL - chay cad\build.bat truoc" }
$Spec = (Resolve-Path $Spec).Path
$thuMuc = Split-Path -Parent $Spec
if (-not $Dwg) { $Dwg = [IO.Path]::ChangeExtension($Spec, '.dwg') }
$Dwg = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Dwg)  # duong dan tuong doi tinh theo $PWD, khong theo thu muc cua tien trinh
$T = Join-Path $env:TEMP ('tkpccc_spec_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null
$env:TKPCCC_CAD_HANG_DOI = "$T\hd"   # anh in ra thu muc tam (plugin tu don anh cu), xong moi chep ve canh spec
$fKq = [IO.Path]::ChangeExtension($Spec, '.kq.json')
Remove-Item $fKq -ErrorAction SilentlyContinue
# kb.scr ghi bang ASCII: duong dan co dau (vd "Nha A" co dau) se hong. Lam tren ban chep trong $T (ten ASCII) roi chep ve.
# Giu ten DWG neu la ASCII: spec co the khoa "ban_ve" theo ten file.
$tenDwg = [IO.Path]::GetFileName($Dwg); if ($tenDwg -match '[^ -~]') { $tenDwg = 'ban_ve.dwg' }
$dwgT = "$T\$tenDwg"; $specT = "$T\spec.json"
Copy-Item -LiteralPath $Spec $specT
if (Test-Path -LiteralPath $Dwg) {
  Copy-Item -LiteralPath $Dwg $dwgT
  $kb = @('NETLOAD', "`"$DLL`"", 'PCCC_SPEC', $specT, '_.QSAVE'); $mo = "/i `"$dwgT`" "
} else {
  $kb = @('INSUNITS', '4', 'NETLOAD', "`"$DLL`"", '_.SAVEAS', '2018', "`"$dwgT`"", 'PCCC_SPEC', $specT, '_.QSAVE'); $mo = ''
}
$kb -join "`r`n" | Set-Content -Encoding ascii "$T\kb.scr"
$p = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "$mo/s `"$T\kb.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb.out.txt"
if (-not $p.WaitForExit(300000)) { Stop-Process -Id $p.Id -Force; throw "accoreconsole qua 5 phut: $T\kb.out.txt" }
if (-not (Test-Path "$T\spec.kq.json")) { throw "PCCC_SPEC khong ghi ket qua, xem $T\kb.out.txt" }
if (-not (Test-Path $dwgT)) { throw "Khong luu duoc ban ve, xem $T\kb.out.txt" }
Copy-Item -LiteralPath "$T\spec.kq.json" -Destination $fKq -Force
Copy-Item $dwgT -Destination $Dwg -Force   # DWG dang mo o AutoCAD khac thi bao loi o day, khong im lang
$kq = Get-Content -Raw -Encoding UTF8 $fKq | ConvertFrom-Json
$i = 0
foreach ($a in @($kq.anh)) {
  if (-not $a -or -not (Test-Path $a)) { continue }
  New-Item -ItemType Directory "$thuMuc\anh" -Force | Out-Null
  $dich = "$thuMuc\anh\$([IO.Path]::GetFileNameWithoutExtension($Spec))_$((++$i)).png"
  Copy-Item $a $dich -Force
  "ANH: $dich"
}
"DWG: $Dwg"
Get-Content -Raw -Encoding UTF8 $fKq
