WiX installer build

Prerequisites:
- WiX Toolset installed (tested with WiX 3.11)
- heat.exe, candle.exe, light.exe on PATH or specify their folder to `build-installer.ps1`

How it works:
1. `build-installer.ps1` builds the solution (msbuild), uses heat to harvest the compiled add-on output into `Components.wxs` and the worker output into `WorkerComponents.wxs`, compiles the `.wxs` files with candle and links with light to produce `installer\B1TuneUp.msi`.
2. `Product.wxs` contains the product metadata. Update `ProductCode`, `ProductVersion`, `Manufacturer` and `UpgradeCode` variables when running candle if necessary.

Example (local machine):
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1 -configuration Release -wixPath "C:\Program Files (x86)\WiX Toolset v3.11\bin"

Example (CI):
powershell -ExecutionPolicy Bypass -File installer\build-installer-ci.ps1 -configuration Release -wixPath "C:\Program Files (x86)\WiX Toolset v3.11\bin" -outputDir "artifacts"

Commercial packaging notes:
- Build Release first with `dotnet build B1TuneUp.sln -c Release`.
- Include the Config Center because metadata repair, package import/export, support package and licensing are now product-level workflows.
- After MSI install, run SAP B1 once as an administrator/consultant user and execute `Run Guided Upgrade` from **Lifecycle / Samples**.
- License tokens are entered after install. The MSI should not contain a customer license unless it is a dedicated build.
- For owner/developer machines, open **Lifecycle / Samples** and click **Generate Owner Premium** to create a signed local premium license.

License token format:

```text
B1TL1.<payload-base64url>.<signature-base64url>
```

For a real commercial release, keep the signing secret outside the MSI and outside the installed add-on. Generate licenses from a private licensing service, then paste the signed token with **Save License**.

Commercial RSA token format:

```text
B1TRSA.<payload-base64url>.<signature-base64url>
```

Production installer expectations:

- Check prerequisites: .NET Framework 4.8, SAP Business One DI API/UI API, Crystal runtime when Crystal reports are enabled, and worker account permissions.
- Register the SAP add-on registry key and install `B1TuneUpWorker` when server-side queues are enabled.
- Keep install logs in `%TEMP%` plus `C:\Program Files\B1TuneUp\install-logs`.
- Sign the MSI and binaries with the publisher certificate in CI.
- Run guided upgrade and metadata repair after upgrade, then export a support package.
