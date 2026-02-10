using System.Collections.Concurrent;
using System.Security.Cryptography;
using DiscordDataMirror.Application.Configuration;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Local file system implementation of attachment storage.
/// </summary>
public class LocalAttachmentStorageService : IAttachmentStorageService
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AttachmentOptions _options;
    private readonly ILogger<LocalAttachmentStorageService> _logger;

    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly ConcurrentQueue<(Snowflake AttachmentId, Snowflake GuildId, Snowflake ChannelId)> _downloadQueue = new();

    private const string HttpClientName = "DiscordCdn";

    public LocalAttachmentStorageService(
        IAttachmentRepository attachmentRepository,
        IChannelRepository channelRepository,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        IOptions<AttachmentOptions> options,
        ILogger<LocalAttachmentStorageService> logger)
    {
        _attachmentRepository = attachmentRepository;
        _channelRepository = channelRepository;
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _downloadSemaphore = new SemaphoreSlim(_options.MaxConcurrentDownloads);
    }

    public async Task<AttachmentDownloadResult> DownloadAsync(
        Attachment attachment,
        Snowflake guildId,
        Snowflake channelId,
        CancellationToken ct = default)
    {
        // Check if already cached
        if (attachment.IsCached && !string.IsNullOrEmpty(attachment.LocalPath) && File.Exists(attachment.LocalPath))
        {
            return new AttachmentDownloadResult(true, attachment.LocalPath, attachment.ContentHash, null, null, true, "Already cached");
        }

        // Check size limit
        if (_options.MaxFileSizeBytes > 0 && attachment.Size > _options.MaxFileSizeBytes)
        {
            var reason = $"File size {attachment.Size:N0} bytes exceeds limit of {_options.MaxFileSizeBytes:N0} bytes";
            attachment.SetSkipped(reason);
            await _unitOfWork.SaveChangesAsync(ct);
            return new AttachmentDownloadResult(false, null, null, null, reason, true, reason);
        }

        // Check content type restrictions
        if (!IsContentTypeAllowed(attachment.ContentType))
        {
            var reason = $"Content type '{attachment.ContentType}' is not allowed";
            attachment.SetSkipped(reason);
            await _unitOfWork.SaveChangesAsync(ct);
            return new AttachmentDownloadResult(false, null, null, null, reason, true, reason);
        }

        // Check for deduplication by hash if enabled
        if (_options.DeduplicateByHash && !string.IsNullOrEmpty(attachment.ContentHash))
        {
            var existing = await _attachmentRepository.GetByContentHashAsync(attachment.ContentHash, ct);
            if (existing != null && existing.Id != attachment.Id && File.Exists(existing.LocalPath))
            {
                // Reuse existing file
                attachment.SetCached(existing.LocalPath!, existing.ContentHash);
                await _unitOfWork.SaveChangesAsync(ct);
                return new AttachmentDownloadResult(true, existing.LocalPath, existing.ContentHash, 0, null, true, "Deduplicated from existing file");
            }
        }

        await _downloadSemaphore.WaitAsync(ct);
        try
        {
            return await DownloadInternalAsync(attachment, guildId, channelId, ct);
        }
        finally
        {
            _downloadSemaphore.Release();
        }
    }

    private async Task<AttachmentDownloadResult> DownloadInternalAsync(
        Attachment attachment,
        Snowflake guildId,
        Snowflake channelId,
        CancellationToken ct)
    {
        attachment.SetDownloading();
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            // Create directory structure: attachments/{guildId}/{channelId}/{messageId}/
            var messageId = attachment.MessageId.Value;
            var directory = Path.Combine(
                _options.StoragePath,
                guildId.Value,
                channelId.Value,
                messageId);

            Directory.CreateDirectory(directory);

            // Handle filename conflicts
            var filename = SanitizeFilename(attachment.Filename);
            var filePath = Path.Combine(directory, filename);
            filePath = GetUniqueFilePath(filePath);

            // Download the file
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            client.Timeout = TimeSpan.FromSeconds(_options.DownloadTimeoutSeconds);

            _logger.LogDebug("Downloading attachment {AttachmentId} from {Url}", attachment.Id, attachment.Url);

            using var response = await client.GetAsync(attachment.Url, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";

                // Handle CDN URL expiration
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    errorMessage = $"CDN URL expired or unavailable: {response.StatusCode}";
                }

                attachment.SetFailed(errorMessage);
                await _unitOfWork.SaveChangesAsync(ct);
                return new AttachmentDownloadResult(false, null, null, null, errorMessage);
            }

            // Stream to disk with hash calculation
            long bytesDownloaded = 0;
            string? contentHash = null;

            using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            using (var hashAlgorithm = SHA256.Create())
            {
                var buffer = new byte[81920];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                    bytesDownloaded += bytesRead;

                    // Check if we've exceeded size limit during download
                    if (_options.MaxFileSizeBytes > 0 && bytesDownloaded > _options.MaxFileSizeBytes)
                    {
                        throw new InvalidOperationException($"Download exceeded size limit at {bytesDownloaded:N0} bytes");
                    }
                }

                hashAlgorithm.TransformFinalBlock([], 0, 0);
                contentHash = Convert.ToHexString(hashAlgorithm.Hash!).ToLowerInvariant();
            }

            // Check for deduplication after download
            if (_options.DeduplicateByHash)
            {
                var existing = await _attachmentRepository.GetByContentHashAsync(contentHash, ct);
                if (existing != null && existing.Id != attachment.Id && File.Exists(existing.LocalPath))
                {
                    // Delete the just-downloaded file and reuse existing
                    File.Delete(filePath);
                    attachment.SetCached(existing.LocalPath!, contentHash);
                    await _unitOfWork.SaveChangesAsync(ct);

                    _logger.LogDebug("Deduplicated attachment {AttachmentId} to existing file {LocalPath}",
                        attachment.Id, existing.LocalPath);

                    return new AttachmentDownloadResult(true, existing.LocalPath, contentHash, bytesDownloaded, null, true, "Deduplicated after download");
                }
            }

            attachment.SetCached(filePath, contentHash);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Downloaded attachment {AttachmentId}: {Filename} ({Bytes:N0} bytes)",
                attachment.Id, attachment.Filename, bytesDownloaded);

            return new AttachmentDownloadResult(true, filePath, contentHash, bytesDownloaded, null);
        }
        catch (OperationCanceledException)
        {
            attachment.SetFailed("Download cancelled");
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Network error: {ex.Message}";
            attachment.SetFailed(errorMessage);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogWarning(ex, "Failed to download attachment {AttachmentId}", attachment.Id);
            return new AttachmentDownloadResult(false, null, null, null, errorMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message;
            attachment.SetFailed(errorMessage);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogError(ex, "Unexpected error downloading attachment {AttachmentId}", attachment.Id);
            return new AttachmentDownloadResult(false, null, null, null, errorMessage);
        }
    }

    public async Task<IReadOnlyList<AttachmentDownloadResult>> DownloadBatchAsync(
        IEnumerable<(Attachment Attachment, Snowflake GuildId, Snowflake ChannelId)> attachments,
        CancellationToken ct = default)
    {
        var results = new List<AttachmentDownloadResult>();
        var tasks = new List<Task<AttachmentDownloadResult>>();

        foreach (var (attachment, guildId, channelId) in attachments)
        {
            ct.ThrowIfCancellationRequested();
            tasks.Add(DownloadAsync(attachment, guildId, channelId, ct));
        }

        var completedResults = await Task.WhenAll(tasks);
        results.AddRange(completedResults);

        return results;
    }

    public Task QueueDownloadAsync(
        Snowflake attachmentId,
        Snowflake guildId,
        Snowflake channelId,
        CancellationToken ct = default)
    {
        _downloadQueue.Enqueue((attachmentId, guildId, channelId));
        _logger.LogDebug("Queued attachment {AttachmentId} for background download", attachmentId);
        return Task.CompletedTask;
    }

    public async Task ProcessQueueAsync(CancellationToken ct = default)
    {
        // First, load any queued items from the database
        var queuedAttachments = await _attachmentRepository.GetQueuedAsync(100, ct);

        foreach (var attachment in queuedAttachments)
        {
            if (ct.IsCancellationRequested) break;

            // Get channel info to find guild ID
            var channel = await _channelRepository.GetByIdAsync(attachment.MessageId, ct);
            if (channel == null)
            {
                _logger.LogWarning("Cannot find channel for queued attachment {AttachmentId}", attachment.Id);
                continue;
            }

            // We need to get the message's channel info
            var message = attachment.Message;
            if (message?.Channel == null)
            {
                _logger.LogWarning("Cannot find message/channel info for queued attachment {AttachmentId}", attachment.Id);
                continue;
            }

            await DownloadAsync(attachment, message.Channel.GuildId, message.ChannelId, ct);
        }

        // Then process any in-memory queue items
        while (_downloadQueue.TryDequeue(out var item) && !ct.IsCancellationRequested)
        {
            var attachment = await _attachmentRepository.GetByIdAsync(item.AttachmentId, ct);
            if (attachment == null) continue;

            await DownloadAsync(attachment, item.GuildId, item.ChannelId, ct);
        }
    }

    public async Task<string?> GetLocalPathAsync(Snowflake attachmentId, CancellationToken ct = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, ct);
        if (attachment?.IsCached == true && !string.IsNullOrEmpty(attachment.LocalPath) && File.Exists(attachment.LocalPath))
        {
            return attachment.LocalPath;
        }
        return null;
    }

    public async Task<Stream?> GetFileStreamAsync(Snowflake attachmentId, CancellationToken ct = default)
    {
        var localPath = await GetLocalPathAsync(attachmentId, ct);
        if (localPath != null)
        {
            return new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        return null;
    }

    public async Task<bool> IsCachedAsync(Snowflake attachmentId, CancellationToken ct = default)
    {
        return await GetLocalPathAsync(attachmentId, ct) != null;
    }

    public async Task<bool> DeleteCachedAsync(Snowflake attachmentId, CancellationToken ct = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, ct);
        if (attachment?.LocalPath == null) return false;

        try
        {
            if (File.Exists(attachment.LocalPath))
            {
                File.Delete(attachment.LocalPath);
            }

            attachment.ResetCache();
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Deleted cached attachment {AttachmentId}", attachmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cached attachment {AttachmentId}", attachmentId);
            return false;
        }
    }

    public async Task<AttachmentStorageStats> GetStorageStatsAsync(CancellationToken ct = default)
    {
        var dbStats = await _attachmentRepository.GetStorageStatisticsAsync(ct);

        return new AttachmentStorageStats(
            dbStats.CachedCount,
            dbStats.CachedSizeBytes,
            dbStats.PendingCount + dbStats.QueuedCount,
            dbStats.FailedCount,
            dbStats.UniqueHashCount);
    }

    public Task<(bool IsValid, string? ErrorMessage)> ValidateStorageAsync(CancellationToken ct = default)
    {
        try
        {
            var storagePath = Path.GetFullPath(_options.StoragePath);

            // Create directory if it doesn't exist
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            // Check if we can write to it
            var testFile = Path.Combine(storagePath, $".write-test-{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            // Check available space (warning if < 1GB)
            var driveInfo = new DriveInfo(Path.GetPathRoot(storagePath) ?? storagePath);
            if (driveInfo.AvailableFreeSpace < 1024 * 1024 * 1024)
            {
                return Task.FromResult<(bool, string?)>((true, $"Warning: Only {driveInfo.AvailableFreeSpace / (1024 * 1024):N0} MB free space available"));
            }

            return Task.FromResult<(bool, string?)>((true, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult<(bool, string?)>((false, $"Storage validation failed: {ex.Message}"));
        }
    }

    private bool IsContentTypeAllowed(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return true;

        // Check blocked types first
        if (_options.BlockedContentTypes.Length > 0)
        {
            foreach (var blocked in _options.BlockedContentTypes)
            {
                if (contentType.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        // If allowed types specified, content type must match one
        if (_options.AllowedContentTypes.Length > 0)
        {
            foreach (var allowed in _options.AllowedContentTypes)
            {
                if (contentType.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        return true;
    }

    private static string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(filename.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

        // Limit length
        if (sanitized.Length > 200)
        {
            var ext = Path.GetExtension(sanitized);
            sanitized = sanitized[..(200 - ext.Length)] + ext;
        }

        return sanitized;
    }

    private static string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath)) return filePath;

        var directory = Path.GetDirectoryName(filePath)!;
        var filename = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        var counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(directory, $"{filename}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath) && counter < 1000);

        return newPath;
    }
}
