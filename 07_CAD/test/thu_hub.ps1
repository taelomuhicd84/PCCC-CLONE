# Kiem thu TKPCCC.Hub.exe tren NHA GIA (khong sua cau hinh may that): build Hub + Mcp ra thu muc tam, chay /cai, /kiem, /go
# roi doc lai file JSON, lien ket bundle. Lenh CLI (claude, agy) o che do nha gia chi ghi ra, khong chay. File chi dung chu ASCII.
# Chay: powershell -ExecutionPolicy Bypass -File 07_CAD\test\thu_hub.ps1
$ErrorActionPreference = 'Stop'
$FW = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$GOC = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$T = Join-Path $env:TEMP ('tkpccc_hub_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$BIN = "$T\cad\bin"
# 17/11: 3 plugin rieng (PCCC, YouTube, Bot chat), moi plugin mot bundle
New-Item -ItemType Directory $BIN, "$T\cad\TKPCCC.bundle\Contents", "$T\cad\TKPCCC_YouTube.bundle\Contents", "$T\cad\TKPCCC_BotChat.bundle\Contents", "$T\cad\module\youtube\webview2" | Out-Null
'gia' | Set-Content "$T\cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll"
'gia' | Set-Content "$T\cad\TKPCCC_YouTube.bundle\Contents\TKPCCC.YouTube.dll"
'gia' | Set-Content "$T\cad\TKPCCC_BotChat.bundle\Contents\TKPCCC.BotChat.dll"
$WV2 = 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll'
foreach ($f in $WV2) { 'gia' | Set-Content "$T\cad\module\youtube\webview2\$f" }

& "$FW\csc.exe" /nologo /target:winexe /codepage:65001 /out:"$BIN\TKPCCC.Hub.exe" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll `
  /r:System.Windows.Forms.dll /r:System.Drawing.dll "$GOC\07_CAD\module\hub\Hub.cs" "$GOC\07_CAD\module\chung\HangDoi.cs"
if ($LASTEXITCODE) { throw 'Bien dich Hub loi' }
& "$FW\csc.exe" /nologo /target:exe /codepage:65001 /out:"$BIN\TKPCCC.Mcp.exe" /r:"$FW\System.Web.Extensions.dll" /r:System.Core.dll `
  "$GOC\07_CAD\module\mcp\Mcp.cs" "$GOC\07_CAD\module\chung\HangDoi.cs"
if ($LASTEXITCODE) { throw 'Bien dich Mcp loi' }
Copy-Item "$GOC\07_CAD\skills\TU_VUNG.md" $BIN

# Nha gia: Claude Desktop co san 1 may chu khac + khoa khac (chuoi co ngoac kep, gach cheo, chu Viet), Gemini co khoa dang nhap.
$NHA = "$T\nha"
$DESK = "$NHA\AppData\Roaming\Claude\claude_desktop_config.json"
$GEM = "$NHA\.gemini\settings.json"
New-Item -ItemType Directory (Split-Path $DESK), (Split-Path $GEM) | Out-Null
$VIET = 'Vi' + [char]0x1EC7 + 't'
[IO.File]::WriteAllText($DESK, '{"mcpServers":{"khac":{"command":"x.exe","args":["-v"]}},"giu":1,"ghi":"a\"b\\c ' + $VIET + '"}', (New-Object Text.UTF8Encoding $false))
'{"security":{"auth":{"selectedType":"oauth-personal"}}}' | Set-Content -Encoding ascii $GEM
$LK = "$NHA\AppData\Roaming\Autodesk\ApplicationPlugins\TKPCCC.bundle"
$LKYT = "$NHA\AppData\Roaming\Autodesk\ApplicationPlugins\TKPCCC_YouTube.bundle"
$LKBOT = "$NHA\AppData\Roaming\Autodesk\ApplicationPlugins\TKPCCC_BotChat.bundle"
$MCP = "$BIN\TKPCCC.Mcp.exe"

$loi = 0
function Kiem($dk, $ten) { if ($dk) { "DAT   $ten" } else { "LOI   $ten"; $script:loi++ } }
function Hub($doiSo, $ra) {
  $p = Start-Process -FilePath "$BIN\TKPCCC.Hub.exe" -ArgumentList (@('/goc', "`"$NHA`"") + $doiSo + @('/ra', "`"$ra`"")) -Wait -PassThru
  @{ ma = $p.ExitCode; ra = [IO.File]::ReadAllText($ra) }
}
$coClaude = [bool](Get-Command claude -ErrorAction SilentlyContinue)
$coAgy = [bool](Get-Command agy -ErrorAction SilentlyContinue) -or (Test-Path "$env:LOCALAPPDATA\agy\bin\agy.exe")
$cai = @('plugin', 'plugin_yt', 'plugin_bot', 'claude_desktop', 'gemini', 'wv2_dll', 'net48', 'wv2_runtime', 'cli_claude', 'brave') + $(if ($coClaude) { 'claude_code' }) + $(if ($coAgy) { 'agy' })

try {
  $r = Hub @('/cai', ($cai -join ',')) "$T\r1.txt"
  Kiem ($r.ma -eq 0) "cai $($cai -join ', ') thoat ma 0"
  Kiem ((Test-Path "$LK\Contents\TKPCCC.Cad.dll") -and ((Get-Item $LK -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'plugin: lien ket bundle trong ApplicationPlugins gia'
  Kiem ((Test-Path "$LKYT\Contents\TKPCCC.YouTube.dll") -and (Test-Path "$LKBOT\Contents\TKPCCC.BotChat.dll")) 'plugin YouTube, Bot chat: moi plugin mot lien ket bundle rieng'
  $d = Get-Content -Raw -Encoding UTF8 $DESK | ConvertFrom-Json
  Kiem ($d.mcpServers.cad.command -eq $MCP -and $d.mcpServers.khac.command -eq 'x.exe' -and $d.mcpServers.khac.args[0] -eq '-v') 'Claude Desktop: them cad, giu may chu khac'
  Kiem ($d.giu -eq 1 -and $d.ghi -eq ('a"b\c ' + $VIET))'Claude Desktop: giu khoa khac, chuoi dac biet nguyen ven'
  Kiem (@(Get-ChildItem "$DESK.tkpccc_*.bak").Count -eq 1) 'Claude Desktop: co ban sao luu truoc khi sua'
  $g = Get-Content -Raw -Encoding UTF8 $GEM | ConvertFrom-Json
  Kiem ($g.mcpServers.cad.command -eq $MCP -and $g.security.auth.selectedType -eq 'oauth-personal') 'Gemini CLI: them cad, giu dang nhap'
  if ($coClaude) { Kiem ($r.ra -match [regex]::Escape("(th") -and $r.ra -match [regex]::Escape("claude mcp add -s user cad `"$MCP`"")) 'Claude Code: nha gia chi ghi lenh claude mcp add, khong chay' }
  if ($coAgy) { Kiem ($r.ra -match [regex]::Escape("agy mcp add cad `"$MCP`"")) 'Antigravity: nha gia chi ghi lenh agy mcp add, khong chay' }
  Kiem (@($WV2 | Where-Object { Test-Path "$T\cad\TKPCCC_YouTube.bundle\Contents\$_" }).Count -eq 3) 'YouTube: chep 3 DLL WebView2 vao bundle YouTube'
  Kiem ($r.ra -match '\(th.\) m. https://dotnet\.microsoft\.com/' -and $r.ra -match '\(th.\) m. https://developer\.microsoft\.com/microsoft-edge/webview2/' -and $r.ra -match '\(th.\) m. https://docs\.claude\.com/' -and $r.ra -match '\(th.\) m. https://brave\.com/download/')'phan mem hang khac: chi mo trang tai chinh thuc (nha gia: chi ghi, khong mo)'

  $r = Hub @('/kiem') "$T\r2.txt"
  Kiem ($r.ra -match 'KIEM mcp 1 ') 'kiem: may chu MCP bat tay duoc (initialize)'
  Kiem ($r.ra -match 'KIEM plugin 1 ' -and $r.ra -match 'KIEM claude_desktop 1 ' -and $r.ra -match 'KIEM gemini 1 ' -and $r.ra -match 'KIEM wv2_dll 1 ' -and $r.ra -match 'KIEM plugin_yt 1 ' -and $r.ra -match 'KIEM plugin_bot 1 ') 'kiem: 3 plugin, Claude Desktop, Gemini, DLL WebView2 da xong'
  Kiem ((@('acad', 'net48', 'plugin', 'plugin_yt', 'plugin_bot', 'mcp', 'claude_code', 'claude_desktop', 'gemini', 'agy', 'cli_claude', 'cli_agy', 'wv2_runtime', 'wv2_dll', 'brave') | Where-Object { $r.ra -notmatch "KIEM $_ " }).Count -eq 0) 'kiem: du 15 muc trong 4 nhom'

  $r = Hub @('/go', 'plugin,plugin_yt,claude_desktop,gemini') "$T\r3.txt"
  Kiem (-not (Test-Path $LKYT) -and (Test-Path "$LKBOT\Contents\TKPCCC.BotChat.dll") -and (Test-Path "$T\cad\TKPCCC_YouTube.bundle\Contents\TKPCCC.YouTube.dll")) 'go rieng plugin YouTube: Bot chat van cai, file YouTube con nguyen'
  Kiem ($r.ma -eq 0 -and -not (Test-Path $LK) -and (Test-Path "$T\cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll")) 'go plugin: bo lien ket, file trong bundle con nguyen'
  $d = Get-Content -Raw -Encoding UTF8 $DESK | ConvertFrom-Json
  $g = Get-Content -Raw -Encoding UTF8 $GEM | ConvertFrom-Json
  Kiem ($null -eq $d.mcpServers.cad -and $d.mcpServers.khac.command -eq 'x.exe' -and $null -eq $g.mcpServers.cad) 'go cad khoi Claude Desktop, Gemini; giu may chu khac'

  # Thu muc that (khong phai lien ket) o cho lien ket: khong duoc xoa.
  New-Item -ItemType Directory "$LK\Contents" | Out-Null
  'cua nguoi dung' | Set-Content "$LK\Contents\giu.txt"
  $r = Hub @('/go', 'plugin') "$T\r4.txt"
  Kiem ($r.ma -ne 0 -and (Test-Path "$LK\Contents\giu.txt")) 'go plugin: thu muc that khong bi xoa'
  Kiem (Test-Path "$NHA\AppData\Roaming\TKPCCC\hub.log") 'ghi nhat ky hub.log'
}
finally { }

"Thu muc thu: $T"
if ($loi) { "CO $loi LOI"; exit 1 } else { 'TAT CA DAT' }
