using DiscordDataMirror.Application.Configuration;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Service for cleaning up orphaned and expired attachment files.
/// </summary>
public class AttachmentCleanupService : IAttachmentCleanupService
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AttachmentOptions _options;
    private readonly ILogger<AttachmentCleanupService> _logger;

    public AttachmentCleanupService(
        IAttachmentRepository attachmentRepository,
        IUnitOfWork unitOfWork,
        IOptions<AttachmentOptions> options,
        ILogger<AttachmentCleanupService> logger)
    {
        _attachmentRepository = attachmentRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CleanupResult> CleanupOrphanedFilesAsync(CancellationToken ct = default)
    {
        var errors = new List<string>();
        var filesDeleted = 0;
        long bytesReclaimed = 0;

        try
        {
            var storagePath = Path.GetFullPath(_options.StoragePath);

            if (!Directory.Exists(storagePath))
            {
                _logger.LogDebug("Storage path does not exist, nothing to clean up");
                return new CleanupResult(0, 0, 0, errors);
            }

            // Get all known file paths from database
            var knownPaths = (await _attachmentRepository.GetAllLocalPathsAsync(ct))
                .Select(p => Path.GetFullPath(p))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Scan directory for all files
            var allFiles = Directory.GetFiles(storagePath, "*", SearchOption.AllDirectories);

            var cutoffDate = DateTime.UtcNow.AddDays(-_options.OrphanRetentionDays);

            foreach (var filePath in allFiles)
            {
                ct.ThrowIfCancellationRequested();

                // Skip if file is known to database
                if (knownPaths.Contains(Path.GetFullPath(filePath)))
                    continue;

                // Skip if file is too new (within retention period)
                if (_options.OrphanRetentionDays > 0)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.CreationTimeUtc > cutoffDate)
                        continue;
                }

                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileSize = fileInfo.Length;

                    File.Delete(filePath);

                    filesDeleted++;
                    bytesReclaimed += fileSize;

                    _logger.LogDebug("Deleted orphaned file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete {filePath}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to delete orphaned file: {FilePath}", filePath);
                }
            }

            // Clean up empty directories
            CleanupEmptyDirectories(storagePath, errors);

            _logger.LogInformation("Orphaned file cleanup complete: {FilesDeleted} files deleted, {BytesReclaimed:N0} bytes reclaimed",
                filesDeleted, bytesReclaimed);
        }
        catch (Exception ex)
        {
            errors.Add($"Cleanup failed: {ex.Message}");
            _logger.LogError(ex, "Orphaned file cleanup failed");
        }

        return new CleanupResult(filesDeleted, bytesReclaimed, 0, errors);
    }

    public async Task<CleanupResult> CleanupMissingFilesAsync(CancellationToken ct = default)
    {
        var errors = new List<string>();
        var recordsUpdated = 0;

        try
        {
            // Find all cached attachments
            var cachedAttachments = await _attachmentRepository.GetByStatusAsync(AttachmentDownloadStatus.Completed, 1000, ct);

            foreach (var attachment in cachedAttachments)
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(attachment.LocalPath) || File.Exists(attachment.LocalPath))
                    continue;

                // File is missing, reset cache status
                attachment.ResetCache();
                await _attachmentRepository.UpdateAsync(attachment, ct);
                recordsUpdated++;

                _logger.LogDebug("Reset cache status for attachment {AttachmentId} - file missing: {LocalPath}",
                    attachment.Id, attachment.LocalPath);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Missing file cleanup complete: {RecordsUpdated} database records updated", recordsUpdated);
        }
        catch (Exception ex)
        {
            errors.Add($"Cleanup failed: {ex.Message}");
            _logger.LogError(ex, "Missing file cleanup failed");
        }

        return new CleanupResult(0, 0, recordsUpdated, errors);
    }

    public async Task<CleanupResult> RunFullCleanupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting full attachment cleanup");

        var missingResult = await CleanupMissingFilesAsync(ct);
        var orphanedResult = await CleanupOrphanedFilesAsync(ct);

        var combinedErrors = new List<string>();
        combinedErrors.AddRange(missingResult.Errors);
        combinedErrors.AddRange(orphanedResult.Errors);

        var result = new CleanupResult(
            orphanedResult.OrphanedFilesDeleted,
            orphanedResult.BytesReclaimed,
            missingResult.DatabaseRecordsUpdated,
            combinedErrors);

        _logger.LogInformation("Full cleanup complete: {FilesDeleted} orphaned files deleted, {BytesReclaimed:N0} bytes reclaimed, {RecordsUpdated} records updated",
            result.OrphanedFilesDeleted, result.BytesReclaimed, result.DatabaseRecordsUpdated);

        return result;
    }

    public async Task<CleanupStats> GetCleanupStatsAsync(CancellationToken ct = default)
    {
        var orphanedFileCount = 0;
        long orphanedFilesBytes = 0;
        var missingFileRecordCount = 0;
        var staleQueueCount = 0;

        try
        {
            var storagePath = Path.GetFullPath(_options.StoragePath);

            if (Directory.Exists(storagePath))
            {
                // Get all known file paths from database
                var knownPaths = (await _attachmentRepository.GetAllLocalPathsAsync(ct))
                    .Select(p => Path.GetFullPath(p))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Scan directory for orphaned files
                var allFiles = Directory.GetFiles(storagePath, "*", SearchOption.AllDirectories);
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.OrphanRetentionDays);

                foreach (var filePath in allFiles)
                {
                    if (knownPaths.Contains(Path.GetFullPath(filePath)))
                        continue;

                    if (_options.OrphanRetentionDays > 0)
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.CreationTimeUtc > cutoffDate)
                            continue;
                    }

                    orphanedFileCount++;
                    orphanedFilesBytes += new FileInfo(filePath).Length;
                }
            }

            // Check for missing files in database records
            var cachedAttachments = await _attachmentRepository.GetByStatusAsync(AttachmentDownloadStatus.Completed, 10000, ct);
            foreach (var attachment in cachedAttachments)
            {
                if (!string.IsNullOrEmpty(attachment.LocalPath) && !File.Exists(attachment.LocalPath))
                {
                    missingFileRecordCount++;
                }
            }

            // Count stale queued items (queued for more than 24 hours)
            var queuedAttachments = await _attachmentRepository.GetQueuedAsync(1000, ct);
            // Note: We'd need a queued timestamp to properly identify stale items
            staleQueueCount = queuedAttachments.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cleanup stats");
        }

        return new CleanupStats(orphanedFileCount, orphanedFilesBytes, missingFileRecordCount, staleQueueCount);
    }

    private void CleanupEmptyDirectories(string storagePath, List<string> errors)
    {
        try
        {
            var directories = Directory.GetDirectories(storagePath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length); // Process deepest first

            foreach (var dir in directories)
            {
                try
                {
                    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                        _logger.LogDebug("Deleted empty directory: {Directory}", dir);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete empty directory {dir}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to enumerate directories: {ex.Message}");
        }
    }
}
