# In 1 ban ve DWG ra PNG (model space, Extents, Fit, 1600x1280) bang accoreconsole de xem bang mat. File chi dung chu ASCII.
# Dung: powershell -ExecutionPolicy Bypass -File in_png.ps1 <ban_ve.dwg> <ra.png>
param([Parameter(Mandatory = $true)][string]$Dwg, [Parameter(Mandatory = $true)][string]$Png)
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$Dwg = (Resolve-Path $Dwg).Path
$Png = [IO.Path]::GetFullPath($Png)
$T = Join-Path $env:TEMP ('tkpccc_in_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null
# In tu ban sao: khong khoa (.dwl) hay luu nham vao file goc; plot.log nam trong thu muc tam.
Copy-Item $Dwg "$T\ban_ve.dwg"
@('-PLOT', 'Y', 'Model', 'PublishToWeb PNG.pc3', 'Sun Hi-Res (1600.00 x 1280.00 Pixels)', 'L', 'N', 'E', 'F', 'C', 'Y', '.', 'Y', '',
  "`"$Png`"", 'N', 'Y') -join "`r`n" | Set-Content -Encoding ascii "$T\in.scr"
$p = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/i `"$T\ban_ve.dwg`" /s `"$T\in.scr`" /l en-US" -WorkingDirectory $T `
  -NoNewWindow -PassThru -RedirectStandardOutput "$T\in.out.txt"
if (-not $p.WaitForExit(120000)) { Stop-Process -Id $p.Id -Force; throw "accoreconsole qua 2 phut: $T\in.out.txt" }
if (-not (Test-Path $Png)) { throw "Khong tao duoc PNG, xem $T\in.out.txt" }
"Da in: $Png"
