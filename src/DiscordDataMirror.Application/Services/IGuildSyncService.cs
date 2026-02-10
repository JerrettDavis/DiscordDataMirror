using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Service for synchronizing Discord guild data.
/// </summary>
public interface IGuildSyncService
{
    /// <summary>
    /// Syncs a guild's metadata (name, icon, description, etc.).
    /// </summary>
    Task<Guild> SyncGuildAsync(
        Snowflake guildId,
        string name,
        string? iconUrl,
        string? description,
        Snowflake ownerId,
        DateTime createdAt,
        string? rawJson = null,
        CancellationToken ct = default);

    /// <summary>
    /// Performs a full sync of all guild data including channels, roles, and members.
    /// </summary>
    Task FullSyncGuildAsync(Snowflake guildId, CancellationToken ct = default);

    /// <summary>
    /// Gets all guilds that need to be synced.
    /// </summary>
    Task<IReadOnlyList<Guild>> GetGuildsNeedingSyncAsync(TimeSpan syncInterval, CancellationToken ct = default);
}
