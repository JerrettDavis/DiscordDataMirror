using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class EmbedRepository : GenericRepository<Embed, int>, IEmbedRepository
{
    public EmbedRepository(DiscordMirrorDbContext context) : base(context)
    {
    }
    
    public async Task<IReadOnlyList<Embed>> GetByMessageIdAsync(Snowflake messageId, CancellationToken ct = default)
        => await DbSet
            .Where(e => e.MessageId == messageId)
            .OrderBy(e => e.Index)
            .ToListAsync(ct);
    
    public async Task DeleteByMessageIdAsync(Snowflake messageId, CancellationToken ct = default)
    {
        var embeds = await DbSet.Where(e => e.MessageId == messageId).ToListAsync(ct);
        DbSet.RemoveRange(embeds);
    }
}
