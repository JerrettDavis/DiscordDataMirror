using Discord;
using Discord.WebSocket;
using DiscordDataMirror.Application.Configuration;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChannelType = DiscordDataMirror.Domain.Entities.ChannelType;
using MessageType = DiscordDataMirror.Domain.Entities.MessageType;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Orchestrates historical sync of all Discord data.
/// Coordinates the sync order: Guilds → Roles → Channels → Users → Messages → Reactions
/// </summary>
public class HistoricalSyncOrchestrator
{
    private readonly DiscordSocketClient _discordClient;
    private readonly IGuildSyncService _guildSyncService;
    private readonly IChannelSyncService _channelSyncService;
    private readonly IRoleSyncService _roleSyncService;
    private readonly IUserSyncService _userSyncService;
    private readonly IMessageSyncService _messageSyncService;
    private readonly IReactionSyncService _reactionSyncService;
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SyncOptions _syncOptions;
    private readonly ILogger<HistoricalSyncOrchestrator> _logger;

    private const string EntityTypeGuild = "Guild";
    private const string EntityTypeChannel = "Channel";

    public HistoricalSyncOrchestrator(
        DiscordSocketClient discordClient,
        IGuildSyncService guildSyncService,
        IChannelSyncService channelSyncService,
        IRoleSyncService roleSyncService,
        IUserSyncService userSyncService,
        IMessageSyncService messageSyncService,
        IReactionSyncService reactionSyncService,
        ISyncStateRepository syncStateRepository,
        IUnitOfWork unitOfWork,
        IOptions<SyncOptions> syncOptions,
        ILogger<HistoricalSyncOrchestrator> logger)
    {
        _discordClient = discordClient;
        _guildSyncService = guildSyncService;
        _channelSyncService = channelSyncService;
        _roleSyncService = roleSyncService;
        _userSyncService = userSyncService;
        _messageSyncService = messageSyncService;
        _reactionSyncService = reactionSyncService;
        _syncStateRepository = syncStateRepository;
        _unitOfWork = unitOfWork;
        _syncOptions = syncOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Performs a full historical sync of all connected guilds.
    /// </summary>
    public async Task SyncAllGuildsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting historical sync of all guilds");

        var guilds = _discordClient.Guilds.ToList();
        _logger.LogInformation("Found {GuildCount} guilds to sync", guilds.Count);

        foreach (var guild in guilds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await SyncGuildAsync(guild, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync guild {GuildName} ({GuildId})", guild.Name, guild.Id);
                await UpdateSyncState(EntityTypeGuild, guild.Id.ToString(), SyncStatus.Failed, ex.Message, ct);
            }
        }

        _logger.LogInformation("Completed historical sync of all guilds");
    }

