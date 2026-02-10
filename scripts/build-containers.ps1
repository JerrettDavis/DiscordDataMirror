#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build Docker container images for Discord Data Mirror

.DESCRIPTION
    Uses .NET SDK container publishing to build OCI-compliant container images
    for the Dashboard and Bot services.

.PARAMETER Tag
    The tag to apply to the images (default: latest)

.PARAMETER Registry
    Optional container registry to push to (e.g., ghcr.io/username)

.PARAMETER Push
    Push images to registry after building

.EXAMPLE
    ./build-containers.ps1
    
.EXAMPLE
    ./build-containers.ps1 -Tag v1.0.0 -Registry ghcr.io/jerrettdavis -Push
#>

param(
    [string]$Tag = "latest",
    [string]$Registry = "",
    [switch]$Push
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "🐳 Building Discord Data Mirror containers..." -ForegroundColor Cyan
Write-Host ""

# Build Dashboard container
Write-Host "📦 Building Dashboard container..." -ForegroundColor Yellow
$dashboardProject = Join-Path $repoRoot "src/DiscordDataMirror.Dashboard/DiscordDataMirror.Dashboard.csproj"

$dashboardArgs = @(
    "publish"
    $dashboardProject
    "--os", "linux"
    "--arch", "x64"
    "-c", "Release"
    "/t:PublishContainer"
    "-p:ContainerImageTag=$Tag"
)

if ($Registry) {
    $dashboardArgs += "-p:ContainerRegistry=$Registry"
}

& dotnet @dashboardArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build Dashboard container" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Dashboard container built successfully" -ForegroundColor Green
Write-Host ""

# Build Bot container
Write-Host "📦 Building Bot container..." -ForegroundColor Yellow
$botProject = Join-Path $repoRoot "src/DiscordDataMirror.Bot/DiscordDataMirror.Bot.csproj"

$botArgs = @(
    "publish"
    $botProject
    "--os", "linux"
    "--arch", "x64"
    "-c", "Release"
    "/t:PublishContainer"
    "-p:ContainerImageTag=$Tag"
)

if ($Registry) {
    $botArgs += "-p:ContainerRegistry=$Registry"
}

& dotnet @botArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build Bot container" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Bot container built successfully" -ForegroundColor Green
Write-Host ""

# Show results
Write-Host "🎉 Container builds complete!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Images created:" -ForegroundColor White

if ($Registry) {
    Write-Host "  - $Registry/discorddatamirror-dashboard:$Tag" -ForegroundColor Gray
    Write-Host "  - $Registry/discorddatamirror-bot:$Tag" -ForegroundColor Gray
} else {
    Write-Host "  - discorddatamirror-dashboard:$Tag" -ForegroundColor Gray
    Write-Host "  - discorddatamirror-bot:$Tag" -ForegroundColor Gray
}

Write-Host ""
Write-Host "To run with Docker Compose:" -ForegroundColor White
Write-Host "  1. cd publish" -ForegroundColor Gray
Write-Host "  2. cp .env.example .env" -ForegroundColor Gray
Write-Host "  3. Edit .env with your settings" -ForegroundColor Gray
Write-Host "  4. docker compose up -d" -ForegroundColor Gray

if ($Push -and $Registry) {
    Write-Host ""
    Write-Host "🚀 Pushing images to registry..." -ForegroundColor Yellow
    
    docker push "$Registry/discorddatamirror-dashboard:$Tag"
    docker push "$Registry/discorddatamirror-bot:$Tag"
    
    Write-Host "✅ Images pushed successfully" -ForegroundColor Green
}
