# SimplePortForwarder Modernization Plan

## Current State
- .NET 5.0 console application (`net5.0` TFM, long EOL)
- Single project in `src/` with solution file alongside the csproj
- Uses legacy APM (Begin/End) async pattern for TCP forwarding
- No tests, no CI/CD, no service support, no installer

---

## Phase 1: Upgrade to .NET 10

- [x] Update `portforwarder.csproj` TFM from `net5.0` → `net10.0`
- [x] Enable modern project defaults: `<ImplicitUsings>enable</ImplicitUsings>`, `<Nullable>enable</Nullable>`
- [x] Clean up explicit `using` statements now covered by implicit usings
- [x] Add nullable annotations to existing types (`ClientPair`, `TcpPortForwarder`)
- [x] Keep solution file in `src/` folder
- [x] Verify build succeeds with `dotnet build`

## Phase 2: Windows Service Support

- [x] Add `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Hosting.WindowsServices` NuGet packages
- [x] Refactor `Program.cs` to use the Generic Host (`Host.CreateDefaultBuilder`)
- [x] Extract forwarding logic into a `BackgroundService` (e.g., `PortForwarderService`)
- [x] Support dual-mode: runs as console app interactively, runs as Windows Service when installed
- [x] Configuration via `appsettings.json` and/or command-line args for `LocalPort`, `RemoteHost`, `RemotePort`
- [x] Wire up `ILogger` to replace `Console.WriteLine` / `Trace` calls

## Phase 3: XUnit v3 Test Coverage

- [x] Create `tests/PortForwarder.Tests/PortForwarder.Tests.csproj` targeting `net10.0` with `xunit.v3` package
- [x] Add test project to solution
- [x] Unit tests for configuration parsing / validation
- [x] Integration tests for `TcpPortForwarder`: start a forwarder, connect through it, verify data flows
- [x] Tests for service lifecycle (start/stop)

## Phase 4: GitHub Actions – Build & Test

- [x] Create `.github/workflows/build.yml`
  - Trigger on push/PR to `main` and `dotnet10-upgrade`
  - Use `actions/setup-dotnet` with .NET 10 SDK
  - Steps: restore → build → test
  - Run on `windows-latest` (Windows Service project)
- [x] Publish test results as workflow artifacts

## Phase 5: GitHub Actions – Installer

- [x] Build a self-contained publish (`dotnet publish -r win-x64 --self-contained`)
- [x] Package as an installer (WiX v6 MSI via `WixToolset.Sdk/6.0.2`)
- [x] Upload installer artifact in the workflow
- [x] Optionally create a GitHub Release on tag push

---

## Resolved Decisions

1. **Installer technology** → WiX v6 MSI (`WixToolset.Sdk/6.0.2`)
2. **Solution structure** → `.sln` in `src/` folder; `tests/` as sibling directory
3. **Configuration model** → `appsettings.json` with `PortForwarder:Rules` section; supports multiple forwarding rules
4. **Async modernization** → Deferred; legacy APM pattern retained for now
