namespace DiscordDataMirror.Application.Configuration;

/// <summary>
/// Configuration options for attachment storage and caching.
/// </summary>
public class AttachmentOptions
{
    public const string SectionName = "Attachments";

    /// <summary>
    /// Base path where attachments will be stored.
    /// Default: ./attachments
    /// </summary>
    public string StoragePath { get; set; } = "./attachments";

    /// <summary>
    /// Maximum file size in bytes to download (0 = unlimited).
    /// Default: 100 MB
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Allowed content types for download. Empty = all allowed.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } = [];

    /// <summary>
    /// Blocked content types for download (takes precedence over AllowedContentTypes).
    /// </summary>
    public string[] BlockedContentTypes { get; set; } = [];

    /// <summary>
    /// Whether to download attachments automatically when syncing messages.
    /// </summary>
    public bool AutoDownload { get; set; } = true;

    /// <summary>
    /// Size threshold in bytes above which downloads are queued for background processing.
    /// Default: 5 MB
    /// </summary>
    public long BackgroundDownloadThreshold { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum concurrent downloads.
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 3;

    /// <summary>
    /// Timeout for individual download operations in seconds.
    /// </summary>
    public int DownloadTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Number of retry attempts for failed downloads.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to use content hashing to detect and skip duplicate files.
    /// </summary>
    public bool DeduplicateByHash { get; set; } = true;

    /// <summary>
    /// Days after which orphaned attachments (no database reference) are deleted.
    /// 0 = never delete.
    /// </summary>
    public int OrphanRetentionDays { get; set; } = 30;

    /// <summary>
    /// Whether to generate thumbnails for images.
    /// </summary>
    public bool GenerateThumbnails { get; set; } = false;

    /// <summary>
    /// Maximum thumbnail dimension (width or height).
    /// </summary>
    public int ThumbnailMaxDimension { get; set; } = 200;
}
