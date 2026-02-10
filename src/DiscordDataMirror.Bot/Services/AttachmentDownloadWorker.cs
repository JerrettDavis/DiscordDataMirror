using DiscordDataMirror.Application.Configuration;
using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace DiscordDataMirror.Bot.Services;

/// <summary>
/// Background worker that processes queued attachment downloads.
/// </summary>
public class AttachmentDownloadWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttachmentDownloadWorker> _logger;
    private readonly AttachmentOptions _options;

    public AttachmentDownloadWorker(
        IServiceProvider serviceProvider,
        ILogger<AttachmentDownloadWorker> logger,
        IOptions<AttachmentOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.AutoDownload)
        {
            _logger.LogInformation("Attachment auto-download is disabled");
            return;
        }

        _logger.LogInformation("Attachment download worker starting...");

        // Wait for the bot to be ready before starting downloads
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDownloadsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing attachment downloads");
            }

            // Wait before next batch
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Attachment download worker stopped");
    }

    private async Task ProcessDownloadsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var attachmentRepository = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
        var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IAttachmentStorageService>();

        // Process queued attachments
        var queuedAttachments = await attachmentRepository.GetQueuedAsync(_options.MaxConcurrentDownloads * 2, ct);
        if (queuedAttachments.Count > 0)
        {
            _logger.LogDebug("Processing {Count} queued attachments", queuedAttachments.Count);

            foreach (var attachment in queuedAttachments)
            {
                ct.ThrowIfCancellationRequested();

                var (guildId, channelId) = await GetAttachmentLocationAsync(attachment, channelRepository, ct);
                if (guildId == null || channelId == null)
                {
                    _logger.LogWarning("Cannot determine location for attachment {AttachmentId}", attachment.Id);
                    continue;
                }

                var result = await storageService.DownloadAsync(attachment, guildId.Value, channelId.Value, ct);

                if (result.Success)
                {
                    _logger.LogDebug("Downloaded attachment {AttachmentId}: {Filename}", attachment.Id, attachment.Filename);
                }
                else if (!result.WasSkipped)
                {
                    _logger.LogWarning("Failed to download attachment {AttachmentId}: {Error}",
                        attachment.Id, result.ErrorMessage);
                }
            }
        }

        // Retry failed attachments (with limited attempts)
        var failedAttachments = await attachmentRepository.GetFailedAsync(_options.MaxRetryAttempts, _options.MaxConcurrentDownloads, ct);
        if (failedAttachments.Count > 0)
        {
            _logger.LogDebug("Retrying {Count} failed attachments", failedAttachments.Count);

            foreach (var attachment in failedAttachments)
            {
                ct.ThrowIfCancellationRequested();

                var (guildId, channelId) = await GetAttachmentLocationAsync(attachment, channelRepository, ct);
                if (guildId == null || channelId == null) continue;

                var result = await storageService.DownloadAsync(attachment, guildId.Value, channelId.Value, ct);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully retried attachment {AttachmentId}: {Filename}",
                        attachment.Id, attachment.Filename);
                }
            }
        }

        // Process pending attachments that weren't caught in real-time
        var pendingAttachments = await attachmentRepository.GetByStatusAsync(
            AttachmentDownloadStatus.Pending,
            _options.MaxConcurrentDownloads,
            ct);

        if (pendingAttachments.Count > 0)
        {
            _logger.LogDebug("Processing {Count} pending attachments", pendingAttachments.Count);

            foreach (var attachment in pendingAttachments)
            {
                ct.ThrowIfCancellationRequested();

                // Skip large files - they should be queued
                if (attachment.Size > _options.BackgroundDownloadThreshold)
                {
                    continue;
                }

                var (guildId, channelId) = await GetAttachmentLocationAsync(attachment, channelRepository, ct);
                if (guildId == null || channelId == null) continue;

                await storageService.DownloadAsync(attachment, guildId.Value, channelId.Value, ct);
            }
        }
    }

    private async Task<(Domain.ValueObjects.Snowflake? GuildId, Domain.ValueObjects.Snowflake? ChannelId)> GetAttachmentLocationAsync(
        Attachment attachment,
        IChannelRepository channelRepository,
        CancellationToken ct)
    {
        // If we have message with channel info loaded
        if (attachment.Message?.Channel != null)
        {
            return (attachment.Message.Channel.GuildId, attachment.Message.ChannelId);
        }

        // Try to find channel by message's channel ID
        if (attachment.Message != null)
        {
            var channel = await channelRepository.GetByIdAsync(attachment.Message.ChannelId, ct);
            if (channel != null)
            {
                return (channel.GuildId, channel.Id);
            }
        }

        return (null, null);
    }
}
