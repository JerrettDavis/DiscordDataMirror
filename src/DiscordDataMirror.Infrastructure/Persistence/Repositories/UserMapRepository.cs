using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class UserMapRepository : GenericRepository<UserMap, int>, IUserMapRepository
{
    public UserMapRepository(DiscordMirrorDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<UserMap>> GetByCanonicalUserAsync(Snowflake canonicalUserId, CancellationToken ct = default)
        => await DbSet
            .Where(m => m.CanonicalUserId == canonicalUserId)
            .Include(m => m.MappedUser)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UserMap>> GetSuggestionsAsync(decimal minConfidence = 0.5m, CancellationToken ct = default)
        => await DbSet
            .Where(m => m.Confidence >= minConfidence)
            .OrderByDescending(m => m.Confidence)
            .Include(m => m.CanonicalUser)
            .Include(m => m.MappedUser)
            .ToListAsync(ct);
}
