# Features

DiscordDataMirror is packed with features designed to give you complete control over your Discord data. This page explains each feature in detail.

## Core Features

### 📝 Message Archiving

**What it does:** Captures every message sent in monitored servers, including edits and deletions.

**Details:**
- Messages are stored with full metadata (author, timestamp, channel, etc.)
- Edit history is preserved — see what a message said before it was changed
- Deleted messages are marked as deleted but retained in the database
- Supports all message types: regular, replies, system messages, pins, etc.
- Embeds (link previews, rich content) are stored as JSON

**Dashboard features:**
- Discord-like message rendering
- Jump to replied messages
- View edit history
- See deleted message indicator

---

### 👥 Member Tracking

**What it does:** Maintains a complete record of server members and their profiles.

**Details:**
- User profiles: username, display name, avatar, account creation date
- Guild-specific data: nickname, roles, join date
- Tracks when users join and leave
- Bot accounts are flagged for easy filtering

**Dashboard features:**
- Member list with search and filters
- User profile cards
- Activity timeline per user
- Cross-server presence (same user in multiple servers)

---

### 📁 Channel & Server Structure

**What it does:** Mirrors the complete server structure including all channel types.

**Supported channel types:**
- Text channels
- Voice channels (metadata only, not audio)
- Announcement channels
- Forum channels
- Stage channels
- Categories
- Threads (public, private, forum posts)

**Details:**
- Channel position and hierarchy preserved
- Topics and descriptions stored
- NSFW flags respected
- Permission overwrites logged

---

### 🧵 Thread Support

**What it does:** Captures thread content that Discord normally auto-archives and hides.

**Details:**
- Active threads synced in real-time
- Archived threads preserved permanently
- Thread metadata: owner, member count, archive settings
- Forum post tags and pinned status

**Why it matters:** Discord auto-archives threads after inactivity. DiscordDataMirror ensures you never lose thread discussions.

---

### 📎 Attachment Handling

**What it does:** Tracks and optionally caches file attachments.

**Tracking (always enabled):**
- Filename, size, dimensions (for images)
- Original URL and proxy URL
- Content type (MIME type)

**Caching (optional):**
- Download attachments to local storage
- Organized by server/channel/message
- Survives if original is deleted from Discord
- Configurable size limits

---

### 💬 Reaction Tracking

**What it does:** Records emoji reactions on messages.

**Details:**
- Both Unicode and custom emoji supported
- Reaction count tracked
- Individual users who reacted can be logged
- Reaction additions and removals captured

---

### 🔍 Full-Text Search

**What it does:** Search across all messages instantly.

**Capabilities:**
- Search by keyword or phrase
- Filter by server, channel, user, or date range
- PostgreSQL full-text search for fast results
- Highlighted search matches in results

**Dashboard features:**
- Global search bar
- Advanced search filters
- Jump to message in context

---

### 🎭 Role Management

**What it does:** Tracks all server roles and their properties.

**Details:**
- Role hierarchy (position)
- Colors and display settings
- Permission sets
- Mentionable and hoisted flags

---

## Advanced Features

### 🔗 Cross-Server User Identity

**What it does:** Links the same person across different Discord accounts or servers.

**Use cases:**
- Track someone who uses different accounts in different servers
- Correlate activity patterns
- Identify alt accounts (manual confirmation required)

**How it works:**
1. Automatic suggestions based on username similarity, avatar, activity
2. Manual confirmation required to create links
3. Confidence scores for each mapping
4. Full audit trail of identity decisions

---

### 📊 Analytics & Statistics

**What it does:** Provides insights into server activity.

**Available metrics:**
- Messages per day/week/month
- Most active channels
- Most active users
- Peak activity hours
- Server growth over time

---

### 🔄 Sync Status Monitoring

**What it does:** Shows real-time progress of data synchronization.

**Dashboard features:**
- Per-server sync status
- Per-channel progress (especially for initial backfill)
- Error indicators with details
- Manual resync triggers

---

### 📤 Data Export (Planned)

**What it does:** Export data in various formats.

**Planned formats:**
- JSON (full data)
- Markdown (readable archives)
- HTML (static website)
- CSV (for spreadsheet analysis)

---

## Technical Features

### 🏗️ .NET Aspire Integration

**Benefits:**
- Automatic service discovery
- Built-in health checks
- Distributed tracing (see request flow)
- Centralized logging
- Easy local development with containers

---

### 🐘 PostgreSQL Backend

**Why PostgreSQL:**
- Rock-solid reliability
- Full-text search built-in
- JSONB for flexible data
- Handles millions of messages
- Easy backups and replication

---

### 🔐 Security Considerations

**What we do:**
- Bot token stored in secure user secrets
- No data leaves your infrastructure
- HTTPS for dashboard access
- Database credentials never exposed

**What we recommend:**
- Use environment variables in production
- Enable database encryption at rest
- Restrict dashboard access to trusted networks
- Regular backups to secure locations

---

## Feature Comparison

| Feature | Free (Self-hosted) | Enterprise (Planned) |
|---------|-------------------|---------------------|
| Message archiving | ✅ | ✅ |
| Member tracking | ✅ | ✅ |
| Full-text search | ✅ | ✅ |
| Attachment caching | ✅ | ✅ |
| Cross-server identity | ✅ | ✅ |
| Multi-tenant support | ❌ | ✅ |
| SSO/SAML integration | ❌ | ✅ |
| Dedicated support | ❌ | ✅ |
| SLA guarantees | ❌ | ✅ |

---

## Roadmap

Features in development or planned:

- [ ] **Wiki generation** — Auto-generate docs from pinned messages
- [ ] **AI summaries** — Summarize long threads and channels
- [ ] **Webhook ingestion** — Capture data from bots that don't use gateway
- [ ] **Scheduled exports** — Automatic backups to cloud storage
- [ ] **Mobile app** — Browse archives on the go
- [ ] **Slack integration** — Archive Slack alongside Discord

See the [TODO.md](https://github.com/JerrettDavis/DiscordDataMirror/blob/main/TODO.md) for the full roadmap.
