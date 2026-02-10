using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Represents an embed within a Discord message.
/// Structure is flexible, so most data is stored as JSON.
/// </summary>
public class Embed : Entity<int> // Auto-generated ID
{
    public Snowflake MessageId { get; private set; }
    public int Index { get; private set; }
    public string? Type { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? Url { get; private set; }
    public DateTime? Timestamp { get; private set; }
    public int? Color { get; private set; }
    public string Data { get; private set; } = "{}"; // Full embed JSON
    
    // Navigation
    public Message? Message { get; private set; }
    
    private Embed() { } // EF Core
    
    public Embed(Snowflake messageId, int index, string data)
    {
        MessageId = messageId;
        Index = index;
        Data = data;
    }
    
    public void Update(string? type, string? title, string? description, string? url, DateTime? timestamp, int? color, string data)
    {
        Type = type;
        Title = title;
        Description = description;
        Url = url;
        Timestamp = timestamp;
        Color = color;
        Data = data;
    }
    
    /// <summary>
    /// Converts the color int to a hex string.
    /// </summary>
    public string? ColorHex => Color.HasValue ? $"#{Color:X6}" : null;
}
