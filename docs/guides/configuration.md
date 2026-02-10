# Configuration Reference

This page documents all configuration options for DiscordDataMirror.

## Configuration Sources

DiscordDataMirror uses the standard .NET configuration system. Settings are loaded from (in order of precedence):

1. **Environment variables** (highest priority)
2. **User secrets** (development only)
3. **appsettings.{Environment}.json**
4. **appsettings.json** (lowest priority)

## Discord Settings

### Bot Token (Required)

The authentication token for your Discord bot.

| Setting | `Discord:Token` |
|---------|-----------------|
| Environment Variable | `Discord__Token` |
| Type | string |
| Required | Yes |
| Sensitive | ⚠️ Yes — never commit to source control |

**How to set:**

```bash
# User secrets (development)
dotnet user-secrets set "Discord:Token" "your-token-here"

# Environment variable (production)
export Discord__Token="your-token-here"
```

### Gateway Intents

Controls which Discord events the bot receives.

| Setting | `Discord:Intents` |
|---------|-------------------|
| Type | array of strings |
| Default | `["Guilds", "GuildMembers", "GuildMessages", "MessageContent", "GuildMessageReactions"]` |

**Available intents:**
- `Guilds` — Server structure changes
- `GuildMembers` — Member join/leave/update (privileged)
- `GuildMessages` — New messages in servers
- `GuildMessageReactions` — Reaction add/remove
- `MessageContent` — Actual message text (privileged)
- `DirectMessages` — DM messages (if needed)

**Example:**
```json
{
  "Discord": {
    "Intents": ["Guilds", "GuildMembers", "GuildMessages", "MessageContent"]
  }
}
```

### Sync on Startup

Whether to perform a full sync when the bot starts.

| Setting | `Discord:SyncOnStartup` |
|---------|------------------------|
| Type | boolean |
| Default | `true` |

Set to `false` if you want faster startup and don't need historical backfill.

---

## Sync Settings

### Message Batch Size

Number of messages to fetch per API request during historical sync.

| Setting | `Sync:MessageBatchSize` |
|---------|------------------------|
| Type | integer |
| Default | `100` |
| Range | 1-100 (Discord API limit) |

Lower values = slower sync but less memory usage.

### Maximum Historical Messages

Limit on how many historical messages to fetch per channel.

| Setting | `Sync:MaxHistoricalMessages` |
|---------|------------------------------|
| Type | integer |
| Default | `10000` |

Set to `-1` for unlimited (may take very long for active channels).

### Sync Interval

How often to re-sync server structure and member list.

| Setting | `Sync:SyncIntervalMinutes` |
|---------|---------------------------|
| Type | integer |
| Default | `60` |

Real-time messages don't depend on this — they're captured via gateway events.

### Parallel Sync Channels

Number of channels to sync simultaneously during backfill.

| Setting | `Sync:ParallelChannels` |
|---------|------------------------|
| Type | integer |
| Default | `3` |

Higher values = faster sync but more API pressure. Be careful of rate limits.

---

## Attachment Settings

### Enable Caching

Whether to download and store attachments locally.

| Setting | `Attachments:EnableCaching` |
|---------|----------------------------|
| Type | boolean |
| Default | `false` |

When enabled, attachments are downloaded to local storage.

### Cache Path

Directory for storing cached attachments.

| Setting | `Attachments:CachePath` |
|---------|------------------------|
| Type | string |
| Default | `./attachments` |

Relative paths are relative to the working directory.

### Maximum File Size

Maximum attachment size to cache (in bytes).

| Setting | `Attachments:MaxFileSizeBytes` |
|---------|-------------------------------|
| Type | integer |
| Default | `26214400` (25 MB) |

Attachments larger than this are tracked but not downloaded.

### Allowed Content Types

MIME types to cache. Empty array = all types allowed.

| Setting | `Attachments:AllowedContentTypes` |
|---------|----------------------------------|
| Type | array of strings |
| Default | `[]` (all types) |

**Example — images only:**
```json
{
  "Attachments": {
    "AllowedContentTypes": ["image/png", "image/jpeg", "image/gif", "image/webp"]
  }
}
```

---

## Database Settings

### Connection String

PostgreSQL connection string.

| Setting | `ConnectionStrings:discorddatamirror` |
|---------|--------------------------------------|
| Type | string |
| Default | Provided by Aspire |

In development, Aspire automatically configures this. For production:

```json
{
  "ConnectionStrings": {
    "discorddatamirror": "Host=localhost;Port=5432;Database=discorddatamirror;Username=postgres;Password=secret"
  }
}
```

### Command Timeout

Database command timeout in seconds.

| Setting | `Database:CommandTimeoutSeconds` |
|---------|----------------------------------|
| Type | integer |
| Default | `30` |

Increase for large data operations.

---

## Dashboard Settings

### Base URL

External URL for the dashboard (used in links).

| Setting | `Dashboard:BaseUrl` |
|---------|---------------------|
| Type | string |
| Default | `https://localhost:5001` |

### Items Per Page

Default pagination size.

| Setting | `Dashboard:ItemsPerPage` |
|---------|-------------------------|
| Type | integer |
| Default | `50` |

### Enable SignalR

Real-time updates via SignalR.

| Setting | `Dashboard:EnableRealTimeUpdates` |
|---------|----------------------------------|
| Type | boolean |
| Default | `true` |

Disable if you're having connection issues.

---

## Logging Settings

Uses standard .NET logging configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Discord": "Warning",
      "DiscordDataMirror": "Debug"
    }
  }
}
```

### Log Levels

| Level | When to use |
|-------|-------------|
| `Trace` | Very detailed, including Discord payloads |
| `Debug` | Useful for development |
| `Information` | Normal operation (default) |
| `Warning` | Potential issues |
| `Error` | Failures that need attention |
| `Critical` | Application-breaking issues |

---

## Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "Discord": {
    "SyncOnStartup": false
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "DiscordDataMirror": "Information"
    }
  },
  "Sync": {
    "MaxHistoricalMessages": -1
  },
  "Attachments": {
    "EnableCaching": true,
    "CachePath": "/data/attachments"
  }
}
```

---

## Docker / Container Configuration

When running in Docker, use environment variables:

```yaml
# docker-compose.yaml
services:
  bot:
    environment:
      Discord__Token: "${DISCORD_TOKEN}"
      Sync__MaxHistoricalMessages: "50000"
      Attachments__EnableCaching: "true"
      Attachments__CachePath: "/data/attachments"
    volumes:
      - attachment-cache:/data/attachments
```

---

## Full Example Configuration

```json
{
  "Discord": {
    "Token": "YOUR_TOKEN_HERE",
    "Intents": ["Guilds", "GuildMembers", "GuildMessages", "MessageContent", "GuildMessageReactions"],
    "SyncOnStartup": true
  },
  "Sync": {
    "MessageBatchSize": 100,
    "MaxHistoricalMessages": 50000,
    "SyncIntervalMinutes": 60,
    "ParallelChannels": 3
  },
  "Attachments": {
    "EnableCaching": true,
    "CachePath": "./attachments",
    "MaxFileSizeBytes": 52428800,
    "AllowedContentTypes": []
  },
  "Dashboard": {
    "BaseUrl": "https://archive.example.com",
    "ItemsPerPage": 50,
    "EnableRealTimeUpdates": true
  },
  "Database": {
    "CommandTimeoutSeconds": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Discord": "Warning"
    }
  }
}
```
