# Changelog

## [Unreleased]

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

### Removed
- Root-level NukeShare.CLI and NukeShare.Server project files
- `Spectre.Console.Cli.Extensions.DependencyInjection` package (incompatible with `Spectre.Console.Cli` 0.55)
- `LocalConfiguration` model (folded into `GlobalConfiguration`)
