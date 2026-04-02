param(
    [string]$configuration = "Release",
    [string]$wixPath = "C:\Program Files (x86)\WiX Toolset v3.11\bin",
    [string]$outputDir = "artifacts"
)

$ErrorActionPreference = 'Stop'

Write-Host "Building solution..."
msbuild ..\B1TuneUp.sln /p:Configuration=$configuration /m

$bin = "..\B1TuneUp\B1TuneUp\bin\$configuration"
if (!(Test-Path $bin)) { throw "Build output not found: $bin" }

$heat = Join-Path $wixPath "heat.exe"
$candle = Join-Path $wixPath "candle.exe"
$light = Join-Path $wixPath "light.exe"

Write-Host "Harvesting files with heat..."
& $heat dir $bin -ag -scom -sreg -dr INSTALLFOLDER -cg B1TuneUpComponents -var var.SourceDir -out "installer\wix\Components.wxs"

# compile wix
& $candle "installer\wix\Product.wxs" -dSourceDir=$bin -dProductVersion=1.0.0 -dProductCode={PUT-GUID-HERE} -dManufacturer="B1TuneUp" -dUpgradeCode={PUT-UPGRADE-GUID-HERE} -out "installer\wix\Product.wixobj"
& $candle "installer\wix\Components.wxs" -dSourceDir=$bin -out "installer\wix\Components.wixobj"
& $candle "installer\wix\RegisterAddOn.wxs" -dSourceDir=$bin -dAddonCode=B1TuneUp -out "installer\wix\RegisterAddOn.wixobj"

& $light "installer\wix\Product.wixobj" "installer\wix\Components.wixobj" "installer\wix\RegisterAddOn.wixobj" -o "$outputDir\B1TuneUp.msi"

Write-Host "MSI created at $outputDir\B1TuneUp.msi"
