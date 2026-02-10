namespace DiscordDataMirror.Application.Configuration;

/// <summary>
/// Configuration options for the Discord bot.
/// </summary>
public class DiscordOptions
{
    public const string SectionName = "Discord";
    
    /// <summary>
    /// Discord bot token.
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to sync guild data when the bot starts.
    /// </summary>
    public bool SyncOnStartup { get; set; } = true;
    
    /// <summary>
    /// Path where attachments will be cached.
    /// </summary>
    public string AttachmentCachePath { get; set; } = "./attachments";
}

/// <summary>
/// Configuration options for sync behavior.
/// </summary>
public class SyncOptions
{
    public const string SectionName = "Sync";
    
    /// <summary>
    /// Number of messages to fetch per batch.
    /// </summary>
    public int MessageBatchSize { get; set; } = 100;
    
    /// <summary>
    /// Maximum number of historical messages to sync per channel.
    /// </summary>
    public int MaxHistoricalMessages { get; set; } = 10000;
    
    /// <summary>
    /// Interval in minutes for periodic sync operations.
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 60;
    
    /// <summary>
    /// Delay in milliseconds between API requests to avoid rate limiting.
    /// </summary>
    public int RateLimitDelayMs { get; set; } = 500;
}
