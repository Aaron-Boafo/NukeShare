param(
    [ValidateSet("win-x64", "linux-x64", "osx-arm64")]
    [string]$Rid = "win-x64"
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$DistDir = Join-Path $RepoRoot "artifacts\dist\$Rid"

Write-Host "Building NukeShare for $Rid..." -ForegroundColor Cyan
Write-Host ""

# Clean dist directory
if (Test-Path $DistDir) {
    Remove-Item -Recurse -Force $DistDir
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# Publish CLI
Write-Host "Publishing NukeShare.CLI (nuke)..." -ForegroundColor Yellow
dotnet publish "$RepoRoot\source\NukeShare.CLI\NukeShare.CLI.csproj" `
    -c Release `
    -r $Rid `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $DistDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Publish Daemon
Write-Host "Publishing NukeShare.Daemon (nuked)..." -ForegroundColor Yellow
dotnet publish "$RepoRoot\source\NukeShare.Daemon\NukeShare.Daemon.csproj" `
    -c Release `
    -r $Rid `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $DistDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Copy config files
$daemonDir = "$RepoRoot\source\NukeShare.Daemon"
foreach ($file in @("appsettings.json", "appsettings.Development.json")) {
    $src = Join-Path $daemonDir $file
    if (Test-Path $src) {
        Copy-Item $src $DistDir -Force
    }
}

# Summary
Write-Host ""
Write-Host "Build complete: $DistDir" -ForegroundColor Green
Write-Host ""

$exeExt = if ($Rid -like "win-*") { ".exe" } else { "" }
$nukePath = Join-Path $DistDir "nuke$exeExt"
$nukedPath = Join-Path $DistDir "nuked$exeExt"

if (Test-Path $nukePath) {
    $size = [math]::Round((Get-Item $nukePath).Length / 1MB, 1)
    Write-Host "  nuke$exeExt  $size MB"
}
if (Test-Path $nukedPath) {
    $size = [math]::Round((Get-Item $nukedPath).Length / 1MB, 1)
    Write-Host "  nuked$exeExt  $size MB"
}

Write-Host ""
Write-Host "To verify on this machine:" -ForegroundColor Cyan
Write-Host "  $nukePath status"
