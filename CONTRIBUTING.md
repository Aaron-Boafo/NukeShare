# Contributing to NukeShare

Thanks for your interest in contributing to NukeShare! This document provides guidelines and instructions for contributing.

## About NukeShare

NukeShare is an open-source peer-to-peer file sharing tool that operates entirely over the command line. No servers, no third parties -- just direct device-to-device transfers on your local network.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- A code editor (Visual Studio, VS Code, Rider, etc.)

## Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/Aaron-Boafo/NukeShare.git
   cd NukeShare
   ```
3. **Build** the solution:
   ```bash
   dotnet build
   ```
4. **Run** the CLI:
   ```bash
   dotnet run --project NukeShare.CLI
   ```

## Development Workflow

1. Create a new branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
2. Make your changes
3. Test your changes locally
4. Commit with a clear, descriptive message
5. Push to your fork and open a Pull Request

## Branch Naming

Use descriptive prefixes:

| Prefix | Purpose |
|--------|---------|
| `feature/` | New features |
| `fix/` | Bug fixes |
| `docs/` | Documentation changes |
| `refactor/` | Code refactoring |
| `test/` | Adding or updating tests |

## Code Style

- Follow standard C# conventions
- Use meaningful variable and method names
- Keep methods focused and concise
- Nullable reference types are enabled -- handle nulls explicitly
- Use `var` when the type is obvious from the right side

## Pull Requests

- Keep PRs focused on a single change
- Provide a clear description of what changed and why
- Reference any related issues
- Ensure the solution builds without errors before submitting

## Reporting Issues

Open an issue on GitHub with:

- A clear title and description
- Steps to reproduce (if applicable)
- Expected vs actual behavior
- Your environment (.NET version, OS, etc.)

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
