using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Represents a reaction on a Discord message.
/// </summary>
public class Reaction : Entity<string> // Composite key: MessageId_EmoteKey
{
    public Snowflake MessageId { get; private set; }
    public string EmoteKey { get; private set; } = string.Empty; // "emoji_name" or "custom:id:name"
    public int Count { get; private set; }
    public List<string> UserIds { get; private set; } = []; // Users who reacted

    // Navigation
    public Message? Message { get; private set; }

    private Reaction() { } // EF Core

    public Reaction(Snowflake messageId, string emoteKey, int count)
    {
        MessageId = messageId;
        EmoteKey = emoteKey;
        Count = count;
        Id = $"{messageId}_{emoteKey}";
    }

    public void Update(int count, List<string> userIds)
    {
        Count = count;
        UserIds = userIds;
    }

    public void AddUser(string userId)
    {
        if (!UserIds.Contains(userId))
        {
            UserIds.Add(userId);
            Count = UserIds.Count;
        }
    }

    public void RemoveUser(string userId)
    {
        if (UserIds.Remove(userId))
            Count = UserIds.Count;
    }

    /// <summary>
    /// Whether this is a custom emoji (vs Unicode).
    /// </summary>
    public bool IsCustom => EmoteKey.StartsWith("custom:");

    /// <summary>
    /// For custom emotes, extracts the ID.
    /// </summary>
    public string? CustomEmoteId => IsCustom ? EmoteKey.Split(':')[1] : null;

    /// <summary>
    /// Display name of the emote.
    /// </summary>
    public string DisplayName => IsCustom ? EmoteKey.Split(':')[2] : EmoteKey;
}
