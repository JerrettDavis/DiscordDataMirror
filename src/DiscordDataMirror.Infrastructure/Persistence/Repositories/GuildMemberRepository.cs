using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class GuildMemberRepository : GenericRepository<GuildMember, string>, IGuildMemberRepository
{
    public GuildMemberRepository(DiscordMirrorDbContext context) : base(context)
    {
    }

    public async Task<GuildMember?> GetByUserAndGuildAsync(Snowflake userId, Snowflake guildId, CancellationToken ct = default)
        => await DbSet
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.GuildId == guildId, ct);

    public async Task<IReadOnlyList<GuildMember>> GetByGuildIdAsync(Snowflake guildId, CancellationToken ct = default)
        => await DbSet
            .Where(m => m.GuildId == guildId)
            .Include(m => m.User)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GuildMember>> GetByUserIdAsync(Snowflake userId, CancellationToken ct = default)
        => await DbSet
            .Where(m => m.UserId == userId)
            .Include(m => m.Guild)
            .ToListAsync(ct);
}
