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
- `ConfigCommand` with `--init`, `--version`, and key/value configuration arguments
- `BannerBadge` for rendering the version banner
- `ConfigurationService` and `GlobalProvider` for global configuration management
- `Microsoft.Extensions.DependencyInjection` to CLI project
- Shared `OutputPath`/`ArtifactsDir` in `Directory.Build.props`

### Changed
- Moved all projects from root into `source/` directory
- Renamed `NukeShare.Server` to `NukeShare.Daemon`
- Updated CLI to use Spectre.Console command framework
- Updated solution file to reference new project paths
- Changed `NukeShare.Core` and `NukeShare.Configuration` to library output types
- Added `NukeShare.Configuration` and `NukeShare.Core` project references to CLI
- Set CLI assembly name to `nuke`
- Fixed `DefaultStoragePath` to use `Path.Combine` segment form
- Replaced incompatible Spectre DI extensions package with `Microsoft.Extensions.DependencyInjection` and a built-in `TypeRegistrar`/`TypeResolver` bridge

### Removed
- Root-level NukeShare.CLI and NukeShare.Server project files
- `Spectre.Console.Cli.Extensions.DependencyInjection` package (incompatible with `Spectre.Console.Cli` 0.55)
