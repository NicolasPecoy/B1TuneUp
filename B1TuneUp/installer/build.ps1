param(
    [string]$configuration = "Debug",
    [string]$output = "output"
)

$solutionPath = "..\B1TuneUp.sln"
$msbuild = "" # use msbuild from PATH

Write-Host "Building solution..."
msbuild $solutionPath /p:Configuration=$configuration /m

# find build output
$bin = "..\B1TuneUp\B1TuneUp\bin\$configuration"
if (!(Test-Path $bin)) { $bin = "..\B1TuneUp\B1TuneUp\bin\Debug" }

if (Test-Path $output) { Remove-Item $output -Recurse -Force }
New-Item -ItemType Directory -Path $output | Out-Null

Copy-Item "$bin\*" -Destination $output -Recurse

# include resources and lang files
Copy-Item "..\B1TuneUp\B1TuneUp\Resources" -Destination $output -Recurse

# create zip
$zip = "B1TuneUp_$configuration.zip"
if (Test-Path $zip) { Remove-Item $zip }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory((Resolve-Path $output).Path, $zip)
Write-Host "Packaged $zip"
