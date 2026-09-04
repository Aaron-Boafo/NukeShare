#!/usr/bin/env bash
set -euo pipefail

# Default RID based on current platform
DEFAULT_RID="$(uname -s)-$(uname -m)"
case "$DEFAULT_RID" in
    Linux-x86_64)   DEFAULT_RID="linux-x64" ;;
    Linux-aarch64)  DEFAULT_RID="linux-arm64" ;;
    Darwin-arm64)   DEFAULT_RID="osx-arm64" ;;
    Darwin-x86_64)  DEFAULT_RID="osx-x64" ;;
    *)              echo "Unsupported platform: $DEFAULT_RID"; exit 1 ;;
esac

RID="${1:-$DEFAULT_RID}"

# Validate RID
case "$RID" in
    win-x64|linux-x64|linux-arm64|osx-arm64|osx-x64) ;;
    *) echo "Error: Invalid RID '$RID'. Valid options: win-x64, linux-x64, linux-arm64, osx-arm64, osx-x64"; exit 1 ;;
esac

REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"
DIST_DIR="$REPO_ROOT/artifacts/dist/$RID"

echo "Building NukeShare for $RID..."
echo ""

# Clean dist directory
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

# Publish CLI
echo "Publishing NukeShare.CLI (nuke)..."
dotnet publish "$REPO_ROOT/source/NukeShare.CLI/NukeShare.CLI.csproj" \
    -c Release \
    -r "$RID" \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$DIST_DIR"

# Publish Daemon
echo "Publishing NukeShare.Daemon (nuked)..."
dotnet publish "$REPO_ROOT/source/NukeShare.Daemon/NukeShare.Daemon.csproj" \
    -c Release \
    -r "$RID" \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$DIST_DIR"

# Copy config files
DAEMON_DIR="$REPO_ROOT/source/NukeShare.Daemon"
for file in appsettings.json appsettings.Development.json; do
    if [ -f "$DAEMON_DIR/$file" ]; then
        cp "$DAEMON_DIR/$file" "$DIST_DIR/"
    fi
done

# Set executable permissions on Unix
chmod +x "$DIST_DIR/nuke" 2>/dev/null || true
chmod +x "$DIST_DIR/nuked" 2>/dev/null || true

# Summary
echo ""
echo "Build complete: $DIST_DIR"
echo ""

for exe in nuke nuked; do
    if [ -f "$DIST_DIR/$exe" ]; then
        size=$(du -m "$DIST_DIR/$exe" | cut -f1)
        echo "  $exe  ${size} MB"
    fi
done

echo ""
echo "To verify on this machine:"
echo "  $DIST_DIR/nuke status"
