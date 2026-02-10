# Development Guide

This guide covers how to set up and run DiscordDataMirror locally.

## Prerequisites

### Required Software

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0+ | [Download](https://dotnet.microsoft.com/download) |
| Docker Desktop | Latest | [Download](https://www.docker.com/products/docker-desktop) |
| Git | Latest | [Download](https://git-scm.com/) |

### Verify Installation

```powershell
# Check .NET version
dotnet --version
# Should output: 10.x.x

# Check Docker
docker --version
docker compose version

# Verify Docker is running
docker ps
```

## Quick Start

```powershell
# 1. Clone the repository
git clone <repo-url>
cd DiscordDataMirror

# 2. Configure Discord bot token
.\scripts\setup-bot.ps1

# 3. Run with Aspire
cd src\DiscordDataMirror.AppHost
dotnet run
```

## Project Structure

```
DiscordDataMirror/
├── src/
│   ├── DiscordDataMirror.AppHost/           # Aspire orchestrator (start here!)
│   ├── DiscordDataMirror.ServiceDefaults/   # Shared Aspire configuration
│   ├── DiscordDataMirror.Domain/            # Domain entities, value objects
│   ├── DiscordDataMirror.Application/       # CQRS commands, queries, handlers
│   ├── DiscordDataMirror.Infrastructure/    # EF Core, Discord.Net, repositories
│   ├── DiscordDataMirror.Bot/               # Discord bot worker service
│   └── DiscordDataMirror.Dashboard/         # Blazor web dashboard
├── tests/                                    # Unit and integration tests
├── scripts/                                  # Setup and utility scripts
└── docs/                                     # Documentation
```

## Configuration

### Discord Bot Token

See [BOT_SETUP.md](BOT_SETUP.md) for complete bot setup instructions.

Quick setup:
```powershell
# Using the setup script (recommended)
.\scripts\setup-bot.ps1

# Or manually
cd src\DiscordDataMirror.Bot
dotnet user-secrets set "Discord:Token" "your-token-here"
```

### Application Settings

Configuration is loaded from `appsettings.json` and can be overridden:

```json
{
  "Discord": {
    "Token": "",           // Set via user-secrets, not here!
    "SyncOnStartup": true  // Whether to sync historical data on startup
  },
  "Sync": {
    "BatchSize": 100,
    "DelayBetweenBatchesMs": 1000,
    "MaxHistoricalMessages": 10000
  }
}
```

## Database Setup

DiscordDataMirror uses PostgreSQL via .NET Aspire. No manual setup required!

### How It Works

1. Aspire automatically provisions a PostgreSQL container
2. Connection strings are injected automatically
3. EF Core migrations run on startup

### Manual Database Operations

```powershell
# Create a new migration
cd src\DiscordDataMirror.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ..\DiscordDataMirror.Bot

# Apply migrations manually (usually automatic)
dotnet ef database update --startup-project ..\DiscordDataMirror.Bot

# Generate SQL script
dotnet ef migrations script --startup-project ..\DiscordDataMirror.Bot -o migration.sql
```

### Database Connection

When running locally via Aspire:
- Host: `localhost`
- Port: Dynamically assigned (check Aspire dashboard)
- Database: `discorddatamirror`
- Credentials: Managed by Aspire

To connect manually, find the connection string in the Aspire dashboard.

## Running the Application

### Option 1: Full Stack via Aspire (Recommended)

```powershell
cd src\DiscordDataMirror.AppHost
dotnet run
```

This starts:
- ✅ PostgreSQL database
- ✅ Discord bot worker
- ✅ Blazor dashboard
- ✅ Aspire dashboard (https://localhost:17113)

### Option 2: Individual Services

For debugging specific services:

```powershell
# Start database first (via Docker)
docker run -d --name discordmirror-db \
  -e POSTGRES_PASSWORD=devpassword \
  -e POSTGRES_DB=discorddatamirror \
  -p 5432:5432 \
  postgres:16

# Then run services individually
cd src\DiscordDataMirror.Bot
dotnet run

# In another terminal
cd src\DiscordDataMirror.Dashboard
dotnet run
```

### Option 3: Docker Compose (Production-like)

```powershell
docker compose up --build
```

## Development Workflow

### Building

```powershell
# Build all projects
dotnet build

# Build specific project
dotnet build src\DiscordDataMirror.Bot
```

### Running Tests

```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests\DiscordDataMirror.Domain.Tests
```

### Code Formatting

```powershell
# Check formatting
dotnet format --verify-no-changes

# Fix formatting
dotnet format
```

### Hot Reload

For the Dashboard (Blazor):
```powershell
cd src\DiscordDataMirror.Dashboard
dotnet watch run
```

> Note: The Bot worker service doesn't support hot reload well due to Discord connection state.

## IDE Setup

### Visual Studio 2022

1. Open `DiscordDataMirror.sln`
2. Set `DiscordDataMirror.AppHost` as startup project
3. Press F5 to debug

### Visual Studio Code

Recommended extensions:
- C# Dev Kit
- Docker
- REST Client

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch AppHost",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/DiscordDataMirror.AppHost/bin/Debug/net10.0/DiscordDataMirror.AppHost.dll",
      "cwd": "${workspaceFolder}/src/DiscordDataMirror.AppHost"
    }
  ]
}
```

### JetBrains Rider

1. Open the solution
2. Run/Debug `DiscordDataMirror.AppHost`

## Debugging

### View Logs

- **Aspire Dashboard**: https://localhost:17113 → Traces/Logs
- **Console**: Logs stream to console when running
- **Structured Logs**: JSON format in production

### Common Debug Scenarios

**Bot not connecting:**
```powershell
# Check if token is set
cd src\DiscordDataMirror.Bot
dotnet user-secrets list
```

**Database connection issues:**
```powershell
# Check if PostgreSQL container is running
docker ps | Select-String postgres

# View container logs
docker logs <container-id>
```

**Memory issues with large syncs:**
- Reduce `Sync:BatchSize` in appsettings
- Increase `Sync:DelayBetweenBatchesMs`

## Troubleshooting

### Docker Issues

**"Cannot connect to the Docker daemon"**
- Start Docker Desktop
- Wait for it to fully initialize
- Check Docker settings if on Windows with WSL2

**Port conflicts**
```powershell
# Find what's using a port
netstat -ano | findstr :5432
```

### .NET Issues

**"SDK not found"**
- Ensure .NET 10 SDK is installed (not just runtime)
- Check `global.json` matches your SDK version

**"User secrets not working"**
```powershell
# Initialize user secrets if needed
dotnet user-secrets init --project src\DiscordDataMirror.Bot
```

### Database Issues

**"Connection refused"**
- Ensure PostgreSQL container is running
- Check Aspire dashboard for correct port
- Verify no firewall blocking

**"Relation does not exist"**
- Migrations haven't run
- Check startup logs for migration errors
- Try running migrations manually

### Discord Issues

See [BOT_SETUP.md](BOT_SETUP.md) troubleshooting section.

## Architecture Notes

### CQRS Pattern

Commands and queries are handled via MediatR:
- `Application/Commands/` - Write operations
- `Application/Queries/` - Read operations
- `Application/Handlers/` - Business logic

### Event Flow

```
Discord Gateway → DiscordEventHandler → MediatR Command → Handler → Repository → Database
```

### Key Services

| Service | Responsibility |
|---------|----------------|
| `DiscordClientService` | Manages Discord connection lifecycle |
| `DiscordEventHandler` | Converts Discord events to commands |
| `HistoricalSyncOrchestrator` | Backfills historical data |

## Contributing

1. Create a feature branch
2. Make changes with tests
3. Ensure `dotnet format` passes
4. Submit PR

## Resources

- [Discord.Net Documentation](https://discordnet.dev/)
- [.NET Aspire Docs](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [EF Core Docs](https://learn.microsoft.com/en-us/ef/core/)
