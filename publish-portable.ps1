$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "MiniWid-portable"

Get-Process MiniWid -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

dotnet publish (Join-Path $root "src\MiniWid.App\MiniWid.App.csproj") `
    -c Release `
    -p:Platform=x64 `
    -r win-x64 `
    --self-contained true `
    -o $out

if ($LASTEXITCODE -ne 0) {
    throw "Publish failed."
}

$buildOut = Join-Path $root "src\MiniWid.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64"
if (Test-Path (Join-Path $buildOut "MiniWid.pri")) {
    Copy-Item (Join-Path $buildOut "MiniWid.pri") $out -Force
}
$ico = Join-Path $buildOut "Assets\AppIcon.ico"
if (Test-Path $ico) {
    $assets = Join-Path $out "Assets"
    if (-not (Test-Path $assets)) { New-Item $assets -ItemType Directory | Out-Null }
    Copy-Item $ico $assets -Force
}
Get-ChildItem $buildOut -Filter *.xbf -Recurse | ForEach-Object {
    $relative = $_.FullName.Substring($buildOut.Length).TrimStart('\')
    $dest = Join-Path $out $relative
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) {
        New-Item $destDir -ItemType Directory | Out-Null
    }
    Copy-Item $_.FullName $dest -Force
}

Write-Host ""
Write-Host "Portable build: $out\MiniWid.exe"
Write-Host "Copy the whole MiniWid-portable folder anywhere and run MiniWid.exe."
