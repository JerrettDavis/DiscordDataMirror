using DiscordDataMirror.Application.Events;
using Microsoft.AspNetCore.SignalR;

namespace DiscordDataMirror.Dashboard.Hubs;

/// <summary>
/// SignalR hub for real-time sync events.
/// Clients can subscribe to specific guilds to receive targeted updates.
/// </summary>
public class SyncHub : Hub<ISyncHubClient>
{
    private readonly ILogger<SyncHub> _logger;

    public SyncHub(ILogger<SyncHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to receive updates for a specific guild.
    /// </summary>
    public async Task SubscribeToGuild(string guildId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"guild:{guildId}");
        _logger.LogDebug("Client {ConnectionId} subscribed to guild {GuildId}", 
            Context.ConnectionId, guildId);
    }

    /// <summary>
    /// Unsubscribe from a specific guild's updates.
    /// </summary>
    public async Task UnsubscribeFromGuild(string guildId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"guild:{guildId}");
        _logger.LogDebug("Client {ConnectionId} unsubscribed from guild {GuildId}", 
            Context.ConnectionId, guildId);
    }

    /// <summary>
    /// Subscribe to receive updates for a specific channel.
    /// </summary>
    public async Task SubscribeToChannel(string channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");
        _logger.LogDebug("Client {ConnectionId} subscribed to channel {ChannelId}", 
            Context.ConnectionId, channelId);
    }

    /// <summary>
    /// Unsubscribe from a specific channel's updates.
    /// </summary>
    public async Task UnsubscribeFromChannel(string channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
        _logger.LogDebug("Client {ConnectionId} unsubscribed from channel {ChannelId}", 
            Context.ConnectionId, channelId);
    }

    /// <summary>
    /// Subscribe to all sync status updates (for sync status page).
    /// </summary>
    public async Task SubscribeToSyncStatus()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "sync-status");
        _logger.LogDebug("Client {ConnectionId} subscribed to sync status", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from sync status updates.
    /// </summary>
    public async Task UnsubscribeFromSyncStatus()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "sync-status");
        _logger.LogDebug("Client {ConnectionId} unsubscribed from sync status", Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to receive sync commands (for Bot service).
    /// </summary>
    public async Task SubscribeToCommands()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "bot-commands");
        _logger.LogInformation("Bot connected and subscribed to commands: {ConnectionId}", Context.ConnectionId);
    }

    /// <summary>
    /// Request a guild sync (from Dashboard to Bot).
    /// </summary>
    public async Task RequestGuildSync(string guildId, bool backfillMessages = true, int? messageLimit = null)
    {
        var command = new SyncGuildCommand(guildId, backfillMessages, messageLimit);
        await Clients.Group("bot-commands").TriggerGuildSync(command);
        _logger.LogInformation("Sync requested for guild {GuildId} by {ConnectionId}", guildId, Context.ConnectionId);
    }

    /// <summary>
    /// Request a channel sync (from Dashboard to Bot).
    /// </summary>
    public async Task RequestChannelSync(string guildId, string channelId, bool backfillMessages = true, int? messageLimit = null)
    {
        var command = new SyncChannelCommand(guildId, channelId, backfillMessages, messageLimit);
        await Clients.Group("bot-commands").TriggerChannelSync(command);
        _logger.LogInformation("Sync requested for channel {ChannelId} in guild {GuildId} by {ConnectionId}", 
            channelId, guildId, Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        // All clients receive global notifications
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", 
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Interface for strongly-typed hub clients.
/// </summary>
public interface ISyncHubClient
{
    Task GuildSynced(GuildSyncedEvent evt);
    Task ChannelSynced(ChannelSyncedEvent evt);
    Task MessageReceived(MessageReceivedEvent evt);
    Task MessageUpdated(MessageUpdatedEvent evt);
    Task MessageDeleted(MessageDeletedEvent evt);
    Task SyncProgress(SyncProgressEvent evt);
    Task SyncError(SyncErrorEvent evt);
    Task MemberUpdated(MemberUpdatedEvent evt);
    Task AttachmentDownloaded(AttachmentDownloadedEvent evt);
    
    // Commands from Dashboard to Bot
    Task TriggerGuildSync(SyncGuildCommand command);
    Task TriggerChannelSync(SyncChannelCommand command);
}

/// <summary>
/// Command to trigger a guild sync.
/// </summary>
public record SyncGuildCommand(
    string GuildId,
    bool BackfillMessages = true,
    int? MessageLimit = null);

/// <summary>
/// Command to trigger a channel sync.
/// </summary>
public record SyncChannelCommand(
    string GuildId,
    string ChannelId,
    bool BackfillMessages = true,
    int? MessageLimit = null);
