# DiscordDataMirror - TODO

## Phase 1: Foundation (Week 1)
> Setup solution structure, Aspire, and basic domain

### 1.1 Solution Setup
- [x] Create solution structure with Aspire AppHost
- [x] Create ServiceDefaults project
- [x] Create Domain project (class library)
- [x] Create Application project (class library)
- [x] Create Infrastructure project (class library)
- [x] Create Bot project (worker service)
- [x] Create Dashboard project (Blazor Server)
- [x] Add project references (layer dependencies)
- [x] Configure Directory.Build.props for shared settings

### 1.2 Aspire Configuration
- [x] Configure AppHost with PostgreSQL
- [x] Add Bot service to AppHost
- [x] Add Dashboard to AppHost
- [x] Configure service discovery
- [ ] Add health checks

### 1.3 Domain Layer
- [x] Create Snowflake value object
- [x] Create Guild aggregate root
- [x] Create Channel entity
- [x] Create User aggregate root
- [x] Create GuildMember entity
- [x] Create Role entity
- [x] Create Message aggregate root
- [x] Create Attachment entity
- [x] Create Embed entity
- [x] Create Reaction entity
- [x] Create Thread entity
- [x] Create SyncState entity
- [x] Create UserMap entity
- [ ] Create domain events (MessageCreated, GuildSynced, etc.)
- [x] Create repository interfaces

### 1.4 Infrastructure - Database
- [x] Create EF Core DbContext
- [x] Create entity configurations (fluent API)
- [x] Create initial migration
- [x] Create repository implementations
- [x] Add connection string configuration (via Aspire)
- [ ] Test database connectivity

## Phase 2: Bot Core (Week 2)
> Discord.Net integration and real-time event handling

### 2.1 Discord.Net Setup
- [x] Install Discord.Net packages
- [x] Create DiscordClient wrapper service
- [x] Configure intents (Guilds, Messages, Members, MessageContent)
- [x] Implement connection/reconnection handling
- [x] Add logging for Discord events
- [x] Create bot configuration (token, settings)

### 2.2 Event Handlers
- [x] GuildAvailable handler
- [x] GuildUpdated handler
- [x] ChannelCreated/Updated/Deleted handlers
- [x] RoleCreated/Updated/Deleted handlers
- [x] UserJoined/Left handlers
- [x] UserUpdated handler
- [x] MessageReceived handler
- [x] MessageUpdated handler
- [x] MessageDeleted handler
- [x] ReactionAdded/Removed handlers
- [x] ThreadCreated/Updated/Deleted handlers

### 2.3 MediatR Integration
- [x] Install MediatR packages
- [x] Create command/query base classes
- [x] Create UpsertGuildCommand + handler
- [x] Create UpsertChannelCommand + handler
- [x] Create UpsertUserCommand + handler
- [x] Create UpsertMessageCommand + handler
- [x] Create logging behavior
- [ ] Create validation behavior

## Phase 3: Historical Sync (Week 3)
> Backfill existing data from Discord

### 3.1 Sync Infrastructure
- [x] Create SyncState entity and tracking
- [x] Create ISyncService interface
- [x] Create SyncService implementations (Guild, Channel, Role, User, Message, Reaction)
- [x] Add rate limiting for Discord API (configurable delay between requests)
- [x] Add retry logic with exponential backoff (via SyncState failure tracking)
- [x] Create HistoricalSyncOrchestrator

### 3.2 Guild Sync
- [x] Sync guild metadata
- [x] Sync all channels
- [x] Sync all roles
- [x] Sync all members (paginated batch sync)
- [ ] Sync guild emojis
- [x] Track sync progress (via SyncState)

### 3.3 Message Sync
- [x] Create message sync service
- [x] Implement backward message fetching
- [x] Handle message pagination (configurable batch size)
- [x] Sync attachments metadata
- [x] Sync embeds
- [x] Sync reactions
- [x] Track last synced message per channel (resume capability)

### 3.4 Thread Sync
- [x] Fetch active/cached threads
- [ ] Fetch archived threads (public + private if permitted)
- [x] Sync thread messages
- [ ] Track thread membership

## Phase 4: Dashboard MVP (Week 4)
> Basic visualization of backed-up data

### 4.1 Dashboard Setup
- [x] Install MudBlazor
- [x] Create layout with navigation (Discord-style sidebar with guild icons)
- [ ] Configure authentication (optional for MVP)
- [x] Add dark theme support (Discord-inspired color palette)

### 4.2 Guild Views
- [x] Guild list page (cards with stats on home page)
- [x] Guild detail page (channels, roles, member count, sync status)
- [x] Channel tree component (in sidebar and Channel Browser page)
- [x] Role list component (on Guild Overview page)

### 4.3 Message Views
- [x] Message list component (Discord-like with avatars, timestamps, grouped)
- [x] Message detail (attachments, embeds, reactions)
- [x] Paginated message loading (load older/newer)
- [x] Jump to oldest/newest

### 4.4 Search
- [x] Basic message search (content) - in Message Viewer
- [ ] Filter by author
- [ ] Filter by channel
- [ ] Filter by date range
- [x] Search results display (in Message Viewer)

### 4.5 User Views
- [x] Member list page (searchable, filterable by role, sortable)
- [ ] User profile page
- [ ] User's messages across guilds
- [x] User's guild memberships (shown on member list)

### 4.6 Sync Status Page
- [x] Show sync progress per guild
- [x] Show sync errors
- [x] Overview stats (in progress, completed, failed, idle)
- [x] Filter by status (tabs)
- [x] Recent activity timeline
- [ ] Manual sync trigger

