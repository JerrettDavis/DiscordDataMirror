using DiscordDataMirror.Domain.Common;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Entities;

public enum SyncStatus
{
    Idle,
    InProgress,
    Completed,
    Failed,
    Paused
}

/// <summary>
/// Tracks sync progress for entities (guilds, channels, etc.).
/// </summary>
public class SyncState : Entity<int>
{
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTime LastSyncedAt { get; private set; }
    public Snowflake? LastMessageId { get; private set; } // For incremental message sync
    public SyncStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    private SyncState() { } // EF Core

    public SyncState(string entityType, string entityId)
    {
        EntityType = entityType;
        EntityId = entityId;
        Status = SyncStatus.Idle;
    }

    public void StartSync()
    {
        Status = SyncStatus.InProgress;
        ErrorMessage = null;
    }

    public void CompleteSync(Snowflake? lastMessageId = null)
    {
        Status = SyncStatus.Completed;
        LastSyncedAt = DateTime.UtcNow;
        LastMessageId = lastMessageId ?? LastMessageId;
        ErrorMessage = null;
    }

    public void FailSync(string errorMessage)
    {
        Status = SyncStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void Pause() => Status = SyncStatus.Paused;
    public void Resume() => Status = SyncStatus.InProgress;
}
