using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class RoleRepository : GenericRepository<Role, Snowflake>, IRoleRepository
{
    public RoleRepository(DiscordMirrorDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Role>> GetByGuildIdAsync(Snowflake guildId, CancellationToken ct = default)
        => await DbSet
            .Where(r => r.GuildId == guildId)
            .OrderByDescending(r => r.Position)
            .ToListAsync(ct);
}
