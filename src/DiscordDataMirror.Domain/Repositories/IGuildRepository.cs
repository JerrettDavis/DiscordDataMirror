using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.ValueObjects;

namespace DiscordDataMirror.Domain.Repositories;

public interface IGuildRepository : IRepository<Guild, Snowflake>
{
    Task<Guild?> GetWithChannelsAsync(Snowflake id, CancellationToken ct = default);
    Task<Guild?> GetWithRolesAsync(Snowflake id, CancellationToken ct = default);
    Task<Guild?> GetWithMembersAsync(Snowflake id, CancellationToken ct = default);
    Task<Guild?> GetFullAsync(Snowflake id, CancellationToken ct = default);
    Task<IReadOnlyList<Guild>> GetAllWithStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guild>> GetNeedingSyncAsync(TimeSpan syncInterval, CancellationToken ct = default);
}

public interface IChannelRepository : IRepository<Channel, Snowflake>
{
    Task<IReadOnlyList<Channel>> GetByGuildIdAsync(Snowflake guildId, CancellationToken ct = default);
    Task<Channel?> GetWithMessagesAsync(Snowflake id, int skip = 0, int take = 50, CancellationToken ct = default);
}

public interface IUserRepository : IRepository<User, Snowflake>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<User>> SearchByUsernameAsync(string searchTerm, int take = 20, CancellationToken ct = default);
}

public interface IMessageRepository : IRepository<Message, Snowflake>
{
    Task<IReadOnlyList<Message>> GetByChannelIdAsync(Snowflake channelId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetByAuthorIdAsync(Snowflake authorId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> SearchAsync(string query, Snowflake? channelId = null, Snowflake? authorId = null, 
        DateTime? from = null, DateTime? to = null, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<Snowflake?> GetLastMessageIdAsync(Snowflake channelId, CancellationToken ct = default);
    Task<int> GetCountByChannelAsync(Snowflake channelId, CancellationToken ct = default);
}

public interface IRoleRepository : IRepository<Role, Snowflake>
{
    Task<IReadOnlyList<Role>> GetByGuildIdAsync(Snowflake guildId, CancellationToken ct = default);
}

public interface IGuildMemberRepository : IRepository<GuildMember, string>
{
    Task<GuildMember?> GetByUserAndGuildAsync(Snowflake userId, Snowflake guildId, CancellationToken ct = default);
    Task<IReadOnlyList<GuildMember>> GetByGuildIdAsync(Snowflake guildId, CancellationToken ct = default);
    Task<IReadOnlyList<GuildMember>> GetByUserIdAsync(Snowflake userId, CancellationToken ct = default);
}

public interface IReactionRepository : IRepository<Reaction, string>
{
    Task<Reaction?> GetByMessageAndEmoteAsync(Snowflake messageId, string emoteKey, CancellationToken ct = default);
    Task<IReadOnlyList<Reaction>> GetByMessageIdAsync(Snowflake messageId, CancellationToken ct = default);
}

public interface IThreadRepository : IRepository<Entities.Thread, Snowflake>
{
    Task<IReadOnlyList<Entities.Thread>> GetByParentChannelAsync(Snowflake parentChannelId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Thread>> GetArchivedAsync(Snowflake guildId, CancellationToken ct = default);
}

public interface IAttachmentRepository : IRepository<Attachment, Snowflake>
{
    Task<IReadOnlyList<Attachment>> GetByMessageIdAsync(Snowflake messageId, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> GetUncachedAsync(int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> GetByStatusAsync(AttachmentDownloadStatus status, int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> GetQueuedAsync(int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> GetFailedAsync(int maxAttempts = 3, int take = 100, CancellationToken ct = default);
    Task<Attachment?> GetByContentHashAsync(string contentHash, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllLocalPathsAsync(CancellationToken ct = default);
    Task<AttachmentStorageStatistics> GetStorageStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Statistics about attachment storage from the database.
/// </summary>
public record AttachmentStorageStatistics(
    long TotalCount,
    long CachedCount,
    long TotalSizeBytes,
    long CachedSizeBytes,
    long PendingCount,
    long QueuedCount,
    long FailedCount,
    long SkippedCount,
    long UniqueHashCount);

public interface IEmbedRepository : IRepository<Embed, int>
{
    Task<IReadOnlyList<Embed>> GetByMessageIdAsync(Snowflake messageId, CancellationToken ct = default);
    Task DeleteByMessageIdAsync(Snowflake messageId, CancellationToken ct = default);
}

public interface ISyncStateRepository : IRepository<SyncState, int>
{
    Task<SyncState?> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default);
    Task<IReadOnlyList<SyncState>> GetByStatusAsync(SyncStatus status, CancellationToken ct = default);
}

public interface IUserMapRepository : IRepository<UserMap, int>
{
    Task<IReadOnlyList<UserMap>> GetByCanonicalUserAsync(Snowflake canonicalUserId, CancellationToken ct = default);
    Task<IReadOnlyList<UserMap>> GetSuggestionsAsync(decimal minConfidence = 0.5m, CancellationToken ct = default);
}