    /// <summary>
    /// Performs a full historical sync of a single guild.
    /// </summary>
    public async Task SyncGuildAsync(SocketGuild guild, CancellationToken ct = default)
    {
        var guildIdStr = guild.Id.ToString();
        _logger.LogInformation("Starting sync for guild: {GuildName} ({GuildId})", guild.Name, guild.Id);

        var syncState = await GetOrCreateSyncState(EntityTypeGuild, guildIdStr, ct);
        syncState.StartSync();
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            // Step 1: Sync guild metadata
            Console.WriteLine($"[SYNC] Step 1: Syncing guild metadata for {guild.Name}...");
            await _guildSyncService.SyncGuildAsync(
                new Snowflake(guild.Id),
                guild.Name,
                guild.IconUrl,
                guild.Description,
                new Snowflake(guild.OwnerId),
                guild.CreatedAt.UtcDateTime,
                null,
                ct);
            Console.WriteLine("[SYNC] Step 1 complete");

            // Step 2: Sync roles
            Console.WriteLine($"[SYNC] Step 2: Syncing {guild.Roles.Count} roles...");
            await SyncRolesAsync(guild, ct);
            Console.WriteLine("[SYNC] Step 2 complete");

            // Step 3: Sync channels (including categories and threads)
            Console.WriteLine($"[SYNC] Step 3: Syncing {guild.Channels.Count} channels...");
            await SyncChannelsAsync(guild, ct);
            Console.WriteLine("[SYNC] Step 3 complete");

            // Step 4: Sync users/members
            Console.WriteLine("[SYNC] Step 4: Syncing members...");
            await SyncMembersAsync(guild, ct);
            Console.WriteLine("[SYNC] Step 4 complete");

            // Step 5: Sync messages for each text channel
            Console.WriteLine("[SYNC] Step 5: Syncing messages...");
            await SyncAllChannelMessagesAsync(guild, ct);
            Console.WriteLine("[SYNC] Step 5 complete");

            // Mark as complete
            syncState.CompleteSync();
            await _unitOfWork.SaveChangesAsync(ct);

            Console.WriteLine($"[SYNC] Completed sync for guild: {guild.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SYNC] FAILED: {ex.Message}");
            Console.WriteLine($"[SYNC] Stack trace: {ex.StackTrace}");
            _logger.LogError(ex, "[{GuildName}] Sync failed with error: {Error}\nStack trace: {StackTrace}",
                guild.Name, ex.Message, ex.StackTrace);
            syncState.FailSync(ex.Message);
            await _unitOfWork.SaveChangesAsync(ct);
            throw;
        }
    }

    private async Task SyncRolesAsync(SocketGuild guild, CancellationToken ct)
    {
        foreach (var role in guild.Roles)
        {
            ct.ThrowIfCancellationRequested();

            await _roleSyncService.SyncRoleAsync(
                new Snowflake(role.Id),
                new Snowflake(guild.Id),
                role.Name,
                (int)role.Color.RawValue,
                role.Position,
                role.Permissions.RawValue.ToString(),
                role.IsHoisted,
                role.IsMentionable,
                role.IsManaged,
                null,
                ct);

            await Task.Delay(_syncOptions.RateLimitDelayMs / 5, ct); // Roles are fast
        }
    }

    private async Task SyncChannelsAsync(SocketGuild guild, CancellationToken ct)
    {
        // First sync all regular channels (including categories)
        foreach (var channel in guild.Channels.OrderBy(c => c.Position))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await SyncChannelAsync(channel, guild.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{GuildName}] Failed to sync channel {ChannelName} ({ChannelId})",
                    guild.Name, channel.Name, channel.Id);
                throw;
            }
        }

        // Then sync active/cached threads
        _logger.LogDebug("[{GuildName}] Syncing cached threads...", guild.Name);
        try
        {
            // Discord.Net caches active threads in ThreadChannels
            var threads = guild.ThreadChannels.ToList();
            foreach (var thread in threads)
            {
                ct.ThrowIfCancellationRequested();
                await SyncThreadAsync(thread, guild.Id, ct);
            }
            _logger.LogDebug("[{GuildName}] Synced {ThreadCount} cached threads", guild.Name, threads.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{GuildName}] Failed to sync threads", guild.Name);
        }
    }

    private async Task SyncChannelAsync(SocketGuildChannel channel, ulong guildId, CancellationToken ct)
    {
        var channelType = MapChannelType(channel);
        var topic = (channel as SocketTextChannel)?.Topic;
        var isNsfw = (channel as SocketTextChannel)?.IsNsfw ?? false;
        var parentId = (channel as INestedChannel)?.CategoryId;

        await _channelSyncService.SyncChannelAsync(
            new Snowflake(channel.Id),
            new Snowflake(guildId),
            channel.Name,
            channelType,
            topic,
            channel.Position,
            isNsfw,
            parentId.HasValue ? new Snowflake(parentId.Value) : null,
            channel.CreatedAt.UtcDateTime,
            null,
            ct);
    }

    private async Task SyncThreadAsync(SocketThreadChannel thread, ulong guildId, CancellationToken ct)
    {
        // First sync as a channel
        var channelType = thread.Type switch
        {
            ThreadType.PublicThread => ChannelType.PublicThread,
            ThreadType.PrivateThread => ChannelType.PrivateThread,
            ThreadType.NewsThread => ChannelType.NewsThread,
            _ => ChannelType.PublicThread
        };

        await _channelSyncService.SyncChannelAsync(
            new Snowflake(thread.Id),
            new Snowflake(guildId),
            thread.Name,
            channelType,
            null,
            0,
            false,
            new Snowflake(thread.ParentChannel.Id),
            thread.CreatedAt.UtcDateTime,
            null,
            ct);

        // Then sync thread-specific data
        var archiveTimestamp = thread.ArchiveTimestamp != default
            ? thread.ArchiveTimestamp.UtcDateTime
            : (DateTime?)null;

        await _channelSyncService.SyncThreadAsync(
            new Snowflake(thread.Id),
            new Snowflake(thread.ParentChannel.Id),
            thread.Owner?.Id != null ? new Snowflake(thread.Owner.Id) : null,
            thread.MessageCount,
            thread.MemberCount,
            thread.IsArchived,
            thread.IsLocked,
            archiveTimestamp,
            (int?)thread.AutoArchiveDuration,
            ct);
    }

    private async Task SyncMembersAsync(SocketGuild guild, CancellationToken ct)
    {
        _logger.LogInformation("[{GuildName}] Starting member sync, downloading users...", guild.Name);

        // Download all users if not already downloaded
        await guild.DownloadUsersAsync();

        var members = guild.Users.ToList();
        _logger.LogInformation("[{GuildName}] Downloaded {MemberCount} members, processing...", guild.Name, members.Count);

        // Log each member ID for debugging
        foreach (var m in members)
        {
            _logger.LogDebug("[{GuildName}] Member: {Username} ({UserId})", guild.Name, m.Username, m.Id);
        }

        var memberData = new List<GuildMemberData>();
        foreach (var m in members)
        {
            try
            {
                Console.WriteLine($"[SYNC] Processing member: {m.Username} ({m.Id})");
                var discrim = m.Discriminator;
                Console.WriteLine($"[SYNC] Discriminator: '{discrim}'");

                var userId = new Snowflake(m.Id);
                Console.WriteLine($"[SYNC] Created userId snowflake: {userId}");

                var roleIds = m.Roles.Select(r => r.Id.ToString()).ToList();
                Console.WriteLine($"[SYNC] Got {roleIds.Count} roles");

                memberData.Add(new GuildMemberData(
                    userId,
                    m.Username,
                    string.IsNullOrEmpty(discrim) || discrim == "0" ? null : discrim,
                    m.GlobalName,
                    m.GetAvatarUrl() ?? m.GetDefaultAvatarUrl(),
                    m.IsBot,
                    m.CreatedAt.UtcDateTime,
                    m.Nickname,
                    m.JoinedAt?.UtcDateTime,
                    m.IsPending ?? false,
                    roleIds,
                    null
                ));
                Console.WriteLine($"[SYNC] Added member data for {m.Username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SYNC] FAILED for {m.Username} ({m.Id}): {ex.Message}");
                Console.WriteLine($"[SYNC] Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "[{GuildName}] Failed to create member data for {Username} ({UserId})",
                    guild.Name, m.Username, m.Id);
                throw;
            }
        }

        // Process in batches to avoid memory issues
        const int batchSize = 100;
        for (int i = 0; i < memberData.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = memberData.Skip(i).Take(batchSize);
            await _userSyncService.SyncGuildMemberBatchAsync(new Snowflake(guild.Id), batch, ct);

            _logger.LogDebug("[{GuildName}] Synced members {Start}-{End} of {Total}",
                guild.Name, i + 1, Math.Min(i + batchSize, memberData.Count), memberData.Count);

            await Task.Delay(_syncOptions.RateLimitDelayMs, ct);
        }
    }

    private async Task SyncAllChannelMessagesAsync(SocketGuild guild, CancellationToken ct)
    {
        var textChannels = guild.Channels
            .OfType<SocketTextChannel>()
            .Where(c => c is not SocketThreadChannel) // Handle threads separately
            .ToList();

        _logger.LogInformation("[{GuildName}] Syncing messages for {ChannelCount} text channels",
            guild.Name, textChannels.Count);

        foreach (var channel in textChannels)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await SyncChannelMessagesAsync(channel, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{GuildName}] Failed to sync messages for channel #{ChannelName}",
                    guild.Name, channel.Name);
            }
        }

        // Also sync messages in cached threads
        try
        {
            var threads = guild.ThreadChannels.ToList();
            foreach (var thread in threads)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await SyncChannelMessagesAsync(thread, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[{GuildName}] Failed to sync messages for thread {ThreadName}",
                        guild.Name, thread.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{GuildName}] Failed to sync thread messages", guild.Name);
        }
    }

    private async Task SyncChannelMessagesAsync(ITextChannel channel, CancellationToken ct)
    {
        var channelIdStr = channel.Id.ToString();
        var syncState = await GetOrCreateSyncState(EntityTypeChannel, channelIdStr, ct);

        // Check if we should resume from last position
        var lastMessageId = syncState.LastMessageId;

        _logger.LogInformation("Syncing messages for channel #{ChannelName} ({ChannelId}), last synced: {LastMessageId}",
            channel.Name, channel.Id, lastMessageId?.ToString() ?? "never");

        syncState.StartSync();
        await _unitOfWork.SaveChangesAsync(ct);

        int totalSynced = 0;
        int maxMessages = _syncOptions.MaxHistoricalMessages;
        int batchSize = _syncOptions.MessageBatchSize;

        try
        {
            IMessage? lastMessage = null;
            bool hasMore = true;

            while (hasMore && totalSynced < maxMessages)
            {
                ct.ThrowIfCancellationRequested();

                // Fetch messages in batches, going backwards from the most recent (or from where we left off)
                IEnumerable<IMessage> messages;

                if (lastMessage is null)
                {
                    // First batch - get most recent messages
                    messages = await channel.GetMessagesAsync(batchSize).FlattenAsync();
                }
                else
                {
                    // Subsequent batches - get messages before the last one
                    messages = await channel.GetMessagesAsync(lastMessage.Id, Direction.Before, batchSize).FlattenAsync();
                }

                var messageList = messages.ToList();

                if (messageList.Count == 0)
                {
                    hasMore = false;
                    break;
                }

                // Check if we've reached previously synced messages
                if (lastMessageId.HasValue)
                {
                    var lastSyncedUlong = lastMessageId.Value.ToUInt64();
                    var lastSyncedIndex = messageList.FindIndex(m => m.Id == lastSyncedUlong);
                    if (lastSyncedIndex >= 0)
                    {
                        // Only take messages newer than what we already have
                        messageList = messageList.Take(lastSyncedIndex).ToList();
                        hasMore = false;
                    }
                }

                if (messageList.Count == 0)
                    break;

                // Convert to our DTO format and sync
                var messageDataList = new List<MessageData>();
                foreach (var msg in messageList)
                {
                    try
                    {
                        // Ensure author is synced
                        var discrim = msg.Author.Discriminator;
                        await _userSyncService.SyncUserAsync(
                            new Snowflake(msg.Author.Id),
                            msg.Author.Username,
                            string.IsNullOrEmpty(discrim) || discrim == "0" ? null : discrim,
                            (msg.Author as IGuildUser)?.DisplayName,
                            msg.Author.GetAvatarUrl() ?? msg.Author.GetDefaultAvatarUrl(),
                            msg.Author.IsBot,
                            msg.Author.CreatedAt.UtcDateTime,
                            null,
                            ct);

                        // Skip referenced message ID during initial sync to avoid FK violations
                        // (referenced messages may not exist yet)
                        var messageData = new MessageData(
                            new Snowflake(msg.Id),
                            new Snowflake(channel.Id),
                            new Snowflake(msg.Author.Id),
                            msg.Content,
                            msg.CleanContent,
                            MapMessageType(msg.Type),
                            msg.IsPinned,
                            msg.IsTTS,
                            msg.Timestamp.UtcDateTime,
                            msg.EditedTimestamp?.UtcDateTime,
                            null, // TODO: Add second pass to update reply references
                            null,
                            msg.Attachments.Select(a => new AttachmentData(
                                new Snowflake(a.Id),
                                a.Filename,
                                a.Url,
                                a.ProxyUrl,
                                (long)a.Size,
                                a.Width,
                                a.Height,
                                a.ContentType)),
                            null); // Skip embeds for now to reduce complexity

                        messageDataList.Add(messageData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process message {MessageId} from author {AuthorId} ({AuthorName})",
                            msg.Id, msg.Author.Id, msg.Author.Username);
                        throw;
                    }
                }

                await _messageSyncService.SyncMessageBatchAsync(messageDataList, ct);

                // Sync reactions for each message
                foreach (var msg in messageList)
                {
                    if (msg.Reactions.Count > 0)
                    {
                        await SyncMessageReactionsAsync(msg, ct);
                    }
                }

                totalSynced += messageList.Count;
                lastMessage = messageList.Last();

                _logger.LogDebug("Synced {Count} messages for #{ChannelName}, total: {Total}",
                    messageList.Count, channel.Name, totalSynced);

                // Respect rate limits
                await Task.Delay(_syncOptions.RateLimitDelayMs, ct);
            }

            // Track the newest message we synced for resume capability
            var newestMessageId = await _messageSyncService.GetLastMessageIdAsync(new Snowflake(channel.Id), ct);
            syncState.CompleteSync(newestMessageId);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Completed syncing {Total} messages for #{ChannelName}",
                totalSynced, channel.Name);
        }
        catch (Exception ex)
        {
            syncState.FailSync(ex.Message);
            await _unitOfWork.SaveChangesAsync(ct);
            throw;
        }
    }

    private async Task SyncMessageReactionsAsync(IMessage message, CancellationToken ct)
    {
        var reactions = new List<ReactionData>();

        foreach (var reaction in message.Reactions)
        {
            try
            {
                // Fetch users who reacted (up to 100 - Discord's limit)
                var users = await message.GetReactionUsersAsync(reaction.Key, 100).FlattenAsync();
                var userIds = users.Select(u => u.Id.ToString()).ToList();

                var emoteKey = GetEmoteKey(reaction.Key);
                reactions.Add(new ReactionData(emoteKey, reaction.Value.ReactionCount, userIds));

                await Task.Delay(_syncOptions.RateLimitDelayMs / 2, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch reaction users for {Emote} on message {MessageId}",
                    reaction.Key.Name, message.Id);
            }
        }

        if (reactions.Count > 0)
        {
            await _reactionSyncService.SyncReactionsAsync(new Snowflake(message.Id), reactions, ct);
        }
    }

    private async Task<SyncState> GetOrCreateSyncState(string entityType, string entityId, CancellationToken ct)
    {
        var syncState = await _syncStateRepository.GetByEntityAsync(entityType, entityId, ct);

        if (syncState is null)
        {
            syncState = new SyncState(entityType, entityId);
            await _syncStateRepository.AddAsync(syncState, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return syncState;
    }

    private async Task UpdateSyncState(string entityType, string entityId, SyncStatus status, string? errorMessage, CancellationToken ct)
    {
        var syncState = await GetOrCreateSyncState(entityType, entityId, ct);

        if (status == SyncStatus.Failed)
            syncState.FailSync(errorMessage ?? "Unknown error");
        else if (status == SyncStatus.Completed)
            syncState.CompleteSync();

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static ChannelType MapChannelType(SocketGuildChannel channel)
    {
        if (channel is IChannel discordChannel)
        {
            return discordChannel.GetChannelType() switch
            {
                Discord.ChannelType.Text => ChannelType.Text,
                Discord.ChannelType.Voice => ChannelType.Voice,
                Discord.ChannelType.Category => ChannelType.Category,
                Discord.ChannelType.News => ChannelType.News,
                Discord.ChannelType.NewsThread => ChannelType.NewsThread,
                Discord.ChannelType.PublicThread => ChannelType.PublicThread,
                Discord.ChannelType.PrivateThread => ChannelType.PrivateThread,
                Discord.ChannelType.Stage => ChannelType.Stage,
                Discord.ChannelType.Forum => ChannelType.Forum,
                Discord.ChannelType.Media => ChannelType.Media,
                _ => ChannelType.Text
            };
        }
        return ChannelType.Text;
    }

    private static MessageType MapMessageType(Discord.MessageType discordType)
    {
        return discordType switch
        {
            Discord.MessageType.Default => MessageType.Default,
            Discord.MessageType.RecipientAdd => MessageType.RecipientAdd,
            Discord.MessageType.RecipientRemove => MessageType.RecipientRemove,
            Discord.MessageType.Call => MessageType.Call,
            Discord.MessageType.ChannelNameChange => MessageType.ChannelNameChange,
            Discord.MessageType.ChannelIconChange => MessageType.ChannelIconChange,
            Discord.MessageType.ChannelPinnedMessage => MessageType.ChannelPinnedMessage,
            Discord.MessageType.GuildMemberJoin => MessageType.GuildMemberJoin,
            Discord.MessageType.Reply => MessageType.Reply,
            Discord.MessageType.ApplicationCommand => MessageType.ApplicationCommand,
            Discord.MessageType.ThreadStarterMessage => MessageType.ThreadStarterMessage,
            Discord.MessageType.ContextMenuCommand => MessageType.ContextMenuCommand,
            Discord.MessageType.AutoModerationAction => MessageType.AutoModerationAction,
            _ => MessageType.Default
        };
    }

    private static string GetEmoteKey(IEmote emote)
    {
        return emote switch
        {
            Emote customEmote => $"custom:{customEmote.Id}:{customEmote.Name}",
            Emoji emoji => emoji.Name,
            _ => emote.Name
        };
    }
}
