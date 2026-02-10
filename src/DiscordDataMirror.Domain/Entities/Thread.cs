using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

/// <summary>
/// Represents a Discord thread (extends channel metadata).
/// </summary>
public class Thread : Entity<Snowflake>
{
    public Snowflake ParentChannelId { get; private set; }
    public Snowflake? OwnerId { get; private set; }
    public int MessageCount { get; private set; }
    public int MemberCount { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsLocked { get; private set; }
    public DateTime? ArchiveTimestamp { get; private set; }
    public int? AutoArchiveDuration { get; private set; }
    
    // Navigation (thread ID = channel ID)
    public Channel? Channel { get; private set; }
    public Channel? ParentChannel { get; private set; }
    public User? Owner { get; private set; }
    
    private Thread() { } // EF Core
    
    public Thread(Snowflake id, Snowflake parentChannelId)
    {
        Id = id;
        ParentChannelId = parentChannelId;
    }
    
    public void Update(Snowflake? ownerId, int messageCount, int memberCount, 
        bool isArchived, bool isLocked, DateTime? archiveTimestamp, int? autoArchiveDuration)
    {
        OwnerId = ownerId;
        MessageCount = messageCount;
        MemberCount = memberCount;
        IsArchived = isArchived;
        IsLocked = isLocked;
        ArchiveTimestamp = archiveTimestamp;
        AutoArchiveDuration = autoArchiveDuration;
    }
}
