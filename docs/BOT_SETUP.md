# Discord Bot Setup Guide

This guide walks you through creating and configuring a Discord bot for DiscordDataMirror.

## Overview

DiscordDataMirror is a **read-only** Discord bot that monitors and backs up server data. It doesn't need to send messages, manage roles, or kick users—it just observes and records.

## Step 1: Create a Discord Application

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **"New Application"**
3. Enter a name (e.g., "DiscordDataMirror" or "Server Backup Bot")
4. Accept the Terms of Service
5. Click **"Create"**

## Step 2: Create the Bot User

1. In your application, click **"Bot"** in the left sidebar
2. Click **"Add Bot"** (if prompted, confirm)
3. Optionally customize:
   - **Username**: Change the bot's display name
   - **Icon**: Upload a profile picture
4. Under **"Privileged Gateway Intents"**, enable:
   - ✅ **SERVER MEMBERS INTENT** - Required to see member list and member updates
   - ✅ **MESSAGE CONTENT INTENT** - Required to read message content

> ⚠️ **Important**: Without these intents, the bot will only receive partial data!

## Step 3: Get Your Bot Token

1. In the **Bot** section, click **"Reset Token"** (or "View Token" if first time)
2. Copy the token immediately—**you won't be able to see it again!**
3. Store it securely (we'll configure it in the next step)

> 🔒 **Security Warning**: Never commit your token to git or share it publicly. Anyone with your token can control your bot.

## Step 4: Configure the Token

### Option A: Using the Setup Script (Recommended)

```powershell
.\scripts\setup-bot.ps1
```

This will:
- Prompt for your bot token
- Store it in .NET user secrets
- Validate the token works
- Generate your invite URL

### Option B: Manual Configuration

Using .NET User Secrets:
```bash
cd src/DiscordDataMirror.Bot
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN_HERE"
```

Or using environment variables (for production):
```bash
# Windows
set Discord__Token=YOUR_BOT_TOKEN_HERE

# PowerShell
$env:Discord__Token = "YOUR_BOT_TOKEN_HERE"

# Linux/Mac
export Discord__Token=YOUR_BOT_TOKEN_HERE
```

## Step 5: Invite the Bot to Your Server

### Required Permissions

The bot needs these permissions (very minimal!):

| Permission | Hex Value | Description |
|------------|-----------|-------------|
| View Channels | 0x0400 | See channels and their names |
| Read Message History | 0x10000 | Read messages in channels |

**Permissions Integer: `66560`**

### Required Gateway Intents

These are enabled in the Developer Portal (Step 2):

| Intent | Required For |
|--------|--------------|
| Guilds | Guild structure, channels, roles |
| Guild Members | Member list, join/leave events |
| Guild Messages | New messages in channels |
| Guild Message Reactions | Reactions on messages |
| Message Content | Actual message text |
| Direct Messages | DM messages (if needed) |

### Generate Invite URL

#### Using the Setup Script
The `setup-bot.ps1` script generates this automatically.

#### Manual URL Construction

Replace `YOUR_CLIENT_ID` with your application's Client ID (found in "General Information"):

```
https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&permissions=66560&scope=bot
```

Or use the Developer Portal:
1. Go to **OAuth2 → URL Generator**
2. Select scope: **bot**
3. Select permissions:
   - ✅ View Channels
   - ✅ Read Message History
4. Copy the generated URL

### Add to Server

1. Open the invite URL in your browser
2. Select the server you want to add the bot to
3. Click **"Authorize"**
4. Complete the CAPTCHA if prompted

> 📝 You need **Manage Server** permission on the target server to add bots.

## Step 6: Verify Everything Works

1. Start the application:
   ```bash
   cd src/DiscordDataMirror.AppHost
   dotnet run
   ```

2. Check the Aspire dashboard (https://localhost:17113) for:
   - Bot service is running
   - No error logs
   - "Discord client ready" message

3. The bot should appear as online in your Discord server

## Troubleshooting

### "Discord token not configured"
- Ensure you set the token using user secrets or environment variable
- Check the token doesn't have extra spaces or quotes

### "Invalid Token"
- The token might be wrong or expired
- Try resetting the token in Developer Portal

### "Missing Access" or "Missing Permissions"
- Re-invite the bot with correct permissions
- Ensure the bot's role is high enough to see channels

### "Disallowed Intents"
- Enable the privileged intents in Developer Portal
- Wait a few minutes for changes to propagate

### Bot Shows Offline
- Check if the application is running
- Look for connection errors in logs
- Verify your network allows Discord API access

## Security Best Practices

1. **Never share your bot token** - Treat it like a password
2. **Use user secrets for development** - Not environment variables in shell history
3. **Use secure secret management in production** - Azure Key Vault, AWS Secrets Manager, etc.
4. **Regenerate token if compromised** - Use "Reset Token" in Developer Portal
5. **Monitor bot activity** - Check Audit Log for unexpected actions

## Quick Reference

| Item | Value |
|------|-------|
| Permissions Integer | `66560` |
| Required Scopes | `bot` |
| Privileged Intents | Server Members, Message Content |
| User Secrets Key | `Discord:Token` |
| Environment Variable | `Discord__Token` |
