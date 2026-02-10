# Troubleshooting Guide

This guide covers common issues and their solutions.

## Bot Issues

### Bot Offline

**Symptoms:** Bot shows as offline in Discord, no data being captured.

**Solutions:**

1. **Check if the application is running**
   ```bash
   # View running containers
   docker ps
   
   # Or check if dotnet process is running
   ps aux | grep dotnet
   ```

2. **Check application logs**
   - Open Aspire dashboard: https://localhost:17113
   - Look for the Bot service
   - Check for error messages

3. **Verify the bot token**
   ```bash
   # Check if token is set
   dotnet user-secrets list --project src/DiscordDataMirror.Bot
   ```
   If token is missing, set it:
   ```bash
   dotnet user-secrets set "Discord:Token" "your-token"
   ```

4. **Validate token is correct**
   - Go to Discord Developer Portal
   - Regenerate token if unsure
   - Update the stored token

5. **Check network connectivity**
   ```bash
   # Test Discord API access
   curl -I https://discord.com/api/v10/gateway
   ```

---

### "Disallowed Intents" Error

**Symptoms:** Error message about intents in logs, bot connects but doesn't receive events.

**Solution:**

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Select your application
3. Go to **Bot** section
4. Enable under "Privileged Gateway Intents":
   - ✅ SERVER MEMBERS INTENT
   - ✅ MESSAGE CONTENT INTENT
5. Save changes
6. Wait 1-2 minutes for propagation
7. Restart the application

---

### "Missing Access" Errors

**Symptoms:** Bot is online but can't see certain channels.

**Solutions:**

1. **Check bot role position**
   - In Discord, go to Server Settings → Roles
   - Drag the bot's role higher in the list
   - Roles can only see channels at or below their level

2. **Check channel permissions**
   - Right-click channel → Edit Channel → Permissions
   - Ensure the bot role has "View Channel" and "Read Message History"

3. **Re-invite with correct permissions**
   ```
   https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&permissions=66560&scope=bot
   ```

---

### Messages Not Captured in Real-Time

**Symptoms:** Old messages sync, but new messages don't appear.

**Solutions:**

1. **Check Message Content Intent**
   - Without this intent, message content is empty
   - Enable in Developer Portal

2. **Check for gateway disconnections**
   - Look for "Disconnected" or "Reconnecting" in logs
   - Network issues can cause temporary gaps

3. **Verify channel is text-based**
   - Voice channels don't have messages
   - Category channels don't have messages

4. **Check for rate limiting**
   - Heavy sync operations may delay real-time capture
   - Wait for backfill to complete

---

## Database Issues

### Connection Failed

**Symptoms:** Application crashes on startup with database errors.

**Solutions:**

1. **Check if PostgreSQL is running**
   ```bash
   docker ps | grep postgres
   ```

2. **Check the connection string**
   - In development, Aspire handles this automatically
   - Verify Docker networking is working

3. **Test database connectivity**
   ```bash
   psql -h localhost -p 5432 -U postgres -d discorddatamirror
   ```

4. **Reset the database container**
   ```bash
   docker compose down -v
   docker compose up -d
   ```

---

### Migrations Failed

**Symptoms:** Errors about schema or missing tables.

**Solutions:**

1. **Run migrations manually**
   ```bash
   cd src/DiscordDataMirror.Infrastructure
   dotnet ef database update
   ```

2. **Check migration history**
   ```sql
   SELECT * FROM "__EFMigrationsHistory";
   ```

3. **Reset database (development only)**
   ```bash
   dotnet ef database drop --force
   dotnet ef database update
   ```

---

### Database Full

**Symptoms:** Insert errors, disk space warnings.

**Solutions:**

1. **Check disk usage**
   ```bash
   df -h
   ```

2. **Check table sizes**
   ```sql
   SELECT 
     relname as table,
     pg_size_pretty(pg_total_relation_size(relid)) as size
   FROM pg_catalog.pg_statio_user_tables
   ORDER BY pg_total_relation_size(relid) DESC;
   ```

3. **Vacuum and analyze**
   ```sql
   VACUUM ANALYZE;
   ```

4. **Consider data retention policies**
   - Delete old messages
   - Archive to cold storage
   - Increase disk space

---

## Dashboard Issues

### Dashboard Won't Load

**Symptoms:** Browser shows error or blank page.

