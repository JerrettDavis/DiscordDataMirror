using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Thread = DiscordDataMirror.Domain.Entities.Thread;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class ThreadRepository : GenericRepository<Thread, Snowflake>, IThreadRepository
{
    public ThreadRepository(DiscordMirrorDbContext context) : base(context)
    {
    }
    
    public async Task<IReadOnlyList<Thread>> GetByParentChannelAsync(Snowflake parentChannelId, CancellationToken ct = default)
        => await DbSet
            .Where(t => t.ParentChannelId == parentChannelId)
            .Include(t => t.Channel)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Thread>> GetArchivedAsync(Snowflake guildId, CancellationToken ct = default)
        => await DbSet
            .Where(t => t.IsArchived && t.Channel != null && t.Channel.GuildId == guildId)
            .Include(t => t.Channel)
            .ToListAsync(ct);
}
