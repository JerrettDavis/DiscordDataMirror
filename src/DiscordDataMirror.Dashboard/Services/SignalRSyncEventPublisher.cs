using DiscordDataMirror.Application.Events;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Dashboard.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DiscordDataMirror.Dashboard.Services;

/// <summary>
/// SignalR implementation of the sync event publisher.
/// Broadcasts events to connected clients using SignalR hub.
/// </summary>
public class SignalRSyncEventPublisher : ISyncEventPublisher
{
    private readonly IHubContext<SyncHub, ISyncHubClient> _hubContext;
    private readonly ILogger<SignalRSyncEventPublisher> _logger;

    public SignalRSyncEventPublisher(
        IHubContext<SyncHub, ISyncHubClient> hubContext,
        ILogger<SignalRSyncEventPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishGuildSyncedAsync(GuildSyncedEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing GuildSynced for {GuildId}", evt.GuildId.SanitizeForLog());

        // Send to subscribers of this guild and sync status
        await Task.WhenAll(
            _hubContext.Clients.Group($"guild:{evt.GuildId}").GuildSynced(evt),
            _hubContext.Clients.Group("sync-status").GuildSynced(evt),
            _hubContext.Clients.Group("all").GuildSynced(evt)
        );
    }

    public async Task PublishChannelSyncedAsync(ChannelSyncedEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing ChannelSynced for {ChannelId}", evt.ChannelId.SanitizeForLog());

        await Task.WhenAll(
            _hubContext.Clients.Group($"guild:{evt.GuildId}").ChannelSynced(evt),
            _hubContext.Clients.Group($"channel:{evt.ChannelId}").ChannelSynced(evt),
            _hubContext.Clients.Group("sync-status").ChannelSynced(evt)
        );
    }

    public async Task PublishMessageReceivedAsync(MessageReceivedEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing MessageReceived for {MessageId} in {ChannelId}",
            evt.MessageId.SanitizeForLog(), evt.ChannelId.SanitizeForLog());

        // Send to channel subscribers and guild subscribers
        await Task.WhenAll(
            _hubContext.Clients.Group($"channel:{evt.ChannelId}").MessageReceived(evt),
            _hubContext.Clients.Group($"guild:{evt.GuildId}").MessageReceived(evt)
        );
    }

    public async Task PublishMessageUpdatedAsync(MessageUpdatedEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing MessageUpdated for {MessageId}", evt.MessageId.SanitizeForLog());

        await Task.WhenAll(
            _hubContext.Clients.Group($"channel:{evt.ChannelId}").MessageUpdated(evt),
            _hubContext.Clients.Group($"guild:{evt.GuildId}").MessageUpdated(evt)
        );
    }

    public async Task PublishMessageDeletedAsync(MessageDeletedEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing MessageDeleted for {MessageId}", evt.MessageId.SanitizeForLog());

        await Task.WhenAll(
            _hubContext.Clients.Group($"channel:{evt.ChannelId}").MessageDeleted(evt),
            _hubContext.Clients.Group($"guild:{evt.GuildId}").MessageDeleted(evt)
        );
    }

    public async Task PublishSyncProgressAsync(SyncProgressEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing SyncProgress for {GuildId}: {EntityType} - {Status}",
            evt.GuildId.SanitizeForLog(), evt.EntityType.SanitizeForLog(), evt.Status.SanitizeForLog());

        await Task.WhenAll(
            _hubContext.Clients.Group($"guild:{evt.GuildId}").SyncProgress(evt),
            _hubContext.Clients.Group("sync-status").SyncProgress(evt)
        );
    }

    public async Task PublishSyncErrorAsync(SyncErrorEvent evt, CancellationToken ct = default)
    {
        _logger.LogWarning("Publishing SyncError for {GuildId}: {ErrorMessage}",
            evt.GuildId.SanitizeForLog(), evt.ErrorMessage.SanitizeForLog());

        await Task.WhenAll(
            _hubContext.Clients.Group($"guild:{evt.GuildId}").SyncError(evt),
            _hubContext.Clients.Group("sync-status").SyncError(evt),
            _hubContext.Clients.Group("all").SyncError(evt)
        );
    }

    public async Task PublishMemberUpdatedAsync(MemberUpdatedEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing MemberUpdated for {UserId} in {GuildId}",
            evt.UserId.SanitizeForLog(), evt.GuildId.SanitizeForLog());

        await _hubContext.Clients.Group($"guild:{evt.GuildId}").MemberUpdated(evt);
    }

    public async Task PublishAttachmentDownloadedAsync(AttachmentDownloadedEvent evt, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing AttachmentDownloaded for {AttachmentId}: {Success}",
            evt.AttachmentId.SanitizeForLog(), evt.Success);

        await Task.WhenAll(
            _hubContext.Clients.Group($"channel:{evt.ChannelId}").AttachmentDownloaded(evt),
            _hubContext.Clients.Group($"guild:{evt.GuildId}").AttachmentDownloaded(evt)
        );
    }
}