**Solutions:**

1. **Check if dashboard service is running**
   - Open Aspire dashboard: https://localhost:17113
   - Verify Dashboard service is green

2. **Check the URL**
   - Default: https://localhost:5001
   - May vary based on configuration

3. **Check browser console**
   - Press F12 → Console tab
   - Look for JavaScript errors

4. **Clear browser cache**
   - Ctrl+Shift+Delete
   - Clear cached images and files

---

### Slow Performance

**Symptoms:** Dashboard takes long to load, searches time out.

**Solutions:**

1. **Add database indexes**
   ```sql
   CREATE INDEX IF NOT EXISTS idx_messages_channel_timestamp 
   ON messages(channel_id, timestamp DESC);
   
   CREATE INDEX IF NOT EXISTS idx_messages_content_search 
   ON messages USING gin(to_tsvector('english', content));
   ```

2. **Reduce page size**
   - Lower `Dashboard:ItemsPerPage` in config

3. **Check database query plans**
   ```sql
   EXPLAIN ANALYZE SELECT * FROM messages WHERE ...;
   ```

4. **Increase server resources**
   - More RAM for PostgreSQL
   - Faster disk (SSD)

---

### Real-Time Updates Not Working

**Symptoms:** Need to refresh page to see new messages.

**Solutions:**

1. **Check SignalR connection**
   - Browser console should show SignalR connected
   - Look for WebSocket errors

2. **Verify SignalR is enabled**
   ```json
   {
     "Dashboard": {
       "EnableRealTimeUpdates": true
     }
   }
   ```

3. **Check firewall/proxy**
   - WebSockets may be blocked
   - Ensure port is accessible

---

## Sync Issues

### Initial Sync Stuck

**Symptoms:** Sync progress doesn't advance, appears frozen.

**Solutions:**

1. **Check logs for errors**
   - Rate limiting messages
   - Permission errors
   - Network timeouts

2. **Check sync status table**
   ```sql
   SELECT * FROM sync_state WHERE status = 'in_progress';
   ```

3. **Reset stuck syncs**
   ```sql
   UPDATE sync_state SET status = 'idle' WHERE status = 'in_progress';
   ```

4. **Reduce parallel channels**
   ```json
   {
     "Sync": {
       "ParallelChannels": 1
     }
   }
   ```

---

### Missing Historical Messages

**Symptoms:** Sync completes but older messages are missing.

**Causes:**

1. **Discord retention limits**
   - Some server features limit message history
   - Deleted messages can't be fetched

2. **Bot joined after messages**
   - Bot can only fetch messages from after its oldest cached message
   - For new channels, this goes back to channel creation

3. **Configuration limits**
   - Check `Sync:MaxHistoricalMessages`
   - Set to `-1` for unlimited

---

## Docker Issues

### Container Won't Start

**Solutions:**

1. **Check container logs**
   ```bash
   docker logs discorddatamirror-bot-1
   ```

2. **Verify image exists**
   ```bash
   docker images | grep discorddatamirror
   ```

3. **Pull latest image**
   ```bash
   docker compose pull
   ```

4. **Rebuild containers**
   ```bash
   docker compose down
   docker compose up --build
   ```

---

### Port Already in Use

**Symptoms:** Error about port binding.

**Solutions:**

1. **Find what's using the port**
   ```bash
   lsof -i :5432  # PostgreSQL
   lsof -i :5001  # Dashboard
   ```

2. **Stop conflicting service**
   ```bash
   sudo systemctl stop postgresql
   ```

3. **Change the port in configuration**
   - Modify docker-compose.yaml
   - Update connection strings

---

## Getting More Help

### Collect Debug Information

Before asking for help, gather:

1. **Application version**
   ```bash
   dotnet --info
   ```

2. **Error logs** (redact sensitive info)

3. **Configuration** (redact tokens)

4. **System info**
   - OS and version
   - Docker version
   - Available RAM/disk

### Where to Get Help

1. **GitHub Issues**: https://github.com/JerrettDavis/DiscordDataMirror/issues
2. **GitHub Discussions**: https://github.com/JerrettDavis/DiscordDataMirror/discussions
3. **Search existing issues** before posting

### Reporting Bugs

Include:
- Steps to reproduce
- Expected behavior
- Actual behavior
- Error messages
- Environment details
