using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

public enum MappingType
{
    Manual,
    Username,
    Avatar,
    Activity,
    Suggested
}

/// <summary>
/// Maps users across servers for identity correlation.
/// </summary>
public class UserMap : Entity<int>
{
    public Snowflake CanonicalUserId { get; private set; }
    public Snowflake MappedUserId { get; private set; }
    public decimal Confidence { get; private set; } // 0.00 to 1.00
    public MappingType MappingType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? Notes { get; private set; }
    
    // Navigation
    public User? CanonicalUser { get; private set; }
    public User? MappedUser { get; private set; }
    
    private UserMap() { } // EF Core
    
    public UserMap(Snowflake canonicalUserId, Snowflake mappedUserId, MappingType mappingType, decimal confidence = 1.0m)
    {
        CanonicalUserId = canonicalUserId;
        MappedUserId = mappedUserId;
        MappingType = mappingType;
        Confidence = Math.Clamp(confidence, 0, 1);
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateConfidence(decimal confidence, string? notes = null)
    {
        Confidence = Math.Clamp(confidence, 0, 1);
        Notes = notes ?? Notes;
    }
    
    public void SetNotes(string notes) => Notes = notes;
    
    /// <summary>
    /// Whether this mapping is confirmed (manual or high confidence).
    /// </summary>
    public bool IsConfirmed => MappingType == MappingType.Manual || Confidence >= 0.9m;
}
