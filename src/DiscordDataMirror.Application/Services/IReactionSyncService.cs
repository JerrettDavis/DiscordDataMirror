using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Application.Services;

/// <summary>
/// Service for synchronizing Discord reaction data.
/// </summary>
public interface IReactionSyncService
{
    /// <summary>
    /// Adds a reaction to a message.
    /// </summary>
    Task AddReactionAsync(
        Snowflake messageId,
        string emoteKey,
        Snowflake userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Removes a reaction from a message.
    /// </summary>
    Task RemoveReactionAsync(
        Snowflake messageId,
        string emoteKey,
        Snowflake userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Syncs all reactions for a message.
    /// </summary>
    Task SyncReactionsAsync(
        Snowflake messageId,
        IEnumerable<ReactionData> reactions,
        CancellationToken ct = default);
}

/// <summary>
/// Data transfer object for reaction sync.
/// </summary>
public record ReactionData(
    string EmoteKey,
    int Count,
    IEnumerable<string> UserIds);
