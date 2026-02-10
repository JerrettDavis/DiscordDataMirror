using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class ReactionRepository : GenericRepository<Reaction, string>, IReactionRepository
{
    public ReactionRepository(DiscordMirrorDbContext context) : base(context)
    {
    }

    public async Task<Reaction?> GetByMessageAndEmoteAsync(Snowflake messageId, string emoteKey, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.EmoteKey == emoteKey, ct);

    public async Task<IReadOnlyList<Reaction>> GetByMessageIdAsync(Snowflake messageId, CancellationToken ct = default)
        => await DbSet
            .Where(r => r.MessageId == messageId)
            .ToListAsync(ct);
}
