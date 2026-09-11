# Build

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7 or Windows PowerShell for repository scripts
- Visual Studio 2022 or later is optional

No third-party installer compiler or runtime package is required. Ghost RDP Setup is built from the `GhostRdp.Setup` project with the same .NET toolchain as the App and Host.

## Restore and build

```powershell
dotnet restore GhostRdp.sln
dotnet format GhostRdp.sln --verify-no-changes --no-restore
dotnet build GhostRdp.sln -c Release --no-restore
dotnet test GhostRdp.sln -c Release --no-build
```

## Security and dependency regression

```powershell
./scripts/security-regression.ps1
```

The regression script verifies both security boundaries and the production dependency policy. Project files below `src/` must not introduce a third-party `PackageReference` or an external file-based `HintPath` assembly reference. Existing Microsoft test packages live under the test projects and are development-only.

## Release preflight

```powershell
./scripts/release-preflight.ps1
```

The baseline preflight verifies that App, Host, and Setup versions match. Release-preparation branches additionally run:

```powershell
./scripts/release-preflight.ps1 -RequireReleaseMetadata
```

The publication workflow adds `-RequireReleaseBranch`, which requires the exact `release/v<version>` branch name. See [Release validation](RELEASE.md).

## Development publish output

```powershell
dotnet publish src/GhostRdp.App/GhostRdp.App.csproj -c Release -r win-x64 --self-contained false -o artifacts/app
dotnet publish src/GhostRdp.Host/GhostRdp.Host.csproj -c Release -r win-x64 --self-contained false -o artifacts/host
./scripts/validate-package.ps1 -AppDirectory ./artifacts/app -HostDirectory ./artifacts/host
```

Framework-dependent output is for development validation only. Production release artifacts use self-contained publishing.

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

The release validator checks PE architecture, SHA-256 integrity, expected ZIP content, prohibited static `.rdp` files, and the no-separate-uninstaller contract. The installer smoke test verifies Windows Installed Apps registration and confirms that uninstall removes the application without installing a distinct `uninstall.exe`.

CI additionally runs ARM64 App/Host/Setup self-tests and installer lifecycle validation on a native Windows ARM64 runner.

## Documentation gate

Build/security validation requires the maintained documentation set, including the documentation index and dependency policy. Functional or release changes should update README, CHANGELOG, ROADMAP, and all affected domain documents in the same pull request. Release preparation must also review Packaging, Release, Release Notes, Security, Privacy and Dependencies. See [Documentation index](README.md).
