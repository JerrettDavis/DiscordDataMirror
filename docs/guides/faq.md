# Frequently Asked Questions

## General Questions

### What is DiscordDataMirror?

DiscordDataMirror is a self-hosted application that creates a complete backup of Discord servers. It runs a Discord bot that monitors and records all messages, members, channels, and other data, storing everything in a PostgreSQL database. A web dashboard lets you browse and search the archived data.

### Is this legal?

**Yes, with caveats:**
- You must comply with Discord's [Terms of Service](https://discord.com/terms)
- You should only archive servers where you have appropriate permissions
- Inform your community that messages are being archived
- Follow applicable privacy laws (GDPR, CCPA, etc.) for personal data

### Do I need to host this myself?

Yes, DiscordDataMirror is designed for self-hosting. This gives you:
- Full control over your data
- No monthly fees
- No third-party access to your archives
- Customization options

### What are the system requirements?

**Minimum:**
- 2 CPU cores
- 2 GB RAM
- 10 GB storage (more for attachment caching)
- Docker or .NET 10 runtime

**Recommended for large servers:**
- 4+ CPU cores
- 8 GB RAM
- SSD storage
- 100+ GB for attachments

### How much storage do I need?

Rough estimates (without attachments):
- Small server (10k messages): ~50 MB
- Medium server (100k messages): ~500 MB
- Large server (1M messages): ~5 GB

Attachments can be 10-100x larger than message data.

---

## Setup Questions

### How do I get a Discord bot token?

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application
3. Go to "Bot" section
4. Click "Reset Token" to generate a new token
5. Copy it immediately (you can't see it again)

See our [complete bot setup guide](../BOT_SETUP.md).

### What permissions does the bot need?

Minimal permissions (integer: 66560):
- View Channels
- Read Message History

The bot is read-only — it never sends messages, kicks users, or modifies anything.

### What are Privileged Gateway Intents?

Discord requires explicit permission for certain data:
- **Server Members Intent**: See member list and join/leave events
- **Message Content Intent**: Read actual message text

Enable these in the Discord Developer Portal under your bot's settings.

### Can I use an existing bot?

Yes, but be careful:
- Ensure the bot has the required intents
- Don't conflict with existing functionality
- Consider using a dedicated bot for archiving

---

## Usage Questions

### How long does initial sync take?

It depends on server size:
- Small server (10k messages): 5-10 minutes
- Medium server (100k messages): 30-60 minutes
- Large server (1M+ messages): Several hours

Sync runs in the background — you can use the dashboard immediately.

### Can I sync specific channels only?

Currently, DiscordDataMirror syncs all accessible channels. Channel filtering is on the roadmap.

### Does it capture deleted messages?

**Yes!** Messages are captured in real-time. When a message is deleted, we mark it as deleted but keep the content. This is one of the main benefits of real-time archiving.

### Does it capture edited messages?

**Partially.** We capture the current state when a message is edited. Full edit history tracking is planned for a future release.

### Can I search across multiple servers?

Yes! The search feature works across all archived servers. You can filter by server, channel, user, or date range.

### How do I export data?

Data export features are planned. Currently, you can:
- Query the PostgreSQL database directly
- Use the API (if enabled)
- Write custom export scripts

---

## Technical Questions

### Why PostgreSQL?

- Proven reliability at scale
- Excellent full-text search
- JSONB for flexible data storage
- Easy backups and replication
- Great tooling ecosystem

### Can I use a different database?

Not currently. The application is built specifically for PostgreSQL. SQL Server or MySQL support could be added with significant effort.

### How does real-time capture work?

DiscordDataMirror uses Discord's Gateway API (WebSocket connection). When a message is sent, Discord pushes it to all connected bots immediately. We capture this event and store it before it can be deleted.

### Does it handle rate limits?

Yes. Discord.Net handles rate limiting automatically. During heavy sync operations, you may see slowdowns, but requests are queued and retried appropriately.

### Can I run multiple instances?

Not recommended without coordination. Multiple bots would receive duplicate events. If you need high availability, consider:
- A single bot with database replication
- Load balancing at the dashboard level only

### How do I back up the database?

Use standard PostgreSQL backup tools:

```bash
# Simple backup
pg_dump discorddatamirror > backup.sql

# Compressed backup
pg_dump discorddatamirror | gzip > backup.sql.gz
```

Schedule regular backups to separate storage.

---

## Privacy & Security

### Who can see the archived data?

Only people with access to your dashboard. By default, there's no authentication — add your own authentication layer in production.

### Is data encrypted?

- **In transit**: Yes, HTTPS for dashboard and encrypted Discord connection
- **At rest**: Depends on your PostgreSQL setup

Enable PostgreSQL encryption for sensitive archives.

### How do I delete archived data?

You can delete data via:
- Direct database queries
- Dashboard bulk actions (planned)
- Retention policies (planned)

### Can users request data deletion?

Yes, you should honor GDPR/CCPA requests. Use database queries to find and delete specific user's data.

---

## Troubleshooting

### The bot is offline

See our [troubleshooting guide](troubleshooting.md#bot-offline).

### Messages aren't being captured

1. Check the bot is online in Discord
2. Verify Message Content Intent is enabled
3. Check logs for errors
4. Ensure the bot can see the channel

### Search isn't finding anything

1. Wait for initial sync to complete
2. Check sync status in dashboard
3. Verify the search index is built
4. Try simpler search terms

### Dashboard is slow

1. Check database connection
2. Add database indexes if missing
3. Consider pagination for large result sets
4. Verify server resources aren't exhausted

---

## Feature Requests

### How do I request a feature?

Open an issue on [GitHub](https://github.com/JerrettDavis/DiscordDataMirror/issues) with the "enhancement" label. Describe your use case in detail.

### Is X feature planned?

Check the [TODO.md](https://github.com/JerrettDavis/DiscordDataMirror/blob/main/TODO.md) for the current roadmap. Popular requests:
- Voice channel recording (not planned — legal/technical complexity)
- Slack integration (planned)
- Mobile app (planned)
- AI summaries (planned)
