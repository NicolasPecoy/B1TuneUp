param(
    [string]$targetDir = "C:\Program Files\B1TuneUp",
    [string]$addonCode = "B1TuneUp",
    [string]$sapRegistryKey = "HKLM:\SOFTWARE\SAP\SAP Business One\AddOns"
)

if (Test-Path $targetDir) { Remove-Item $targetDir -Recurse -Force }

$regPath = Join-Path $sapRegistryKey $addonCode
if (Test-Path $regPath) { Remove-Item $regPath -Recurse -Force }

Write-Host "Uninstalled $addonCode"
