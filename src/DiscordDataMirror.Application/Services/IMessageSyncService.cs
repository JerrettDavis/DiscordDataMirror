using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Service for synchronizing Discord messages.
/// </summary>
public interface IMessageSyncService
{
    /// <summary>
    /// Syncs a single message.
    /// </summary>
    Task<Message> SyncMessageAsync(
        Snowflake messageId,
        Snowflake channelId,
        Snowflake authorId,
        string? content,
        string? cleanContent,
        MessageType type,
        bool isPinned,
        bool isTts,
        DateTime timestamp,
        DateTime? editedTimestamp,
        Snowflake? referencedMessageId,
        string? rawJson = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Marks a message as deleted.
    /// </summary>
    Task DeleteMessageAsync(Snowflake messageId, CancellationToken ct = default);
    
    /// <summary>
    /// Syncs attachments for a message.
    /// </summary>
    Task SyncAttachmentsAsync(
        Snowflake messageId,
        IEnumerable<AttachmentData> attachments,
        CancellationToken ct = default);
    
    /// <summary>
    /// Syncs embeds for a message.
    /// </summary>
    Task SyncEmbedsAsync(
        Snowflake messageId,
        IEnumerable<EmbedData> embeds,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the last synced message ID for a channel.
    /// </summary>
    Task<Snowflake?> GetLastMessageIdAsync(Snowflake channelId, CancellationToken ct = default);
    
    /// <summary>
    /// Batch sync messages (for historical sync).
    /// </summary>
    Task SyncMessageBatchAsync(IEnumerable<MessageData> messages, CancellationToken ct = default);
}

/// <summary>
/// Data transfer object for attachment sync.
/// </summary>
public record AttachmentData(
    Snowflake Id,
    string Filename,
    string Url,
    string? ProxyUrl,
    long Size,
    int? Width,
    int? Height,
    string? ContentType);

/// <summary>
/// Data transfer object for embed sync.
/// </summary>
public record EmbedData(
    int Index,
    string? Type,
    string? Title,
    string? Description,
    string? Url,
    DateTime? Timestamp,
    int? Color,
    string? JsonData);

/// <summary>
/// Data transfer object for message batch sync.
/// </summary>
public record MessageData(
    Snowflake Id,
    Snowflake ChannelId,
    Snowflake AuthorId,
    string? Content,
    string? CleanContent,
    MessageType Type,
    bool IsPinned,
    bool IsTts,
    DateTime Timestamp,
    DateTime? EditedTimestamp,
    Snowflake? ReferencedMessageId,
    string? RawJson,
    IEnumerable<AttachmentData>? Attachments,
    IEnumerable<EmbedData>? Embeds);
