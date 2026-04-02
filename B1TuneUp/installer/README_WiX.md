WiX installer build

Prerequisites:
- WiX Toolset installed (tested with WiX 3.11)
- heat.exe, candle.exe, light.exe on PATH or specify their folder to `build-installer.ps1`

How it works:
1. `build-installer.ps1` builds the solution (msbuild), uses heat to harvest the compiled output into `Components.wxs`, compiles the `.wxs` files with candle and links with light to produce `installer\B1TuneUp.msi`.
2. `Product.wxs` contains the product metadata. Update `ProductCode`, `ProductVersion`, `Manufacturer` and `UpgradeCode` variables when running candle if necessary.

Example (local machine):
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1 -configuration Release -wixPath "C:\Program Files (x86)\WiX Toolset v3.11\bin"

Example (CI):
powershell -ExecutionPolicy Bypass -File installer\build-installer-ci.ps1 -configuration Release -wixPath "C:\Program Files (x86)\WiX Toolset v3.11\bin" -outputDir "artifacts"
