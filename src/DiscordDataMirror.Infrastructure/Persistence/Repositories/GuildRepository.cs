using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class GuildRepository : GenericRepository<Guild, Snowflake>, IGuildRepository
{
    public GuildRepository(DiscordMirrorDbContext context) : base(context)
    {
    }

    public async Task<Guild?> GetWithChannelsAsync(Snowflake id, CancellationToken ct = default)
        => await DbSet
            .Include(g => g.Channels)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<Guild?> GetWithRolesAsync(Snowflake id, CancellationToken ct = default)
        => await DbSet
            .Include(g => g.Roles)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<Guild?> GetWithMembersAsync(Snowflake id, CancellationToken ct = default)
        => await DbSet
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<Guild?> GetFullAsync(Snowflake id, CancellationToken ct = default)
        => await DbSet
            .Include(g => g.Channels)
            .Include(g => g.Roles)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IReadOnlyList<Guild>> GetAllWithStatsAsync(CancellationToken ct = default)
        => await DbSet
            .Include(g => g.Channels)
            .Include(g => g.Members)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guild>> GetNeedingSyncAsync(TimeSpan syncInterval, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - syncInterval;
        return await DbSet
            .Where(g => g.LastSyncedAt == null || g.LastSyncedAt < cutoff)
            .ToListAsync(ct);
    }
}
