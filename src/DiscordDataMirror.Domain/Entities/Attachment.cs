using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Status of attachment download/cache operation.
/// </summary>
public enum AttachmentDownloadStatus
{
    /// <summary>Not yet attempted.</summary>
    Pending = 0,
    
    /// <summary>Currently downloading.</summary>
    InProgress = 1,
    
    /// <summary>Successfully downloaded and cached.</summary>
    Completed = 2,
    
    /// <summary>Download failed.</summary>
    Failed = 3,
    
    /// <summary>Skipped (too large, blocked type, etc.).</summary>
    Skipped = 4,
    
    /// <summary>Queued for background download.</summary>
    Queued = 5
}

/// <summary>
/// Represents a file attachment on a Discord message.
/// </summary>
public class Attachment : Entity<Snowflake>
{
    public Snowflake MessageId { get; private set; }
    public string Filename { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? ProxyUrl { get; private set; }
    public long Size { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string? ContentType { get; private set; }
    public string? LocalPath { get; private set; }
    public bool IsCached { get; private set; }
    
    // Download tracking
    public AttachmentDownloadStatus DownloadStatus { get; private set; } = AttachmentDownloadStatus.Pending;
    public string? ContentHash { get; private set; }
    public DateTime? DownloadedAt { get; private set; }
    public int DownloadAttempts { get; private set; }
    public string? LastDownloadError { get; private set; }
    public string? SkipReason { get; private set; }
    
    // Navigation
    public Message? Message { get; private set; }
    
    private Attachment() { } // EF Core
    
    public Attachment(Snowflake id, Snowflake messageId, string filename, string url, long size)
    {
        Id = id;
        MessageId = messageId;
        Filename = filename;
        Url = url;
        Size = size;
    }
    
    public void Update(string? proxyUrl, int? width, int? height, string? contentType)
    {
        ProxyUrl = proxyUrl;
        Width = width;
        Height = height;
        ContentType = contentType;
    }
    
    /// <summary>
    /// Marks the attachment as successfully cached.
    /// </summary>
    public void SetCached(string localPath, string? contentHash = null)
    {
        LocalPath = localPath;
        ContentHash = contentHash;
        IsCached = true;
        DownloadStatus = AttachmentDownloadStatus.Completed;
        DownloadedAt = DateTime.UtcNow;
        LastDownloadError = null;
    }
    
    /// <summary>
    /// Marks the download as in progress.
    /// </summary>
    public void SetDownloading()
    {
        DownloadStatus = AttachmentDownloadStatus.InProgress;
        DownloadAttempts++;
    }
    
    /// <summary>
    /// Marks the download as queued for background processing.
    /// </summary>
    public void SetQueued()
    {
        DownloadStatus = AttachmentDownloadStatus.Queued;
    }
    
    /// <summary>
    /// Marks the download as failed.
    /// </summary>
    public void SetFailed(string errorMessage)
    {
        DownloadStatus = AttachmentDownloadStatus.Failed;
        LastDownloadError = errorMessage;
        IsCached = false;
        LocalPath = null;
    }
    
    /// <summary>
    /// Marks the attachment as skipped (won't be downloaded).
    /// </summary>
    public void SetSkipped(string reason)
    {
        DownloadStatus = AttachmentDownloadStatus.Skipped;
        SkipReason = reason;
    }
    
    /// <summary>
    /// Resets the cache status (e.g., when file is missing).
    /// </summary>
    public void ResetCache()
    {
        IsCached = false;
        LocalPath = null;
        ContentHash = null;
        DownloadedAt = null;
        DownloadStatus = AttachmentDownloadStatus.Pending;
    }
    
    /// <summary>
    /// Whether this attachment is an image based on content type or file extension.
    /// </summary>
    public bool IsImage => ContentType?.StartsWith("image/") == true 
        || Filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// Whether this attachment is a video based on content type or file extension.
    /// </summary>
    public bool IsVideo => ContentType?.StartsWith("video/") == true
        || Filename.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".mov", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// Whether this attachment is audio based on content type or file extension.
    /// </summary>
    public bool IsAudio => ContentType?.StartsWith("audio/") == true
        || Filename.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
        || Filename.EndsWith(".flac", StringComparison.OrdinalIgnoreCase);
}
