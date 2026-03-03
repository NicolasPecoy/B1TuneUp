This folder contains basic scripts to build a simple installer and deployment artifacts for the B1TuneUp add-on.

Notes:
- The add-on targets .NET Framework 4.8 and is an SAP B1 add-on (DLL + config). The recommended installer tools are WiX Toolset or an Inno Setup script.
- The scripts here are minimal examples to produce a zip containing the compiled add-on and resource files, and a PowerShell script to deploy to a target machine.

Files:
- build.ps1 - builds the project using MSBuild and packages the output into a zip
- deploy.ps1 - copies files to a target directory and registers the add-on with SAP B1 registry keys (example)
- uninstall.ps1 - removes deployed files and registry entries

Before using: install WiX if you want MSI support, or adapt Inno Setup script for a full installer.
