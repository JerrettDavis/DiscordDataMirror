using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Service for synchronizing Discord channel data.
/// </summary>
public interface IChannelSyncService
{
    /// <summary>
    /// Syncs a channel's metadata.
    /// </summary>
    Task<Channel> SyncChannelAsync(
        Snowflake channelId,
        Snowflake guildId,
        string name,
        ChannelType type,
        string? topic,
        int position,
        bool isNsfw,
        Snowflake? parentId,
        DateTime createdAt,
        string? rawJson = null,
        CancellationToken ct = default);

    /// <summary>
    /// Syncs thread-specific data for a channel.
    /// </summary>
    Task<Domain.Entities.Thread> SyncThreadAsync(
        Snowflake threadId,
        Snowflake parentChannelId,
        Snowflake? ownerId,
        int messageCount,
        int memberCount,
        bool isArchived,
        bool isLocked,
        DateTime? archiveTimestamp,
        int? autoArchiveDuration,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a channel as deleted (soft delete or removal).
    /// </summary>
    Task DeleteChannelAsync(Snowflake channelId, CancellationToken ct = default);

    /// <summary>
    /// Gets all channels for a guild.
    /// </summary>
    Task<IReadOnlyList<Channel>> GetGuildChannelsAsync(Snowflake guildId, CancellationToken ct = default);
}
