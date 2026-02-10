using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DiscordDataMirror.Infrastructure.Persistence.Repositories;

public class SyncStateRepository : GenericRepository<SyncState, int>, ISyncStateRepository
{
    public SyncStateRepository(DiscordMirrorDbContext context) : base(context)
    {
    }
    
    public async Task<SyncState?> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.EntityId == entityId, ct);
    
    public async Task<IReadOnlyList<SyncState>> GetByStatusAsync(SyncStatus status, CancellationToken ct = default)
        => await DbSet
            .Where(s => s.Status == status)
            .ToListAsync(ct);
}
