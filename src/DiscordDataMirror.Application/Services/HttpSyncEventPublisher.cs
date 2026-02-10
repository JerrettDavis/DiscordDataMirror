using System.Net.Http.Json;
using DiscordDataMirror.Application.Events;
using Microsoft.Extensions.Logging;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// HTTP implementation of the sync event publisher.
/// Publishes events to the Dashboard's API endpoints.
/// </summary>
public class HttpSyncEventPublisher : ISyncEventPublisher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpSyncEventPublisher> _logger;

    public HttpSyncEventPublisher(HttpClient httpClient, ILogger<HttpSyncEventPublisher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task PublishGuildSyncedAsync(GuildSyncedEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/guild-synced", evt, ct);
    }

    public async Task PublishChannelSyncedAsync(ChannelSyncedEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/channel-synced", evt, ct);
    }

    public async Task PublishMessageReceivedAsync(MessageReceivedEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/message-received", evt, ct);
    }

    public async Task PublishMessageUpdatedAsync(MessageUpdatedEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/message-updated", evt, ct);
    }

    public async Task PublishMessageDeletedAsync(MessageDeletedEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/message-deleted", evt, ct);
    }

    public async Task PublishSyncProgressAsync(SyncProgressEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/sync-progress", evt, ct);
    }

    public async Task PublishSyncErrorAsync(SyncErrorEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/sync-error", evt, ct);
    }

    public async Task PublishMemberUpdatedAsync(MemberUpdatedEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/member-updated", evt, ct);
    }

    public async Task PublishAttachmentDownloadedAsync(AttachmentDownloadedEvent evt, CancellationToken ct = default)
    {
        await PostEventAsync("/api/events/attachment-downloaded", evt, ct);
    }

    private async Task PostEventAsync<T>(string endpoint, T evt, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, evt, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to publish event to {Endpoint}: {StatusCode}", 
                    endpoint, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            // Don't fail operations if event publishing fails
            _logger.LogWarning(ex, "Failed to publish event to {Endpoint}", endpoint);
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation
        }
    }
}
