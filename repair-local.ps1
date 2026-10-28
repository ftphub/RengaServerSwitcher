$ErrorActionPreference = "Stop"
$settings = Join-Path $env:LOCALAPPDATA "Renga Software\Renga Professional\settings.ini"
if (!(Test-Path $settings)) { throw "settings.ini not found: $settings" }
$backup = "$settings.rss.repair.bak"
Copy-Item $settings $backup -Force
$lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $settings -Encoding UTF8)
$start = -1
for ($i=0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -ieq '[Collaboration]') { $start=$i; break } }
if ($start -lt 0) { $lines.Insert(0,'[Collaboration]'); $start=0 }
$end=$lines.Count
for ($i=$start+1; $i -lt $lines.Count; $i++) { if ($lines[$i].TrimStart().StartsWith('[')) { $end=$i; break } }
function Set-IniValue([string]$key,[string]$value) {
    $script:end = $end
    $prefix="$key="
    for ($j=$start+1; $j -lt $end; $j++) {
        if ($lines[$j].StartsWith($prefix,[System.StringComparison]::OrdinalIgnoreCase)) { $lines[$j]="$prefix$value"; return }
    }
    $lines.Insert($end,"$prefix$value"); $script:end=$end+1
}
Set-IniValue 'ServerHostName' ''
Set-IniValue 'CollaborationUserName' ''
Set-IniValue 'CollaborationPort' '8820'
Set-IniValue 'CollaborationServerAccessKey' ''
Set-IniValue 'RealtimeCollaboration' 'false'
[System.IO.File]::WriteAllLines($settings,$lines,(New-Object System.Text.UTF8Encoding($false)))
Write-Host "Renga collaboration settings switched to local mode." -ForegroundColor Green
Write-Host "Backup: $backup"
Write-Host "Start Renga normally."
