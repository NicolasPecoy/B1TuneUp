This folder contains basic scripts to build a simple installer and deployment artifacts for the B1TuneUp add-on.

Notes:
- The add-on targets .NET Framework 4.8 and is an SAP B1 add-on (DLL + config). The recommended installer tools are WiX Toolset or an Inno Setup script.
- The scripts here are minimal examples to produce a zip containing the compiled add-on and resource files, and a PowerShell script to deploy to a target machine.

Files:
- build.ps1 - builds the project using MSBuild and packages the output into a zip
- deploy.ps1 - copies files to a target directory and registers the add-on with SAP B1 registry keys (example)
- uninstall.ps1 - removes deployed files and registry entries

Before using: install WiX if you want MSI support, or adapt Inno Setup script for a full installer.

## Commercial/product checklist

For a production-like commercial deployment, package the add-on with:

1. The compiled `bin/Release/net48` output.
2. `Resources/lang` files.
3. Installer scripts and SAP add-on registration metadata.
4. A post-install step that opens SAP and runs **B1TuneUp Config Center > Modules / Diagnostics > Repair Metadata**.
5. A license provisioning step.

## Licensing flow

B1TuneUp now supports signed offline license tokens:

```text
B1TL1.<payload-base64url>.<signature-base64url>
```

For your own premium/developer installation:

1. Install and run the add-on.
2. Open **B1TuneUp Config Center > Lifecycle / Samples**.
3. Click **Generate Owner Premium**.
4. Confirm that the license status changes to `LicensedPremium`.

For customer deployments, generate the token outside the installed add-on and paste it with **Save License**. Do not ship a production signing secret with customer machines. In a real commercial setup, the signing secret should live in a private licensing portal or build/release system.

## Upgrade flow

Recommended guided upgrade:

1. Backup the SAP company database.
2. Export a B1TuneUp package from **Config Center > Modules / Diagnostics > Export**.
3. Install the new add-on build.
4. Run **Lifecycle / Samples > Run Guided Upgrade**.
5. Run **Support > Run Health Checks**.
6. Export a support package and archive it with the deployment notes.
