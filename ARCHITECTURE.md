# DiscordDataMirror - Architecture

## Overview

DiscordDataMirror is a .NET 10 Aspire 13.1.0 solution that monitors, scrapes, and backs up all data from Discord servers, providing a Blazor dashboard for visualization and navigation.

## Solution Structure

```
DiscordDataMirror/
├── src/
│   ├── DiscordDataMirror.AppHost/           # Aspire orchestrator
│   ├── DiscordDataMirror.ServiceDefaults/   # Shared Aspire defaults
│   ├── DiscordDataMirror.Domain/            # Domain layer (entities, aggregates, events)
│   ├── DiscordDataMirror.Application/       # Application layer (CQRS, services)
│   ├── DiscordDataMirror.Infrastructure/    # Infrastructure (EF Core, Discord.Net)
│   ├── DiscordDataMirror.Bot/               # Worker service for Discord bot
│   └── DiscordDataMirror.Dashboard/         # Blazor Server dashboard
├── tests/
│   ├── DiscordDataMirror.Domain.Tests/
│   ├── DiscordDataMirror.Application.Tests/
│   └── DiscordDataMirror.Integration.Tests/
├── ARCHITECTURE.md
├── TODO.md
└── README.md
```

## Bounded Contexts

### 1. **Guild Context** (Core)
- Guilds, Channels, Roles, Categories
- The structural backbone of Discord data

### 2. **Member Context**
- Users, GuildMembers, Presence, UserMaps
- Cross-server user tracking (future: identity correlation)

### 3. **Message Context**
- Messages, Attachments, Embeds, Reactions
- The content layer

### 4. **Thread Context**
- Threads, ThreadMembers
- Ephemeral and archived thread data

### 5. **Audit Context** (Future)
- AuditLogs, ModActions
- Tracking server changes

## Layer Architecture (Clean/Onion)

```
┌─────────────────────────────────────────────┐
│              Presentation                    │
│    (Dashboard, Bot Worker Service)          │
├─────────────────────────────────────────────┤
│              Application                     │
│    (Commands, Queries, Handlers, DTOs)      │
├─────────────────────────────────────────────┤
│              Domain                          │
│    (Entities, Aggregates, Value Objects,    │
│     Domain Events, Specifications)          │
├─────────────────────────────────────────────┤
│              Infrastructure                  │
│    (EF Core, Discord.Net, Repositories)     │
└─────────────────────────────────────────────┘
```

## Domain Model

### Aggregates

#### Guild (Aggregate Root)
- `GuildId` (ulong → stored as decimal/string for PG compatibility)
- `Name`, `IconUrl`, `Description`
- `OwnerId`
- `CreatedAt`, `LastSyncedAt`
- Navigation: Channels, Roles, Members

#### Channel (Entity)
- `ChannelId`, `GuildId`
- `Name`, `Topic`, `Type` (Text, Voice, Category, Thread, Forum, etc.)
- `ParentId` (for categories/threads)
- `Position`, `IsNsfw`
- Navigation: Messages, Threads

#### Message (Aggregate Root)
- `MessageId`, `ChannelId`, `AuthorId`
- `Content`, `CleanContent`
- `Timestamp`, `EditedTimestamp`
- `IsPinned`, `Type`
- Navigation: Attachments, Embeds, Reactions, ReferencedMessage

#### User (Aggregate Root)
- `UserId`
- `Username`, `Discriminator`, `GlobalName`
- `AvatarUrl`, `IsBot`
- `CreatedAt`

#### GuildMember (Entity)
- `UserId`, `GuildId`
- `Nickname`, `JoinedAt`
- `Roles` (list of RoleIds)
- `IsPending`

#### Role (Entity)
- `RoleId`, `GuildId`
- `Name`, `Color`, `Position`
- `Permissions`, `IsHoisted`, `IsMentionable`

#### Attachment (Value Object)
- `AttachmentId`, `MessageId`
- `Filename`, `Url`, `ProxyUrl`
- `Size`, `Width`, `Height`
- `ContentType`
- `LocalPath` (for cached copies)

#### Embed (Value Object)
- `MessageId`, `Index`
- `Type`, `Title`, `Description`, `Url`
- `Timestamp`, `Color`
- `Footer`, `Image`, `Thumbnail`, `Author`, `Fields` (JSON)

#### Reaction (Entity)
- `MessageId`, `EmoteKey` (unified name or id)
- `Count`
- `Users` (list of UserIds who reacted)

#### Thread (Entity extends Channel)
- `ParentChannelId`
- `OwnerId`, `MessageCount`, `MemberCount`
- `IsArchived`, `IsLocked`, `ArchiveTimestamp`

### Value Objects
- `Snowflake` - Discord ID wrapper (ulong → string for DB)
- `EmoteRef` - Unicode emoji or custom emote reference
- `Permission` - Discord permission flags
- `Color` - Role/embed color

