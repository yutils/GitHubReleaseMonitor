param(
    [switch]$DebugBuild
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$cscCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw 'csc.exe not found. Windows 10/11 is required.'
}

$icon = Join-Path $root 'assets\app.ico'
$manifest = Join-Path $root 'src\app.manifest'
$dist = Join-Path $root 'dist'
$assemblyInfo = Get-Content (Join-Path $root 'src\AssemblyInfo.cs') -Raw
$versionMatch = [regex]::Match($assemblyInfo, 'AssemblyVersion\("([0-9]+\.[0-9]+\.[0-9]+)')
if (-not $versionMatch.Success) {
    throw 'AssemblyVersion not found in src\AssemblyInfo.cs.'
}
$version = $versionMatch.Groups[1].Value
$output = Join-Path $dist ('GitHubReleaseMonitor-v' + $version + '.exe')
$sources = Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }

New-Item -ItemType Directory -Force $dist | Out-Null
Get-ChildItem $dist -Filter 'GitHubReleaseMonitor*.exe' -File -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem $dist -Filter 'GitHubReleaseMonitor*.pdb' -File -ErrorAction SilentlyContinue | Remove-Item -Force

if ($DebugBuild) {
    $optimize = '/optimize-'
    $debug = '/debug+'
} else {
    $optimize = '/optimize+'
    $debug = '/debug-'
}

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    $optimize,
    $debug,
    '/codepage:65001',
    ('/out:' + $output),
    ('/win32icon:' + $icon),
    ('/win32manifest:' + $manifest),
    ('/resource:' + $icon + ',app.ico'),
    '/r:System.dll',
    '/r:System.Core.dll',
    '/r:System.Drawing.dll',
    '/r:System.Windows.Forms.dll',
    '/r:System.Runtime.Serialization.dll',
    '/r:System.Security.dll',
    '/r:System.Xml.dll'
)

Write-Host 'Building...' -ForegroundColor Cyan
& $csc @cscArgs $sources
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Get-ChildItem $dist | Select-Object Name, Length, LastWriteTime
Write-Host ('Build finished: ' + $output) -ForegroundColor Green
