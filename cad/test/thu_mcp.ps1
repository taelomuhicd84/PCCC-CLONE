# Kiem thu may chu MCP khong can mo AutoCAD: build Mcp.exe + plugin ra thu muc tam, gui initialize / tools/list / tools/call
# qua stdin, dong vai plugin bang accoreconsole (chay PCCC_SPEC tren viec lay tu hang doi). File chi dung chu ASCII.
# Chay: powershell -ExecutionPolicy Bypass -File cad\test\thu_mcp.ps1
$ErrorActionPreference = 'Stop'
$ACAD = if ($env:ACAD_DIR) { $env:ACAD_DIR } else { 'C:\Program Files\Autodesk\AutoCAD 2021' }
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$T = Join-Path $env:TEMP ('tkpccc_mcp_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $T | Out-Null

& "$FW\csc.exe" /nologo /target:exe /codepage:65001 /out:"$T\TKPCCC.Mcp.exe" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll `
  "$GOC\cad\module\mcp\Mcp.cs" "$GOC\cad\module\chung\HangDoi.cs"
if ($LASTEXITCODE) { throw 'Bien dich Mcp loi' }
& "$FW\csc.exe" /nologo /target:library /platform:x64 /codepage:65001 /out:"$T\TKPCCC.Cad.dll" `
  /r:"$ACAD\acdbmgd.dll" /r:"$ACAD\accoremgd.dll" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  "$GOC\cad\module\plugin\*.cs" "$GOC\cad\module\chung\HangDoi.cs"
if ($LASTEXITCODE) { throw 'Bien dich plugin loi' }
Copy-Item "$GOC\data\vat_tu.json", "$GOC\cad\skills\TU_VUNG.md" $T

# Hang doi rieng cho lan thu: ca Mcp.exe lan accoreconsole deu doc bien moi truong nay.
$env:TKPCCC_CAD_HANG_DOI = "$T\hd"
'vao', 'dang', 'ra' | ForEach-Object { New-Item -ItemType Directory "$T\hd\$_" | Out-Null }

$psi = New-Object Diagnostics.ProcessStartInfo "$T\TKPCCC.Mcp.exe"
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
$loi = 0
function Kiem($dk, $ten) { if ($dk) { "DAT   $ten" } else { "LOI   $ten"; $script:loi++ } }

try {
  Gui @{ jsonrpc = '2.0'; id = 1; method = 'initialize'; params = @{ protocolVersion = '2025-06-18'; capabilities = @{}; clientInfo = @{ name = 'thu'; version = '0' } } }
  $r = Nhan 15000
  Kiem ($r.result.serverInfo.name -eq 'tkpccc-cad' -and $r.result.protocolVersion -eq '2025-06-18') 'initialize'
  $hd = [string]$r.result.instructions
  Kiem ($hd.Length -gt 200 -and $hd.Length -le 1900 -and $hd -match 'luoi_truc') "huong dan tom tat duoi 2 KB ($($hd.Length) ky tu)"
  Gui @{ jsonrpc = '2.0'; method = 'notifications/initialized' }
  Gui @{ jsonrpc = '2.0'; id = 2; method = 'tools/list' }
  $r = Nhan 15000
  Kiem (((@($r.result.tools) | ForEach-Object { $_.name }) -join ',') -eq 'cad_tt,cad_chay,cad_anh') 'tools/list co cad_tt, cad_chay, cad_anh'

  # Chua co song.json: phai bao ngay la AutoCAD chua mo, khong treo.
  Gui @{ jsonrpc = '2.0'; id = 3; method = 'tools/call'; params = @{ name = 'cad_chay'; arguments = @{ ops = @(@{ op = 'dem' }) } } }
  $r = Nhan 15000
  Kiem ($r.result.isError -eq $true -and $r.result.content[0].text -match 'AutoCAD') 'bao AutoCAD chua mo khi khong co nhip song'

  # Gia lam plugin con song, gui viec that, roi chay viec do bang accoreconsole nhu plugin se lam.
  '{"luc":"' + (Get-Date).ToUniversalTime().ToString('o') + '","ban_ve":"thu.dwg"}' | Set-Content -Encoding ascii "$T\hd\song.json"
  Gui @{ jsonrpc = '2.0'; id = 4; method = 'tools/call'; params = @{ name = 'cad_chay'; arguments = @{ ops = @(@{ op = 'luoi_truc'; x = @(3600, 3600); y = @(3600) }) } } }
  $viec = $null
  for ($i = 0; $i -lt 100 -and -not $viec; $i++) { Start-Sleep -Milliseconds 100; $viec = Get-ChildItem "$T\hd\vao\*.json" | Select-Object -First 1 }
  Kiem ($null -ne $viec) 'Mcp.exe ghi viec vao hang doi'
  if ($viec) {
    Move-Item $viec.FullName "$T\hd\dang\"
    @('INSUNITS', '4', 'NETLOAD', "`"$T\TKPCCC.Cad.dll`"", 'PCCC_SPEC', "$T\hd\dang\$($viec.Name)") -join "`r`n" | Set-Content -Encoding ascii "$T\kb.scr"
    $p = Start-Process "$ACAD\accoreconsole.exe" -ArgumentList "/s `"$T\kb.scr`" /l en-US" -WorkingDirectory $T -NoNewWindow -PassThru -RedirectStandardOutput "$T\kb.out.txt"
    if (-not $p.WaitForExit(120000)) { Stop-Process -Id $p.Id -Force }
    $r = Nhan 60000
    $kq = $r.result.content[0].text | ConvertFrom-Json
    Kiem ($r.result.isError -eq $false -and $kq.ok -eq $true -and $kq.ve -eq 29) "cad_chay tra ket qua cua plugin (ve = $($kq.ve), can 29)"
    Kiem (-not (Test-Path "$T\hd\dang\$($viec.Name)") -and -not (Test-Path "$T\hd\ra\$($viec.Name)")) 'hang doi don sach sau khi xong'
  }

  # cad_anh: Mcp.exe phai goi thanh goi {"ops":[{"op":"anh",...}]}. Thao tac "anh" that kiem o chay_thu.ps1;
  # o day dua chinh file viec sang "ra" lam ket qua gia de khong phai mo accoreconsole lan nua.
  '{"luc":"' + (Get-Date).ToUniversalTime().ToString('o') + '","ban_ve":"thu.dwg"}' | Set-Content -Encoding ascii "$T\hd\song.json"
  Gui @{ jsonrpc = '2.0'; id = 5; method = 'tools/call'; params = @{ name = 'cad_anh'; arguments = @{ hop = @(0, 0, 100, 100); rong = 800 } } }
  $viec = $null
  for ($i = 0; $i -lt 100 -and -not $viec; $i++) { Start-Sleep -Milliseconds 100; $viec = Get-ChildItem "$T\hd\vao\*.json" | Select-Object -First 1 }
  $g = if ($viec) { Get-Content -Raw -Encoding UTF8 $viec.FullName | ConvertFrom-Json }
  Kiem ($g -and $g.ops[0].op -eq 'anh' -and $g.ops[0].rong -eq 800 -and @($g.ops[0].hop).Count -eq 4) 'cad_anh ghi goi {op: anh, hop, rong} vao hang doi'
  if ($viec) {
    Move-Item $viec.FullName "$T\hd\ra\"
    $r = Nhan 15000
    Kiem ($r.result.isError -eq $false -and $r.result.content[0].text -match '"anh"') 'cad_anh tra ket qua cua plugin'
  }
}
finally { $mcp.StandardInput.Close(); if (-not $mcp.WaitForExit(5000)) { $mcp.Kill() } }
"Thu muc thu: $T"
if ($loi) { exit 1 } else { 'TAT CA DAT' }
