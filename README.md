# DiscordDataMirror

A .NET 10 Aspire solution for monitoring, scraping, and backing up all data from Discord servers.

## Features

- **Discord Bot Service** - Monitors and backs up all server data in real-time
- **Blazor Dashboard** - Visualize and navigate backed-up data
- **PostgreSQL Database** - Reliable storage via Aspire
- **DDD Architecture** - Clean, maintainable, and testable code

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- A Discord bot token ([setup guide](docs/BOT_SETUP.md))

### 1. Clone and Setup

```powershell
git clone <repo-url>
cd DiscordDataMirror

# Run the interactive setup script
.\scripts\setup-bot.ps1
```

The setup script will:
- ✅ Check prerequisites
- ✅ Prompt for your bot token
- ✅ Validate the token works
- ✅ Store it securely in user-secrets
- ✅ Generate your bot invite URL

### 2. Add Bot to Server

1. Enable **Privileged Gateway Intents** in [Discord Developer Portal](https://discord.com/developers/applications):
   - SERVER MEMBERS INTENT
   - MESSAGE CONTENT INTENT
2. Use the invite URL from the setup script (or generate one with permissions `66560`)
3. Add the bot to your server

### 3. Run

```powershell
cd src\DiscordDataMirror.AppHost
dotnet run
```

### 4. View Dashboard

- **Aspire Dashboard**: https://localhost:17113 (services, logs, traces)
- **Data Dashboard**: https://localhost:5001 (view backed up data)

## Architecture

```
┌─────────────────────────────────────────────┐
│              Presentation                    │
│    (Dashboard, Bot Worker Service)          │
├─────────────────────────────────────────────┤
│              Application                     │
│    (Commands, Queries, Handlers, DTOs)      │
├─────────────────────────────────────────────┤
│              Domain                          │
│    (Entities, Aggregates, Value Objects)    │
├─────────────────────────────────────────────┤
│              Infrastructure                  │
│    (EF Core, Discord.Net, Repositories)     │
└─────────────────────────────────────────────┘
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed documentation.

## Project Structure

```
DiscordDataMirror/
├── src/
│   ├── DiscordDataMirror.AppHost/           # Aspire orchestrator (start here!)
│   ├── DiscordDataMirror.ServiceDefaults/   # Shared Aspire defaults
│   ├── DiscordDataMirror.Domain/            # Domain layer
│   ├── DiscordDataMirror.Application/       # Application layer (CQRS)
│   ├── DiscordDataMirror.Infrastructure/    # Infrastructure layer
│   ├── DiscordDataMirror.Bot/               # Discord bot worker
│   └── DiscordDataMirror.Dashboard/         # Blazor dashboard
├── tests/                                    # Unit and integration tests
├── scripts/                                  # Setup and utility scripts
├── docs/                                     # Documentation
└── README.md
```

## Documentation

| Document | Description |
|----------|-------------|
| [BOT_SETUP.md](docs/BOT_SETUP.md) | Complete Discord bot setup guide |
| [DEVELOPMENT.md](docs/DEVELOPMENT.md) | Development environment and workflow |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System architecture and design |
| [TODO.md](TODO.md) | Development roadmap |

## Development

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for complete development guide.

```bash
# Build
dotnet build

# Test
dotnet test

# Run (full stack via Aspire)
cd src/DiscordDataMirror.AppHost
dotnet run
```

## Discord Bot Requirements

### Permissions (Integer: `66560`)
- View Channels
- Read Message History

### Privileged Gateway Intents
Enable these in the [Discord Developer Portal](https://discord.com/developers/applications):
- ✅ SERVER MEMBERS INTENT
- ✅ MESSAGE CONTENT INTENT

See [docs/BOT_SETUP.md](docs/BOT_SETUP.md) for detailed setup instructions.

## License

MIT
