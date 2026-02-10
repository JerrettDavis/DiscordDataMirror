using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Result of an attachment download operation.
/// </summary>
public record AttachmentDownloadResult(
    bool Success,
    string? LocalPath,
    string? ContentHash,
    long? BytesDownloaded,
    string? ErrorMessage,
    bool WasSkipped = false,
    string? SkipReason = null);

/// <summary>
/// Service for downloading and managing attachment file storage.
/// </summary>
public interface IAttachmentStorageService
{
    /// <summary>
    /// Downloads an attachment from Discord CDN and stores it locally.
    /// </summary>
    /// <param name="attachment">The attachment entity to download.</param>
    /// <param name="guildId">Guild ID for organizing storage.</param>
    /// <param name="channelId">Channel ID for organizing storage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the download operation.</returns>
    Task<AttachmentDownloadResult> DownloadAsync(
        Attachment attachment,
        Snowflake guildId,
        Snowflake channelId,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads multiple attachments, respecting concurrency limits.
    /// </summary>
    Task<IReadOnlyList<AttachmentDownloadResult>> DownloadBatchAsync(
        IEnumerable<(Attachment Attachment, Snowflake GuildId, Snowflake ChannelId)> attachments,
        CancellationToken ct = default);

    /// <summary>
    /// Queues an attachment for background download.
    /// </summary>
    Task QueueDownloadAsync(
        Snowflake attachmentId,
        Snowflake guildId,
        Snowflake channelId,
        CancellationToken ct = default);

    /// <summary>
    /// Processes queued downloads in the background.
    /// </summary>
    Task ProcessQueueAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the local file path for a cached attachment.
    /// Returns null if not cached.
    /// </summary>
    Task<string?> GetLocalPathAsync(Snowflake attachmentId, CancellationToken ct = default);

    /// <summary>
    /// Gets a stream to read the cached attachment file.
    /// Returns null if not cached.
    /// </summary>
    Task<Stream?> GetFileStreamAsync(Snowflake attachmentId, CancellationToken ct = default);

    /// <summary>
    /// Checks if an attachment is cached locally.
    /// </summary>
    Task<bool> IsCachedAsync(Snowflake attachmentId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a cached attachment file.
    /// </summary>
    Task<bool> DeleteCachedAsync(Snowflake attachmentId, CancellationToken ct = default);

    /// <summary>
    /// Gets attachment storage statistics.
    /// </summary>
    Task<AttachmentStorageStats> GetStorageStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Validates storage path is accessible and has space.
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidateStorageAsync(CancellationToken ct = default);
}

/// <summary>
/// Statistics about attachment storage.
/// </summary>
public record AttachmentStorageStats(
    long TotalCachedCount,
    long TotalCachedBytes,
    long PendingDownloadCount,
    long FailedDownloadCount,
    long UniqueHashCount);
