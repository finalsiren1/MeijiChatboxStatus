<#
  Builds the files to share with other people into .\dist

    MeijiChatboxStatus.exe          — self-contained single file (no .NET install needed), ~70 MB
    MeijiChatboxStatus-lite.exe     — needs the .NET 8 Desktop Runtime, a few MB   (-Lite)

  Usage:  powershell -ExecutionPolicy Bypass -File .\publish.ps1 [-Lite]
#>
param([switch]$Lite)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'src\MeijiChatboxStatus\MeijiChatboxStatus.csproj'
$dist = Join-Path $root 'dist'
$tmp = Join-Path $root 'artifacts\publish'

Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $dist | Out-Null

function Publish([string]$name, [string[]]$extra) {
    $out = Join-Path $tmp $name
    dotnet publish $proj -c Release -r win-x64 -o $out `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none @extra
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($name)" }
    $exe = Join-Path $out 'MeijiChatboxStatus.exe'
    $target = Join-Path $dist ($(if ($name -eq 'full') { 'MeijiChatboxStatus.exe' } else { 'MeijiChatboxStatus-lite.exe' }))
    Copy-Item $exe $target -Force
    "{0}  {1:N1} MB" -f $target, ((Get-Item $target).Length / 1MB)
}

dotnet test (Join-Path $root 'tests\MeijiChatboxStatus.Tests') -c Release
if ($LASTEXITCODE -ne 0) { throw 'tests failed' }

Publish 'full' @('--self-contained', 'true', '-p:EnableCompressionInSingleFile=true')
if ($Lite) { Publish 'lite' @('--self-contained', 'false') }

Copy-Item (Join-Path $root 'README.md'), (Join-Path $root 'LICENSE') $dist -Force
Remove-Item -Recurse -Force (Join-Path $root 'artifacts') -ErrorAction SilentlyContinue
"done → $dist"
