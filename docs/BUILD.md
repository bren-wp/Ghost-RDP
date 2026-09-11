# Build

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7 or Windows PowerShell for repository scripts
- Visual Studio 2022 is optional
- Inno Setup 6 is required only when building Setup/Portable release packages

## Restore and build

```powershell
dotnet restore GhostRdp.sln
dotnet format GhostRdp.sln --verify-no-changes --no-restore
dotnet build GhostRdp.sln -c Release --no-restore
dotnet test GhostRdp.sln -c Release --no-build
```

## Security regression script

```powershell
./scripts/security-regression.ps1
```

## Development publish output

```powershell
dotnet publish src/GhostRdp.App/GhostRdp.App.csproj -c Release -r win-x64 --self-contained false -o artifacts/app
dotnet publish src/GhostRdp.Host/GhostRdp.Host.csproj -c Release -r win-x64 --self-contained false -o artifacts/host
./scripts/validate-package.ps1 -AppDirectory ./artifacts/app -HostDirectory ./artifacts/host
```

Development output is framework-dependent and remains useful for CI/build inspection.

## Setup and Portable release packages

```powershell
./scripts/build-release-packages.ps1 -OutputDirectory ./artifacts/release
./scripts/validate-release-package.ps1 -ReleaseDirectory ./artifacts/release
./scripts/test-installer.ps1 -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe
```

Release packaging publishes self-contained single-file App/Host executables, creates the Portable ZIP and Inno Setup installer, writes SHA-256 hashes, validates the archive, and smoke-tests install/uninstall behavior. See [PACKAGING.md](PACKAGING.md).
