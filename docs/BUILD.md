# Build

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7 or Windows PowerShell for repository scripts
- Visual Studio 2022 is optional

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

These outputs are development binaries, not the final Setup or Portable deliverables. Installer and portable packaging are separate roadmap milestones.
