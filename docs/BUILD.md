# Build

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7 or Windows PowerShell for repository scripts
- Visual Studio 2022 or later is optional

No third-party installer compiler is required. Ghost RDP Setup is built from the `GhostRdp.Setup` project with the same .NET toolchain as the App and Host.

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

## Production multi-architecture packages

```powershell
./scripts/build-release-packages.ps1 -Architecture all -OutputDirectory ./artifacts/release
./scripts/validate-release-package.ps1 -ReleaseDirectory ./artifacts/release -Architecture all
```

Valid architecture values are `x86`, `x64`, `arm64`, and `all`. Each production binary is self-contained, so end users do not need to preinstall the .NET runtime.

## Runtime and installer smoke tests

```powershell
./scripts/smoke-test-portable.ps1 `
  -AppPath ./artifacts/release/GhostRDP-Portable-x64.exe `
  -HostPath ./artifacts/release/GhostRDP-Host-x64.exe `
  -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe

./scripts/test-installer.ps1 -SetupPath ./artifacts/release/GhostRDP-Setup-x64.exe
```

The release validator checks PE architecture, SHA-256 integrity, expected ZIP content, prohibited static `.rdp` files, and the no-separate-uninstaller contract. The installer smoke test verifies the Windows Installed Apps registration and confirms that uninstall removes the application without installing a distinct `uninstall.exe`.
