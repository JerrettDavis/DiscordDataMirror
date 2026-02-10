namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Result of a cleanup operation.
/// </summary>
public record CleanupResult(
    int OrphanedFilesDeleted,
    long BytesReclaimed,
    int DatabaseRecordsUpdated,
    IReadOnlyList<string> Errors);

/// <summary>
/// Service for cleaning up orphaned and expired attachment files.
/// </summary>
public interface IAttachmentCleanupService
{
    /// <summary>
    /// Finds and removes orphaned attachment files (files on disk with no database reference).
    /// </summary>
    Task<CleanupResult> CleanupOrphanedFilesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Finds database records pointing to missing files and resets their cache status.
    /// </summary>
    Task<CleanupResult> CleanupMissingFilesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Runs all cleanup operations.
    /// </summary>
    Task<CleanupResult> RunFullCleanupAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets cleanup statistics without performing any deletions.
    /// </summary>
    Task<CleanupStats> GetCleanupStatsAsync(CancellationToken ct = default);
}

/// <summary>
/// Statistics about potential cleanup items.
/// </summary>
public record CleanupStats(
    int OrphanedFileCount,
    long OrphanedFilesBytes,
    int MissingFileRecordCount,
    int StaleDownloadQueueCount);
