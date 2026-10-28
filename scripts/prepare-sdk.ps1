param(
    [Parameter(Mandatory = $true)][string]$SdkZip
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source = (Resolve-Path -LiteralPath $SdkZip).Path
$lib = Join-Path $root 'lib'
New-Item -ItemType Directory -Path $lib -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($source)
try {
    $map = @{
        'Net/Renga.NET8.PluginUtility.dll' = 'Renga.NET8.PluginUtility.dll'
        'tlb/RengaCOMAPI.tlb' = 'RengaCOMAPI.tlb'
    }
    foreach ($entryName in $map.Keys) {
        $entry = $archive.Entries | Where-Object { $_.FullName.Replace('\','/') -ieq $entryName } | Select-Object -First 1
        if ($null -eq $entry) { throw "Not found in SDK ZIP: $entryName" }
        $target = Join-Path $lib $map[$entryName]
        $inputStream = $entry.Open()
        try {
            $outputStream = [System.IO.File]::Create($target)
            try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose() }
        } finally { $inputStream.Dispose() }
        Write-Host "Prepared locally: $target"
    }
} finally { $archive.Dispose() }
Write-Host 'SDK dependencies are ready in lib/. This folder is excluded from Git.'
