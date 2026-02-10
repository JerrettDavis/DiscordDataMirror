using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class MessageRepository : GenericRepository<Message, Snowflake>, IMessageRepository
{
    public MessageRepository(DiscordMirrorDbContext context) : base(context)
    {
    }
    
    public async Task<IReadOnlyList<Message>> GetByChannelIdAsync(Snowflake channelId, int skip = 0, int take = 50, CancellationToken ct = default)
        => await DbSet
            .Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .Include(m => m.Author)
            .Include(m => m.Attachments)
            .Include(m => m.Embeds)
            .Include(m => m.Reactions)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Message>> GetByAuthorIdAsync(Snowflake authorId, int skip = 0, int take = 50, CancellationToken ct = default)
        => await DbSet
            .Where(m => m.AuthorId == authorId)
            .OrderByDescending(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .Include(m => m.Channel)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Message>> SearchAsync(
        string query,
        Snowflake? channelId = null,
        Snowflake? authorId = null,
        DateTime? from = null,
        DateTime? to = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        var queryable = DbSet.AsQueryable();
        
        if (channelId.HasValue)
            queryable = queryable.Where(m => m.ChannelId == channelId.Value);
        
        if (authorId.HasValue)
            queryable = queryable.Where(m => m.AuthorId == authorId.Value);
        
        if (from.HasValue)
            queryable = queryable.Where(m => m.Timestamp >= from.Value);
        
        if (to.HasValue)
            queryable = queryable.Where(m => m.Timestamp <= to.Value);
        
        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(m => m.Content != null && EF.Functions.ILike(m.Content, $"%{query}%"));
        
        return await queryable
            .OrderByDescending(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .Include(m => m.Author)
            .Include(m => m.Channel)
            .ToListAsync(ct);
    }
    
    public async Task<Snowflake?> GetLastMessageIdAsync(Snowflake channelId, CancellationToken ct = default)
    {
        var message = await DbSet
            .Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(ct);
        
        return message?.Id;
    }
    
    public async Task<int> GetCountByChannelAsync(Snowflake channelId, CancellationToken ct = default)
        => await DbSet.CountAsync(m => m.ChannelId == channelId, ct);
}