## Database Schema (PostgreSQL)

### Core Tables

```sql
-- Guilds
CREATE TABLE guilds (
    id VARCHAR(20) PRIMARY KEY,  -- Snowflake as string
    name VARCHAR(100) NOT NULL,
    icon_url TEXT,
    description TEXT,
    owner_id VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    last_synced_at TIMESTAMPTZ,
    raw_json JSONB  -- Full Discord payload for future-proofing
);

-- Channels
CREATE TABLE channels (
    id VARCHAR(20) PRIMARY KEY,
    guild_id VARCHAR(20) NOT NULL REFERENCES guilds(id),
    parent_id VARCHAR(20) REFERENCES channels(id),
    name VARCHAR(100) NOT NULL,
    type SMALLINT NOT NULL,
    topic TEXT,
    position INT,
    is_nsfw BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL,
    last_synced_at TIMESTAMPTZ,
    raw_json JSONB
);

-- Users (global, not per-guild)
CREATE TABLE users (
    id VARCHAR(20) PRIMARY KEY,
    username VARCHAR(32) NOT NULL,
    discriminator VARCHAR(4),
    global_name VARCHAR(32),
    avatar_url TEXT,
    is_bot BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL,
    last_seen_at TIMESTAMPTZ,
    raw_json JSONB
);

-- Guild Members (junction with extra data)
CREATE TABLE guild_members (
    user_id VARCHAR(20) NOT NULL REFERENCES users(id),
    guild_id VARCHAR(20) NOT NULL REFERENCES guilds(id),
    nickname VARCHAR(32),
    joined_at TIMESTAMPTZ,
    is_pending BOOLEAN DEFAULT FALSE,
    role_ids JSONB,  -- Array of role IDs
    last_synced_at TIMESTAMPTZ,
    raw_json JSONB,
    PRIMARY KEY (user_id, guild_id)
);

-- Roles
CREATE TABLE roles (
    id VARCHAR(20) PRIMARY KEY,
    guild_id VARCHAR(20) NOT NULL REFERENCES guilds(id),
    name VARCHAR(100) NOT NULL,
    color INT,
    position INT,
    permissions VARCHAR(20),  -- BigInt as string
    is_hoisted BOOLEAN DEFAULT FALSE,
    is_mentionable BOOLEAN DEFAULT FALSE,
    raw_json JSONB
);

-- Messages
CREATE TABLE messages (
    id VARCHAR(20) PRIMARY KEY,
    channel_id VARCHAR(20) NOT NULL REFERENCES channels(id),
    author_id VARCHAR(20) NOT NULL REFERENCES users(id),
    content TEXT,
    clean_content TEXT,
    timestamp TIMESTAMPTZ NOT NULL,
    edited_timestamp TIMESTAMPTZ,
    type SMALLINT NOT NULL DEFAULT 0,
    is_pinned BOOLEAN DEFAULT FALSE,
    is_tts BOOLEAN DEFAULT FALSE,
    referenced_message_id VARCHAR(20),
    raw_json JSONB
);

-- Attachments
CREATE TABLE attachments (
    id VARCHAR(20) PRIMARY KEY,
    message_id VARCHAR(20) NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    filename VARCHAR(255) NOT NULL,
    url TEXT NOT NULL,
    proxy_url TEXT,
    size BIGINT,
    width INT,
    height INT,
    content_type VARCHAR(100),
    local_path TEXT,  -- Path to cached file
    is_cached BOOLEAN DEFAULT FALSE
);

-- Embeds (stored as JSONB since structure varies)
CREATE TABLE embeds (
    id SERIAL PRIMARY KEY,
    message_id VARCHAR(20) NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    index SMALLINT NOT NULL,
    type VARCHAR(20),
    data JSONB NOT NULL
);

-- Reactions
CREATE TABLE reactions (
    message_id VARCHAR(20) NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    emote_key VARCHAR(100) NOT NULL,  -- "emoji_name" or "custom:id:name"
    count INT NOT NULL DEFAULT 0,
    user_ids JSONB,  -- Array of user IDs who reacted
    PRIMARY KEY (message_id, emote_key)
);

-- Threads (extension of channels)
CREATE TABLE threads (
    id VARCHAR(20) PRIMARY KEY REFERENCES channels(id),
    parent_channel_id VARCHAR(20) NOT NULL REFERENCES channels(id),
    owner_id VARCHAR(20) REFERENCES users(id),
    message_count INT DEFAULT 0,
    member_count INT DEFAULT 0,
    is_archived BOOLEAN DEFAULT FALSE,
    is_locked BOOLEAN DEFAULT FALSE,
    archive_timestamp TIMESTAMPTZ,
    auto_archive_duration INT
);

-- User Maps (for cross-server identity tracking)
CREATE TABLE user_maps (
    id SERIAL PRIMARY KEY,
    canonical_user_id VARCHAR(20) REFERENCES users(id),
    mapped_user_id VARCHAR(20) NOT NULL REFERENCES users(id),
    confidence DECIMAL(3,2),  -- 0.00 to 1.00
    mapping_type VARCHAR(20),  -- 'manual', 'username', 'avatar', etc.
    created_at TIMESTAMPTZ DEFAULT NOW(),
    notes TEXT,
    UNIQUE (canonical_user_id, mapped_user_id)
);

-- Sync State (tracking what's been synced)
CREATE TABLE sync_state (
    id SERIAL PRIMARY KEY,
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(20) NOT NULL,
    last_synced_at TIMESTAMPTZ NOT NULL,
    last_message_id VARCHAR(20),  -- For incremental message sync
    status VARCHAR(20) DEFAULT 'idle',
    error_message TEXT,
    UNIQUE (entity_type, entity_id)
);
```

