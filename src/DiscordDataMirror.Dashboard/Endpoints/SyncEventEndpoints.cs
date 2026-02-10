using DiscordDataMirror.Application.Events;
using DiscordDataMirror.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DiscordDataMirror.Dashboard.Endpoints;

/// <summary>
/// API endpoints for publishing sync events from the Bot service.
/// </summary>
public static class SyncEventEndpoints
{
    public static IEndpointRouteBuilder MapSyncEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/events")
            .WithTags("Sync Events");

        group.MapPost("/guild-synced", async (
            [FromBody] GuildSyncedEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishGuildSyncedAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishGuildSynced");

        group.MapPost("/channel-synced", async (
            [FromBody] ChannelSyncedEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishChannelSyncedAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishChannelSynced");

        group.MapPost("/message-received", async (
            [FromBody] MessageReceivedEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishMessageReceivedAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishMessageReceived");

        group.MapPost("/message-updated", async (
            [FromBody] MessageUpdatedEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishMessageUpdatedAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishMessageUpdated");

        group.MapPost("/message-deleted", async (
            [FromBody] MessageDeletedEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishMessageDeletedAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishMessageDeleted");

        group.MapPost("/sync-progress", async (
            [FromBody] SyncProgressEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishSyncProgressAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishSyncProgress");

        group.MapPost("/sync-error", async (
            [FromBody] SyncErrorEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishSyncErrorAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishSyncError");

        group.MapPost("/member-updated", async (
            [FromBody] MemberUpdatedEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishMemberUpdatedAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishMemberUpdated");

        group.MapPost("/attachment-downloaded", async (
            [FromBody] AttachmentDownloadedEvent evt,
            [FromServices] ISyncEventPublisher publisher,
            CancellationToken ct) =>
        {
            await publisher.PublishAttachmentDownloadedAsync(evt, ct);
            return Results.Ok();
        }).WithName("PublishAttachmentDownloaded");

        return endpoints;
    }
}
