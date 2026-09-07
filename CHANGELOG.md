# Changelog

## [Unreleased]

## [0.8.0-beta] - 2026-09-07

### Added
- `source/NukeShare.Core` project (placeholder)
- `source/NukeShare.Network` project (placeholder)
- `source/NukeShare.Configuration` project (library)
- `Directory.Build.props` for shared MSBuild publish configuration
- Spectre.Console and Spectre.Console.Cli to CLI project
- `ILogger`/`Logger` in `NukeShare.Core` with a public `ILogger` contract
- `TypeRegistrar`/`TypeResolver` DI bridge for Spectre.Console.Cli
- `ConfigCommand` with `--init`, `--version`, `--list`, and key/value configuration arguments
- `BannerBadge` for rendering the version banner
- `ConfigurationService` and `GlobalProvider` for global configuration management
- `Microsoft.Extensions.DependencyInjection` to CLI project
- Shared `OutputPath`/`ArtifactsDir` in `Directory.Build.props`
- `StartCommand` (`nuke start`) to launch the daemon with `--port` and `--background` support
- `StopCommand` (`nuke stop`) to terminate the daemon process
- `DaemonProcessLauncher` for cross-platform daemon process management
- Daemon `/health` endpoint returning running status, PID, and timestamp
- OpenAPI and Scalar API reference for the daemon in development/interactive mode
- `UdpDiscoveryService` for LAN peer discovery via UDP broadcast beacons
- `PeerBeacon` model for discovery protocol serialization
- `PeerInfo` record with trust state tracking (`Untrusted`, `Pending`, `Trusted`)
- `PeerRegistry` in-memory peer store with `MaxPeers` enforcement and subnet filtering
- `StatusCommand` (`nuke status`) with `--health`, `--peers`, `--transfers`, `--config`, `--shutdown` flags
- `StatusRestApi` HTTP client for all daemon status endpoints
- DTOs for status, health, peers, transfers, shutdown, and config responses
- `StatusController` with endpoints: `GET /v1/status`, `GET /v1/status/health`, `GET /v1/status/peers`, `GET /v1/status/transfers`, `GET /v1/status/config`, `POST /v1/status/shutdown`
- Daemon loads `GlobalConfiguration` at startup and passes config to discovery service
- Config-driven discovery: ports, broadcast interval, trust mode, auto-accept, allowed subnets
- `--config` flag on `nuke status` to display full daemon configuration table
- `PeersCommand` (`nuke peers`) with `--approve`, `--reject`, `--node`, and `--remove` flags
- `PATCH /v1/status/peers/{nodeId}/trust` endpoint for peer trust management
- `DELETE /v1/status/peers/{nodeId}` endpoint for peer removal
- Trust DTOs (`TrustRequestDTO`, `TrustResponseDTO`, `RemovePeerResponseDTO`)
- `SetPeerTrustAsync` and `RemovePeerAsync` methods in `StatusRestApi`
- Self-contained publish support for cross-platform single-file executables
- `publish.ps1` PowerShell build script for Windows (supports win-x64, linux-x64, osx-arm64)
- `publish.sh` Bash build script for Linux/macOS with auto-detection
- `RootCommand` banner so running `nuke` with no arguments renders the version badge
- `RestartCommand` (`nuke restart`) to stop and relaunch the daemon with `--port` and `--background` support
- `TcpFileReceiverService` hosted in the daemon to accept incoming file transfers on `DefaultTransferPort`
- `TransferController` with endpoints: `GET /v1/transfer`, `GET /v1/transfer/recent`, `GET /v1/transfer/{transferId}`, `POST /v1/transfer`, `POST /v1/transfer/{transferId}/cancel`
- `TransferTracker` service for starting, monitoring, and cancelling outgoing transfers with progress and speed tracking
- `TransferCommand` (`nuke transfer`) to send files to peers, list active and recent transfers, and cancel transfers with a live `--watch` progress view
- `TransferRestApi` HTTP client for all transfer endpoints

