param(
    [switch]$ThirdPartyRedistributionPermissionConfirmed
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$build = Join-Path $root '_build\RengaServerSwitcher'
if (-not (Test-Path (Join-Path $build 'RengaServerSwitcher.Plugin.dll'))) {
    throw 'No built plugin found. Build Release | x64 in Visual Studio first.'
}
if (-not (Test-Path (Join-Path $build 'RengaServerSwitcher.RestartHelper.exe'))) {
    throw 'RestartHelper.exe is missing. Build both projects.'
}
$vendorBinary = Join-Path $build 'Renga.NET8.PluginUtility.dll'
if ((Test-Path $vendorBinary) -and -not $ThirdPartyRedistributionPermissionConfirmed) {
    throw 'Release folder contains a third-party Renga SDK DLL. Check the redistribution terms, then rerun with -ThirdPartyRedistributionPermissionConfirmed only when authorized.'
}
$outDir = Join-Path $root 'releases'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$stage = Join-Path $env:TEMP ('RengaServerSwitcherRelease-' + [guid]::NewGuid().ToString('N'))
$stagePlugin = Join-Path $stage 'RengaServerSwitcher'
New-Item -ItemType Directory -Path $stagePlugin -Force | Out-Null
try {
    Get-ChildItem -LiteralPath $build -File | Where-Object {
        $_.Extension -notin @('.pdb', '.xml')
    } | Copy-Item -Destination $stagePlugin
    $zipFile = Join-Path $outDir 'RengaServerSwitcher_v1.1.2_Win_x64.zip'
    if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
    Compress-Archive -Path $stagePlugin -DestinationPath $zipFile -CompressionLevel Optimal
    Write-Host "Created: $zipFile"
    Write-Host 'Test the package on Renga Professional 9.3 before attaching it to GitHub Release.'
} finally {
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
}