### Indexes

```sql
CREATE INDEX idx_messages_channel_timestamp ON messages(channel_id, timestamp DESC);
CREATE INDEX idx_messages_author ON messages(author_id);
CREATE INDEX idx_messages_content_search ON messages USING gin(to_tsvector('english', content));
CREATE INDEX idx_guild_members_guild ON guild_members(guild_id);
CREATE INDEX idx_channels_guild ON channels(guild_id);
CREATE INDEX idx_attachments_message ON attachments(message_id);
CREATE INDEX idx_embeds_message ON embeds(message_id);
```

## CQRS Pattern (MediatR)

### Commands (Write Operations)
- `SyncGuildCommand` - Full guild sync
- `SyncChannelMessagesCommand` - Sync messages for a channel
- `UpsertMessageCommand` - Insert/update single message
- `CacheAttachmentCommand` - Download and cache attachment
- `MapUsersCommand` - Create user identity mapping

### Queries (Read Operations)
- `GetGuildQuery` - Get guild with channels/roles
- `GetChannelMessagesQuery` - Paginated messages
- `SearchMessagesQuery` - Full-text search
- `GetUserActivityQuery` - User's messages across servers
- `GetUserMapSuggestionsQuery` - Suggested user mappings

## Bot Service Architecture

```
┌────────────────────────────────────────┐
│           Discord Gateway              │
└────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────┐
│         Discord.Net Client             │
│   (DiscordSocketClient / Sharding)     │
└────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────┐
│         Event Handlers                 │
│  - GuildAvailable    - MessageReceived │
│  - MessageUpdated    - MessageDeleted  │
│  - UserJoined        - ReactionAdded   │
│  - ChannelCreated    - ThreadCreated   │
└────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────┐
│         MediatR Pipeline               │
│    (Commands → Handlers → Repos)       │
└────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────┐
│         PostgreSQL (via EF Core)       │
└────────────────────────────────────────┘
```

## Dashboard Features

### Views
1. **Guild Overview** - List of monitored guilds with stats
2. **Channel Browser** - Hierarchical channel tree
3. **Message Viewer** - Discord-like message display with search
4. **User Profile** - User activity across all guilds
5. **User Maps** - Cross-server identity management
6. **Sync Status** - Real-time sync progress and errors
7. **Analytics** - Message volume, active users, etc.

### Tech Stack
- Blazor Server (for real-time updates)
- MudBlazor or Radzen for UI components
- SignalR for live updates from bot

## Future Considerations

### Wiki Generation
- Extract pinned messages as wiki pages
- Thread summaries as documentation
- AI-powered topic extraction

### User Identity Correlation
- Username similarity matching
- Avatar hash comparison
- Activity pattern analysis
- Manual confirmation workflow

### Data Export
- Markdown/HTML export
- JSON backup format
- Search result export

## Configuration

```json
{
  "Discord": {
    "Token": "...",
    "Intents": ["Guilds", "GuildMessages", "GuildMembers", "MessageContent"],
    "SyncOnStartup": true,
    "AttachmentCachePath": "./attachments"
  },
  "Sync": {
    "MessageBatchSize": 100,
    "MaxHistoricalMessages": 10000,
    "SyncIntervalMinutes": 60
  }
}
```

## Design Decisions

1. **Snowflakes as strings**: PostgreSQL doesn't handle unsigned 64-bit integers well. Strings preserve precision and are human-readable.

2. **raw_json columns**: Store original Discord payloads for future-proofing. New Discord features won't require schema changes.

3. **Soft deletes considered but rejected**: Discord data is already ephemeral. We track deletions via missing IDs in sync.

4. **Single DbContext**: Keep it simple. Split only if performance demands.

5. **Blazor Server over WASM**: Real-time updates from bot are easier; data stays server-side.

6. **MediatR over direct services**: Clean separation, easy testing, pipeline behaviors for logging/validation.
