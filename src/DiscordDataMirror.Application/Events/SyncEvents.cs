namespace DiscordDataMirror.Application.Events;

/// <summary>
/// Event raised when a guild is synced.
/// </summary>
public record GuildSyncedEvent(
    string GuildId,
    string GuildName,
    DateTime SyncedAt);

/// <summary>
/// Event raised when a channel is synced.
/// </summary>
public record ChannelSyncedEvent(
    string GuildId,
    string ChannelId,
    string ChannelName,
    DateTime SyncedAt);

/// <summary>
/// Event raised when a new message is received.
/// </summary>
public record MessageReceivedEvent(
    string GuildId,
    string ChannelId,
    string MessageId,
    string AuthorId,
    string AuthorName,
    string? Content,
    DateTime Timestamp);

/// <summary>
/// Event raised when a message is updated.
/// </summary>
public record MessageUpdatedEvent(
    string GuildId,
    string ChannelId,
    string MessageId,
    string? NewContent,
    DateTime EditedAt);

/// <summary>
/// Event raised when a message is deleted.
/// </summary>
public record MessageDeletedEvent(
    string GuildId,
    string ChannelId,
    string MessageId,
    DateTime DeletedAt);

/// <summary>
/// Event raised for sync progress updates.
/// </summary>
public record SyncProgressEvent(
    string GuildId,
    string GuildName,
    string EntityType,
    string? EntityName,
    int CurrentCount,
    int? TotalCount,
    double? PercentComplete,
    string Status);

/// <summary>
/// Event raised when a sync error occurs.
/// </summary>
public record SyncErrorEvent(
    string GuildId,
    string? GuildName,
    string EntityType,
    string EntityId,
    string ErrorMessage,
    DateTime OccurredAt);

/// <summary>
/// Event raised when a member joins or is updated.
/// </summary>
public record MemberUpdatedEvent(
    string GuildId,
    string UserId,
    string Username,
    string? Nickname,
    DateTime UpdatedAt);

/// <summary>
/// Event raised when attachment download completes.
/// </summary>
public record AttachmentDownloadedEvent(
    string GuildId,
    string ChannelId,
    string MessageId,
    string AttachmentId,
    string Filename,
    bool Success,
    DateTime CompletedAt);
