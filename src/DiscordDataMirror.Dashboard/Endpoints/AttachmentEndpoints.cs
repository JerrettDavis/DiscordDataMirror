using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace DiscordDataMirror.Dashboard.Endpoints;

/// <summary>
/// Minimal API endpoints for serving cached attachments.
/// </summary>
public static class AttachmentEndpoints
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/attachments")
            .WithTags("Attachments");

        // Get cached attachment by ID
        group.MapGet("/{attachmentId}", GetAttachmentAsync)
            .WithName("GetAttachment")
            .WithDescription("Serves a cached attachment file by its ID")
            .Produces(200)
            .Produces(404)
            .Produces(302);

        // Get attachment info
        group.MapGet("/{attachmentId}/info", GetAttachmentInfoAsync)
            .WithName("GetAttachmentInfo")
            .WithDescription("Gets metadata about an attachment");

        // Get storage stats
        group.MapGet("/stats", GetStorageStatsAsync)
            .WithName("GetAttachmentStats")
            .WithDescription("Gets attachment storage statistics");

        // Trigger download for an attachment
        group.MapPost("/{attachmentId}/download", TriggerDownloadAsync)
            .WithName("TriggerAttachmentDownload")
            .WithDescription("Triggers download of an attachment from Discord CDN");

        return endpoints;
    }

    private static async Task<IResult> GetAttachmentAsync(
        string attachmentId,
        [FromServices] IAttachmentStorageService storageService,
        [FromServices] IAttachmentRepository attachmentRepository,
        CancellationToken ct,
        [FromQuery] bool fallbackToDiscord = true)
    {
        if (!Snowflake.TryParse(attachmentId, out var snowflake))
        {
            return Results.BadRequest("Invalid attachment ID format");
        }

        // Try to get local file
        var fileStream = await storageService.GetFileStreamAsync(snowflake, ct);
        if (fileStream != null)
        {
            var attachment = await attachmentRepository.GetByIdAsync(snowflake, ct);
            var contentType = attachment?.ContentType ?? GetContentType(attachment?.Filename);
            var filename = attachment?.Filename ?? $"{attachmentId}";

            return Results.File(fileStream, contentType, filename, enableRangeProcessing: true);
        }

        // Fallback to Discord CDN URL
        if (fallbackToDiscord)
        {
            var attachment = await attachmentRepository.GetByIdAsync(snowflake, ct);
            if (attachment != null && !string.IsNullOrEmpty(attachment.Url))
            {
                return Results.Redirect(attachment.Url);
            }
        }

        return Results.NotFound("Attachment not found or not cached");
    }

    private static async Task<IResult> GetAttachmentInfoAsync(
        string attachmentId,
        [FromServices] IAttachmentRepository attachmentRepository,
        CancellationToken ct)
    {
        if (!Snowflake.TryParse(attachmentId, out var snowflake))
        {
            return Results.BadRequest("Invalid attachment ID format");
        }

        var attachment = await attachmentRepository.GetByIdAsync(snowflake, ct);
        if (attachment == null)
        {
            return Results.NotFound("Attachment not found");
        }

        return Results.Ok(new
        {
            attachment.Id,
            attachment.MessageId,
            attachment.Filename,
            attachment.Size,
            attachment.Width,
            attachment.Height,
            attachment.ContentType,
            attachment.IsCached,
            attachment.LocalPath,
            DownloadStatus = attachment.DownloadStatus.ToString(),
            attachment.ContentHash,
            attachment.DownloadedAt,
            attachment.DownloadAttempts,
            attachment.LastDownloadError,
            attachment.SkipReason,
            attachment.IsImage,
            attachment.IsVideo,
            attachment.IsAudio,
            DiscordUrl = attachment.Url,
            ProxyUrl = attachment.ProxyUrl
        });
    }

    private static async Task<IResult> GetStorageStatsAsync(
        [FromServices] IAttachmentStorageService storageService,
        CancellationToken ct)
    {
        var stats = await storageService.GetStorageStatsAsync(ct);

        return Results.Ok(new
        {
            stats.TotalCachedCount,
            TotalCachedMB = stats.TotalCachedBytes / (1024.0 * 1024.0),
            stats.PendingDownloadCount,
            stats.FailedDownloadCount,
            stats.UniqueHashCount,
            DeduplicationRatio = stats.UniqueHashCount > 0
                ? (double)stats.TotalCachedCount / stats.UniqueHashCount
                : 1.0
        });
    }

    private static async Task<IResult> TriggerDownloadAsync(
        string attachmentId,
        [FromQuery] string? guildId,
        [FromQuery] string? channelId,
        [FromServices] IAttachmentStorageService storageService,
        [FromServices] IAttachmentRepository attachmentRepository,
        CancellationToken ct)
    {
        if (!Snowflake.TryParse(attachmentId, out var attachmentSnowflake))
        {
            return Results.BadRequest("Invalid attachment ID format");
        }

        var attachment = await attachmentRepository.GetByIdAsync(attachmentSnowflake, ct);
        if (attachment == null)
        {
            return Results.NotFound("Attachment not found");
        }

        // Get guild and channel from message if not provided
        Snowflake guildSnowflake;
        Snowflake channelSnowflake;

        if (!string.IsNullOrEmpty(guildId) && Snowflake.TryParse(guildId, out var g))
        {
            guildSnowflake = g;
        }
        else if (attachment.Message?.Channel != null)
        {
            guildSnowflake = attachment.Message.Channel.GuildId;
        }
        else
        {
            return Results.BadRequest("Guild ID required - could not determine from attachment");
        }

        if (!string.IsNullOrEmpty(channelId) && Snowflake.TryParse(channelId, out var c))
        {
            channelSnowflake = c;
        }
        else if (attachment.Message != null)
        {
            channelSnowflake = attachment.Message.ChannelId;
        }
        else
        {
            return Results.BadRequest("Channel ID required - could not determine from attachment");
        }

        var result = await storageService.DownloadAsync(attachment, guildSnowflake, channelSnowflake, ct);

        return Results.Ok(new
        {
            result.Success,
            result.LocalPath,
            result.ContentHash,
            result.BytesDownloaded,
            result.ErrorMessage,
            result.WasSkipped,
            result.SkipReason
        });
    }

    private static string GetContentType(string? filename)
    {
        if (string.IsNullOrEmpty(filename))
            return "application/octet-stream";

        return ContentTypeProvider.TryGetContentType(filename, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
