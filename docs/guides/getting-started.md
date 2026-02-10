# Getting Started

This guide will walk you through setting up DiscordDataMirror from scratch. By the end, you'll have a fully functional Discord archiving system running on your machine.

## Prerequisites

Before you begin, ensure you have the following installed:

| Requirement | Minimum Version | Download |
|-------------|-----------------|----------|
| .NET SDK | 10.0 | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Docker Desktop | 4.0+ | [Download](https://www.docker.com/products/docker-desktop) |
| Git | 2.0+ | [Download](https://git-scm.com/downloads) |

### Verify Installation

```bash
# Check .NET version
dotnet --version
# Should output: 10.0.x

# Check Docker is running
docker --version
# Should output: Docker version 24.x or higher
```

## Step 1: Clone the Repository

```bash
git clone https://github.com/JerrettDavis/DiscordDataMirror.git
cd DiscordDataMirror
```

## Step 2: Create a Discord Bot

You'll need a Discord bot to monitor your servers. Follow the [complete bot setup guide](../BOT_SETUP.md) or use the quick steps below:

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **"New Application"** and name it (e.g., "Server Backup Bot")
3. Navigate to **Bot** → **Add Bot**
4. Enable **Privileged Gateway Intents**:
   - ✅ SERVER MEMBERS INTENT
   - ✅ MESSAGE CONTENT INTENT
5. Click **Reset Token** and copy your bot token

> ⚠️ **Keep your token secret!** Never commit it to git or share it publicly.

## Step 3: Configure the Bot Token

### Option A: Interactive Setup Script (Recommended)

```powershell
# Windows PowerShell
.\scripts\setup-bot.ps1
```

```bash
# Linux/Mac
./scripts/setup-bot.sh
```

The script will:
- Prompt for your bot token
- Store it securely in .NET User Secrets
- Validate the token works
- Generate your bot invite URL

### Option B: Manual Configuration

```bash
cd src/DiscordDataMirror.Bot
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN_HERE"
```

## Step 4: Invite the Bot to Your Server

1. The setup script provides an invite URL, or construct one manually:
   ```
   https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&permissions=66560&scope=bot
   ```

2. Open the URL in your browser
3. Select the server you want to monitor
4. Click **Authorize**

> 📝 **Note:** You need "Manage Server" permission to add bots.

## Step 5: Run the Application

```bash
cd src/DiscordDataMirror.AppHost
dotnet run
```

On first run, .NET Aspire will:
1. Start a PostgreSQL container
2. Apply database migrations
3. Start the Discord bot service
4. Launch the Blazor dashboard

## Step 6: Verify Everything Works

### Aspire Dashboard
Open [https://localhost:17113](https://localhost:17113) to see:
- All running services (green = healthy)
- Real-time logs from each service
- Distributed traces for debugging

### Data Dashboard
Open [https://localhost:5001](https://localhost:5001) to:
- Browse your Discord servers
- View channels and messages
- Search across all data

### Discord Server
Check your Discord server — the bot should appear online!

## What Happens Next?

Once running, DiscordDataMirror will:

1. **Initial Sync** — Fetch all accessible message history (may take time for large servers)
2. **Real-time Capture** — Record all new messages, edits, and deletions
3. **Background Tasks** — Periodically sync user profiles and server structure

## Common Issues

### "Discord token not configured"
- Ensure you ran the setup script or set the user secret correctly
- Check there are no extra spaces in your token

### "Missing Access" errors in logs
- Ensure the bot has the required permissions (66560)
- Verify privileged intents are enabled in Developer Portal

### Bot shows offline
- Check Docker Desktop is running
- Look for errors in the Aspire dashboard logs
- Verify your network allows Discord API access

### Database connection errors
- Ensure Docker Desktop is running
- Try restarting the application
- Check no other service is using port 5432

## Next Steps

- [Configure advanced options](configuration.md)
- [Understand the features](features.md)
- [Deploy to production](../DEPLOYMENT.md)
- [Contribute to the project](contributing.md)

## Video Walkthrough

*Coming soon: A step-by-step video guide for setting up DiscordDataMirror.*
