using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Represents a user's membership in a specific guild.
/// </summary>
public class GuildMember : Entity<string> // Composite key: UserId_GuildId
{
    public Snowflake UserId { get; private set; }
    public Snowflake GuildId { get; private set; }
    public string? Nickname { get; private set; }
    public DateTime? JoinedAt { get; private set; }
    public bool IsPending { get; private set; }
    public List<string> RoleIds { get; private set; } = [];
    public DateTime? LastSyncedAt { get; private set; }
    public string? RawJson { get; private set; }

    // Navigation
    public User? User { get; private set; }
    public Guild? Guild { get; private set; }

    private GuildMember() { } // EF Core

    public GuildMember(Snowflake userId, Snowflake guildId)
    {
        UserId = userId;
        GuildId = guildId;
        Id = $"{userId}_{guildId}";
    }

    public void Update(string? nickname, DateTime? joinedAt, bool isPending, List<string> roleIds, string? rawJson = null)
    {
        Nickname = nickname;
        JoinedAt = joinedAt;
        IsPending = isPending;
        RoleIds = roleIds;
        RawJson = rawJson;
    }

    public void MarkSynced() => LastSyncedAt = DateTime.UtcNow;

    /// <summary>
    /// Display name: Nickname if set, otherwise user's display name.
    /// </summary>
    public string GetDisplayName(User user) => Nickname ?? user.DisplayName;
}
