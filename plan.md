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
- [x] Move solution file to repo root for conventional layout
- [x] Verify build succeeds with `dotnet build`

## Phase 2: Windows Service Support

- [x] Add `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Hosting.WindowsServices` NuGet packages
- [x] Refactor `Program.cs` to use the Generic Host (`Host.CreateDefaultBuilder`)
- [x] Extract forwarding logic into a `BackgroundService` (e.g., `PortForwarderService`)
- [x] Support dual-mode: runs as console app interactively, runs as Windows Service when installed
- [x] Configuration via `appsettings.json` and/or command-line args for `LocalPort`, `RemoteHost`, `RemotePort`
- [x] Wire up `ILogger` to replace `Console.WriteLine` / `Trace` calls

## Phase 3: XUnit v3 Test Coverage

- [ ] Create `tests/PortForwarder.Tests/PortForwarder.Tests.csproj` targeting `net10.0` with `xunit.v3` package
- [ ] Add test project to solution
- [ ] Unit tests for configuration parsing / validation
- [ ] Integration tests for `TcpPortForwarder`: start a forwarder, connect through it, verify data flows
- [ ] Tests for service lifecycle (start/stop)

## Phase 4: GitHub Actions – Build & Test

- [ ] Create `.github/workflows/build.yml`
  - Trigger on push/PR to `main` and `dotnet10-upgrade`
  - Use `actions/setup-dotnet` with .NET 10 SDK
  - Steps: restore → build → test
  - Run on `windows-latest` (Windows Service project)
- [ ] Publish test results as workflow artifacts

## Phase 5: GitHub Actions – Installer

- [ ] Build a self-contained publish (`dotnet publish -r win-x64 --self-contained`)
- [ ] Package as an installer (see **open question** below)
- [ ] Upload installer artifact in the workflow
- [ ] Optionally create a GitHub Release on tag push

---

## Open Questions (need your input)

### 1. Installer technology
What kind of installer do you want?
- **A) Inno Setup** – free, produces a classic `Setup.exe`, can register/unregister the Windows Service during install/uninstall. Well-supported in GitHub Actions.
- **B) WiX v5 (MSI)** – produces an `.msi`, enterprise-friendly, GPO-deployable. More complex to set up.
- **C) ZIP + sc.exe script** – simplest option: just publish a zip with a `install-service.ps1` / `uninstall-service.ps1` script that uses `sc.exe create` / `sc.exe delete`.
- **D) MSIX** – modern Windows packaging, but less common for services.

### 2. Solution structure
I plan to move the `.sln` to the repo root so the layout becomes:
```
SimplePortForwarder.sln
src/portforwarder/portforwarder.csproj
tests/PortForwarder.Tests/PortForwarder.Tests.csproj
```
Is that acceptable, or do you prefer keeping the sln inside `src/`?

### 3. Configuration model
For Windows Service mode the app needs to know `LocalPort`, `RemoteHost`, `RemotePort` without interactive console input. I plan to support:
- `appsettings.json` (primary for service mode)
- Command-line args (existing behavior preserved for console mode)

Does that work, or do you want registry-based config, environment variables, etc.?

### 4. Async modernization
The current code uses the legacy `BeginXxx`/`EndXxx` APM pattern. Should I modernize it to `async`/`await` (`ReadAsync`, `WriteAsync`, `AcceptTcpClientAsync`) as part of this upgrade, or leave the forwarding logic as-is?

---

## Implementation Order
Phases will be implemented sequentially (1 → 2 → 3 → 4 → 5) since each phase builds on the previous.
