param(
    [string]$configuration = "Release",
    [string]$msBuildPath = "msbuild",
    [string]$wixPath = "C:\Program Files (x86)\WiX Toolset v3.11\bin"
)

$solution = "..\B1TuneUp.sln"
$projectDir = "..\B1TuneUp\B1TuneUp"

Write-Host "Building solution..."
& $msBuildPath $solution /p:Configuration=$configuration /m

# Run heat to harvest files
$heat = Join-Path $wixPath "heat.exe"
$bin = Join-Path $projectDir "bin\$configuration"
if (!(Test-Path $bin)) { Write-Error "Build output not found: $bin"; exit 1 }

Write-Host "Harvesting files with heat..."
& $heat dir $bin -ag -scom -sreg -dr INSTALLFOLDER -cg B1TuneUpComponents -var var.SourceDir -out "installer\wix\Components.wxs"

# Build MSI
$candle = Join-Path $wixPath "candle.exe"
$light = Join-Path $wixPath "light.exe"

Write-Host "Compiling WiX sources..."
& $candle "installer\wix\Product.wxs" -dSourceDir=$bin -out "installer\wix\Product.wixobj"
& $candle "installer\wix\Components.wxs" -dSourceDir=$bin -out "installer\wix\Components.wixobj"
& $light "installer\wix\Product.wixobj" "installer\wix\Components.wixobj" -o "installer\B1TuneUp.msi"

Write-Host "MSI created at installer\B1TuneUp.msi"