## Phase 5: Attachment Handling (Week 5)
> Download and cache Discord attachments

### 5.1 Attachment Cache
- [x] Create attachment storage service (`IAttachmentStorageService`, `LocalAttachmentStorageService`)
- [x] Configure storage path (`AttachmentOptions` with configurable storage path)
- [x] Implement download queue (background worker + queued status)
- [x] Handle rate limiting for downloads (Polly retry policies, semaphore concurrency)
- [x] Store with original filename structure (`attachments/{guildId}/{channelId}/{messageId}/`)
- [x] Add content hashing for deduplication
- [x] Track download status in database (Pending, InProgress, Completed, Failed, Skipped, Queued)
- [x] Support resumable/retry logic for failed downloads

### 5.2 Attachment Serving
- [x] Create attachment controller/endpoint (`/api/attachments/{id}`)
- [x] Serve cached attachments with proper content types
- [x] Fallback to Discord CDN if not cached (redirect option)
- [x] Handle missing attachments gracefully
- [x] Add storage statistics endpoint (`/api/attachments/stats`)
- [x] Add manual download trigger endpoint (`/api/attachments/{id}/download`)

### 5.3 Cleanup Service
- [x] Create `IAttachmentCleanupService` for orphaned files
- [x] Cleanup orphaned files on disk (not in database)
- [x] Reset cache status for missing files
- [x] Configurable retention policies

### 5.4 Dashboard Integration
- [ ] Display cached images inline
- [ ] Link to cached files
- [ ] Show cache status indicator

## Phase 6: Real-time Updates (Week 6)
> Live dashboard updates from bot events

### 6.1 SignalR Hub
- [x] Create SyncHub (DiscordEventsHub)
- [x] Broadcast new messages (MessageReceived event)
- [x] Broadcast message edits/deletes (MessageUpdated, MessageDeleted events)
- [x] Broadcast member changes (MemberUpdated event)
- [x] Broadcast sync progress (SyncProgress event)
- [x] Broadcast guild/channel synced events
- [x] Broadcast sync errors
- [x] Broadcast attachment download events
- [x] Group connections by guild/channel for targeted updates

### 6.2 Dashboard Integration
- [x] Connect to SignalR hub (SyncHubConnection service)
- [x] Update message list in real-time (MessageViewer)
- [x] Show live sync status (SyncStatusPage)
- [x] Toast notifications for events (MudBlazor Snackbar)
- [x] Connection status indicator in layout
- [x] Automatic reconnection handling

### 6.3 Sync Status Page
- [x] Show sync progress per guild (real-time updates)
- [x] Show sync errors (real-time notifications)
- [ ] Manual sync trigger
- [ ] Pause/resume sync

### 6.4 Bot Event Publishing
- [x] HTTP client for publishing events to Dashboard
- [x] Message received/updated/deleted event publishing
- [x] Guild synced event publishing
- [x] Member updated event publishing
- [x] Service discovery integration with Aspire

## Phase 7: User Maps (Week 7)
> Cross-server user identity correlation

### 7.1 Infrastructure
- [x] Create UserMap entity
- [ ] Create mapping confidence score system
- [ ] Create manual mapping workflow

### 7.2 Auto-Detection
- [ ] Username similarity matching
- [ ] Avatar hash comparison
- [ ] Create suggestions list

### 7.3 Dashboard
- [ ] User map management page
- [ ] Approve/reject suggestions
- [ ] Manual map creation
- [ ] View merged user activity

## Phase 8: Advanced Features (Week 8+)
> Polish and extended functionality

### 8.1 Analytics
- [ ] Message volume charts
- [ ] Active users over time
- [ ] Channel activity heatmap
- [ ] Word cloud generation

### 8.2 Export
- [ ] Export channel to Markdown
- [ ] Export channel to HTML (Discord-styled)
- [ ] Export search results
- [ ] Bulk export options

### 8.3 Wiki Generation (Future)
- [ ] Extract pinned messages
- [ ] Thread summaries
- [ ] Topic tagging
- [ ] Markdown generation

### 8.4 Performance
- [ ] Add Redis caching
- [ ] Optimize heavy queries
- [ ] Add database indexes as needed
- [ ] Consider read replicas

### 8.5 Operations
- [ ] Add structured logging
- [ ] Add metrics (Prometheus)
- [ ] Add alerting rules
- [ ] Create backup strategy
- [ ] Document deployment

## Technical Debt & Nice-to-Haves
- [ ] Unit tests for domain logic
- [ ] Integration tests for repositories
- [ ] API documentation
- [ ] Docker Compose for local dev
- [ ] CI/CD pipeline
- [ ] Rate limit dashboard feedback
- [ ] Bulk delete handling
- [ ] Audit log sync
- [ ] Voice channel state tracking
- [ ] Scheduled message handling
- [ ] Webhook message attribution

---

## Progress Tracking

| Phase | Status | Completion |
|-------|--------|------------|
| 1. Foundation | 🟢 Mostly Complete | 90% |
| 2. Bot Core | 🟢 Complete | 95% |
| 3. Historical Sync | 🟢 Mostly Complete | 85% |
| 4. Dashboard MVP | 🟢 Mostly Complete | 85% |
| 5. Attachments | 🟢 Mostly Complete | 85% |
| 6. Real-time | 🟢 Complete | 90% |
| 7. User Maps | 🟡 Started | 10% |
| 8. Advanced | ⚪ Not Started | 0% |

---

*Last Updated: 2025-06-02*
