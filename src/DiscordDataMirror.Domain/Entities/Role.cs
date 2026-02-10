using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Represents a Discord role within a guild.
/// </summary>
public class Role : Entity<Snowflake>
{
    public Snowflake GuildId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Color { get; private set; }
    public int Position { get; private set; }
    public string? Permissions { get; private set; } // BigInt stored as string
    public bool IsHoisted { get; private set; }
    public bool IsMentionable { get; private set; }
    public bool IsManaged { get; private set; }
    public string? RawJson { get; private set; }

    // Navigation
    public Guild? Guild { get; private set; }

    private Role() { } // EF Core

    public Role(Snowflake id, Snowflake guildId, string name)
    {
        Id = id;
        GuildId = guildId;
        Name = name;
    }

    public void Update(string name, int color, int position, string? permissions,
        bool isHoisted, bool isMentionable, bool isManaged, string? rawJson = null)
    {
        Name = name;
        Color = color;
        Position = position;
        Permissions = permissions;
        IsHoisted = isHoisted;
        IsMentionable = isMentionable;
        IsManaged = isManaged;
        RawJson = rawJson;
    }

    /// <summary>
    /// Converts the color int to a hex string (e.g., "#FF5733").
    /// </summary>
    public string ColorHex => $"#{Color:X6}";
}
