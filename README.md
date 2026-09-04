# NukeShare

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![GitHub stars](https://img.shields.io/github/stars/Aaron-Boafo/NukeShare?style=social)](https://github.com/Aaron-Boafo/NukeShare/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/Aaron-Boafo/NukeShare)](https://github.com/Aaron-Boafo/NukeShare/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/Aaron-Boafo/NukeShare)](https://github.com/Aaron-Boafo/NukeShare/pulls)
[![GitHub last commit](https://img.shields.io/github/last-commit/Aaron-Boafo/NukeShare)](https://github.com/Aaron-Boafo/NukeShare/commits/main)
[![GitHub workflow status](https://img.shields.io/github/actions/workflow/status/Aaron-Boafo/NukeShare/main.yml?branch=main)](https://github.com/Aaron-Boafo/NukeShare/actions)

**NukeShare** is an open-source peer-to-peer file sharing tool that operates entirely over the command line. No servers, no third parties -- just direct device-to-device transfers on your local network.

---

## Features

### Current Features
- **CLI Command System** - Rich command-line interface powered by Spectre.Console
- **Configuration Management** - Cross-platform JSON-based configuration with reflection-based key discovery
- **Daemon Lifecycle** - Start and stop the background daemon (`nuke start` / `nuke stop`)
- **Cross-Platform Daemon Launch** - Windows, macOS, and Linux process management
- **Daemon Health Check** - `/health` endpoint returning running status, PID, and timestamp
- **Rich Console UI** - Colored boxed panels, tables, and status messages via Spectre.Console
- **Status Dashboard** - `nuke status` with `--health`, `--peers`, `--transfers`, `--config`, `--shutdown` flags
- **UDP Network Discovery** - Broadcast-based peer discovery with config-driven intervals and trust modes
- **Peer Management** - `nuke peers` to list, approve, reject, and remove peers
- **Self-Contained Builds** - Single-file executables for Windows, Linux, and macOS
- **Cross-Platform Build Scripts** - `publish.ps1` and `publish.sh` for easy publishing

### Planned Features
- **Peer-to-Peer File Transfer** - Chunked transfers with configurable chunk size and concurrent chunks
- **End-to-End Encryption** - AES-256-GCM encryption with peer trust modes
- **Transfer Throttling** - Bandwidth limits and peer connection limits
- **Resumable Transfers** - Resume interrupted transfers automatically
- **Shared Directory Publishing** - Publish directories to peers on the network

---

## File Structure

```
NukeShare/
├── .gitattributes
├── .gitignore
├── CHANGELOG.md
├── CONTRIBUTING.md
├── CONTRIBUTORS.md
├── Directory.Build.props
├── LICENSE
├── NukeShare.slnx
├── publish.ps1
├── publish.sh
├── artifacts/
│   ├── bin/Release/
│   ├── publish/Release/
│   └── dist/
│       ├── win-x64/
│       ├── linux-x64/
│       └── osx-arm64/
└── source/
    ├── NukeShare.CLI/
    │   ├── NukeShare.CLI.csproj
    │   ├── Program.cs
    │   ├── Commands/
    │   │   ├── ConfigurationCommand.cs
    │   │   ├── StartCommand.cs
    │   │   ├── StopCommand.cs
    │   │   ├── StatusCommand.cs
    │   │   └── PeersCommand.cs
    │   ├── Infrastructure/
    │   │   ├── TypeResolver.cs
    │   │   ├── DaemonProcessLauncher.cs
    │   │   ├── StatusRestApi.cs
    │   │   └── DaemonStatusDto.cs
    │   └── UI/
    │       └── BannerBadge.cs
    ├── NukeShare.Configuration/
    │   ├── NukeShare.Configuration.csproj
    │   ├── Models/
    │   │   └── GlobalConfiguration.cs
    │   ├── Providers/
    │   │   ├── index.cs
    │   │   └── GlobalProvider.cs
    │   └── Service/
    │       └── ConfigurationService.cs
    ├── NukeShare.Core/
    │   ├── NukeShare.Core.csproj
    │   └── Logger/
    │       └── Logger.cs
    ├── NukeShare.Daemon/
    │   ├── NukeShare.Daemon.csproj
    │   ├── Program.cs
    │   ├── Controller/
    │   │   └── StatusController.cs
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Properties/
    │       └── launchSettings.json
    └── NukeShare.Network/
        ├── NukeShare.Network.csproj
        └── Discovery/
            ├── Discovery.cs
            ├── PeerBeacon.cs
            ├── PeerInfo.cs
            └── PeerRegistry.cs
```

### Project Descriptions

| Project | Description |
|---------|-------------|
| **NukeShare.CLI** | Main CLI application (entry point) - the `nuke` command |
| **NukeShare.Daemon** | ASP.NET Core daemon with health check, status endpoints, and OpenAPI documentation |
| **NukeShare.Configuration** | Cross-platform configuration management library |
| **NukeShare.Core** | Core shared types and interfaces (logging) |
| **NukeShare.Network** | P2P networking library (UDP discovery, peer registry) |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- A code editor (Visual Studio, VS Code, Rider, etc.)

---

## Installation

### From Source

1. **Clone** the repository:
   ```bash
   git clone https://github.com/Aaron-Boafo/NukeShare.git
   cd NukeShare
   ```

2. **Build** the solution:
   ```bash
   dotnet build
   ```

3. **Run** the CLI:
   ```bash
   dotnet run --project source/NukeShare.CLI
   ```

### From Release

1. Download the latest release from [GitHub Releases](https://github.com/Aaron-Boafo/NukeShare/releases)
2. Extract the archive
3. Run the `nuke` executable

### Building for Distribution

PowerShell (Windows):
```powershell
.\publish.ps1 -Rid win-x64
.\publish.ps1 -Rid linux-x64
.\publish.ps1 -Rid osx-arm64
```

Bash (Linux/macOS):
```bash
./publish.sh linux-x64
./publish.sh osx-arm64
./publish.sh  # auto-detects platform
```

Output: `artifacts/dist/<RID>/`

---

## Usage

### Initialize Configuration

```bash
nuke config --init
```

This creates the configuration directory and scaffolds runtime directories (storage, incoming, temp, logs).

### List Configuration

```bash
nuke config --list
```

Displays all configuration keys with their current values.

### Update Configuration

```bash
nuke config <KEY> <VALUE>
```

Example:
```bash
nuke config Username "MyComputer"
nuke config DefaultListenPort 8080
```

### View Version

```bash
nuke config --version
```

### Start Daemon

```bash
nuke start
```

Start with a custom port:
```bash
nuke start --port 7654
```

Run the daemon in the background:
```bash
nuke start --background
```

### Stop Daemon

```bash
nuke stop
```

Alias: `nuke kill`

### Check Status

```bash
nuke status
```

With flags:
```bash
nuke status --health
nuke status --peers
nuke status --transfers
nuke status --config
nuke status --shutdown
```

### Manage Peers

```bash
nuke peers
nuke peers --approve
nuke peers --approve --node <nodeId>
nuke peers --reject
nuke peers --reject --node <nodeId>
nuke peers --remove <nodeId>
```

---

## Configuration

Configuration is stored in JSON format. The default location depends on your operating system:

| Platform | Location |
|----------|----------|
| Windows | `%APPDATA%/NukeShare/config.json` |
| macOS | `~/Library/Application Support/NukeShare/config.json` |
| Linux | `$XDG_CONFIG_HOME/NukeShare/config.json` or `~/.config/NukeShare/config.json` |

### Key Configuration Sections

- **Identity** - Device nickname and unique identifier
- **Storage** - File storage paths and limits
- **Network** - Discovery, ports, and connection settings
- **Security** - Encryption and trust modes
- **Transfer** - Chunk size, concurrent transfers, and throttling
- **Daemon** - API and daemon settings

---

## Development

### Branch Naming

Use descriptive prefixes:

| Prefix | Purpose |
|--------|---------|
| `feature/` | New features |
| `fix/` | Bug fixes |
| `docs/` | Documentation changes |
| `refactor/` | Code refactoring |
| `test/` | Adding or updating tests |

### Code Style

- Follow standard C# conventions
- Use meaningful variable and method names
- Keep methods focused and concise
- Nullable reference types are enabled
- Use `var` when the type is obvious from the right side

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- [Spectre.Console](https://spectreconsole.net/) - Rich console rendering
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core) - Web framework
- [Scalar](https://scalar.com/) - API documentation
