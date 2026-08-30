# Changelog

## [Unreleased]

### Added
- `source/NukeShare.Core` project (placeholder)
- `source/NukeShare.Network` project (placeholder)
- `Directory.Build.props` for shared MSBuild publish configuration
- Spectre.Console and Spectre.Console.Cli to CLI project

### Changed
- Moved all projects from root into `source/` directory
- Renamed `NukeShare.Server` to `NukeShare.Daemon`
- Updated CLI to use Spectre.Console command framework
- Updated solution file to reference new project paths

### Removed
- Root-level NukeShare.CLI and NukeShare.Server project files
