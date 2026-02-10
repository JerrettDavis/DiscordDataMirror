<div align="center">

# 🔮 DiscordDataMirror

**A powerful, self-hosted solution for archiving and exploring Discord server data**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Release](https://img.shields.io/github/v/release/JerrettDavis/DiscordDataMirror?style=flat-square)](https://github.com/JerrettDavis/DiscordDataMirror/releases)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat-square&logo=docker)](https://github.com/JerrettDavis/DiscordDataMirror/pkgs/container/discorddatamirror-bot)

[📖 Documentation](https://jerrettdavis.github.io/DiscordDataMirror) · [🐛 Report Bug](https://github.com/JerrettDavis/DiscordDataMirror/issues) · [💡 Request Feature](https://github.com/JerrettDavis/DiscordDataMirror/discussions)

</div>

---

## 🎯 What is DiscordDataMirror?

DiscordDataMirror is a **complete Discord archiving solution** that captures, stores, and lets you explore all data from your Discord servers. It runs as a background service, silently recording every message, member, channel, and reaction in real-time — creating a permanent, searchable backup that you fully control.

### Why do you need this?

- **Discord has retention limits** — Messages can be auto-deleted, and server owners can purge history at any time
- **Users delete messages** — Important context disappears when people delete their messages
- **Servers get deleted** — Community history can vanish overnight
- **Discord's search is limited** — You can't search deleted messages or export your data easily
- **Compliance requirements** — Some organizations need to archive communications

DiscordDataMirror solves all of these problems by maintaining a complete, independent copy of your Discord data.

---

## ✨ Features

### 📝 Comprehensive Message Archiving
Every message is captured the instant it's sent, before it can be deleted or modified. You'll never lose important discussions, announcements, or memories again.

- Real-time capture via Discord's Gateway API
- Preserves message content, embeds, and formatting
- Tracks edits and deletions (original content retained)
- Supports all message types: replies, pins, system messages
- Full attachment metadata (with optional local caching)

### 👥 Complete Member Tracking
Maintain a complete record of everyone who has ever been in your servers.

- User profiles with avatars, usernames, and creation dates
- Guild-specific data: nicknames, roles, join dates
- Join/leave history tracking
- Bot account flagging

### 📁 Full Server Structure Mirroring
The entire server hierarchy is preserved, including ephemeral content.

- All channel types: text, voice, forums, stages, categories
- Thread capture (including auto-archived threads)
- Role hierarchy and permissions
- Channel topics and descriptions

### 🔍 Powerful Search
Find any message across all your servers instantly.

- Full-text search powered by PostgreSQL
- Filter by server, channel, user, or date range
- Search deleted messages
- Export search results

### 🎨 Beautiful Dashboard
A modern, Discord-like interface for browsing your archives.

![Dashboard Preview](docs/images/dashboard-preview.svg)

- Familiar Discord-style message rendering
- Channel browser with message counts
- User activity timelines
- Real-time sync status monitoring

### 🐳 Docker Ready
Deploy anywhere with Docker Compose.

```bash
docker pull ghcr.io/jerrettdavis/discorddatamirror-bot:latest
docker pull ghcr.io/jerrettdavis/discorddatamirror-dashboard:latest
```

### 🔭 Built with .NET Aspire
Modern cloud-native architecture with built-in observability.

- Automatic service discovery
- Distributed tracing
- Health checks and metrics
- Centralized logging

---

## 🚀 Quick Start

### Prerequisites

| Requirement | Version |
|-------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | 4.0+ |
| Discord Bot Token | [Get one here](docs/BOT_SETUP.md) |

### Installation

#### Option 1: Docker Compose (Recommended for Production)

```bash
# Clone the repository
git clone https://github.com/JerrettDavis/DiscordDataMirror.git
cd DiscordDataMirror/publish

# Configure environment
cp .env.example .env
# Edit .env with your Discord token and settings

# Start the stack
docker compose up -d
```

#### Option 2: Local Development

```bash
# Clone the repository
git clone https://github.com/JerrettDavis/DiscordDataMirror.git
cd DiscordDataMirror

# Configure bot token
./scripts/setup-bot.ps1  # Windows
./scripts/setup-bot.sh   # Linux/Mac

# Run with Aspire
cd src/DiscordDataMirror.AppHost
dotnet run
```

### First Run

1. **Aspire Dashboard**: Open https://localhost:17113 to see all services
2. **Data Dashboard**: Open https://localhost:5001 to browse archived data
3. **Discord Server**: Verify the bot appears online in your server

The bot will begin syncing historical messages in the background. This may take several minutes for large servers.

---

## 📊 Screenshots

<details>
<summary><strong>Dashboard Overview</strong></summary>

![Dashboard Overview](docs/images/dashboard-overview.svg)

View all your monitored servers at a glance with message counts, member stats, and sync status.
</details>

<details>
<summary><strong>Channel Browser</strong></summary>

![Channel Browser](docs/images/channel-browser.svg)

Navigate your server's channel structure with Discord-like familiarity. See message counts and quickly jump to any channel.
</details>

<details>
<summary><strong>Message Viewer</strong></summary>

![Message Viewer](docs/images/message-viewer.svg)

Read messages exactly as they appeared in Discord, complete with embeds, reactions, and reply chains.
</details>

<details>
<summary><strong>Sync Status</strong></summary>

![Sync Status](docs/images/sync-status.svg)

Monitor real-time synchronization progress across all your servers.
</details>

---

## ⚙️ Configuration

### Essential Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `Discord:Token` | Your bot's authentication token | *Required* |
| `Discord:SyncOnStartup` | Fetch historical messages on start | `true` |
| `Sync:MaxHistoricalMessages` | Max messages per channel to backfill | `10000` |
| `Attachments:EnableCaching` | Download attachments locally | `false` |

### Environment Variables (Docker)

```bash
Discord__Token=your-bot-token
Sync__MaxHistoricalMessages=50000
Attachments__EnableCaching=true
```

📖 See the [full configuration reference](docs/guides/configuration.md) for all options.

---

## 🏗️ Architecture

DiscordDataMirror follows **Domain-Driven Design** and **Clean Architecture** principles:

```
┌─────────────────────────────────────────────────┐
│              Presentation                       │
│    (Dashboard, Bot Worker Service)              │
├─────────────────────────────────────────────────┤
│              Application                        │
│    (Commands, Queries, Handlers, DTOs)          │
├─────────────────────────────────────────────────┤
│              Domain                             │
│    (Entities, Aggregates, Value Objects)        │
├─────────────────────────────────────────────────┤
│              Infrastructure                     │
│    (EF Core, Discord.Net, Repositories)         │
└─────────────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 |
| Orchestration | Aspire 13.1 |
| Dashboard | Blazor Server + MudBlazor |
| Database | PostgreSQL 17 |
| Discord API | Discord.Net |
| CQRS | MediatR |
| ORM | Entity Framework Core |

📖 See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed documentation.

---

## 📁 Project Structure

```
DiscordDataMirror/
├── src/
│   ├── DiscordDataMirror.AppHost/           # Aspire orchestrator (start here!)
│   ├── DiscordDataMirror.ServiceDefaults/   # Shared Aspire defaults
│   ├── DiscordDataMirror.Domain/            # Domain layer (entities, events)
│   ├── DiscordDataMirror.Application/       # Application layer (CQRS)
│   ├── DiscordDataMirror.Infrastructure/    # Infrastructure layer (EF, Discord)
│   ├── DiscordDataMirror.Bot/               # Discord bot worker
│   └── DiscordDataMirror.Dashboard/         # Blazor dashboard
├── tests/                                    # Unit and integration tests
├── docs/                                     # Documentation (DocFX)
├── scripts/                                  # Setup and utility scripts
├── publish/                                  # Docker Compose files
└── README.md
```

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](docs/guides/getting-started.md) | Complete setup walkthrough |
| [Features](docs/guides/features.md) | Detailed feature descriptions |
| [Configuration](docs/guides/configuration.md) | All configuration options |
| [Bot Setup](docs/BOT_SETUP.md) | Discord Developer Portal guide |
| [Deployment](docs/DEPLOYMENT.md) | Production deployment with Docker |
| [Architecture](ARCHITECTURE.md) | System design and patterns |
| [FAQ](docs/guides/faq.md) | Frequently asked questions |
| [Troubleshooting](docs/guides/troubleshooting.md) | Common issues and solutions |
| [Contributing](docs/guides/contributing.md) | How to contribute |

---

## 🛡️ Privacy & Security

### What data is collected?
- All messages, members, channels, and roles from servers the bot can access
- No data leaves your infrastructure — everything stays on your servers

### Security best practices
- Store bot tokens in user secrets or secure vaults (never in code)
- Use HTTPS for dashboard access
- Restrict database access to trusted networks
- Enable PostgreSQL encryption at rest for sensitive data
- Regular backups to secure, separate storage

### Compliance considerations
- Inform your community that messages are being archived
- Honor GDPR/CCPA data deletion requests
- Consider retention policies for sensitive data

---

## 🤝 Contributing

We welcome contributions of all kinds!

- 🐛 **Bug reports**: [Open an issue](https://github.com/JerrettDavis/DiscordDataMirror/issues)
- 💡 **Feature ideas**: [Start a discussion](https://github.com/JerrettDavis/DiscordDataMirror/discussions)
- 📝 **Documentation**: Fix typos, add examples, improve clarity
- 🔧 **Code**: See [CONTRIBUTING.md](docs/guides/contributing.md)

```bash
# Development workflow
git clone https://github.com/JerrettDavis/DiscordDataMirror.git
cd DiscordDataMirror
dotnet build
dotnet test
```

---

## 📜 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- [Discord.Net](https://github.com/discord-net/Discord.Net) — Discord API wrapper
- [MudBlazor](https://mudblazor.com/) — Blazor component library
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) — Cloud-native orchestration

---

<div align="center">

**Made with ❤️ by the community**

⭐ Star this repo if you find it useful!

</div>
