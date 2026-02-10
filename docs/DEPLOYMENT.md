# Deployment Guide

Discord Data Mirror can be deployed using Docker containers orchestrated with Docker Compose or Kubernetes.

## Prerequisites

- Docker Desktop or Docker Engine with Compose plugin
- .NET 10 SDK (for building containers)
- A Discord bot token ([setup guide](BOT_SETUP.md))

## Quick Start with Docker Compose

### 1. Generate Deployment Artifacts

From the repository root, use the Aspire CLI to generate Docker Compose configuration:

```bash
cd src/DiscordDataMirror.AppHost
aspire publish -o ../../deploy
```

This creates a `docker-compose.yaml` with:
- PostgreSQL database with persistent storage
- Dashboard web application
- Discord bot worker service
- Aspire dashboard for monitoring

### 2. Build Container Images

Use the .NET SDK to build OCI-compliant container images:

```bash
# Build both containers locally
./scripts/build-containers.ps1

# Or build with a specific tag
./scripts/build-containers.ps1 -Tag v1.0.0

# Build and push to a registry
./scripts/build-containers.ps1 -Tag v1.0.0 -Registry ghcr.io/yourusername -Push
```

Alternatively, build manually:

```bash
# Dashboard
dotnet publish src/DiscordDataMirror.Dashboard/DiscordDataMirror.Dashboard.csproj \
    --os linux --arch x64 -c Release /t:PublishContainer

# Bot
dotnet publish src/DiscordDataMirror.Bot/DiscordDataMirror.Bot.csproj \
    --os linux --arch x64 -c Release /t:PublishContainer
```

### 3. Configure Environment

```bash
cd deploy
cp .env.example .env
```

Edit `.env` with your values:

```env
# Required
POSTGRES_PASSWORD=your-secure-password
DISCORD_TOKEN=your-discord-bot-token

# Optional
DASHBOARD_PORT=8080
DASHBOARD_IMAGE=discorddatamirror-dashboard:latest
BOT_IMAGE=discorddatamirror-bot:latest
```

### 4. Start Services

```bash
docker compose up -d
```

Access the dashboard at `http://localhost:8080` (or your configured port).

## Container Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Docker Network (aspire)                  │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  PostgreSQL │  │  Dashboard  │  │        Bot          │  │
│  │   :5432     │←─│   :8080     │←─│   (worker service)  │  │
│  │             │  │   Blazor    │  │   Discord.NET       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│         ↑                ↑                    ↑              │
│         └────────────────┴────────────────────┘              │
│                    Aspire Dashboard                          │
│                      :18888 (metrics)                        │
└─────────────────────────────────────────────────────────────┘
```

## Production Considerations

### Persistent Storage

The PostgreSQL data is stored in a Docker volume (`discorddatamirror-postgres-data`).
For production, consider:

- Regular database backups
- Using a managed PostgreSQL service
- Configuring volume drivers for cloud storage

### Attachment Storage

Discord attachments are cached locally. Configure a persistent volume:

```yaml
# Add to docker-compose.yaml under the dashboard service
volumes:
  - type: bind
    source: /path/to/attachments
    target: /app/attachments
```

And set the environment variable:
```env
Attachments__StoragePath=/app/attachments
```

### Secrets Management

For production, avoid storing secrets in `.env` files. Consider:

- Docker Secrets
- HashiCorp Vault
- Cloud provider secret managers (AWS Secrets Manager, Azure Key Vault)

### Scaling

- **Dashboard**: Can run multiple replicas behind a load balancer
- **Bot**: Should run as a single instance (Discord gateway limitations)
- **Database**: Consider read replicas for heavy query loads

## Kubernetes Deployment

Aspire also supports Kubernetes. Generate manifests with:

```bash
# Add Kubernetes package to AppHost
dotnet add src/DiscordDataMirror.AppHost package Aspire.Hosting.Kubernetes

# Publish to Kubernetes manifests
aspire publish -o deploy/k8s --publisher kubernetes
```

## Monitoring

The Aspire Dashboard provides:
- Distributed tracing
- Metrics visualization
- Log aggregation
- Health checks

Access at `http://localhost:18888` when running with Docker Compose.

## Troubleshooting

### Container won't start

Check logs:
```bash
docker compose logs dashboard
docker compose logs bot
```

### Database connection issues

Verify PostgreSQL is healthy:
```bash
docker compose exec postgres pg_isready
```

### Bot not connecting to Discord

1. Verify your token is correct in `.env`
2. Check bot has required intents enabled in Discord Developer Portal
3. Review bot logs: `docker compose logs bot`
