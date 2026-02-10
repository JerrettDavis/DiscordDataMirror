using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DiscordDataMirror.Application.Commands;
using DiscordDataMirror.Application.Events;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChannelType = DiscordDataMirror.Domain.Entities.ChannelType;
using MessageType = DiscordDataMirror.Domain.Entities.MessageType;
using ThreadType = Discord.ThreadType;

namespace DiscordDataMirror.Bot.Services;

/// <summary>
/// Handles Discord gateway events and dispatches MediatR commands.
/// </summary>
public class DiscordEventHandler
{
    private readonly DiscordClientService _clientService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordEventHandler> _logger;
    
    public DiscordEventHandler(
        DiscordClientService clientService,
        IServiceProvider serviceProvider,
        ILogger<DiscordEventHandler> logger)
    {
        _clientService = clientService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private async Task PublishEventAsync(Func<ISyncEventPublisher, Task> publishAction)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<ISyncEventPublisher>();
            await publishAction(publisher);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish event");
        }
    }
    
    public void RegisterEventHandlers()
    {
        var client = _clientService.Client;
        
        // Guild events
        client.GuildAvailable += OnGuildAvailableAsync;
        client.GuildUpdated += OnGuildUpdatedAsync;
        
        // Channel events
        client.ChannelCreated += OnChannelCreatedAsync;
        client.ChannelUpdated += OnChannelUpdatedAsync;
        client.ChannelDestroyed += OnChannelDeletedAsync;
        
        // Role events
        client.RoleCreated += OnRoleCreatedAsync;
        client.RoleUpdated += OnRoleUpdatedAsync;
        client.RoleDeleted += OnRoleDeletedAsync;
        
        // User/Member events
        client.UserJoined += OnUserJoinedAsync;
        client.UserLeft += OnUserLeftAsync;
        client.GuildMemberUpdated += OnGuildMemberUpdatedAsync;
        client.UserUpdated += OnUserUpdatedAsync;
        
        // Message events
        client.MessageReceived += OnMessageReceivedAsync;
        client.MessageUpdated += OnMessageUpdatedAsync;
        client.MessageDeleted += OnMessageDeletedAsync;
        
        // Reaction events
        client.ReactionAdded += OnReactionAddedAsync;
        client.ReactionRemoved += OnReactionRemovedAsync;
        
        // Thread events
        client.ThreadCreated += OnThreadCreatedAsync;
        client.ThreadUpdated += OnThreadUpdatedAsync;
        client.ThreadDeleted += OnThreadDeletedAsync;
        
        _logger.LogInformation("Discord event handlers registered");
    }
    
    #region Guild Events
    
    private async Task OnGuildAvailableAsync(SocketGuild guild)
    {
        _logger.LogInformation("Guild available: {GuildName} ({GuildId})", guild.Name, guild.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await mediator.Send(new UpsertGuildCommand(
                guild.Id.ToString(),
                guild.Name,
                guild.IconUrl,
                guild.Description,
                guild.OwnerId.ToString(),
                guild.CreatedAt.UtcDateTime,
                null // Skip raw JSON for now
            ));
            
            // Sync all channels - categories first, then children (skip threads, they're separate)
            var orderedChannels = guild.Channels
                .Where(c => c is not SocketThreadChannel) // Threads handled separately
                .OrderBy(c => c is ICategoryChannel ? 0 : 1) // Categories first
                .ThenBy(c => (c as INestedChannel)?.CategoryId != null ? 1 : 0) // Then uncategorized
                .ThenBy(c => c.Position)
                .ToList();

            _logger.LogInformation("Syncing {ChannelCount} channels for guild {GuildName}", 
                orderedChannels.Count, guild.Name);

            // First pass: sync all categories
            var categories = orderedChannels.Where(c => c is ICategoryChannel).ToList();
            _logger.LogInformation("First pass: syncing {CategoryCount} categories", categories.Count);
            foreach (var channel in categories)
            {
                try
                {
                    await SyncChannelAsync(mediator, channel, guild.Id);
                    _logger.LogDebug("Synced category: {ChannelName} ({ChannelId})", channel.Name, channel.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync category {ChannelName} ({ChannelId})", channel.Name, channel.Id);
                }
            }
            
            // Second pass: sync remaining channels (they can now reference parents)
            var nonCategories = orderedChannels.Where(c => c is not ICategoryChannel).ToList();
            _logger.LogInformation("Second pass: syncing {NonCategoryCount} non-category channels", nonCategories.Count);
            foreach (var channel in nonCategories)
            {
                try
                {
                    await SyncChannelAsync(mediator, channel, guild.Id);
                    _logger.LogDebug("Synced channel: {ChannelName} ({ChannelId})", channel.Name, channel.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync channel {ChannelName} ({ChannelId})", channel.Name, channel.Id);
                }
            }
            
            // Sync all roles
            foreach (var role in guild.Roles)
            {
                await SyncRoleAsync(mediator, role);
            }
            
            _logger.LogInformation("Guild synced: {GuildName} - {ChannelCount} channels, {RoleCount} roles",
                guild.Name, guild.Channels.Count, guild.Roles.Count);
            
            // Publish real-time event
            await PublishEventAsync(p => p.PublishGuildSyncedAsync(new GuildSyncedEvent(
                guild.Id.ToString(),
                guild.Name,
                DateTime.UtcNow
            )));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GuildAvailable for {GuildId}", guild.Id);
            
            // Publish error event
            await PublishEventAsync(p => p.PublishSyncErrorAsync(new SyncErrorEvent(
                guild.Id.ToString(),
                guild.Name,
                "Guild",
                guild.Id.ToString(),
                ex.Message,
                DateTime.UtcNow
            )));
        }
    }
    
    private async Task OnGuildUpdatedAsync(SocketGuild before, SocketGuild after)
    {
        _logger.LogDebug("Guild updated: {GuildName} ({GuildId})", after.Name, after.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await mediator.Send(new UpsertGuildCommand(
                after.Id.ToString(),
                after.Name,
                after.IconUrl,
                after.Description,
                after.OwnerId.ToString(),
                after.CreatedAt.UtcDateTime,
                null
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GuildUpdated for {GuildId}", after.Id);
        }
    }
    
    #endregion
    
    #region Channel Events
    
    private async Task OnChannelCreatedAsync(SocketChannel channel)
    {
        if (channel is not SocketGuildChannel guildChannel)
            return;
        
        _logger.LogDebug("Channel created: {ChannelName} ({ChannelId})", guildChannel.Name, channel.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await SyncChannelAsync(mediator, guildChannel, guildChannel.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ChannelCreated for {ChannelId}", channel.Id);
        }
    }
    
    private async Task OnChannelUpdatedAsync(SocketChannel before, SocketChannel after)
    {
        if (after is not SocketGuildChannel guildChannel)
            return;
        
        _logger.LogDebug("Channel updated: {ChannelName} ({ChannelId})", guildChannel.Name, after.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await SyncChannelAsync(mediator, guildChannel, guildChannel.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ChannelUpdated for {ChannelId}", after.Id);
        }
    }
    
    private async Task OnChannelDeletedAsync(SocketChannel channel)
    {
        _logger.LogDebug("Channel deleted: {ChannelId}", channel.Id);
        
        // Note: We don't actually delete - we could mark it or just log
        // For now, just log the deletion
        _logger.LogInformation("Channel {ChannelId} was deleted", channel.Id);
        await Task.CompletedTask;
    }
    
    #endregion
    
    #region Role Events
    
    private async Task OnRoleCreatedAsync(SocketRole role)
    {
        _logger.LogDebug("Role created: {RoleName} ({RoleId})", role.Name, role.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await SyncRoleAsync(mediator, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RoleCreated for {RoleId}", role.Id);
        }
    }
    
    private async Task OnRoleUpdatedAsync(SocketRole before, SocketRole after)
    {
        _logger.LogDebug("Role updated: {RoleName} ({RoleId})", after.Name, after.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await SyncRoleAsync(mediator, after);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RoleUpdated for {RoleId}", after.Id);
        }
    }
    
    private async Task OnRoleDeletedAsync(SocketRole role)
    {
        _logger.LogDebug("Role deleted: {RoleName} ({RoleId})", role.Name, role.Id);
        
        // Just log for now - could implement soft delete
        _logger.LogInformation("Role {RoleId} was deleted", role.Id);
        await Task.CompletedTask;
    }
    
    #endregion
    
    #region User/Member Events
    
    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        _logger.LogDebug("User joined: {Username} ({UserId}) in {GuildId}", user.Username, user.Id, user.Guild.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Sync user first
            await SyncUserAsync(mediator, user);
            
            // Then sync guild membership
            await mediator.Send(new UpsertGuildMemberCommand(
                user.Id.ToString(),
                user.Guild.Id.ToString(),
                user.Nickname,
                user.JoinedAt?.UtcDateTime,
                user.IsPending ?? false,
                user.Roles.Select(r => r.Id.ToString()).ToList(),
                null
            ));
            
            // Publish real-time event
            await PublishEventAsync(p => p.PublishMemberUpdatedAsync(new MemberUpdatedEvent(
                user.Guild.Id.ToString(),
                user.Id.ToString(),
                user.GlobalName ?? user.Username,
                user.Nickname,
                DateTime.UtcNow
            )));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserJoined for {UserId}", user.Id);
        }
    }
    
    private async Task OnUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        _logger.LogDebug("User left: {Username} ({UserId}) from {GuildId}", user.Username, user.Id, guild.Id);
        
        // Just log for now - could implement soft delete of membership
        _logger.LogInformation("User {UserId} left guild {GuildId}", user.Id, guild.Id);
        await Task.CompletedTask;
    }
    
    private async Task OnGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        _logger.LogDebug("Guild member updated: {Username} ({UserId}) in {GuildId}", after.Username, after.Id, after.Guild.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await mediator.Send(new UpsertGuildMemberCommand(
                after.Id.ToString(),
                after.Guild.Id.ToString(),
                after.Nickname,
                after.JoinedAt?.UtcDateTime,
                after.IsPending ?? false,
                after.Roles.Select(r => r.Id.ToString()).ToList(),
                null
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GuildMemberUpdated for {UserId}", after.Id);
        }
    }
    
    private async Task OnUserUpdatedAsync(SocketUser before, SocketUser after)
    {
        _logger.LogDebug("User updated: {Username} ({UserId})", after.Username, after.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await SyncUserAsync(mediator, after);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserUpdated for {UserId}", after.Id);
        }
    }
    
    #endregion
    
    #region Message Events
    
    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message is not SocketUserMessage userMessage)
            return;
        
        if (message.Channel is not SocketGuildChannel guildChannel)
            return; // Skip DMs
        
        // Validate we have valid IDs
        if (message.Id == 0 || message.Channel.Id == 0 || message.Author?.Id == 0)
        {
            _logger.LogWarning("Skipping message with invalid IDs: MessageId={MessageId}, ChannelId={ChannelId}, AuthorId={AuthorId}",
                message.Id, message.Channel.Id, message.Author?.Id);
            return;
        }
        
        _logger.LogDebug("Message received: {MessageId} in {ChannelId}", message.Id, message.Channel.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Ensure user exists
            await SyncUserAsync(mediator, message.Author!);
            
            // Sync message - handle referenced message ID carefully
            var referencedMessageId = message.Reference?.MessageId;
            string? refMsgIdStr = referencedMessageId.HasValue && referencedMessageId.Value.IsSpecified 
                ? referencedMessageId.Value.Value.ToString() 
                : null;
            
            await mediator.Send(new UpsertMessageCommand(
                message.Id.ToString(),
                message.Channel.Id.ToString(),
                message.Author.Id.ToString(),
                message.Content,
                message.CleanContent,
                MapMessageType(message.Type),
                message.IsPinned,
                message.IsTTS,
                message.Timestamp.UtcDateTime,
                message.EditedTimestamp?.UtcDateTime,
                refMsgIdStr,
                null
            ));
            
            // Publish real-time event
            await PublishEventAsync(p => p.PublishMessageReceivedAsync(new MessageReceivedEvent(
                guildChannel.Guild.Id.ToString(),
                message.Channel.Id.ToString(),
                message.Id.ToString(),
                message.Author.Id.ToString(),
                message.Author.GlobalName ?? message.Author.Username,
                message.Content?.Length > 100 ? message.Content[..100] : message.Content,
                message.Timestamp.UtcDateTime
            )));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MessageReceived for {MessageId}", message.Id);
        }
    }
    
    private async Task OnMessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        if (after is not SocketUserMessage userMessage)
            return;
        
        if (channel is not SocketGuildChannel guildChannel)
            return;
        
        _logger.LogDebug("Message updated: {MessageId} in {ChannelId}", after.Id, channel.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await mediator.Send(new UpsertMessageCommand(
                after.Id.ToString(),
                channel.Id.ToString(),
                after.Author.Id.ToString(),
                after.Content,
                after.CleanContent,
                MapMessageType(after.Type),
                after.IsPinned,
                after.IsTTS,
                after.Timestamp.UtcDateTime,
                after.EditedTimestamp?.UtcDateTime,
                after.Reference?.MessageId.ToString(),
                null
            ));
            
            // Publish real-time event
            await PublishEventAsync(p => p.PublishMessageUpdatedAsync(new MessageUpdatedEvent(
                guildChannel.Guild.Id.ToString(),
                channel.Id.ToString(),
                after.Id.ToString(),
                after.Content,
                after.EditedTimestamp?.UtcDateTime ?? DateTime.UtcNow
            )));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MessageUpdated for {MessageId}", after.Id);
        }
    }
    
    private async Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        _logger.LogDebug("Message deleted: {MessageId} in {ChannelId}", message.Id, channel.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await mediator.Send(new DeleteMessageCommand(message.Id.ToString()));
            
            // Get guild ID from channel if available
            var guildId = "";
            if (channel.HasValue && channel.Value is SocketGuildChannel guildChannel)
            {
                guildId = guildChannel.Guild.Id.ToString();
            }
            
            // Publish real-time event
            await PublishEventAsync(p => p.PublishMessageDeletedAsync(new MessageDeletedEvent(
                guildId,
                channel.Id.ToString(),
                message.Id.ToString(),
                DateTime.UtcNow
            )));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MessageDeleted for {MessageId}", message.Id);
        }
    }
    
    #endregion
    
    #region Reaction Events
    
    private async Task OnReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        if (!reaction.User.IsSpecified)
            return;
        
        _logger.LogDebug("Reaction added: {Emote} to {MessageId} by {UserId}",
            reaction.Emote.Name, message.Id, reaction.UserId);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            var emoteKey = GetEmoteKey(reaction.Emote);
            
            await mediator.Send(new AddReactionCommand(
                message.Id.ToString(),
                emoteKey,
                reaction.UserId.ToString()
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ReactionAdded for {MessageId}", message.Id);
        }
    }
    
    private async Task OnReactionRemovedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        _logger.LogDebug("Reaction removed: {Emote} from {MessageId} by {UserId}",
            reaction.Emote.Name, message.Id, reaction.UserId);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            var emoteKey = GetEmoteKey(reaction.Emote);
            
            await mediator.Send(new RemoveReactionCommand(
                message.Id.ToString(),
                emoteKey,
                reaction.UserId.ToString()
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ReactionRemoved for {MessageId}", message.Id);
        }
    }
    
    #endregion
    
    #region Thread Events
    
    private async Task OnThreadCreatedAsync(SocketThreadChannel thread)
    {
        _logger.LogDebug("Thread created: {ThreadName} ({ThreadId})", thread.Name, thread.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // First sync as a channel
            await SyncChannelAsync(mediator, thread, thread.Guild.Id);
            
            // Then sync thread-specific data
            var archiveTimestamp = thread.ArchiveTimestamp != default ? thread.ArchiveTimestamp.UtcDateTime : (DateTime?)null;
            await mediator.Send(new UpsertThreadCommand(
                thread.Id.ToString(),
                thread.ParentChannel.Id.ToString(),
                thread.Owner?.Id.ToString(),
                thread.MessageCount,
                thread.MemberCount,
                thread.IsArchived,
                thread.IsLocked,
                archiveTimestamp,
                (int?)thread.AutoArchiveDuration
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ThreadCreated for {ThreadId}", thread.Id);
        }
    }
    
    private async Task OnThreadUpdatedAsync(Cacheable<SocketThreadChannel, ulong> before, SocketThreadChannel after)
    {
        _logger.LogDebug("Thread updated: {ThreadName} ({ThreadId})", after.Name, after.Id);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            await SyncChannelAsync(mediator, after, after.Guild.Id);
            
            var archiveTimestamp = after.ArchiveTimestamp != default ? after.ArchiveTimestamp.UtcDateTime : (DateTime?)null;
            await mediator.Send(new UpsertThreadCommand(
                after.Id.ToString(),
                after.ParentChannel.Id.ToString(),
                after.Owner?.Id.ToString(),
                after.MessageCount,
                after.MemberCount,
                after.IsArchived,
                after.IsLocked,
                archiveTimestamp,
                (int?)after.AutoArchiveDuration
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ThreadUpdated for {ThreadId}", after.Id);
        }
    }
    
    private async Task OnThreadDeletedAsync(Cacheable<SocketThreadChannel, ulong> thread)
    {
        _logger.LogDebug("Thread deleted: {ThreadId}", thread.Id);
        
        _logger.LogInformation("Thread {ThreadId} was deleted", thread.Id);
        await Task.CompletedTask;
    }
    
    #endregion
    
    #region Helper Methods
    
    private async Task SyncChannelAsync(IMediator mediator, SocketGuildChannel channel, ulong guildId)
    {
        var channelType = MapChannelType(channel);
        var topic = (channel as SocketTextChannel)?.Topic;
        var isNsfw = (channel as SocketTextChannel)?.IsNsfw ?? false;
        var parentId = (channel as SocketTextChannel)?.CategoryId?.ToString() ??
                       (channel as SocketVoiceChannel)?.CategoryId?.ToString();
        
        await mediator.Send(new UpsertChannelCommand(
            channel.Id.ToString(),
            guildId.ToString(),
            channel.Name,
            channelType,
            topic,
            channel.Position,
            isNsfw,
            parentId,
            channel.CreatedAt.UtcDateTime,
            null
        ));
    }
    
    private async Task SyncRoleAsync(IMediator mediator, SocketRole role)
    {
        await mediator.Send(new UpsertRoleCommand(
            role.Id.ToString(),
            role.Guild.Id.ToString(),
            role.Name,
            role.Color.RawValue > 0 ? (int)role.Color.RawValue : 0,
            role.Position,
            role.Permissions.RawValue.ToString(),
            role.IsHoisted,
            role.IsMentionable,
            role.IsManaged,
            null
        ));
    }
    
    private async Task SyncUserAsync(IMediator mediator, SocketUser user)
    {
        await mediator.Send(new UpsertUserCommand(
            user.Id.ToString(),
            user.Username,
            user.Discriminator == "0" ? null : user.Discriminator,
            user.GlobalName,
            user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
            user.IsBot,
            user.CreatedAt.UtcDateTime,
            null
        ));
    }
    
    private static ChannelType MapChannelType(SocketGuildChannel channel)
    {
        // Use the underlying channel type from Discord directly to avoid inheritance issues
        if (channel is IChannel discordChannel)
        {
            return discordChannel.GetChannelType() switch
            {
                Discord.ChannelType.Text => ChannelType.Text,
                Discord.ChannelType.DM => ChannelType.DM,
                Discord.ChannelType.Voice => ChannelType.Voice,
                Discord.ChannelType.Group => ChannelType.GroupDM,
                Discord.ChannelType.Category => ChannelType.Category,
                Discord.ChannelType.News => ChannelType.News,
                Discord.ChannelType.NewsThread => ChannelType.NewsThread,
                Discord.ChannelType.PublicThread => ChannelType.PublicThread,
                Discord.ChannelType.PrivateThread => ChannelType.PrivateThread,
                Discord.ChannelType.Stage => ChannelType.Stage,
                Discord.ChannelType.GuildDirectory => ChannelType.GuildDirectory,
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
            Discord.MessageType.UserPremiumGuildSubscription => MessageType.UserPremiumGuildSubscription,
            Discord.MessageType.UserPremiumGuildSubscriptionTier1 => MessageType.UserPremiumGuildSubscriptionTier1,
            Discord.MessageType.UserPremiumGuildSubscriptionTier2 => MessageType.UserPremiumGuildSubscriptionTier2,
            Discord.MessageType.UserPremiumGuildSubscriptionTier3 => MessageType.UserPremiumGuildSubscriptionTier3,
            Discord.MessageType.ChannelFollowAdd => MessageType.ChannelFollowAdd,
            Discord.MessageType.GuildDiscoveryDisqualified => MessageType.GuildDiscoveryDisqualified,
            Discord.MessageType.GuildDiscoveryRequalified => MessageType.GuildDiscoveryRequalified,
            Discord.MessageType.Reply => MessageType.Reply,
            Discord.MessageType.ApplicationCommand => MessageType.ApplicationCommand,
            Discord.MessageType.ThreadStarterMessage => MessageType.ThreadStarterMessage,
            Discord.MessageType.GuildInviteReminder => MessageType.GuildInviteReminder,
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
    
    #endregion
}
