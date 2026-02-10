using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

public enum ChannelType
{
    Text = 0,
    DM = 1,
    Voice = 2,
    GroupDM = 3,
    Category = 4,
    News = 5,
    NewsThread = 10,
    PublicThread = 11,
    PrivateThread = 12,
    Stage = 13,
    GuildDirectory = 14,
    Forum = 15,
    Media = 16
}

/// <summary>
/// Represents a Discord channel within a guild.
/// </summary>
public class Channel : Entity<Snowflake>
{
    public Snowflake GuildId { get; private set; }
    public Snowflake? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ChannelType Type { get; private set; }
    public string? Topic { get; private set; }
    public int Position { get; private set; }
    public bool IsNsfw { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public string? RawJson { get; private set; }

    // Navigation
    public Guild? Guild { get; private set; }
    public Channel? Parent { get; private set; }

    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private Channel() { } // EF Core

    public Channel(Snowflake id, Snowflake guildId, string name, ChannelType type, DateTime createdAt)
    {
        Id = id;
        GuildId = guildId;
        Name = name;
        Type = type;
        CreatedAt = createdAt;
    }

    public void Update(string name, ChannelType type, string? topic, int position, bool isNsfw, Snowflake? parentId, string? rawJson = null)
    {
        Name = name;
        Type = type;
        Topic = topic;
        Position = position;
        IsNsfw = isNsfw;
        ParentId = parentId;
        RawJson = rawJson;
    }

    public void MarkSynced() => LastSyncedAt = DateTime.UtcNow;

    public bool IsThread => Type is ChannelType.NewsThread or ChannelType.PublicThread or ChannelType.PrivateThread;
}
