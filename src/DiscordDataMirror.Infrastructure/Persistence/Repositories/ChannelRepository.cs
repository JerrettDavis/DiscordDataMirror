using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class ChannelRepository : GenericRepository<Channel, Snowflake>, IChannelRepository
{
    public ChannelRepository(DiscordMirrorDbContext context) : base(context)
    {
    }
    
    public async Task<IReadOnlyList<Channel>> GetByGuildIdAsync(Snowflake guildId, CancellationToken ct = default)
        => await DbSet
            .Where(c => c.GuildId == guildId)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);
    
    public async Task<Channel?> GetWithMessagesAsync(Snowflake id, int skip = 0, int take = 50, CancellationToken ct = default)
        => await DbSet
            .Include(c => c.Messages
                .OrderByDescending(m => m.Timestamp)
                .Skip(skip)
                .Take(take))
            .ThenInclude(m => m.Author)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
