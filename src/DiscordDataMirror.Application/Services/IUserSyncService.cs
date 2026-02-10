using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Service for synchronizing Discord user data.
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// Syncs a user's global data.
    /// </summary>
    Task<User> SyncUserAsync(
        Snowflake userId,
        string username,
        string? discriminator,
        string? globalName,
        string? avatarUrl,
        bool isBot,
        DateTime createdAt,
        string? rawJson = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Syncs a user's guild membership.
    /// </summary>
    Task<GuildMember> SyncGuildMemberAsync(
        Snowflake userId,
        Snowflake guildId,
        string? nickname,
        DateTime? joinedAt,
        bool isPending,
        IEnumerable<string> roleIds,
        string? rawJson = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Marks a user as having left a guild.
    /// </summary>
    Task RemoveGuildMemberAsync(Snowflake userId, Snowflake guildId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all members of a guild.
    /// </summary>
    Task<IReadOnlyList<GuildMember>> GetGuildMembersAsync(Snowflake guildId, CancellationToken ct = default);
    
    /// <summary>
    /// Batch sync guild members (for historical sync).
    /// </summary>
    Task SyncGuildMemberBatchAsync(Snowflake guildId, IEnumerable<GuildMemberData> members, CancellationToken ct = default);
}

/// <summary>
/// Data transfer object for guild member batch sync.
/// </summary>
public record GuildMemberData(
    Snowflake UserId,
    string Username,
    string? Discriminator,
    string? GlobalName,
    string? AvatarUrl,
    bool IsBot,
    DateTime UserCreatedAt,
    string? Nickname,
    DateTime? JoinedAt,
    bool IsPending,
    IEnumerable<string> RoleIds,
    string? RawJson);
