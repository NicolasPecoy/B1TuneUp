param(
    [string]$sourceZip = "B1TuneUp_Debug.zip",
    [string]$targetDir = "C:\Program Files\B1TuneUp",
    [string]$addonCode = "B1TuneUp",
    [string]$sapRegistryKey = "HKLM:\SOFTWARE\SAP\SAP Business One\AddOns"
)

if (!(Test-Path $sourceZip)) { Write-Error "Source zip not found: $sourceZip"; exit 1 }

# extract
if (Test-Path $targetDir) { Remove-Item $targetDir -Recurse -Force }
New-Item -ItemType Directory -Path $targetDir | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path $sourceZip).Path, $targetDir)

# Register addon in SAP registry keys (example - adjust for your environment)
$regPath = Join-Path $sapRegistryKey $addonCode
if (!(Test-Path $regPath)) { New-Item -Path $regPath | Out-Null }
Set-ItemProperty -Path $regPath -Name "Path" -Value (Join-Path $targetDir "B1TuneUp.exe")
Set-ItemProperty -Path $regPath -Name "Description" -Value "B1TuneUp Addon"
Set-ItemProperty -Path $regPath -Name "Version" -Value "1.0.0"

Write-Host "Deployed to $targetDir and registered in $regPath"
