using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Represents a Discord user (global, not per-guild).
/// </summary>
public class User : Entity<Snowflake>
{
    public string Username { get; private set; } = string.Empty;
    public string? Discriminator { get; private set; }
    public string? GlobalName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsBot { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public string? RawJson { get; private set; }

    // Navigation
    private readonly List<GuildMember> _memberships = [];
    public IReadOnlyCollection<GuildMember> Memberships => _memberships.AsReadOnly();

    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private User() { } // EF Core

    public User(Snowflake id, string username, DateTime createdAt)
    {
        Id = id;
        Username = username;
        CreatedAt = createdAt;
    }

    public void Update(string username, string? discriminator, string? globalName, string? avatarUrl, bool isBot, string? rawJson = null)
    {
        Username = username;
        Discriminator = discriminator;
        GlobalName = globalName;
        AvatarUrl = avatarUrl;
        IsBot = isBot;
        RawJson = rawJson;
    }

    public void MarkSeen() => LastSeenAt = DateTime.UtcNow;

    /// <summary>
    /// Display name: GlobalName if set, otherwise Username.
    /// </summary>
    public string DisplayName => GlobalName ?? Username;
}
