using DiscordDataMirror.Application.Events;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Interface for publishing sync events to connected clients.
/// </summary>
public interface ISyncEventPublisher
{
    /// <summary>
    /// Publishes a guild synced event.
    /// </summary>
    Task PublishGuildSyncedAsync(GuildSyncedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes a channel synced event.
    /// </summary>
    Task PublishChannelSyncedAsync(ChannelSyncedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes a message received event.
    /// </summary>
    Task PublishMessageReceivedAsync(MessageReceivedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes a message updated event.
    /// </summary>
    Task PublishMessageUpdatedAsync(MessageUpdatedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes a message deleted event.
    /// </summary>
    Task PublishMessageDeletedAsync(MessageDeletedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes a sync progress event.
    /// </summary>
    Task PublishSyncProgressAsync(SyncProgressEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes a sync error event.
    /// </summary>
    Task PublishSyncErrorAsync(SyncErrorEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes a member updated event.
    /// </summary>
    Task PublishMemberUpdatedAsync(MemberUpdatedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes an attachment downloaded event.
    /// </summary>
    Task PublishAttachmentDownloadedAsync(AttachmentDownloadedEvent evt, CancellationToken ct = default);
}

/// <summary>
/// No-op implementation used when SignalR is not available (e.g., in Bot service).
/// </summary>
public class NullSyncEventPublisher : ISyncEventPublisher
{
    public Task PublishGuildSyncedAsync(GuildSyncedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishChannelSyncedAsync(ChannelSyncedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishMessageReceivedAsync(MessageReceivedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishMessageUpdatedAsync(MessageUpdatedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishMessageDeletedAsync(MessageDeletedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishSyncProgressAsync(SyncProgressEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishSyncErrorAsync(SyncErrorEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishMemberUpdatedAsync(MemberUpdatedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishAttachmentDownloadedAsync(AttachmentDownloadedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
}