### Changed
- Moved all projects from root into `source/` directory
- Renamed `NukeShare.Server` to `NukeShare.Daemon`
- Updated CLI to use Spectre.Console command framework
- Updated solution file to reference new project paths
- Changed `NukeShare.Core` and `NukeShare.Configuration` to library output types
- Added `NukeShare.Configuration` and `NukeShare.Core` project references to CLI
- Set CLI assembly name to `nuke`
- Set daemon assembly name to `nuked`
- Fixed `DefaultStoragePath` to use `Path.Combine` segment form
- Replaced incompatible Spectre DI extensions package with `Microsoft.Extensions.DependencyInjection` and a built-in `TypeRegistrar`/`TypeResolver` bridge
- Reworked configuration console messages to consistent, production-standard boxed panels with clear status, usage guidance, and escaped values
- Refactored `--list` to auto-derive all config keys from `GlobalConfiguration` via reflection and render a Spectre table with `[Description]` attribute text
- `--init` now scaffolds runtime directories (storage, incoming, temp, logs) and refreshes config.json with the latest model settings
- Expanded `GlobalConfiguration` with network/discovery, security/encryption, transfer/throttling, storage/files, and daemon/API settings
- `nuke start` now passes the selected port to the daemon via `ASPNETCORE_URLS`, with port validation
- `DaemonProcessLauncher` resolves the daemon executable both in published output and sibling development build directories
- `DaemonProcessLauncher` sets `ASPNETCORE_URLS`/`ASPNETCORE_ENVIRONMENT` for both foreground and background launches
- Restyled `start` and `stop` command console output to the app's boxed-panel UI
- `BannerBadge` reads the build version from assembly metadata instead of a hardcoded string
- Implemented functional `Logger` with level-prefixed, timestamped, color-coded console output
- Fixed `StatusController` routing from `v1/status/[controller]` to `v1/status` for clean REST paths
- `StatusDTO.Uptime` type changed from `DateTime` to `TimeSpan` to match daemon response
- `StatusRestApi` URL corrected from `API/status` to `v1/status`
- `StatusCommand` now reads daemon port from `GlobalConfiguration` via `ConfigurationService`
- `PeerRegistry.GetActivePeers` now uses configured `PeerTimeoutSeconds` instead of hardcoded 30s
- Peers endpoint returns trust state per peer with color-coded CLI display
- Daemon DI registrations use factory delegates to resolve config-dependent services
- `DaemonProcessLauncher` redirects stdout/stderr to prevent log leaking into CLI terminal
- Daemon uses `ASPNETCORE_ENVIRONMENT=Production` to prevent `launchSettings.json` port override
- `PeerRegistry` filters local addresses to prevent self-discovery
- Self-contained publish settings moved from `Directory.Build.props` to individual exe csproj files to avoid `NETSDK1099` errors on library projects
- `publish.ps1` and `publish.sh` output to `artifacts/dist/<RID>/` directory
- Default listen port resolution moved from `StartCommand` into `DaemonProcessLauncher.GetDefaultPort`
- `StartCommand.ProceedStart` extracted as a reusable static start routine shared by `start` and `restart`
- `DefaultTransferPort` config key added for direct file streaming, distinct from the daemon HTTP port
- Discovery beacon now advertises `DefaultTransferPort` instead of the daemon HTTP port
- `v1/status/transfers` now returns live transfer data from `TransferTracker` instead of placeholder data
- `DaemonProcessLauncher.StopDaemon` now returns a process exit code (`1` when no daemon is running)

### Removed
- Root-level NukeShare.CLI and NukeShare.Server project files
- `Spectre.Console.Cli.Extensions.DependencyInjection` package (incompatible with `Spectre.Console.Cli` 0.55)
- `LocalConfiguration` model (folded into `GlobalConfiguration`)
- Empty duplicate `UdpDiscoveryService` class and duplicate using block from `Dicovery.cs`
- Hardcoded discovery values (ports, intervals, username, deviceName) replaced with config-driven values
