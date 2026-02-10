using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Service for synchronizing Discord role data.
/// </summary>
public interface IRoleSyncService
{
    /// <summary>
    /// Syncs a role's data.
    /// </summary>
    Task<Role> SyncRoleAsync(
        Snowflake roleId,
        Snowflake guildId,
        string name,
        int color,
        int position,
        string? permissions,
        bool isHoisted,
        bool isMentionable,
        bool isManaged,
        string? rawJson = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Marks a role as deleted.
    /// </summary>
    Task DeleteRoleAsync(Snowflake roleId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all roles for a guild.
    /// </summary>
    Task<IReadOnlyList<Role>> GetGuildRolesAsync(Snowflake guildId, CancellationToken ct = default);
}
